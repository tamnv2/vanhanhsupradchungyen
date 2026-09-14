const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN'];
for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const expectedAccount = '1b1695e4f2a3abfe08dc475b352c7f42';
const expectedWorker = 'vhdchy-beta';
const expectedD1Name = 'vhdchy-data-beta';
const expectedD1Id = '37eb7d59-05c0-4ba2-8162-cb6a9fe5d492';

if (accountId !== expectedAccount) throw new Error(`CLOUDFLARE_ACCOUNT_MISMATCH actual=${accountId}`);

async function cf(path, init = {}) {
  const response = await fetch(`https://api.cloudflare.com/client/v4${path}`, {
    ...init,
    headers: {
      authorization: `Bearer ${token}`,
      ...(init.body ? { 'content-type': 'application/json' } : {}),
      ...(init.headers || {})
    }
  });
  let payload;
  try { payload = await response.json(); }
  catch { throw new Error(`Cloudflare returned non-JSON HTTP ${response.status}`); }
  if (!response.ok || payload?.success !== true) {
    throw new Error(`Cloudflare API failed HTTP ${response.status}: ${JSON.stringify(payload?.errors || []).slice(0, 1200)}`);
  }
  return payload;
}

function firstRows(payload) {
  const first = Array.isArray(payload?.result) ? payload.result[0] : payload?.result;
  if (!first || first.success !== true || !Array.isArray(first.results)) throw new Error('Unexpected D1 query response');
  return first.results;
}

async function query(sql, params = []) {
  const payload = await cf(`/accounts/${encodeURIComponent(accountId)}/d1/database/${encodeURIComponent(expectedD1Id)}/query`, {
    method: 'POST',
    body: JSON.stringify({ sql, params })
  });
  return firstRows(payload);
}

const workers = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts`);
if (!(workers.result || []).some(item => item?.id === expectedWorker)) throw new Error('BETA_WORKER_MISSING');

const databases = await cf(`/accounts/${encodeURIComponent(accountId)}/d1/database?name=${encodeURIComponent(expectedD1Name)}&per_page=100`);
const database = (Array.isArray(databases.result) ? databases.result : []).find(item => item?.name === expectedD1Name);
if ((database?.uuid || database?.id || '') !== expectedD1Id) throw new Error('BETA_D1_IDENTITY_MISMATCH');

const settings = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(expectedWorker)}/settings`);
const bindings = Array.isArray(settings?.result?.bindings) ? settings.result.bindings : [];
const byName = new Map(bindings.map(item => [String(item?.name || ''), String(item?.type || '')]));
if (byName.get('DB') !== 'd1') throw new Error('BETA_DB_BINDING_MISSING_OR_INVALID');
if (byName.get('APP_ENV') !== 'plain_text') throw new Error('BETA_APP_ENV_BINDING_MISSING_OR_INVALID');

const schemaRows = await query('SELECT value FROM vhdchy_meta WHERE key=? LIMIT 1', ['schema_version']);
if (String(schemaRows[0]?.value || '') !== 'business_core_v3') throw new Error('BETA_D1_SCHEMA_VERSION_MISMATCH');
const tables = (await query("SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")).map(row => String(row.name || ''));
for (const requiredTable of ['edge_sources', 'edge_event_ingest', 'integration_receipts', 'edge_sync_checkpoints']) {
  if (!tables.includes(requiredTable)) throw new Error(`BETA_RECONCILIATION_TABLE_MISSING=${requiredTable}`);
}
const columns = (await query('PRAGMA table_info(edge_event_ingest)')).map(row => String(row.name || ''));
for (const requiredColumn of ['resulting_entity_version', 'payload_json', 'actor_user_id', 'canonical_event_id', 'conflict_id']) {
  if (!columns.includes(requiredColumn)) throw new Error(`BETA_RECONCILIATION_COLUMN_MISSING=${requiredColumn}`);
}

const accountSubdomain = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/subdomain`);
const workerSubdomain = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(expectedWorker)}/subdomain`);
if (workerSubdomain?.result?.enabled === true) throw new Error('BETA_WORKERS_DEV_UNEXPECTEDLY_ENABLED');
const domainsPayload = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/domains`);
const domains = (Array.isArray(domainsPayload?.result) ? domainsPayload.result : []).filter(item => item?.service === expectedWorker);
if (domains.length !== 1 || domains[0]?.hostname !== 'beta.supra.cc.cd') {
  throw new Error(`BETA_CUSTOM_DOMAIN_MISMATCH=${JSON.stringify(domains.map(item => item?.hostname || null))}`);
}

console.log(`WORKER_PREDEPLOY_PASS worker=${expectedWorker} d1=${expectedD1Id} tables=${tables.length} account_subdomain=${String(accountSubdomain?.result?.subdomain || '')}`);
console.log(`WORKER_PREDEPLOY_EXISTING_RECON_KEY_ID=${byName.has('LAN_RECONCILIATION_KEY_ID') ? 'yes' : 'no'}`);
console.log(`WORKER_PREDEPLOY_EXISTING_RECON_SECRET=${byName.has('LAN_RECONCILIATION_SHARED_SECRET') ? 'yes' : 'no'}`);
