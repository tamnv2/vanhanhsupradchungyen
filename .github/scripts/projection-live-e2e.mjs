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
  'CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN',
  'GOOGLE_SHEETS_PROJECTION_ID', 'GOOGLE_OAUTH_CLIENT_ID',
  'GOOGLE_OAUTH_CLIENT_SECRET', 'GOOGLE_OAUTH_REFRESH_TOKEN'
];
for (const name of required) if (!process.env[name]) throw new Error(`Missing ${name}`);

const DB_ID = '37eb7d59-05c0-4ba2-8162-cb6a9fe5d492';
const SHEET_TITLE = 'LỊCH SỬ NGHIỆP VỤ';
const EVENT_ID_COLUMN = 10; // K, zero-based in Sheets API value arrays.
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

async function sheetsRequest(token, url, options = {}) {
  const response = await fetch(url, {
    ...options,
    headers: {
      authorization: `Bearer ${token}`,
      'content-type': 'application/json',
      ...(options.headers || {})
    }
  });
  const text = await response.text();
  let payload = {};
  if (text) {
    try { payload = JSON.parse(text); }
    catch { throw new Error(`Sheets API returned non-JSON HTTP ${response.status}`); }
  }
  if (!response.ok) throw new Error(`Sheets API HTTP ${response.status}: ${JSON.stringify(payload?.error || {}).slice(0, 600)}`);
  return payload;
}

async function readSheetRows(token) {
  const range = encodeURIComponent(`'${SHEET_TITLE}'!A:M`);
  const url = `https://sheets.googleapis.com/v4/spreadsheets/${process.env.GOOGLE_SHEETS_PROJECTION_ID}/values/${range}?majorDimension=ROWS`;
  const payload = await sheetsRequest(token, url);
  return Array.isArray(payload.values) ? payload.values : [];
}

function markerRows(rows) {
  const matches = [];
  rows.forEach((row, index) => {
    if (String(row?.[EVENT_ID_COLUMN] || '') === marker) matches.push({ index, row });
  });
  return matches;
}

async function deleteMarkerRows(token) {
  const rows = await readSheetRows(token);
  const matches = markerRows(rows).filter(item => item.index > 0).sort((a, b) => b.index - a.index);
  if (!matches.length) return 0;
  const metaUrl = `https://sheets.googleapis.com/v4/spreadsheets/${process.env.GOOGLE_SHEETS_PROJECTION_ID}?fields=sheets.properties`;
  const meta = await sheetsRequest(token, metaUrl);
  const sheet = (meta.sheets || []).find(item => item?.properties?.title === SHEET_TITLE);
  if (!sheet) throw new Error('Projection target sheet metadata not found during cleanup');
  const requests = matches.map(item => ({
    deleteDimension: {
      range: {
        sheetId: sheet.properties.sheetId,
        dimension: 'ROWS',
        startIndex: item.index,
        endIndex: item.index + 1
      }
    }
  }));
  const url = `https://sheets.googleapis.com/v4/spreadsheets/${process.env.GOOGLE_SHEETS_PROJECTION_ID}:batchUpdate`;
  await sheetsRequest(token, url, { method: 'POST', body: JSON.stringify({ requests }) });
  return matches.length;
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
let primaryError = null;
try {
  await verifyTrigger();
  const preExisting = await d1Rows('SELECT event_id FROM domain_events WHERE event_id=?', [marker]);
  if (preExisting.length) throw new Error('E2E marker unexpectedly already exists');

  googleToken = await googleAccessToken();
  const beforeRows = markerRows(await readSheetRows(googleToken));
  if (beforeRows.length) throw new Error('E2E marker unexpectedly exists in Sheet before test');

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
      sql: `INSERT INTO domain_events(event_id,event_type,entity_type,entity_id,entity_version,payload_json,app_version,occurred_at) VALUES(?,?,?,?,?,?,?,?)`,
      params: [marker, 'E2E_PROJECTION_TEST', 'system_test', marker, 1, '{}', String(process.env.GITHUB_SHA || 'unknown'), nowIso]
    },
    {
      sql: `INSERT INTO projection_outbox(event_id,projection_target,payload_json,status,attempts,created_at,updated_at) VALUES(?, 'GOOGLE_SHEETS', ?, 'PENDING', 0, ?, ?)`,
      params: [marker, payload, nowIso, nowIso]
    }
  ]});
  console.log(`PROJECTION_E2E_MARKER_INJECTED id=${marker}`);

  const firstAck = await waitForAck();
  const firstMatches = markerRows(await readSheetRows(googleToken));
  if (firstMatches.length !== 1) throw new Error(`Expected one projected row after first ACK, found ${firstMatches.length}`);
  if (String(firstMatches[0].row?.[9] || '') !== marker) throw new Error('Projected detail marker mismatch');
  console.log(`PROJECTION_E2E_FIRST_ACK_PASS attempts=${firstAck.attempts}`);

  const requeueAt = new Date().toISOString();
  await d1({
    sql: `UPDATE projection_outbox SET status='PENDING', next_attempt_at=NULL, last_error_code=NULL, updated_at=? WHERE event_id=? AND status='ACKED'`,
    params: [requeueAt, marker]
  });
  const secondAck = await waitForAck(requeueAt);
  const secondMatches = markerRows(await readSheetRows(googleToken));
  if (secondMatches.length !== 1) throw new Error(`Projection replay duplicated logical Sheet row: ${secondMatches.length}`);
  console.log(`PROJECTION_E2E_IDEMPOTENCY_PASS rows=1 attempts=${secondAck.attempts}`);
} catch (error) {
  primaryError = error;
} finally {
  let cleanupError = null;
  try {
    if (!googleToken) googleToken = await googleAccessToken();
    const deletedSheetRows = await deleteMarkerRows(googleToken);

    await d1({ batch: [
      { sql: 'DELETE FROM projection_outbox WHERE event_id=?', params: [marker] },
      { sql: 'DROP TRIGGER IF EXISTS trg_domain_events_no_delete', params: [] },
      { sql: 'DELETE FROM domain_events WHERE event_id=?', params: [marker] },
      { sql: "CREATE TRIGGER IF NOT EXISTS trg_domain_events_no_delete BEFORE DELETE ON domain_events BEGIN SELECT RAISE(ABORT, 'domain_events are immutable'); END", params: [] }
    ]});

    await verifyTrigger();
    const d1Left = await d1Rows('SELECT event_id FROM domain_events WHERE event_id=? UNION ALL SELECT event_id FROM projection_outbox WHERE event_id=?', [marker, marker]);
    const sheetLeft = markerRows(await readSheetRows(googleToken));
    if (d1Left.length || sheetLeft.length) throw new Error(`Cleanup incomplete d1=${d1Left.length} sheet=${sheetLeft.length}`);
    console.log(`PROJECTION_E2E_CLEANUP_PASS sheetRowsDeleted=${deletedSheetRows} d1Markers=0 trigger=PASS`);
  } catch (error) {
    cleanupError = error;
  }

  if (primaryError || cleanupError) {
    const primary = primaryError ? String(primaryError?.stack || primaryError) : 'none';
    const cleanup = cleanupError ? String(cleanupError?.stack || cleanupError) : 'none';
    throw new Error(`PROJECTION_LIVE_E2E_FAILED primary=${primary} cleanup=${cleanup}`);
  }
}

console.log('PROJECTION_LIVE_E2E_PASS d1Outbox=PASS cron=PASS googleSheet=PASS replayDedup=PASS cleanup=PASS');
