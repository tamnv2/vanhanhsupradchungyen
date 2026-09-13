const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN', 'EXPECTED_WORKER', 'EXPECTED_D1'];
for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const expectedWorker = process.env.EXPECTED_WORKER;
const expectedD1 = process.env.EXPECTED_D1;

async function cf(path) {
  const response = await fetch(`https://api.cloudflare.com/client/v4${path}`, {
    headers: { authorization: `Bearer ${token}` }
  });
  let payload;
  try { payload = await response.json(); }
  catch { throw new Error(`Cloudflare returned non-JSON HTTP ${response.status}`); }
  if (!response.ok || payload?.success !== true) {
    throw new Error(`Cloudflare API ${path} failed HTTP ${response.status}: ${JSON.stringify(payload?.errors || []).slice(0, 1200)}`);
  }
  return payload;
}

const workersPayload = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts`);
const worker = (workersPayload.result || []).find((item) => item?.id === expectedWorker) || null;

const d1Payload = await cf(`/accounts/${encodeURIComponent(accountId)}/d1/database?name=${encodeURIComponent(expectedD1)}&per_page=100`);
const databases = Array.isArray(d1Payload.result) ? d1Payload.result : [];
const database = databases.find((item) => item?.name === expectedD1) || null;

console.log(`CLOUDFLARE_ACCOUNT_ID=${accountId}`);
console.log(`WORKER_EXPECTED=${expectedWorker}`);
console.log(`WORKER_FOUND=${worker ? 'yes' : 'no'}`);
console.log(`D1_EXPECTED=${expectedD1}`);
console.log(`D1_FOUND=${database ? 'yes' : 'no'}`);
if (database) console.log(`D1_DATABASE_ID=${database.uuid || database.id || ''}`);

if (worker) {
  const subdomain = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(expectedWorker)}/subdomain`);
  const settings = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/scripts/${encodeURIComponent(expectedWorker)}/settings`);
  const domainsPayload = await cf(`/accounts/${encodeURIComponent(accountId)}/workers/domains`);
  const domains = (Array.isArray(domainsPayload.result) ? domainsPayload.result : [])
    .filter((item) => item?.service === expectedWorker)
    .map((item) => ({ hostname: item.hostname, zone_name: item.zone_name, environment: item.environment || null }));
  const bindings = (Array.isArray(settings?.result?.bindings) ? settings.result.bindings : [])
    .map((item) => ({ name: item?.name || '', type: item?.type || '' }))
    .filter((item) => item.name && item.type)
    .sort((a, b) => a.name.localeCompare(b.name));
  console.log(`WORKERS_DEV_ENABLED=${subdomain?.result?.enabled === true ? 'yes' : 'no'}`);
  console.log(`WORKER_CUSTOM_DOMAINS=${JSON.stringify(domains)}`);
  console.log(`WORKER_BINDING_NAMES_TYPES=${JSON.stringify(bindings)}`);
}

console.log(`TARGET_RESOURCE_STATE=${worker && database ? 'BOTH_FOUND' : worker ? 'WORKER_ONLY' : database ? 'D1_ONLY' : 'BOTH_MISSING'}`);
console.log('READ_ONLY_TARGET_INSPECTION_PASS');
