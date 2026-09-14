const encoder = new TextEncoder();

function bytesToHex(bytes) {
  return Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
}

export async function sha256PayloadJson(payloadJson) {
  const digest = await crypto.subtle.digest('SHA-256', encoder.encode(String(payloadJson ?? '')));
  return bytesToHex(new Uint8Array(digest));
}

export async function verifyReconciliationPayloadHash(envelope) {
  const declared = String(envelope?.payloadHash || '').trim().toLowerCase();
  if (!/^[0-9a-f]{64}$/.test(declared)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'PAYLOAD_HASH_INVALID' };
  }

  const actual = await sha256PayloadJson(envelope?.payloadJson);
  if (declared !== actual) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'PAYLOAD_HASH_MISMATCH' };
  }

  return { ok: true, payloadHash: actual };
}
