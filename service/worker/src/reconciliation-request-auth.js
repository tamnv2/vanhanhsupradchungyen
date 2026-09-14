const encoder = new TextEncoder();

export const RECONCILIATION_AUTH_VERSION = 'VHDCHY_RECONCILIATION_HMAC_V1';
export const RECONCILIATION_AUTH_MAX_SKEW_MS = 5 * 60 * 1000;

function bytesToHex(bytes) {
  return Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
}

function hexToBytes(value) {
  const normalized = String(value || '').toLowerCase();
  if (!/^[0-9a-f]{64}$/.test(normalized)) return null;
  const bytes = new Uint8Array(32);
  for (let i = 0; i < normalized.length; i += 2) {
    bytes[i / 2] = Number.parseInt(normalized.slice(i, i + 2), 16);
  }
  return bytes;
}

async function sha256Hex(value) {
  const digest = await crypto.subtle.digest('SHA-256', encoder.encode(String(value ?? '')));
  return bytesToHex(new Uint8Array(digest));
}

async function importHmacKey(keyMaterial) {
  const value = String(keyMaterial || '');
  if (value.length < 32) throw new Error('RECONCILIATION_AUTH_KEY_REQUIRED');
  return crypto.subtle.importKey(
    'raw',
    encoder.encode(value),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign', 'verify']
  );
}

export function buildReconciliationCanonicalRequest({ method, path, timestampMs, nonce, bodyHash, environment, keyId }) {
  return [
    RECONCILIATION_AUTH_VERSION,
    String(method || '').toUpperCase(),
    String(path || ''),
    String(timestampMs),
    String(nonce || ''),
    String(bodyHash || '').toLowerCase(),
    String(environment || '').toUpperCase(),
    String(keyId || '')
  ].join('\n');
}

export async function signReconciliationRequest(options = {}) {
  const bodyHash = await sha256Hex(options.rawBody);
  const canonical = buildReconciliationCanonicalRequest({ ...options, bodyHash });
  const key = await importHmacKey(options.keyMaterial);
  const signature = await crypto.subtle.sign('HMAC', key, encoder.encode(canonical));
  return { bodyHash, signature: bytesToHex(new Uint8Array(signature)) };
}

export async function verifyReconciliationRequestSignature(options = {}) {
  const timestampMs = Number(options.timestampMs);
  const nowMs = Number(options.nowMs ?? Date.now());
  if (!Number.isSafeInteger(timestampMs)) return { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'TIMESTAMP_INVALID' };
  if (Math.abs(nowMs - timestampMs) > RECONCILIATION_AUTH_MAX_SKEW_MS) {
    return { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'TIMESTAMP_OUTSIDE_WINDOW' };
  }
  if (!/^[A-Za-z0-9._:-]{3,128}$/.test(String(options.keyId || ''))) {
    return { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'KEY_ID_INVALID' };
  }
  if (!/^[A-Za-z0-9_-]{16,128}$/.test(String(options.nonce || ''))) {
    return { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'NONCE_INVALID' };
  }

  const expectedBodyHash = await sha256Hex(options.rawBody);
  if (String(options.bodyHash || '').toLowerCase() !== expectedBodyHash) {
    return { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'BODY_HASH_MISMATCH' };
  }

  const signatureBytes = hexToBytes(options.signature);
  if (!signatureBytes) return { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'SIGNATURE_INVALID' };

  let key;
  try {
    key = await importHmacKey(options.keyMaterial);
  } catch {
    return { ok: false, code: 'MACHINE_AUTH_UNAVAILABLE', reason: 'KEY_NOT_CONFIGURED' };
  }

  const canonical = buildReconciliationCanonicalRequest({
    method: options.method,
    path: options.path,
    timestampMs,
    nonce: options.nonce,
    bodyHash: expectedBodyHash,
    environment: options.environment,
    keyId: options.keyId
  });
  const verified = await crypto.subtle.verify('HMAC', key, signatureBytes, encoder.encode(canonical));
  return verified
    ? { ok: true, authVersion: RECONCILIATION_AUTH_VERSION }
    : { ok: false, code: 'MACHINE_AUTH_INVALID', reason: 'SIGNATURE_MISMATCH' };
}
