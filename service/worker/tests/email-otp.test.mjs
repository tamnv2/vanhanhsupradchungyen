import test from 'node:test';
import assert from 'node:assert/strict';
import {
  EMAIL_OTP_DIGITS,
  EMAIL_OTP_PURPOSES,
  EMAIL_OTP_RESEND_COOLDOWN_MS,
  EMAIL_OTP_TTL_MS,
  consumeEmailOtp,
  evaluateOtpCooldown,
  generateEmailOtp,
  hashEmailOtp,
  hashOtpDestination,
  issueEmailOtp,
  otpPolicyWindow
} from '../src/email-otp.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}

const PEPPER = 'vhdchy-test-pepper-must-be-at-least-32-bytes-long';

class FakeOtpDb {
  constructor() {
    this.records = new Map();
    this.audits = [];
  }

  prepare(sql) {
    const db = this;
    return {
      bind(...args) {
        return {
          sql,
          args,
          async first() {
            if (sql.includes('SELECT issued_at') && sql.includes('auth_email_otp_challenges')) {
              const [userId, purpose] = args;
              const rows = [...db.records.values()]
                .filter(row => row.user_id === userId && row.purpose === purpose && row.issued_at)
                .sort((a, b) => String(b.issued_at).localeCompare(String(a.issued_at)));
              return rows.length ? { issued_at: rows[0].issued_at } : null;
            }
            return null;
          },
          async run() {
            if (sql.includes("SET state = 'EXPIRED'")) {
              const [challengeId, userId, purpose, occurredAt] = args;
              const row = db.records.get(challengeId);
              if (row && row.user_id === userId && row.purpose === purpose && row.state === 'ISSUED' && row.expires_at <= occurredAt) {
                row.state = 'EXPIRED';
              }
              return { success: true };
            }
            if (sql.includes('INSERT INTO auth_audit_events')) {
              db.audits.push({ sql, args });
              return { success: true };
            }
            return { success: true };
          }
        };
      }
    };
  }

  async batch(statements) {
    const results = [];
    for (const statement of statements) {
      const { sql, args } = statement;
      if (sql.includes('INSERT INTO auth_email_otp_challenges')) {
        const [challengeId, userId, purpose, destinationHash, destinationHint, codeHash, requestedAt, requestId] = args;
        if ([...this.records.values()].some(row => row.user_id === userId && row.purpose === purpose && row.state === 'REQUESTED')) {
          throw new Error('unique requested');
        }
        this.records.set(challengeId, {
          challenge_id: challengeId,
          user_id: userId,
          purpose,
          destination_hash: destinationHash,
          destination_hint: destinationHint,
          code_hash: codeHash,
          state: 'REQUESTED',
          requested_at: requestedAt,
          request_id: requestId
        });
        results.push({ success: true, results: [] });
        continue;
      }

      if (sql.includes("SET state = 'SUPERSEDED'")) {
        const [at, userId, purpose, currentId] = args;
        for (const row of this.records.values()) {
          if (row.user_id === userId && row.purpose === purpose && row.state === 'ISSUED' && row.challenge_id !== currentId) {
            row.state = 'SUPERSEDED';
            row.superseded_at = at;
          }
        }
        results.push({ success: true, results: [] });
        continue;
      }

      if (sql.includes("SET state = 'ISSUED'") && sql.includes('RETURNING challenge_id')) {
        const [issuedAt, expiresAt, resendNotBefore, provider, deliveryRefHash, challengeId, userId, purpose] = args;
        const row = this.records.get(challengeId);
        if (row && row.user_id === userId && row.purpose === purpose && row.state === 'REQUESTED') {
          Object.assign(row, {
            state: 'ISSUED', issued_at: issuedAt, expires_at: expiresAt,
            resend_not_before: resendNotBefore, delivery_provider: provider,
            delivery_ref_hash: deliveryRefHash
          });
          results.push({ success: true, results: [{ challenge_id: challengeId }] });
        } else {
          results.push({ success: true, results: [] });
        }
        continue;
      }

      if (sql.includes("SET state = 'DELIVERY_FAILED'") && sql.includes('RETURNING challenge_id')) {
        const [at, challengeId, userId] = args;
        const row = this.records.get(challengeId);
        if (row && row.user_id === userId && row.state === 'REQUESTED') {
          row.state = 'DELIVERY_FAILED';
          row.delivery_failed_at = at;
          results.push({ success: true, results: [{ challenge_id: challengeId }] });
        } else {
          results.push({ success: true, results: [] });
        }
        continue;
      }

      if (sql.includes("SET state = 'USED'") && sql.includes('RETURNING challenge_id')) {
        const [usedAt, challengeId, userId, purpose, nowIso, codeHash] = args;
        const row = this.records.get(challengeId);
        if (row && row.user_id === userId && row.purpose === purpose && row.state === 'ISSUED' && row.expires_at > nowIso && row.code_hash === codeHash) {
          row.state = 'USED';
          row.used_at = usedAt;
          results.push({ success: true, results: [{ challenge_id: challengeId, user_id: userId, purpose }] });
        } else {
          results.push({ success: true, results: [] });
        }
        continue;
      }

      if (sql.includes('INSERT INTO auth_audit_events')) {
        this.audits.push({ sql, args });
        results.push({ success: true, results: [] });
        continue;
      }

      results.push({ success: true, results: [] });
    }
    return results;
  }
}

