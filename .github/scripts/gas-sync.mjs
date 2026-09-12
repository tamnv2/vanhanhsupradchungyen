import fs from 'node:fs/promises';

const required = [
  'APP_ENV',
  'OWNER_EMAIL',
  'GOOGLE_SHEETS_PROJECTION_ID',
  'GAS_SCRIPT_ID',
  'GOOGLE_OAUTH_CLIENT_ID',
  'GOOGLE_OAUTH_CLIENT_SECRET',
  'GOOGLE_OAUTH_REFRESH_TOKEN'
];

for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}

if (process.env.APP_ENV !== 'BETA') throw new Error('APP_ENV must be BETA for this workflow');

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

async function scriptApi(path, token, init = {}) {
  const response = await fetch(`https://script.googleapis.com/v1/${path}`, {
    ...init,
    headers: {
      authorization: `Bearer ${token}`,
      'content-type': 'application/json',
      ...(init.headers || {})
    }
  });

  const text = await response.text();
  if (!response.ok) throw new Error(`Apps Script API ${path} failed with HTTP ${response.status}: ${text.slice(0, 1000)}`);
  return text ? JSON.parse(text) : {};
}

function renderCode(source) {
  const replacements = new Map([
    ['__ENVIRONMENT__', process.env.APP_ENV],
    ['__OWNER_EMAIL__', process.env.OWNER_EMAIL],
    ['__PROJECTION_SPREADSHEET_ID__', process.env.GOOGLE_SHEETS_PROJECTION_ID]
  ]);

  let rendered = source;
  for (const [needle, value] of replacements) {
    if (!rendered.includes(needle)) throw new Error(`Expected placeholder not found: ${needle}`);
    rendered = rendered.replaceAll(needle, value);
  }
  if (rendered.includes('__ENVIRONMENT__') || rendered.includes('__OWNER_EMAIL__') || rendered.includes('__PROJECTION_SPREADSHEET_ID__')) {
    throw new Error('Unresolved Google Gateway placeholder remains');
  }
  return rendered;
}

function webAppUrl(deployment) {
  const entry = (deployment.entryPoints || []).find(item => item?.webApp?.url);
  return entry?.webApp?.url || null;
}

async function sleep(ms) {
  await new Promise(resolve => setTimeout(resolve, ms));
}

async function deployVersion(token, scriptId) {
  const deploymentDescription = 'VHDCHY BETA Google Gateway';
  const sourceRevision = process.env.GITHUB_SHA || 'unknown';
  const versionDescription = `VHDCHY BETA ${sourceRevision}`;

  const versions = await scriptApi(`projects/${scriptId}/versions?pageSize=200`, token);
  let version = (versions.versions || []).find(item => item.description === versionDescription);
  if (!version) {
    version = await scriptApi(`projects/${scriptId}/versions`, token, {
      method: 'POST',
      body: JSON.stringify({ description: versionDescription })
    });
  }
  if (!Number.isInteger(version.versionNumber)) throw new Error('Version creation did not return versionNumber');

  const deployments = await scriptApi(`projects/${scriptId}/deployments?pageSize=50`, token);
  const managed = (deployments.deployments || []).filter(
    item => item?.deploymentConfig?.description === deploymentDescription
  );
  if (managed.length > 1) throw new Error(`Multiple managed GAS deployments found: ${managed.length}`);

  const config = {
    scriptId: process.env.GAS_SCRIPT_ID,
    versionNumber: version.versionNumber,
    manifestFileName: 'appsscript',
    description: deploymentDescription
  };

  let deployment;
  if (managed.length === 0) {
    deployment = await scriptApi(`projects/${scriptId}/deployments`, token, {
      method: 'POST',
      body: JSON.stringify(config)
    });
  } else {
    const deploymentId = encodeURIComponent(managed[0].deploymentId);
    deployment = await scriptApi(`projects/${scriptId}/deployments/${deploymentId}`, token, {
      method: 'PUT',
      body: JSON.stringify({ deploymentConfig: config })
    });
  }

  if (!deployment.deploymentId) throw new Error('Deployment response did not contain deploymentId');

  let execUrl = webAppUrl(deployment);
  for (let attempt = 0; !execUrl && attempt < 5; attempt += 1) {
    await sleep(2000);
    deployment = await scriptApi(
      `projects/${scriptId}/deployments/${encodeURIComponent(deployment.deploymentId)}`,
      token
    );
    execUrl = webAppUrl(deployment);
  }
  if (!execUrl) throw new Error('Deployment did not expose a Web App URL');

  let lastError = 'unknown';
  for (let attempt = 1; attempt <= 10; attempt += 1) {
    try {
      const response = await fetch(execUrl, { redirect: 'follow', cache: 'no-store' });
      const text = await response.text();
      if (!response.ok) {
        lastError = `HTTP ${response.status}: ${text.slice(0, 500)}`;
      } else {
        const health = JSON.parse(text);
        const ok =
          health?.ok === true &&
          health?.service === 'VHDCHY_GOOGLE_GATEWAY' &&
          String(health?.environment || '').toUpperCase() === 'BETA' &&
          health?.scriptId === process.env.GAS_SCRIPT_ID &&
          health?.bootstrap?.authorized === true &&
          health?.bootstrap?.configMatch === true;
        if (ok) {
          console.log(`GAS deployment PASS: version=${version.versionNumber}`);
          console.log(`GAS_DEPLOYMENT_ID=${deployment.deploymentId}`);
          console.log(`GAS_EXEC_URL=${execUrl}`);
          console.log('GAS bootstrap verification PASS');
          return;
        }
        lastError = `identity/bootstrap mismatch: ${JSON.stringify(health)}`;
      }
    } catch (error) {
      lastError = error instanceof Error ? error.message : String(error);
    }
    await sleep(3000);
  }

  throw new Error(`Web App verification failed after retries: ${lastError}`);
}

