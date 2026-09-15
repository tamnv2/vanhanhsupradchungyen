const required = [
  'APP_ENV',
  'GAS_SCRIPT_ID',
  'GOOGLE_OAUTH_CLIENT_ID',
  'GOOGLE_OAUTH_CLIENT_SECRET',
  'GOOGLE_OAUTH_REFRESH_TOKEN'
];

for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}
if (process.env.APP_ENV !== 'BETA') throw new Error('APP_ENV must be BETA for management probe');

async function sleep(ms) {
  await new Promise(resolve => setTimeout(resolve, ms));
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
  if (!response.ok) throw new Error(`OAuth refresh failed with HTTP ${response.status}`);
  const payload = await response.json();
  if (!payload.access_token) throw new Error('OAuth refresh response did not contain access_token');
  return payload.access_token;
}

async function runHealth(token) {
  const scriptId = encodeURIComponent(process.env.GAS_SCRIPT_ID);
  const response = await fetch(`https://script.googleapis.com/v1/scripts/${scriptId}:run`, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${token}`,
      'content-type': 'application/json'
    },
    body: JSON.stringify({
      function: 'projectionManagementHealth',
      parameters: ['BETA'],
      devMode: false
    })
  });
  const text = await response.text();
  if (!response.ok) throw new Error(`scripts.run failed with HTTP ${response.status}: ${text.slice(0, 800)}`);
  const operation = text ? JSON.parse(text) : {};
  if (operation?.error) throw new Error(`scripts.run operation failed: ${String(operation.error?.message || 'unknown').slice(0, 500)}`);
  return operation?.response?.result;
}

function validate(result) {
  const ok =
    result?.ok === true &&
    result?.managementVersion === 'VHDCHY_PROJECTION_MANAGEMENT_V1' &&
    String(result?.environment || '').toUpperCase() === 'BETA' &&
    result?.bootstrap?.authorized === true &&
    result?.bootstrap?.configMatch === true &&
    typeof result?.projection?.authConfigured === 'boolean' &&
    result?.projection?.enabled === false;
  if (!ok) throw new Error(`Projection management health mismatch: ${JSON.stringify(result).slice(0, 1000)}`);
  return result;
}

const token = await googleAccessToken();
let lastError = 'unknown';
for (let attempt = 1; attempt <= 8; attempt += 1) {
  try {
    const result = validate(await runHealth(token));
    console.log(`PROJECTION_MANAGEMENT_E2E_PASS authConfigured=${result.projection.authConfigured} enabled=false`);
    process.exit(0);
  } catch (error) {
    lastError = error instanceof Error ? error.message : String(error);
    if (attempt < 8) await sleep(2500);
  }
}

throw new Error(`Projection management E2E probe failed after retries: ${lastError}`);
