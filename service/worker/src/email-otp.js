const encoder = new TextEncoder();

export const EMAIL_OTP_DIGITS = 4;
export const EMAIL_OTP_TTL_MS = 5 * 60 * 1000;
export const EMAIL_OTP_RESEND_COOLDOWN_MS = 5 * 60 * 1000;
export const EMAIL_OTP_PURPOSES = Object.freeze({
  ROOT_LOGIN: 'ROOT_LOGIN',
  PASSWORD_RECOVERY: 'PASSWORD_RECOVERY'
});

function iso(value) {
  return new Date(Number(value)).toISOString();
}

function normalizePurpose(value) {
  const purpose = String(value || '').toUpperCase();
  return Object.values(EMAIL_OTP_PURPOSES).includes(purpose) ? purpose : null;
}

function normalizePepper(value) {
  const pepper = String(value || '');
  if (pepper.length < 32) throw new Error('OTP_PEPPER_REQUIRED');
  return pepper;
}

function bytesToHex(bytes) {
  return Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
}

async function hmacHex(pepper, namespace, value) {
  const key = await crypto.subtle.importKey(
    'raw',
    encoder.encode(normalizePepper(pepper)),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign']
  );
  const signature = await crypto.subtle.sign(
    'HMAC',
    key,
    encoder.encode(`${namespace}:${String(value)}`)
  );
  return bytesToHex(new Uint8Array(signature));
}

export function generateEmailOtp() {
  const range = 10000;
  const limit = 0x100000000 - (0x100000000 % range);
  let value;
  do {
    value = crypto.getRandomValues(new Uint32Array(1))[0];
  } while (value >= limit);
  return String(value % range).padStart(EMAIL_OTP_DIGITS, '0');
}

export async function hashEmailOtp(challengeId, code, pepper) {
  const normalized = String(code || '').trim();
  if (!/^\d{4}$/.test(normalized) || !challengeId) return null;
  return hmacHex(pepper, 'VHDCHY_EMAIL_OTP', `${challengeId}:${normalized}`);
}

export async function hashOtpDestination(email, pepper) {
  const normalized = String(email || '').trim().toLowerCase();
  if (!normalized || !normalized.includes('@')) return null;
  return hmacHex(pepper, 'VHDCHY_EMAIL_DESTINATION', normalized);
}

async function hashDeliveryRef(value, pepper) {
  if (!value) return null;
  return hmacHex(pepper, 'VHDCHY_EMAIL_DELIVERY_REF', value);
}

export function otpPolicyWindow(nowMs = Date.now()) {
  const issuedAtMs = Number(nowMs);
  return {
    issuedAt: iso(issuedAtMs),
    expiresAt: iso(issuedAtMs + EMAIL_OTP_TTL_MS),
    resendNotBefore: iso(issuedAtMs + EMAIL_OTP_RESEND_COOLDOWN_MS)
  };
}

export function evaluateOtpCooldown(lastIssuedAt, nowMs = Date.now()) {
  if (!lastIssuedAt) return { allowed: true, retryAfterMs: 0 };
  const issuedMs = Date.parse(lastIssuedAt);
  if (!Number.isFinite(issuedMs)) return { allowed: false, retryAfterMs: EMAIL_OTP_RESEND_COOLDOWN_MS };
  const remaining = issuedMs + EMAIL_OTP_RESEND_COOLDOWN_MS - Number(nowMs);
  return remaining > 0
    ? { allowed: false, retryAfterMs: remaining }
    : { allowed: true, retryAfterMs: 0 };
}

function batchSucceeded(results) {
  return Array.isArray(results) && results.every(result => result?.success !== false);
}

function firstBatchRow(result) {
  return Array.isArray(result?.results) && result.results.length ? result.results[0] : null;
}

function auditInsert(db, { eventId, eventType, userId, outcome, reasonCode, requestId, occurredAt, metadata }) {
  return db.prepare(`
    INSERT INTO auth_audit_events(
      auth_event_id, event_type, user_id, outcome, reason_code,
      request_id, metadata_json, occurred_at
    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  `).bind(
    eventId,
    eventType,
    userId,
    outcome,
    reasonCode || null,
    requestId || null,
    JSON.stringify(metadata || {}),
    occurredAt
  );
}

