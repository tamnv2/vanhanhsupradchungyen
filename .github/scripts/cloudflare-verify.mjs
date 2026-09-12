const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN'];
for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const expectedWorker = 'vhdchy-beta';
const expectedD1 = 'vhdchy-data-beta';

async function cfRaw(path) {
  const response = await fetch(`https://api.cloudflare.com/client/v4${path}`, {
    headers: { authorization: `Bearer ${token}` }
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

async function cf(path) {
  const { response, payload } = await cfRaw(path);
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

console.log('Cloudflare BETA identity verification PASS');
