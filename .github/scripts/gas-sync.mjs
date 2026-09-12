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
  scriptId: process.env.GAS_SCRIPT_ID,
  files: [
    { name: 'Code', type: 'SERVER_JS', source: code },
    { name: 'appsscript', type: 'JSON', source: JSON.stringify(manifest) }
  ]
};

await scriptApi(`projects/${scriptId}/content`, token, {
  method: 'PUT',
  body: JSON.stringify({ files: desired.files })
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
