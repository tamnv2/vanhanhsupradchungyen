import fs from 'node:fs';

const dispatch = JSON.parse(fs.readFileSync('.github/dispatch/projection-live-e2e.json', 'utf8'));
if (dispatch.enabled !== true) {
  console.log('PROJECTION_LIVE_E2E_DISABLED_NOOP');
  process.exit(0);
}
if (dispatch.target !== 'beta' || dispatch.operation !== 'projection_live_e2e') {
  throw new Error('Projection live E2E dispatch contract mismatch');
}

const required = [
  'CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN', 'GAS_SCRIPT_ID',
  'GOOGLE_OAUTH_CLIENT_ID', 'GOOGLE_OAUTH_CLIENT_SECRET', 'GOOGLE_OAUTH_REFRESH_TOKEN'
];
for (const name of required) if (!process.env[name]) throw new Error(`Missing ${name}`);

const DB_ID = '37eb7d59-05c0-4ba2-8162-cb6a9fe5d492';
const SHEET_TITLE = 'LỊCH SỬ NGHIỆP VỤ';
const runId = String(process.env.GITHUB_RUN_ID || Date.now());
const marker = `VHDCHY-E2E-PROJECTION-${runId}`;
const nowIso = new Date().toISOString();
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

async function d1(body) {
  const url = `https://api.cloudflare.com/client/v4/accounts/${process.env.CLOUDFLARE_ACCOUNT_ID}/d1/database/${DB_ID}/query`;
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${process.env.CLOUDFLARE_API_TOKEN}`,
      'content-type': 'application/json'
    },
    body: JSON.stringify(body)
  });
  const text = await response.text();
  let payload;
  try { payload = JSON.parse(text); }
  catch { throw new Error(`D1 returned non-JSON HTTP ${response.status}`); }
  if (!response.ok || payload?.success !== true || !Array.isArray(payload?.result)) {
    throw new Error(`D1 query failed HTTP ${response.status}: ${JSON.stringify(payload?.errors || []).slice(0, 600)}`);
  }
  for (const result of payload.result) {
    if (result?.success === false) throw new Error(`D1 statement failed: ${JSON.stringify(result).slice(0, 600)}`);
  }
  return payload.result;
}

async function d1Rows(sql, params = []) {
  const result = await d1({ sql, params });
  return Array.isArray(result?.[0]?.results) ? result[0].results : [];
}

async function googleAccessToken() {
  const body = new URLSearchParams({
    client_id: process.env.GOOGLE_OAUTH_CLIENT_ID,
    client_secret: process.env.GOOGLE_OAUTH_CLIENT_SECRET,
    refresh_token: process.env.GOOGLE_OAUTH_REFRESH_TOKEN,
    grant_type: 'refresh_token'
  });
  const response = await fetch('https://oauth2.googleapis.com/token', {
    method: 'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body
  });
  if (!response.ok) throw new Error(`Google OAuth refresh failed HTTP ${response.status}`);
  const payload = await response.json();
  if (!payload.access_token) throw new Error('Google OAuth response missing access_token');
  return payload.access_token;
}

async function runScriptFunction(token, functionName, parameters = [], options = {}) {
  const maxAttempts = Math.max(1, Math.min(40, Number(options.maxAttempts || 8)));
  const delayMs = Math.max(500, Math.min(10000, Number(options.delayMs || 2500)));
  let lastError = 'unknown';
  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    try {
      const response = await fetch(`https://script.googleapis.com/v1/scripts/${encodeURIComponent(process.env.GAS_SCRIPT_ID)}:run`, {
        method: 'POST',
        headers: {
          authorization: `Bearer ${token}`,
          'content-type': 'application/json'
        },
        body: JSON.stringify({ function: functionName, parameters, devMode: false }),
        cache: 'no-store'
      });
      const text = await response.text();
      let operation;
      try { operation = text ? JSON.parse(text) : {}; }
      catch { throw new Error(`Apps Script execution returned non-JSON HTTP ${response.status}`); }
      if (!response.ok) throw new Error(`Apps Script execution HTTP ${response.status}: ${text.slice(0, 700)}`);
      if (operation?.error) {
        const detail = operation.error?.details?.[0]?.errorMessage || operation.error?.message || 'Apps Script execution failed';
        throw new Error(`${functionName} execution error: ${detail}`);
      }
      if (!operation?.response || !Object.hasOwn(operation.response, 'result')) {
        throw new Error(`${functionName} execution response did not contain result`);
      }
      return operation.response.result;
    } catch (error) {
      lastError = error instanceof Error ? error.message : String(error);
      if (attempt < maxAttempts) await sleep(delayMs);
    }
  }
  throw new Error(`Apps Script execution ${functionName} failed after retries: ${lastError}`);
}

