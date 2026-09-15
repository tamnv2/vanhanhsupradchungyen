const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN'];
for (const name of required) if (!process.env[name]) throw new Error(`Missing ${name}`);

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const worker = 'vhdchy-beta';
const databaseId = '37eb7d59-05c0-4ba2-8162-cb6a9fe5d492';

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

const schedules = await cf(`https://api.cloudflare.com/client/v4/accounts/${accountId}/workers/scripts/${worker}/schedules`);
console.log(`CRON_LIVE_SCHEDULES=${JSON.stringify((schedules.result?.schedules || []).map(x => ({ cron: x.cron, created_on: x.created_on || null, modified_on: x.modified_on || null })))}`);

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
