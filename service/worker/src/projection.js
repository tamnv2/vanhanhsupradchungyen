export const PROJECTION_PROTOCOL = 'VHDCHY_PROJECTION_V1';
export const PROJECTION_TARGET = 'GOOGLE_SHEETS';
export const PROJECTION_MAX_ATTEMPTS = 8;
export const PROJECTION_PROCESSING_LEASE_MS = 5 * 60 * 1000;

function parsePayload(value) {
  if (value && typeof value === 'object' && !Array.isArray(value)) return value;
  try {
    const parsed = JSON.parse(String(value || '{}'));
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

export function buildProjectionEnvelope(row, environment = 'BETA') {
  if (!row?.event_id || !Number.isSafeInteger(Number(row?.outbox_id))) {
    throw new Error('INVALID_OUTBOX_ROW');
  }
  return {
    protocol: PROJECTION_PROTOCOL,
    environment: String(environment || 'BETA').toUpperCase(),
    target: String(row.projection_target || PROJECTION_TARGET),
    outboxId: Number(row.outbox_id),
    eventId: String(row.event_id),
    payload: parsePayload(row.payload_json)
  };
}

export function retryDelayMs(attemptNumber) {
  const attempt = Math.max(1, Math.min(PROJECTION_MAX_ATTEMPTS, Number(attemptNumber) || 1));
  return Math.min(60 * 60 * 1000, 30 * 1000 * (2 ** (attempt - 1)));
}

export function nextRetryAt(attemptNumber, nowMs = Date.now()) {
  return new Date(nowMs + retryDelayMs(attemptNumber)).toISOString();
}

export function classifyProjectionFailure(currentAttempts, nowMs = Date.now()) {
  const attempts = Math.max(0, Number(currentAttempts) || 0) + 1;
  if (attempts >= PROJECTION_MAX_ATTEMPTS) {
    return { status: 'DEAD', attempts, nextAttemptAt: null };
  }
  return { status: 'PENDING', attempts, nextAttemptAt: nextRetryAt(attempts, nowMs) };
}

export function projectionLeaseStaleBefore(nowMs = Date.now()) {
  return new Date(Number(nowMs) - PROJECTION_PROCESSING_LEASE_MS).toISOString();
}

export async function loadPendingOutbox(db, limit = 25, nowMs = Date.now()) {
  if (!db) return [];
  const safeLimit = Math.max(1, Math.min(100, Number(limit) || 25));
  const nowIso = new Date(nowMs).toISOString();
  const staleBeforeIso = projectionLeaseStaleBefore(nowMs);
  const result = await db.prepare(`
    SELECT outbox_id, event_id, projection_target, payload_json, status, attempts, next_attempt_at, updated_at
    FROM projection_outbox
    WHERE projection_target = ?
      AND (
        (status = 'PENDING' AND (next_attempt_at IS NULL OR next_attempt_at <= ?))
        OR (status = 'PROCESSING' AND updated_at <= ?)
      )
    ORDER BY outbox_id ASC
    LIMIT ?
  `).bind(PROJECTION_TARGET, nowIso, staleBeforeIso, safeLimit).all();
  return Array.isArray(result?.results) ? result.results : [];
}

export async function markOutboxProcessing(db, rows, nowIso = new Date().toISOString(), staleBeforeIso = projectionLeaseStaleBefore(Date.parse(nowIso))) {
  const statements = (Array.isArray(rows) ? rows : []).map(row =>
    db.prepare(`
      UPDATE projection_outbox
      SET status = 'PROCESSING', updated_at = ?
      WHERE outbox_id = ? AND event_id = ?
        AND (
          status = 'PENDING'
          OR (status = 'PROCESSING' AND updated_at <= ?)
        )
    `).bind(nowIso, Number(row.outbox_id), String(row.event_id), staleBeforeIso)
  );
  if (!statements.length) return [];
  return db.batch(statements);
}

export async function markOutboxAcked(db, row, nowIso = new Date().toISOString()) {
  return db.prepare(`
    UPDATE projection_outbox
    SET status = 'ACKED', last_error_code = NULL, next_attempt_at = NULL, updated_at = ?
    WHERE outbox_id = ? AND event_id = ? AND status = 'PROCESSING'
  `).bind(nowIso, Number(row.outbox_id), String(row.event_id)).run();
}

export async function markOutboxFailed(db, row, errorCode, nowMs = Date.now()) {
  const state = classifyProjectionFailure(row?.attempts, nowMs);
  return db.prepare(`
    UPDATE projection_outbox
    SET status = ?, attempts = ?, next_attempt_at = ?, last_error_code = ?, updated_at = ?
    WHERE outbox_id = ? AND event_id = ? AND status = 'PROCESSING'
  `).bind(
    state.status,
    state.attempts,
    state.nextAttemptAt,
    String(errorCode || 'PROJECTION_ERROR').slice(0, 128),
    new Date(nowMs).toISOString(),
    Number(row.outbox_id),
    String(row.event_id)
  ).run();
}