function assertLiveManagement(result) {
  const ok =
    result?.ok === true &&
    result?.managementVersion === 'VHDCHY_PROJECTION_MANAGEMENT_V1' &&
    String(result?.environment || '').toUpperCase() === 'BETA' &&
    result?.bootstrap?.authorized === true &&
    result?.bootstrap?.configMatch === true &&
    result?.projection?.authConfigured === true &&
    result?.projection?.enabled === true;
  if (!ok) throw new Error(`Projection management is not LIVE: ${JSON.stringify(result).slice(0, 1200)}`);
}

async function inspectMarker(token, options = {}) {
  return runScriptFunction(token, 'projectionE2EInspect', [marker, 'BETA'], options);
}

async function cleanupSheetMarker(token) {
  return runScriptFunction(token, 'projectionE2ECleanup', [marker, 'BETA'], { maxAttempts: 8, delayMs: 2500 });
}

async function waitForManagementHelpers(token) {
  const management = await runScriptFunction(token, 'projectionManagementHealth', ['BETA'], { maxAttempts: 16, delayMs: 3000 });
  assertLiveManagement(management);
  const inspect = await inspectMarker(token, { maxAttempts: 40, delayMs: 3000 });
  if (inspect?.ok !== true || inspect?.marker !== marker || inspect?.sheet !== SHEET_TITLE) {
    throw new Error(`Projection E2E management helper mismatch: ${JSON.stringify(inspect).slice(0, 1000)}`);
  }
  return inspect;
}

async function outboxRow() {
  const rows = await d1Rows(
    'SELECT outbox_id,event_id,status,attempts,next_attempt_at,last_error_code,updated_at FROM projection_outbox WHERE event_id=?',
    [marker]
  );
  return rows[0] || null;
}

async function waitForAck(previousUpdatedAt = null, maxMs = 360000) {
  const deadline = Date.now() + maxMs;
  let last = null;
  while (Date.now() < deadline) {
    last = await outboxRow();
    if (last?.status === 'DEAD') throw new Error(`Projection reached DEAD: ${last.last_error_code || 'unknown'}`);
    if (last?.status === 'ACKED' && (!previousUpdatedAt || last.updated_at !== previousUpdatedAt)) return last;
    await sleep(10000);
  }
  throw new Error(`Projection ACK timeout; last=${JSON.stringify(last)}`);
}

async function verifyTrigger() {
  const rows = await d1Rows("SELECT name FROM sqlite_master WHERE type='trigger' AND name='trg_domain_events_no_delete'");
  if (rows.length !== 1) throw new Error('Immutable domain-event delete trigger is missing');
}

