const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN'];
for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const expectedWorker = 'vhdchy-beta';
const expectedD1 = 'vhdchy-data-beta';

async function cfRaw(path, init = {}) {
  const response = await fetch(`https://api.cloudflare.com/client/v4${path}`, {
    ...init,
    headers: {
      authorization: `Bearer ${token}`,
      ...(init.body ? { 'content-type': 'application/json' } : {}),
      ...(init.headers || {})
    }
  });
  let payload;
  try {
    payload = await response.json();
  } catch {
    payload = { success: false, errors: [{ message: 'Non-JSON Cloudflare response' }] };
  }
  return { response, payload };
}

function summarizeErrors(payload) {
  return JSON.stringify(payload?.errors || []).slice(0, 1200);
}

async function cf(path, init = {}) {
  const { response, payload } = await cfRaw(path, init);
  if (!response.ok || payload?.success !== true) {
    throw new Error(`Cloudflare API ${path} failed with HTTP ${response.status}: ${summarizeErrors(payload)}`);
  }
  return payload;
}

async function verifyToken() {
  const userAttempt = await cfRaw('/user/tokens/verify');
  if (userAttempt.response.ok && userAttempt.payload?.success === true) {
    if (userAttempt.payload?.result?.status !== 'active') throw new Error('Cloudflare user API token is not active');
    return 'user';
  }

  const accountPath = `/accounts/${encodeURIComponent(accountId)}/tokens/verify`;
  const accountAttempt = await cfRaw(accountPath);
  if (accountAttempt.response.ok && accountAttempt.payload?.success === true) {
    if (accountAttempt.payload?.result?.status !== 'active') throw new Error('Cloudflare account API token is not active');
    return 'account';
  }

  throw new Error(
    `Cloudflare token verification failed. ` +
    `user-endpoint HTTP ${userAttempt.response.status}: ${summarizeErrors(userAttempt.payload)}; ` +
    `account-endpoint HTTP ${accountAttempt.response.status}: ${summarizeErrors(accountAttempt.payload)}`
  );
}

function firstQueryRows(payload) {
  const first = Array.isArray(payload?.result) ? payload.result[0] : payload?.result;
  if (!first || first.success !== true || !Array.isArray(first.results)) {
    throw new Error(`Unexpected D1 query response shape: ${JSON.stringify(payload).slice(0, 1200)}`);
  }
  return first.results;
}

async function d1Select(databaseId, sql, params = []) {
  const path = `/accounts/${encodeURIComponent(accountId)}/d1/database/${encodeURIComponent(databaseId)}/query`;
  const payload = await cf(path, {
    method: 'POST',
    body: JSON.stringify({ sql, params })
  });
  return firstQueryRows(payload);
}

async function inspectD1ReadOnly(databaseId) {
  const tableRows = await d1Select(
    databaseId,
    `SELECT name FROM sqlite_schema WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name`
  );
  const tables = tableRows.map(row => String(row.name || '')).filter(Boolean);
  console.log(`D1_READ_ONLY_TABLES=${JSON.stringify(tables)}`);

  let schemaVersion = '(missing)';
  if (tables.includes('vhdchy_meta')) {
    const rows = await d1Select(databaseId, `SELECT value FROM vhdchy_meta WHERE key = ? LIMIT 1`, ['schema_version']);
    if (rows.length > 0 && rows[0]?.value != null) schemaVersion = String(rows[0].value);
  }
  console.log(`D1_SCHEMA_VERSION=${schemaVersion}`);

  const counts = {};
  for (const table of tables) {
    if (table === '_cf_KV') continue;
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(table)) {
      throw new Error(`Unsafe table identifier returned by sqlite_schema: ${table}`);
    }
    const rows = await d1Select(databaseId, `SELECT COUNT(*) AS row_count FROM "${table}"`);
    const count = Number(rows[0]?.row_count ?? 0);
    if (!Number.isSafeInteger(count) || count < 0) throw new Error(`Invalid row count for ${table}`);
    counts[table] = count;
  }
  console.log(`D1_READ_ONLY_ROW_COUNTS=${JSON.stringify(counts)}`);
  console.log('D1 read-only inspection PASS');
}

async function inspectWorkerRoutingReadOnly() {
  const accountSubdomain = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/subdomain`);
  const workerSubdomain = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(expectedWorker)}/subdomain`);
  const domainsPayload = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/domains`);
  const subdomain = String(accountSubdomain?.result?.subdomain || '');
  const enabled = workerSubdomain?.result?.enabled === true;
  const previewsEnabled = workerSubdomain?.result?.previews_enabled === true;
  const domains = (Array.isArray(domainsPayload?.result) ? domainsPayload.result : [])
    .filter(item => item?.service === expectedWorker)
    .map(item => ({ hostname: item.hostname, zone_name: item.zone_name, environment: item.environment || null }));
  console.log(`WORKERS_DEV_ACCOUNT_SUBDOMAIN=${subdomain}`);
  console.log(`WORKERS_DEV_ENABLED=${enabled ? 'yes' : 'no'}`);
  console.log(`WORKERS_DEV_PREVIEWS_ENABLED=${previewsEnabled ? 'yes' : 'no'}`);
  console.log(`WORKER_CUSTOM_DOMAINS=${JSON.stringify(domains)}`);
  if (subdomain && enabled) {
    console.log(`WORKER_PUBLIC_URL=https://${expectedWorker}.${subdomain}.workers.dev`);
  }
}

const tokenKind = await verifyToken();

const workers = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts`);
const worker = (workers.result || []).find(item => item?.id === expectedWorker) || null;

const d1 = await cf(`/accounts/${encodeURIComponent(accountId)}/d1/database?name=${encodeURIComponent(expectedD1)}&per_page=100`);
const databases = Array.isArray(d1.result) ? d1.result : [];
const database = databases.find(item => item?.name === expectedD1) || null;

console.log(`Cloudflare token verification PASS (${tokenKind}-owned token)`);
console.log(`CLOUDFLARE_ACCOUNT_ID=${accountId}`);
console.log(`WORKER_EXPECTED=${expectedWorker}`);
console.log(`WORKER_FOUND=${worker ? 'yes' : 'no'}`);
if (worker) console.log(`WORKER_ID=${worker.id}`);
console.log(`D1_EXPECTED=${expectedD1}`);
console.log(`D1_FOUND=${database ? 'yes' : 'no'}`);
if (database) console.log(`D1_DATABASE_ID=${database.uuid || database.id || ''}`);

if (!worker || !database) {
  throw new Error(`Cloudflare expected-resource mismatch: worker=${worker ? 'found' : 'missing'}, d1=${database ? 'found' : 'missing'}. Fail closed; no resources were created.`);
}

await inspectWorkerRoutingReadOnly();

const databaseId = database.uuid || database.id;
await inspectD1ReadOnly(databaseId);

console.log('Cloudflare BETA identity verification PASS');