const dispatch = JSON.parse(await fs.readFile('.github/dispatch/gas-beta-sync.json', 'utf8'));
if (!['sync', 'deploy'].includes(dispatch.operation)) throw new Error(`Unsupported operation: ${dispatch.operation}`);

const codeTemplate = await fs.readFile('service/google-gateway/Code.gs', 'utf8');
const manifestSource = await fs.readFile('service/google-gateway/appsscript.json', 'utf8');
const manifest = JSON.parse(manifestSource);

const expectedScopes = new Set([
  'https://www.googleapis.com/auth/spreadsheets',
  'https://www.googleapis.com/auth/userinfo.email'
]);
const actualScopes = new Set(manifest.oauthScopes || []);
if (actualScopes.size !== expectedScopes.size || [...expectedScopes].some(scope => !actualScopes.has(scope))) {
  throw new Error('Unexpected Google Gateway runtime scopes');
}

const code = renderCode(codeTemplate);
const token = await googleAccessToken();
const scriptId = encodeURIComponent(process.env.GAS_SCRIPT_ID);

const before = await scriptApi(`projects/${scriptId}/content`, token);
console.log(`Current GAS files: ${(before.files || []).map(file => file.name).join(', ') || '(none)'}`);

const desired = {
  files: [
    { name: 'Code', type: 'SERVER_JS', source: code },
    { name: 'appsscript', type: 'JSON', source: JSON.stringify(manifest) }
  ]
};

await scriptApi(`projects/${scriptId}/content`, token, {
  method: 'PUT',
  body: JSON.stringify(desired)
});

const after = await scriptApi(`projects/${scriptId}/content`, token);
const byName = new Map((after.files || []).map(file => [file.name, file]));
if (byName.size !== 2 || !byName.has('Code') || !byName.has('appsscript')) {
  throw new Error(`Unexpected GAS file set after sync: ${[...byName.keys()].join(', ')}`);
}
if (byName.get('Code').source !== code) throw new Error('Code.gs verification mismatch after sync');
const remoteManifest = JSON.parse(byName.get('appsscript').source);
if (JSON.stringify(remoteManifest) !== JSON.stringify(manifest)) throw new Error('appsscript.json verification mismatch after sync');

console.log(`GAS sync PASS: ${process.env.APP_ENV} / ${process.env.GAS_SCRIPT_ID}`);

if (dispatch.operation === 'deploy' || dispatch.deploy === true) {
  await deployVersion(token, scriptId);
}