async function writeAudit(db, event) {
  try {
    const result = await auditInsert(db, event).run();
    return result?.success !== false;
  } catch {
    return false;
  }
}

export async function createEmailOtpRequest(db, options = {}) {
  const userId = String(options.userId || '');
  const purpose = normalizePurpose(options.purpose);
  const destinationHash = String(options.destinationHash || '');
  const destinationHint = options.destinationHint ? String(options.destinationHint).slice(0, 128) : null;
  const requestId = options.requestId ? String(options.requestId) : null;
  const nowMs = Number(options.nowMs ?? Date.now());
  const requestedAt = iso(nowMs);
  if (!db || !userId || !purpose || !destinationHash) {
    return { ok: false, code: 'OTP_REQUEST_CONTEXT_REQUIRED' };
  }

  const last = await db.prepare(`
    SELECT issued_at
    FROM auth_email_otp_challenges
    WHERE user_id = ? AND purpose = ? AND issued_at IS NOT NULL
    ORDER BY issued_at DESC
    LIMIT 1
  `).bind(userId, purpose).first();
  const cooldown = evaluateOtpCooldown(last?.issued_at, nowMs);
  if (!cooldown.allowed) {
    return { ok: false, code: 'OTP_RESEND_COOLDOWN', retryAfterMs: cooldown.retryAfterMs };
  }

  const challengeId = crypto.randomUUID();
  const code = generateEmailOtp();
  const codeHash = await hashEmailOtp(challengeId, code, options.pepper);
  const eventId = crypto.randomUUID();

  const insertChallenge = db.prepare(`
    INSERT INTO auth_email_otp_challenges(
      challenge_id, user_id, purpose, destination_hash, destination_hint,
      code_hash, code_digits, state, requested_at, request_id, metadata_json
    ) VALUES (?, ?, ?, ?, ?, ?, 4, 'REQUESTED', ?, ?, '{}')
  `).bind(
    challengeId,
    userId,
    purpose,
    destinationHash,
    destinationHint,
    codeHash,
    requestedAt,
    requestId
  );
  const insertAudit = auditInsert(db, {
    eventId,
    eventType: 'EMAIL_OTP_REQUESTED',
    userId,
    outcome: 'REQUESTED',
    reasonCode: null,
    requestId,
    occurredAt: requestedAt,
    metadata: { purpose }
  });

  try {
    const results = await db.batch([insertChallenge, insertAudit]);
    if (!batchSucceeded(results)) return { ok: false, code: 'OTP_REQUEST_FAILED' };
  } catch {
    return { ok: false, code: 'OTP_REQUEST_IN_PROGRESS' };
  }

  return {
    ok: true,
    challengeId,
    userId,
    purpose,
    destinationHash,
    destinationHint,
    requestId,
    requestedAt,
    code
  };
}

