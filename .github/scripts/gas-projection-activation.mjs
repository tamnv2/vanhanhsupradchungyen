import fs from 'node:fs';

const dispatch = JSON.parse(fs.readFileSync('.github/dispatch/gas-beta-sync.json', 'utf8'));
if (dispatch.activate_projection !== true) {
  console.log('PROJECTION_ACTIVATION_SKIPPED');
  process.exit(0);
}
if (dispatch.target !== 'beta' || dispatch.operation !== 'deploy' || dispatch.deploy !== true) {
  throw new Error('Projection activation requires reviewed BETA deploy operation');
}

const required = [
  'APP_ENV',
  'GAS_SCRIPT_ID',
  'GOOGLE_OAUTH_CLIENT_ID',
  'GOOGLE_OAUTH_CLIENT_SECRET',
  'GOOGLE_OAUTH_REFRESH_TOKEN',
  'VHDCHY_PROJECTION_SHARED_TOKEN'
];
for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}
if (process.env.APP_ENV !== 'BETA') throw new Error('APP_ENV must be BETA for projection activation');

const rawToken = String(process.env.VHDCHY_PROJECTION_SHARED_TOKEN || '');
if (rawToken.length < 32 || rawToken.length > 256) throw new Error('Projection token length is invalid');

const spec = JSON.parse(fs.readFileSync('service/worker/deploy.beta.json', 'utf8'));
const gatewayUrl = String(spec?.vars?.GAS_EXEC_URL || '').trim();
if (!gatewayUrl.startsWith('https://script.google.com/macros/s/')) throw new Error('Reviewed GAS_EXEC_URL is invalid');

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
  if (!response.ok) throw new Error(`OAuth refresh failed with HTTP ${response.status}`);
  const payload = await response.json();
  if (!payload.access_token) throw new Error('OAuth refresh response did not contain access_token');
  return payload.access_token;
}

async function runFunction(accessToken, functionName, parameters) {
  const scriptId = encodeURIComponent(process.env.GAS_SCRIPT_ID);
  const response = await fetch(`https://script.googleapis.com/v1/scripts/${scriptId}:run`, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${accessToken}`,
      'content-type': 'application/json'
    },
    body: JSON.stringify({ function: functionName, parameters, devMode: false })
  });
  const text = await response.text();
  if (!response.ok) throw new Error(`scripts.run ${functionName} failed with HTTP ${response.status}: ${text.slice(0, 800)}`);
  const operation = text ? JSON.parse(text) : {};
  if (operation?.error) throw new Error(`scripts.run ${functionName} operation failed: ${String(operation.error?.message || 'unknown').slice(0, 500)}`);
  return operation?.response?.result;
}

function assertLiveHealth(result, label) {
  const ok =
    result?.ok === true &&
    result?.managementVersion === 'VHDCHY_PROJECTION_MANAGEMENT_V1' &&
    String(result?.environment || '').toUpperCase() === 'BETA' &&
    result?.bootstrap?.authorized === true &&
    result?.bootstrap?.configMatch === true &&
    result?.projection?.authConfigured === true &&
    result?.projection?.enabled === true;
  if (!ok) throw new Error(`${label} projection management state mismatch`);
}

async function publicGet() {
  const response = await fetch(gatewayUrl, { redirect: 'follow', cache: 'no-store' });
  const payload = await response.json();
  if (!response.ok || payload?.ok !== true || payload?.projection?.authConfigured !== true || payload?.projection?.enabled !== true) {
    throw new Error('Public Gateway did not converge to enabled=true');
  }
  return payload;
}

async function publicPost(sharedToken) {
  const response = await fetch(gatewayUrl, {
    method: 'POST',
    redirect: 'follow',
    cache: 'no-store',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      protocol: 'VHDCHY_PROJECTION_V1',
      environment: 'BETA',
      sharedToken,
      items: []
    })
  });
  const payload = await response.json();
  if (!response.ok) throw new Error(`Public projection probe returned HTTP ${response.status}`);
  return payload;
}

const accessToken = await googleAccessToken();
const activated = await runFunction(accessToken, 'setProjectionEnabled', [true, 'BETA']);
assertLiveHealth(activated, 'activation');
const readback = await runFunction(accessToken, 'projectionManagementHealth', ['BETA']);
assertLiveHealth(readback, 'readback');
await publicGet();

const wrongToken = `${rawToken[0] === 'A' ? 'B' : 'A'}${rawToken.slice(1)}`;
const wrong = await publicPost(wrongToken);
if (wrong?.ok !== false || wrong?.code !== 'PROJECTION_AUTH_FAILED') {
  throw new Error(`Wrong-token live probe mismatch: ${String(wrong?.code || 'missing')}`);
}
const correct = await publicPost(rawToken);
if (correct?.ok !== false || correct?.code !== 'INVALID_PROJECTION_BATCH') {
  throw new Error(`Correct-token live probe mismatch: ${String(correct?.code || 'missing')}`);
}

console.log('PROJECTION_ACTIVATION_E2E_PASS enabled=true wrong=REJECTED correct=AUTHENTICATED_NO_WRITE');