test('V6 email OTP policy is exactly four digits with five-minute validity and cooldown', () => {
  assert.equal(EMAIL_OTP_DIGITS, 4);
  assert.equal(EMAIL_OTP_TTL_MS, 300000);
  assert.equal(EMAIL_OTP_RESEND_COOLDOWN_MS, 300000);
  for (let i = 0; i < 20; i += 1) assert.match(generateEmailOtp(), /^\d{4}$/);

  const now = Date.parse('2026-09-13T10:00:00Z');
  const window = otpPolicyWindow(now);
  assert.equal(window.expiresAt, '2026-09-13T10:05:00.000Z');
  assert.equal(window.resendNotBefore, '2026-09-13T10:05:00.000Z');
  assert.equal(evaluateOtpCooldown('2026-09-13T10:00:00.000Z', now + 60000).allowed, false);
  assert.equal(evaluateOtpCooldown('2026-09-13T10:00:00.000Z', now + 300000).allowed, true);
});

test('OTP and destination hashes are deterministic, peppered, and challenge-bound', async () => {
  const a = await hashEmailOtp('C1', '0123', PEPPER);
  const b = await hashEmailOtp('C1', '0123', PEPPER);
  const c = await hashEmailOtp('C2', '0123', PEPPER);
  assert.equal(a, b);
  assert.notEqual(a, c);
  assert.equal(a.length, 64);

  const emailA = await hashOtpDestination('Root@Example.com', PEPPER);
  const emailB = await hashOtpDestination('root@example.com', PEPPER);
  assert.equal(emailA, emailB);
  assert.equal(emailA.length, 64);
});

test('OTP issuance fails closed before DB mutation when no delivery provider exists', async () => {
  const db = new FakeOtpDb();
  const result = await issueEmailOtp(db, {
    userId: 'U1',
    purpose: EMAIL_OTP_PURPOSES.PASSWORD_RECOVERY,
    destinationHash: 'destination-hash',
    pepper: PEPPER
  });
  assert.equal(result.code, 'OTP_DELIVERY_NOT_CONFIGURED');
  assert.equal(db.records.size, 0);
});

test('issued OTP is single-use, audited, and never persisted in readable form', async () => {
  const db = new FakeOtpDb();
  const now = Date.parse('2026-09-13T10:00:00Z');
  let deliveredCode = null;
  const issued = await issueEmailOtp(db, {
    userId: 'ROOT1',
    purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
    destinationHash: 'root-destination-hash',
    destinationHint: 'r***@example.com',
    pepper: PEPPER,
    nowMs: now,
    requestId: 'REQ1',
    deliver: async payload => {
      deliveredCode = payload.code;
      return { ok: true, provider: 'TEST', deliveryRef: 'provider-message-1' };
    }
  });
  assert.equal(issued.ok, true);
  assert.match(deliveredCode, /^\d{4}$/);
  const row = db.records.get(issued.challengeId);
  assert.equal(row.state, 'ISSUED');
  assert.notEqual(row.code_hash, deliveredCode);
  assert.equal(JSON.stringify([...db.records.values()]).includes(deliveredCode), false);
  assert.equal(JSON.stringify(db.audits).includes(deliveredCode), false);

  const first = await consumeEmailOtp(db, {
    challengeId: issued.challengeId,
    userId: 'ROOT1',
    purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
    code: deliveredCode,
    pepper: PEPPER,
    nowMs: now + 60000,
    requestId: 'REQ2'
  });
  assert.equal(first.ok, true);
  assert.equal(row.state, 'USED');

  const replay = await consumeEmailOtp(db, {
    challengeId: issued.challengeId,
    userId: 'ROOT1',
    purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
    code: deliveredCode,
    pepper: PEPPER,
    nowMs: now + 120000,
    requestId: 'REQ3'
  });
  assert.equal(replay.ok, false);
  assert.equal(replay.code, 'OTP_INVALID_OR_EXPIRED');
});

test('successful send enforces five-minute resend cooldown regardless of use state', async () => {
  const db = new FakeOtpDb();
  const now = Date.parse('2026-09-13T10:00:00Z');
  const options = {
    userId: 'U1',
    purpose: EMAIL_OTP_PURPOSES.PASSWORD_RECOVERY,
    destinationHash: 'destination-hash',
    pepper: PEPPER,
    nowMs: now,
    deliver: async () => ({ ok: true, provider: 'TEST' })
  };
  const first = await issueEmailOtp(db, options);
  assert.equal(first.ok, true);
  const second = await issueEmailOtp(db, { ...options, nowMs: now + 60000 });
  assert.equal(second.ok, false);
  assert.equal(second.code, 'OTP_RESEND_COOLDOWN');
  assert.equal(second.retryAfterMs, 240000);
});