export async function markEmailOtpDelivered(db, challenge, options = {}) {
  if (!db || !challenge?.challengeId || !challenge?.userId || !challenge?.purpose) {
    return { ok: false, code: 'OTP_DELIVERY_CONTEXT_REQUIRED' };
  }
  const nowMs = Number(options.nowMs ?? Date.now());
  const window = otpPolicyWindow(nowMs);
  const provider = options.provider ? String(options.provider).slice(0, 64) : null;
  const deliveryRefHash = await hashDeliveryRef(options.deliveryRef, options.pepper);
  const eventId = crypto.randomUUID();

  const supersede = db.prepare(`
    UPDATE auth_email_otp_challenges
    SET state = 'SUPERSEDED', superseded_at = ?
    WHERE user_id = ? AND purpose = ? AND state = 'ISSUED' AND challenge_id <> ?
  `).bind(window.issuedAt, challenge.userId, challenge.purpose, challenge.challengeId);

  const issue = db.prepare(`
    UPDATE auth_email_otp_challenges
    SET state = 'ISSUED', issued_at = ?, expires_at = ?, resend_not_before = ?,
        delivery_provider = ?, delivery_ref_hash = ?
    WHERE challenge_id = ? AND user_id = ? AND purpose = ? AND state = 'REQUESTED'
    RETURNING challenge_id
  `).bind(
    window.issuedAt,
    window.expiresAt,
    window.resendNotBefore,
    provider,
    deliveryRefHash,
    challenge.challengeId,
    challenge.userId,
    challenge.purpose
  );

  const audit = db.prepare(`
    INSERT INTO auth_audit_events(
      auth_event_id, event_type, user_id, outcome, request_id, metadata_json, occurred_at
    )
    SELECT ?, 'EMAIL_OTP_ISSUED', user_id, 'SUCCESS', ?, ?, ?
    FROM auth_email_otp_challenges
    WHERE challenge_id = ? AND user_id = ? AND state = 'ISSUED' AND issued_at = ?
  `).bind(
    eventId,
    challenge.requestId || null,
    JSON.stringify({ purpose: challenge.purpose, provider }),
    window.issuedAt,
    challenge.challengeId,
    challenge.userId,
    window.issuedAt
  );

  try {
    const results = await db.batch([supersede, issue, audit]);
    const issued = firstBatchRow(results?.[1]);
    if (!batchSucceeded(results) || !issued) return { ok: false, code: 'OTP_STATE_COMMIT_FAILED' };
    return { ok: true, challengeId: challenge.challengeId, ...window };
  } catch {
    return { ok: false, code: 'OTP_STATE_COMMIT_FAILED' };
  }
}

export async function markEmailOtpDeliveryFailed(db, challenge, options = {}) {
  if (!db || !challenge?.challengeId || !challenge?.userId) {
    return { ok: false, code: 'OTP_DELIVERY_CONTEXT_REQUIRED' };
  }
  const occurredAt = iso(Number(options.nowMs ?? Date.now()));
  const reasonCode = String(options.reasonCode || 'DELIVERY_FAILED').slice(0, 80);
  const fail = db.prepare(`
    UPDATE auth_email_otp_challenges
    SET state = 'DELIVERY_FAILED', delivery_failed_at = ?
    WHERE challenge_id = ? AND user_id = ? AND state = 'REQUESTED'
    RETURNING challenge_id
  `).bind(occurredAt, challenge.challengeId, challenge.userId);
  const audit = db.prepare(`
    INSERT INTO auth_audit_events(
      auth_event_id, event_type, user_id, outcome, reason_code,
      request_id, metadata_json, occurred_at
    )
    SELECT ?, 'EMAIL_OTP_DELIVERY_FAILED', user_id, 'FAILURE', ?, ?, ?, ?
    FROM auth_email_otp_challenges
    WHERE challenge_id = ? AND user_id = ? AND state = 'DELIVERY_FAILED' AND delivery_failed_at = ?
  `).bind(
    crypto.randomUUID(),
    reasonCode,
    challenge.requestId || null,
    JSON.stringify({ purpose: challenge.purpose }),
    occurredAt,
    challenge.challengeId,
    challenge.userId,
    occurredAt
  );
  try {
    const results = await db.batch([fail, audit]);
    const failed = firstBatchRow(results?.[0]);
    return batchSucceeded(results) && failed
      ? { ok: true }
      : { ok: false, code: 'OTP_FAILURE_STATE_COMMIT_FAILED' };
  } catch {
    return { ok: false, code: 'OTP_FAILURE_STATE_COMMIT_FAILED' };
  }
}

