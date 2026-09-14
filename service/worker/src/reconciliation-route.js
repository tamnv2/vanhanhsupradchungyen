import { createD1ReconciliationStore, ingestReconciliationEnvelope } from './reconciliation.js';
import { verifyReconciliationRequestSignature } from './reconciliation-request-auth.js';

const MAX_RECONCILIATION_BODY_BYTES = 256 * 1024;

function reply(payload, status, requestId) {
  return Response.json(requestId ? { ...payload, requestId } : payload, {
    status,
    headers: {
      'cache-control': 'no-store',
      'x-content-type-options': 'nosniff'
    }
  });
}

function failure(code, reason, status, requestId) {
  return reply({ ok: false, error: { code, reason } }, status, requestId);
}

async function readRawBody(request, requestId) {
  const declaredLength = Number(request.headers.get('content-length') || 0);
  if (Number.isFinite(declaredLength) && declaredLength > MAX_RECONCILIATION_BODY_BYTES) {
    return { response: failure('REQUEST_BODY_TOO_LARGE', 'BODY_LIMIT_EXCEEDED', 413, requestId), rawBody: null };
  }

  let rawBody;
  try {
    rawBody = await request.text();
  } catch {
    return { response: failure('REQUEST_BODY_INVALID', 'BODY_READ_FAILED', 400, requestId), rawBody: null };
  }

  if (new TextEncoder().encode(rawBody).byteLength > MAX_RECONCILIATION_BODY_BYTES) {
    return { response: failure('REQUEST_BODY_TOO_LARGE', 'BODY_LIMIT_EXCEEDED', 413, requestId), rawBody: null };
  }
  return { response: null, rawBody };
}

export async function handleReconciliationIngestRoute(request, env, requestId) {
  if (!env?.DB) return failure('RECONCILIATION_UNAVAILABLE', 'D1_BINDING_MISSING', 503, requestId);

  const url = new URL(request.url);
  if (url.search) return failure('MACHINE_AUTH_INVALID', 'QUERY_NOT_ALLOWED', 401, requestId);

  const expectedKeyId = String(env.LAN_RECONCILIATION_KEY_ID || '');
  const keyMaterial = String(env.LAN_RECONCILIATION_SHARED_SECRET || '');
  if (!expectedKeyId || keyMaterial.length < 32) {
    return failure('MACHINE_AUTH_UNAVAILABLE', 'MACHINE_CREDENTIAL_NOT_CONFIGURED', 503, requestId);
  }

  const body = await readRawBody(request, requestId);
  if (body.response) return body.response;

  const keyId = request.headers.get('x-vhdchy-machine-key-id') || '';
  if (keyId !== expectedKeyId) return failure('MACHINE_AUTH_INVALID', 'KEY_ID_MISMATCH', 401, requestId);

  const auth = await verifyReconciliationRequestSignature({
    method: request.method,
    path: url.pathname,
    timestampMs: request.headers.get('x-vhdchy-machine-timestamp'),
    nonce: request.headers.get('x-vhdchy-machine-nonce'),
    bodyHash: request.headers.get('x-vhdchy-content-sha256'),
    signature: request.headers.get('x-vhdchy-machine-signature'),
    environment: String(env.APP_ENV || '').toUpperCase(),
    keyId,
    rawBody: body.rawBody,
    keyMaterial
  });
  if (!auth.ok) {
    const status = auth.code === 'MACHINE_AUTH_UNAVAILABLE' ? 503 : 401;
    return failure(auth.code, auth.reason, status, requestId);
  }

  let envelope;
  try {
    envelope = JSON.parse(body.rawBody || '{}');
  } catch {
    return failure('REQUEST_JSON_INVALID', 'BODY_MUST_BE_JSON', 400, requestId);
  }
  if (!envelope || typeof envelope !== 'object' || Array.isArray(envelope)) {
    return failure('REQUEST_JSON_OBJECT_REQUIRED', 'ENVELOPE_OBJECT_REQUIRED', 400, requestId);
  }

  let result;
  try {
    result = await ingestReconciliationEnvelope(
      createD1ReconciliationStore(env.DB),
      envelope,
      String(env.APP_ENV || '').toUpperCase()
    );
  } catch {
    return failure('RECONCILIATION_UNAVAILABLE', 'INGEST_STORE_FAILURE', 503, requestId);
  }

  if (!result.ok) {
    return reply({ ok: false, error: { code: result.code, reason: result.reason, details: result } }, result.status || 409, requestId);
  }

  return reply({
    ok: true,
    reconciliationStatus: result.reconciliationStatus,
    edgeEventId: result.edgeEventId,
    duplicate: result.duplicate,
    attachedReceiptCount: result.attachedReceiptCount,
    machineAuthVersion: auth.authVersion
  }, result.duplicate ? 200 : 202, requestId);
}
