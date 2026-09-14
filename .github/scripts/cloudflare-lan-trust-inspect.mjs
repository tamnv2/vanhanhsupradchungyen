const required = ['CLOUDFLARE_ACCOUNT_ID', 'CLOUDFLARE_API_TOKEN'];
for (const name of required) {
  if (!process.env[name]) throw new Error(`Missing required environment value: ${name}`);
}

const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
const token = process.env.CLOUDFLARE_API_TOKEN;
const expectedZone = 'supra.cc.cd';
const betaLanHost = 'lan-beta.supra.cc.cd';
const acmeHost = `_acme-challenge.${betaLanHost}`;

async function cfRaw(path, init = {}) {
  const response = await fetch(`https://api.cloudflare.com/client/v4${path}`, {
    ...init,
    headers: {
      authorization: `Bearer ${token}`,
      ...(init.body ? { 'content-type': 'application/json' } : {}),
      ...(init.headers || {})
    }
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

async function cf(path, init = {}) {
  const { response, payload } = await cfRaw(path, init);
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

function classifyContent(record) {
  const value = String(record?.content || '');
  if (record?.type === 'A') {
    const parts = value.split('.').map(Number);
    if (parts.length === 4 && parts.every(part => Number.isInteger(part) && part >= 0 && part <= 255)) {
      const [a, b] = parts;
      const isPrivate = a === 10 || a === 127 || (a === 192 && b === 168) || (a === 172 && b >= 16 && b <= 31);
      return isPrivate ? 'ipv4-private-or-loopback' : 'ipv4-public';
    }
    return 'ipv4-unparsed';
  }
  if (record?.type === 'AAAA') return 'ipv6';
  if (record?.type === 'CNAME') return 'hostname';
  if (record?.type === 'TXT') return 'txt-hidden';
  return 'other-hidden';
}

function safeRecord(record) {
  return {
    id: record?.id || null,
    type: record?.type || null,
    name: record?.name || null,
    proxied: record?.proxied === true,
    ttl: record?.ttl ?? null,
    content_class: classifyContent(record)
  };
}

const tokenKind = await verifyToken();
console.log(`TOKEN_VERIFY=PASS:${tokenKind}`);
console.log(`ACCOUNT_EXPECTED=${accountId}`);

const zones = await cf(
  `/zones?name=${encodeURIComponent(expectedZone)}&account.id=${encodeURIComponent(accountId)}&per_page=50`
);
const matches = (Array.isArray(zones.result) ? zones.result : []).filter(zone => zone?.name === expectedZone);
if (matches.length !== 1) {
  throw new Error(`Expected exactly one ${expectedZone} zone in the configured account; found ${matches.length}`);
}

const zone = matches[0];
if (zone?.account?.id && zone.account.id !== accountId) {
  throw new Error(`Zone/account mismatch for ${expectedZone}`);
}
console.log(`ZONE_NAME=${zone.name}`);
console.log(`ZONE_ID=${zone.id}`);
console.log(`ZONE_STATUS=${zone.status || 'unknown'}`);

async function inspectName(name) {
  const payload = await cf(
    `/zones/${encodeURIComponent(zone.id)}/dns_records?name=${encodeURIComponent(name)}&per_page=100`
  );
  const records = Array.isArray(payload.result) ? payload.result : [];
  console.log(`DNS_RECORDS_${name.replace(/[^A-Za-z0-9]+/g, '_').toUpperCase()}=${JSON.stringify(records.map(safeRecord))}`);
  return records;
}

const betaRecords = await inspectName(betaLanHost);
const acmeRecords = await inspectName(acmeHost);

const conflicting = betaRecords.filter(record => !['A', 'AAAA', 'CNAME'].includes(String(record?.type || '')));
if (conflicting.length > 0) {
  throw new Error(`Unexpected DNS record type already occupies ${betaLanHost}`);
}

console.log(`BETA_LAN_RECORD_COUNT=${betaRecords.length}`);
console.log(`ACME_CHALLENGE_RECORD_COUNT=${acmeRecords.length}`);
console.log('CLOUDFLARE_LAN_TRUST_INSPECT=PASS');
