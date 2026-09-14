import { PROJECTION_PROTOCOL, buildProjectionEnvelope } from './projection.js';

export const PROJECTION_SEND_MAX_ITEMS = 25;
export const PROJECTION_SEND_TIMEOUT_MS = 8000;

function validGatewayUrl(value) {
  try {
    const url = new URL(String(value || ''));
    return url.protocol === 'https:' && Boolean(url.hostname) ? url.toString() : null;
  } catch {
    return null;
  }
}

function projectionItem(row, environment) {
  const envelope = buildProjectionEnvelope(row, environment);
  const payload = envelope.payload;
  if (typeof payload.sheet !== 'string' || !payload.sheet.trim()) throw new Error('PROJECTION_SHEET_REQUIRED');
  if (!payload.values || typeof payload.values !== 'object' || Array.isArray(payload.values)) {
    throw new Error('PROJECTION_VALUES_REQUIRED');
  }
  return {
    outboxId: envelope.outboxId,
    eventId: envelope.eventId,
    sheet: payload.sheet.trim(),
    values: payload.values
  };
}

export function buildGatewayProjectionBatch(rows, options = {}) {
  if (!Array.isArray(rows) || rows.length < 1 || rows.length > PROJECTION_SEND_MAX_ITEMS) {
    throw new Error('PROJECTION_BATCH_SIZE_INVALID');
  }
  const environment = String(options.environment || 'BETA').toUpperCase();
  const sharedToken = String(options.sharedToken || '');
  if (sharedToken.length < 32 || sharedToken.length > 256) throw new Error('PROJECTION_SHARED_TOKEN_REQUIRED');
  const items = rows.map(row => projectionItem(row, environment));
  const identities = new Set(items.map(item => `${item.outboxId}:${item.eventId}`));
  if (identities.size !== items.length) throw new Error('PROJECTION_BATCH_IDENTITY_DUPLICATE');
  return { protocol: PROJECTION_PROTOCOL, environment, sharedToken, items };
}

function retryableStatus(status) {
  return status === 408 || status === 429 || status >= 500;
}

export async function sendProjectionBatch(rows, options = {}) {
  const gatewayUrl = validGatewayUrl(options.gatewayUrl);
  if (!gatewayUrl) return { ok: false, code: 'PROJECTION_GATEWAY_URL_INVALID', retryable: false };

  let body;
  try {
    body = buildGatewayProjectionBatch(rows, options);
  } catch (error) {
    return { ok: false, code: error?.message || 'PROJECTION_BATCH_INVALID', retryable: false };
  }

  const fetchImpl = options.fetchImpl || globalThis.fetch?.bind(globalThis);
  if (typeof fetchImpl !== 'function') return { ok: false, code: 'PROJECTION_FETCH_UNAVAILABLE', retryable: true };
  const timeoutMs = Math.max(1000, Math.min(30000, Number(options.timeoutMs || PROJECTION_SEND_TIMEOUT_MS)));
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  let response;
  try {
    response = await fetchImpl(gatewayUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      redirect: 'follow',
      cache: 'no-store',
      signal: controller.signal
    });
  } catch {
    return { ok: false, code: 'PROJECTION_NETWORK_ERROR', retryable: true };
  } finally {
    clearTimeout(timer);
  }

  if (!response?.ok) {
    const status = Number(response?.status || 0);
    return { ok: false, code: `PROJECTION_HTTP_${status || 'ERROR'}`, status, retryable: retryableStatus(status) };
  }

  let payload;
  try {
    payload = await response.json();
  } catch {
    return { ok: false, code: 'PROJECTION_RESPONSE_INVALID', retryable: true };
  }

  if (payload?.protocol !== PROJECTION_PROTOCOL || String(payload?.environment || '').toUpperCase() !== body.environment) {
    return { ok: false, code: 'PROJECTION_RESPONSE_IDENTITY_MISMATCH', retryable: false };
  }
  if (!Array.isArray(payload.results) || payload.results.length !== body.items.length) {
    return { ok: false, code: 'PROJECTION_RECEIPT_COUNT_MISMATCH', retryable: true };
  }

  const receipts = payload.results.map((result, index) => ({
    outboxId: body.items[index].outboxId,
    eventId: body.items[index].eventId,
    ok: result?.ok === true,
    status: result?.status || null,
    code: result?.code || null,
    sheet: result?.sheet || body.items[index].sheet,
    key: result?.key || null
  }));
  const failed = receipts.find(receipt => receipt.ok !== true);
  if (payload.ok !== true || failed) {
    return {
      ok: false,
      code: failed?.code || 'PROJECTION_GATEWAY_REJECTED',
      retryable: true,
      receipts
    };
  }

  return { ok: true, protocol: PROJECTION_PROTOCOL, environment: body.environment, receipts };
}