let googleToken = null;
let markerInjected = false;
let primaryError = null;
try {
  await verifyTrigger();
  const preExisting = await d1Rows('SELECT event_id FROM domain_events WHERE event_id=?', [marker]);
  if (preExisting.length) throw new Error('E2E marker unexpectedly already exists in D1');

  googleToken = await googleAccessToken();
  const before = await waitForManagementHelpers(googleToken);
  if (Number(before.count || 0) !== 0) throw new Error('E2E marker unexpectedly exists in Sheet before test');
  console.log('PROJECTION_E2E_MANAGEMENT_READY_PASS enabled=true sheetReadback=PASS');

  const payload = JSON.stringify({
    sheet: SHEET_TITLE,
    values: {
      'Ngày': '15/09/2026',
      'Loại sự kiện': 'E2E_PROJECTION_TEST',
      'Nhãn sự kiện': 'PROVIDER_E2E',
      'Thời gian': nowIso,
      'Người xử lý': 'SYSTEM_E2E',
      'Chi tiết': marker,
      'Event ID': marker,
      'Phạm vi': 'BETA_TEST',
      'App Revision': String(process.env.GITHUB_SHA || 'unknown')
    }
  });

  await d1({ batch: [
    {
      sql: 'INSERT INTO domain_events(event_id,event_type,entity_type,entity_id,entity_version,payload_json,app_version,occurred_at) VALUES(?,?,?,?,?,?,?,?)',
      params: [marker, 'E2E_PROJECTION_TEST', 'system_test', marker, 1, '{}', String(process.env.GITHUB_SHA || 'unknown'), nowIso]
    },
    {
      sql: "INSERT INTO projection_outbox(event_id,projection_target,payload_json,status,attempts,created_at,updated_at) VALUES(?, 'GOOGLE_SHEETS', ?, 'PENDING', 0, ?, ?)",
      params: [marker, payload, nowIso, nowIso]
    }
  ]});
  markerInjected = true;
  console.log(`PROJECTION_E2E_MARKER_INJECTED id=${marker}`);

  const firstAck = await waitForAck();
  const firstInspect = await inspectMarker(googleToken);
  if (Number(firstInspect?.count || 0) !== 1) throw new Error(`Expected one projected row after first ACK, found ${firstInspect?.count}`);
  if (Number(firstInspect?.detailMatches || 0) !== 1) throw new Error('Projected detail marker mismatch');
  console.log(`PROJECTION_E2E_FIRST_ACK_PASS attempts=${firstAck.attempts}`);

  const requeueAt = new Date().toISOString();
  await d1({
    sql: "UPDATE projection_outbox SET status='PENDING', next_attempt_at=NULL, last_error_code=NULL, updated_at=? WHERE event_id=? AND status='ACKED'",
    params: [requeueAt, marker]
  });
  const secondAck = await waitForAck(requeueAt);
  const secondInspect = await inspectMarker(googleToken);
  if (Number(secondInspect?.count || 0) !== 1) throw new Error(`Projection replay duplicated logical Sheet row: ${secondInspect?.count}`);
  if (Number(secondInspect?.detailMatches || 0) !== 1) throw new Error('Projection replay detail marker mismatch');
  console.log(`PROJECTION_E2E_IDEMPOTENCY_PASS rows=1 attempts=${secondAck.attempts}`);
} catch (error) {
  primaryError = error;
} finally {
  let cleanupError = null;
  try {
    if (!googleToken) googleToken = await googleAccessToken();
    const sheetCleanup = await cleanupSheetMarker(googleToken);
    if (sheetCleanup?.ok !== true || Number(sheetCleanup?.remaining || 0) !== 0) {
      throw new Error(`Sheet cleanup readback mismatch: ${JSON.stringify(sheetCleanup).slice(0, 800)}`);
    }

    if (markerInjected) {
      await d1({ batch: [
        { sql: 'DELETE FROM projection_outbox WHERE event_id=?', params: [marker] },
        { sql: 'DROP TRIGGER IF EXISTS trg_domain_events_no_delete', params: [] },
        { sql: 'DELETE FROM domain_events WHERE event_id=?', params: [marker] },
        { sql: "CREATE TRIGGER IF NOT EXISTS trg_domain_events_no_delete BEFORE DELETE ON domain_events BEGIN SELECT RAISE(ABORT, 'domain_events are immutable'); END", params: [] }
      ]});
    }

    await verifyTrigger();
    const d1Left = await d1Rows('SELECT event_id FROM domain_events WHERE event_id=? UNION ALL SELECT event_id FROM projection_outbox WHERE event_id=?', [marker, marker]);
    const sheetLeft = await inspectMarker(googleToken);
    if (d1Left.length || Number(sheetLeft?.count || 0) !== 0) {
      throw new Error(`Cleanup incomplete d1=${d1Left.length} sheet=${sheetLeft?.count}`);
    }
    console.log(`PROJECTION_E2E_CLEANUP_PASS sheetRowsDeleted=${Number(sheetCleanup?.deleted || 0)} d1Markers=0 trigger=PASS`);
  } catch (error) {
    cleanupError = error;
  }

  if (primaryError || cleanupError) {
    const primary = primaryError ? String(primaryError?.stack || primaryError) : 'none';
    const cleanup = cleanupError ? String(cleanupError?.stack || cleanupError) : 'none';
    throw new Error(`PROJECTION_LIVE_E2E_FAILED primary=${primary} cleanup=${cleanup}`);
  }
}

console.log('PROJECTION_LIVE_E2E_PASS d1Outbox=PASS cron=PASS googleGatewaySheet=PASS replayDedup=PASS cleanup=PASS');