export async function issueEmailOtp(db, options = {}) {
  if (typeof options.deliver !== 'function') {
    return { ok: false, code: 'OTP_DELIVERY_NOT_CONFIGURED' };
  }
  const pending = await createEmailOtpRequest(db, options);
  if (!pending.ok) return pending;

  let delivery;
  try {
    delivery = await options.deliver({
      challengeId: pending.challengeId,
      code: pending.code,
      digits: EMAIL_OTP_DIGITS,
      purpose: pending.purpose,
      destinationHint: pending.destinationHint,
      validForMs: EMAIL_OTP_TTL_MS
    });
  } catch {
    delivery = { ok: false, reasonCode: 'DELIVERY_EXCEPTION' };
  }

  if (delivery?.ok !== true) {
    const failed = await markEmailOtpDeliveryFailed(db, pending, {
      nowMs: options.nowMs,
      reasonCode: delivery?.reasonCode || 'DELIVERY_REJECTED'
    });
    return failed.ok ? { ok: false, code: 'OTP_DELIVERY_FAILED' } : failed;
  }

  const committed = await markEmailOtpDelivered(db, pending, {
    nowMs: options.nowMs,
    provider: delivery.provider,
    deliveryRef: delivery.deliveryRef,
    pepper: options.pepper
  });
  if (!committed.ok) return committed;
  return {
    ok: true,
    challengeId: pending.challengeId,
    expiresAt: committed.expiresAt,
    resendNotBefore: committed.resendNotBefore,
    destinationHint: pending.destinationHint
  };
}

export async function consumeEmailOtp(db, options = {}) {
  const challengeId = String(options.challengeId || '');
  const userId = String(options.userId || '');
  const purpose = normalizePurpose(options.purpose);
  const codeHash = await hashEmailOtp(challengeId, options.code, options.pepper);
  const requestId = options.requestId ? String(options.requestId) : null;
  const occurredAt = iso(Number(options.nowMs ?? Date.now()));
  if (!db || !challengeId || !userId || !purpose || !codeHash) {
    return { ok: false, code: 'OTP_INVALID_OR_EXPIRED' };
  }

  const consume = db.prepare(`
    UPDATE auth_email_otp_challenges
    SET state = 'USED', used_at = ?
    WHERE challenge_id = ?
      AND user_id = ?
      AND purpose = ?
      AND state = 'ISSUED'
      AND expires_at > ?
      AND code_hash = ?
    RETURNING challenge_id, user_id, purpose, issued_at, expires_at
  `).bind(occurredAt, challengeId, userId, purpose, occurredAt, codeHash);

  const auditSuccess = db.prepare(`
    INSERT INTO auth_audit_events(
      auth_event_id, event_type, user_id, outcome, request_id, metadata_json, occurred_at
    )
    SELECT ?, 'EMAIL_OTP_USED', user_id, 'SUCCESS', ?, ?, ?
    FROM auth_email_otp_challenges
    WHERE challenge_id = ? AND user_id = ? AND state = 'USED' AND used_at = ?
  `).bind(
    crypto.randomUUID(),
    requestId,
    JSON.stringify({ purpose }),
    occurredAt,
    challengeId,
    userId,
    occurredAt
  );

  try {
    const results = await db.batch([consume, auditSuccess]);
    const row = firstBatchRow(results?.[0]);
    if (batchSucceeded(results) && row) {
      return { ok: true, challengeId: row.challenge_id, userId: row.user_id, purpose: row.purpose };
    }
  } catch {
    return { ok: false, code: 'OTP_CONSUME_FAILED' };
  }

  try {
    await db.prepare(`
      UPDATE auth_email_otp_challenges
      SET state = 'EXPIRED'
      WHERE challenge_id = ? AND user_id = ? AND purpose = ?
        AND state = 'ISSUED' AND expires_at <= ?
    `).bind(challengeId, userId, purpose, occurredAt).run();
  } catch {
    return { ok: false, code: 'OTP_CONSUME_FAILED' };
  }

  const audited = await writeAudit(db, {
    eventId: crypto.randomUUID(),
    eventType: 'EMAIL_OTP_REJECTED',
    userId,
    outcome: 'FAILURE',
    reasonCode: 'INVALID_OR_EXPIRED',
    requestId,
    occurredAt,
    metadata: { purpose }
  });
  return audited
    ? { ok: false, code: 'OTP_INVALID_OR_EXPIRED' }
    : { ok: false, code: 'OTP_AUDIT_FAILED' };
}
