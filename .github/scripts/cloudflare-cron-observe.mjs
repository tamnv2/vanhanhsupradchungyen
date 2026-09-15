const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN'];
for (const name of required) if (!process.env[name]) throw new Error(`Missing ${name}`);

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const worker = 'vhdchy-beta';
const databaseId = '37eb7d59-05c0-4ba2-8162-cb6a9fe5d492';
const scriptBaseUrl = `https://api.cloudflare.com/client/v4/accounts/${accountId}/workers/scripts/${worker}`;

async function cf(url, init = {}) {
  const response = await fetch(url, {
    ...init,
    headers: {
      authorization: `Bearer ${token}`,
      ...(init.body ? { 'content-type': 'application/json' } : {}),
      ...(init.headers || {})
    }
  });
  const text = await response.text();
  let payload;
  try { payload = text ? JSON.parse(text) : {}; }
  catch { throw new Error(`Cloudflare returned non-JSON HTTP ${response.status}`); }
  if (!response.ok || payload?.success !== true) {
    throw new Error(`Cloudflare HTTP ${response.status}: ${JSON.stringify(payload?.errors || []).slice(0, 1000)}`);
  }
  return payload;
}

const deployments = await cf(`${scriptBaseUrl}/deployments`);
const activeDeployment = deployments.result?.deployments?.[0] || null;
if (!activeDeployment?.id) throw new Error('No active Worker deployment was returned');
const servingVersions = (Array.isArray(activeDeployment.versions) ? activeDeployment.versions : [])
  .filter(entry => Number(entry?.percentage) > 0 && entry?.version_id);
if (!servingVersions.length) throw new Error('Active Worker deployment has no serving versions');

const activeVersionEvidence = [];
for (const serving of servingVersions) {
  const versionPayload = await cf(`${scriptBaseUrl}/versions/${encodeURIComponent(serving.version_id)}`);
  const version = versionPayload.result || {};
  const handlers = Array.isArray(version?.resources?.script?.handlers)
    ? version.resources.script.handlers.map(String).sort()
    : [];
  const safeEvidence = {
    version_id: String(serving.version_id),
    percentage: Number(serving.percentage),
    number: Number.isFinite(Number(version.number)) ? Number(version.number) : null,
    source: version?.metadata?.source || null,
    handlers,
    last_deployed_from: version?.resources?.script?.last_deployed_from || null,
    compatibility_date: version?.resources?.script_runtime?.compatibility_date || null
  };
  activeVersionEvidence.push(safeEvidence);
  if (!handlers.includes('fetch') || !handlers.includes('scheduled')) {
    throw new Error(`Active Worker version ${serving.version_id} missing required handlers; handlers=${JSON.stringify(handlers)}`);
  }
}
console.log(`CRON_LIVE_ACTIVE_DEPLOYMENT=${JSON.stringify({
  id: activeDeployment.id,
  created_on: activeDeployment.created_on || null,
  source: activeDeployment.source || null,
  strategy: activeDeployment.strategy || null,
  versions: servingVersions.map(entry => ({ version_id: entry.version_id, percentage: Number(entry.percentage) }))
})}`);
console.log(`CRON_LIVE_ACTIVE_VERSIONS=${JSON.stringify(activeVersionEvidence)}`);
console.log('CRON_LIVE_ACTIVE_HANDLERS_PASS');

const schedules = await cf(`${scriptBaseUrl}/schedules`);
const liveSchedules = (schedules.result?.schedules || []).map(x => ({
  cron: x.cron,
  created_on: x.created_on || null,
  modified_on: x.modified_on || null
}));
console.log(`CRON_LIVE_SCHEDULES=${JSON.stringify(liveSchedules)}`);
const cronValues = liveSchedules.map(item => String(item.cron || '')).sort();
if (JSON.stringify(cronValues) !== JSON.stringify(['*/2 * * * *'])) {
  throw new Error(`Unexpected live Cron schedules: ${JSON.stringify(cronValues)}`);
}
console.log('CRON_LIVE_SCHEDULE_PASS');

const d1 = await cf(`https://api.cloudflare.com/client/v4/accounts/${accountId}/d1/database/${databaseId}/query`, {
  method: 'POST',
  body: JSON.stringify({ batch: [
    { sql: "SELECT key,value,updated_at FROM vhdchy_meta WHERE key='projection_scheduler_probe'", params: [] },
    { sql: "SELECT outbox_id,event_id,status,attempts,next_attempt_at,last_error_code,created_at,updated_at FROM projection_outbox ORDER BY outbox_id DESC LIMIT 5", params: [] },
    { sql: "SELECT COUNT(*) AS count FROM projection_outbox WHERE status='PENDING'", params: [] }
  ] })
});
const result = Array.isArray(d1.result) ? d1.result : [];
console.log(`CRON_LIVE_PROBE=${JSON.stringify(result[0]?.results || [])}`);
console.log(`CRON_LIVE_OUTBOX=${JSON.stringify(result[1]?.results || [])}`);
console.log(`CRON_LIVE_PENDING_COUNT=${JSON.stringify(result[2]?.results || [])}`);
console.log('CRON_LIVE_OBSERVE_PASS');
