import fs from 'node:fs';

const dispatch = JSON.parse(fs.readFileSync('.github/dispatch/gas-beta-sync.json', 'utf8'));
if (dispatch.operation !== 'provision_projection') {
  console.log(`PROJECTION_PUBLIC_AUTH_PROBE_SKIPPED operation=${dispatch.operation}`);
  process.exit(0);
}

const token = String(process.env.VHDCHY_PROJECTION_SHARED_TOKEN || '');
if (token.length < 32 || token.length > 256) {
  throw new Error('VHDCHY_PROJECTION_SHARED_TOKEN is missing or outside the approved length range');
}

const spec = JSON.parse(fs.readFileSync('service/worker/deploy.beta.json', 'utf8'));
const gatewayUrl = String(spec?.vars?.GAS_EXEC_URL || '').trim();
if (!gatewayUrl.startsWith('https://script.google.com/macros/s/')) {
  throw new Error('Reviewed GAS_EXEC_URL is missing or invalid');
}

async function post(sharedToken, includeToken = true) {
  const body = {
    protocol: 'VHDCHY_PROJECTION_V1',
    environment: 'BETA',
    items: []
  };
  if (includeToken) body.sharedToken = sharedToken;

  const response = await fetch(gatewayUrl, {
    method: 'POST',
    redirect: 'follow',
    cache: 'no-store',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(body)
  });
  const text = await response.text();
  let payload;
  try {
    payload = JSON.parse(text);
  } catch {
    throw new Error(`Projection public auth probe returned non-JSON HTTP ${response.status}`);
  }
  if (!response.ok) throw new Error(`Projection public auth probe returned HTTP ${response.status}`);
  return payload;
}

function expectCode(payload, expected, label) {
  if (payload?.ok !== false || payload?.code !== expected || String(payload?.environment || '').toUpperCase() !== 'BETA') {
    throw new Error(`${label} mismatch: expected ${expected}, received ${String(payload?.code || 'missing')}`);
  }
}

const missing = await post('', false);
expectCode(missing, 'PROJECTION_AUTH_FAILED', 'missing-token');

const wrongToken = `${token[0] === 'A' ? 'B' : 'A'}${token.slice(1)}`;
const wrong = await post(wrongToken, true);
expectCode(wrong, 'PROJECTION_AUTH_FAILED', 'wrong-token');

const correct = await post(token, true);
expectCode(correct, 'PROJECTION_NOT_LIVE', 'correct-token-disabled-gate');

console.log('PROJECTION_PUBLIC_AUTH_E2E_PASS missing=REJECTED wrong=REJECTED correct=AUTHENTICATED_NOT_LIVE');
