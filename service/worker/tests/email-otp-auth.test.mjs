import test from 'node:test';
import assert from 'node:assert/strict';
import {
  EMAIL_OTP_REQUEST_PATH,
  EMAIL_OTP_USE_PATH,
  createRuntimeEmailOtpDelivery,
  requestEmailOtpLogin,
  useEmailOtpLogin
} from '../src/email-otp-auth.js';
import { EMAIL_OTP_PURPOSES } from '../src/email-otp.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}

const PEPPER = 'vhdchy-email-otp-auth-test-pepper-at-least-32-bytes';
const NOW = Date.parse('2026-09-15T05:00:00Z');

class FakeAuthDb {
  constructor(options = {}) {
    this.account = options.account || null;
    this.allowlist = options.allowlist || null;
    this.challenge = options.challenge || null;
    this.totpActive = options.totpActive === true;
    this.calls = [];
  }

  prepare(sql) {
    const db = this;
    return {
      bind(...args) {
        db.calls.push({ sql, args });
        return {
          async first() {
            if (sql.includes('FROM root_recovery_allowlist')) return db.allowlist;
            if (sql.includes('FROM auth_email_otp_challenges')) return db.challenge;
            if (sql.includes('FROM auth_mfa_methods')) {
              return db.totpActive ? { mfa_method_id: 'TOTP1' } : null;
            }
            if (sql.includes('FROM auth_users')) return db.account;
            return null;
          }
        };
      }
    };
  }
}

function issuedResult(destinationHint = 'u***@example.com') {
  return {
    ok: true,
    challengeId: 'C1',
    expiresAt: '2026-09-15T05:05:00.000Z',
    resendNotBefore: '2026-09-15T05:05:00.000Z',
    destinationHint
  };
}

function sessionResult(mustChangePassword) {
  return {
    sessionId: 'S1',
    token: 'x'.repeat(48),
    issuedAt: '2026-09-15T05:00:00.000Z',
    expiresAt: '2026-09-15T13:00:00.000Z',
    mustChangePassword
  };
}

test('V6 OTP route constants are stable', () => {
  assert.equal(EMAIL_OTP_REQUEST_PATH, '/api/v1/auth/email-otp/request');
  assert.equal(EMAIL_OTP_USE_PATH, '/api/v1/auth/email-otp/use');
});

test('normal account request uses registered email, hashes destination and exposes only a hint', async () => {
  const db = new FakeAuthDb({
    account: {
      user_id: 'U1', username: 'operator', email: 'Operator@Example.com',
      user_status: 'ACTIVE', security_level: 'NORMAL'
    }
  });
  let issueOptions = null;
  let deliveryPayload = null;

  const result = await requestEmailOtpLogin(db, 'operator', {
    pepper: PEPPER,
    nowMs: NOW,
    requestId: 'REQ1',
    issueOtp: async (_db, options) => {
      issueOptions = options;
      const delivered = await options.deliver({
        challengeId: 'C1',
        code: '0123',
        digits: 4,
        purpose: options.purpose,
        validForMs: 300000
      });
      assert.equal(delivered.ok, true);
      return issuedResult(options.destinationHint);
    },
    deliver: async payload => {
      deliveryPayload = payload;
      return { ok: true, provider: 'TEST' };
    }
  });

  assert.equal(result.ok, true);
  assert.equal(result.purpose, EMAIL_OTP_PURPOSES.PASSWORD_RECOVERY);
  assert.equal(result.destinationHint, 'o***@example.com');
  assert.equal(issueOptions.userId, 'U1');
  assert.equal(issueOptions.destinationHash.length, 64);
  assert.notEqual(issueOptions.destinationHash, 'operator@example.com');
  assert.equal(deliveryPayload.destination, 'operator@example.com');
  assert.equal(deliveryPayload.subject, 'VHDCHY OTP 0123');
  assert.equal(JSON.stringify(result).includes('operator@example.com'), false);
  assert.equal(JSON.stringify(result).includes('0123'), false);
});

test('ROOT request requires runtime destination to match active recovery allowlist before issuance', async () => {
  const db = new FakeAuthDb({
    account: {
      user_id: 'ROOT1', username: 'root', email: null,
      user_status: 'ACTIVE', security_level: 'ROOT'
    },
    allowlist: null
  });
  let issued = false;

  const result = await requestEmailOtpLogin(db, 'root', {
    pepper: PEPPER,
    rootRecoveryEmail: 'root@example.com',
    issueOtp: async () => {
      issued = true;
      return issuedResult();
    },
    deliver: async () => ({ ok: true })
  });

  assert.equal(result.ok, false);
  assert.equal(result.code, 'OTP_RECOVERY_NOT_AVAILABLE');
  assert.equal(issued, false);
  const allowlistCall = db.calls.find(call => call.sql.includes('FROM root_recovery_allowlist'));
  assert.ok(allowlistCall);
  assert.equal(String(allowlistCall.args[0]).length, 64);
  assert.notEqual(allowlistCall.args[0], 'root@example.com');
});

test('ROOT request uses only allowlisted destination metadata and does not return raw destination', async () => {
  const db = new FakeAuthDb({
    account: {
      user_id: 'ROOT1', username: 'root', email: null,
      user_status: 'ACTIVE', security_level: 'ROOT'
    },
    allowlist: { destination_hash: 'hash', destination_hint: 'r***@example.com' }
  });
  let deliveredTo = null;
  const result = await requestEmailOtpLogin(db, 'root', {
    pepper: PEPPER,
    rootRecoveryEmail: 'root@example.com',
    issueOtp: async (_db, options) => {
      await options.deliver({
        challengeId: 'ROOT-C1', code: '9876', purpose: options.purpose, validForMs: 300000
      });
      return { ...issuedResult(options.destinationHint), challengeId: 'ROOT-C1' };
    },
    deliver: async payload => {
      deliveredTo = payload.destination;
      return { ok: true };
    }
  });

  assert.equal(result.ok, true);
  assert.equal(result.purpose, EMAIL_OTP_PURPOSES.ROOT_LOGIN);
  assert.equal(result.destinationHint, 'r***@example.com');
  assert.equal(deliveredTo, 'root@example.com');
  assert.equal(JSON.stringify(result).includes('root@example.com'), false);
  assert.equal(JSON.stringify(result).includes('9876'), false);
});

test('normal recovery OTP use issues a restricted MUST_CHANGE_PASSWORD session', async () => {
  const db = new FakeAuthDb({
    challenge: {
      challenge_id: 'C1', user_id: 'U1', purpose: EMAIL_OTP_PURPOSES.PASSWORD_RECOVERY,
      state: 'ISSUED', expires_at: '2026-09-15T05:05:00.000Z', username: 'operator',
      employee_id: 'E1', display_name: 'Operator', user_status: 'ACTIVE', security_level: 'NORMAL'
    }
  });
  let consumed = null;
  let sessionUser = null;
  let sessionOptions = null;

  const result = await useEmailOtpLogin(db, {
    username: 'operator', challengeId: 'C1', code: '0123'
  }, {
    pepper: PEPPER,
    nowMs: NOW,
    consumeOtp: async (_db, options) => {
      consumed = options;
      return { ok: true };
    },
    issueSessionFn: async (_db, user, options) => {
      sessionUser = user;
      sessionOptions = options;
      return sessionResult(true);
    }
  });

  assert.equal(result.ok, true);
  assert.equal(result.authMethodCode, 'EMAIL_OTP_RECOVERY');
  assert.equal(result.user.mustChangePassword, true);
  assert.equal(result.session.mustChangePassword, true);
  assert.equal(consumed.purpose, EMAIL_OTP_PURPOSES.PASSWORD_RECOVERY);
  assert.equal(sessionUser.mustChangePassword, true);
  assert.equal(sessionOptions.authMethodCode, 'EMAIL_OTP_RECOVERY');
});

test('ROOT OTP without active TOTP issues a session without password-change requirement', async () => {
  const db = new FakeAuthDb({
    challenge: {
      challenge_id: 'C1', user_id: 'ROOT1', purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
      state: 'ISSUED', expires_at: '2026-09-15T05:05:00.000Z', username: 'root',
      employee_id: null, display_name: 'Root', user_status: 'ACTIVE', security_level: 'ROOT'
    },
    totpActive: false
  });

  const result = await useEmailOtpLogin(db, { username: 'root', challengeId: 'C1', code: '9876' }, {
    pepper: PEPPER,
    nowMs: NOW,
    consumeOtp: async () => ({ ok: true }),
    issueSessionFn: async (_db, user, options) => {
      assert.equal(user.mustChangePassword, false);
      assert.equal(options.authMethodCode, 'EMAIL_OTP');
      return sessionResult(false);
    }
  });

  assert.equal(result.ok, true);
  assert.equal(result.authMethodCode, 'EMAIL_OTP');
  assert.equal(result.session.mustChangePassword, false);
});

test('ROOT with active verified TOTP fails closed before consuming email OTP', async () => {
  const db = new FakeAuthDb({
    challenge: {
      challenge_id: 'C1', user_id: 'ROOT1', purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
      state: 'ISSUED', expires_at: '2026-09-15T05:05:00.000Z', username: 'root',
      employee_id: null, display_name: 'Root', user_status: 'ACTIVE', security_level: 'ROOT'
    },
    totpActive: true
  });
  let consumeCount = 0;
  let sessionCount = 0;

  const result = await useEmailOtpLogin(db, { username: 'root', challengeId: 'C1', code: '9876' }, {
    pepper: PEPPER,
    nowMs: NOW,
    consumeOtp: async () => {
      consumeCount += 1;
      return { ok: true };
    },
    issueSessionFn: async () => {
      sessionCount += 1;
      return sessionResult(false);
    }
  });

  assert.equal(result.ok, false);
  assert.equal(result.code, 'ROOT_TOTP_REQUIRED');
  assert.equal(result.challenge.requiredMethod, 'TOTP');
  assert.equal(consumeCount, 0);
  assert.equal(sessionCount, 0);
});

test('trusted server-side TOTP satisfaction composes with email OTP and records combined auth method', async () => {
  const db = new FakeAuthDb({
    challenge: {
      challenge_id: 'C1', user_id: 'ROOT1', purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
      state: 'ISSUED', expires_at: '2026-09-15T05:05:00.000Z', username: 'root',
      employee_id: null, display_name: 'Root', user_status: 'ACTIVE', security_level: 'ROOT'
    },
    totpActive: true
  });

  const result = await useEmailOtpLogin(db, { username: 'root', challengeId: 'C1', code: '9876' }, {
    pepper: PEPPER,
    nowMs: NOW,
    totpSatisfied: true,
    consumeOtp: async () => ({ ok: true }),
    issueSessionFn: async (_db, user, options) => {
      assert.equal(user.mustChangePassword, false);
      assert.equal(options.authMethodCode, 'EMAIL_OTP_TOTP');
      return sessionResult(false);
    }
  });

  assert.equal(result.ok, true);
  assert.equal(result.authMethodCode, 'EMAIL_OTP_TOTP');
});

test('runtime delivery adapter is absent without binding and forwards no readable OTP field outside subject', async () => {
  assert.equal(createRuntimeEmailOtpDelivery({}), null);

  let requestBody = null;
  const delivery = createRuntimeEmailOtpDelivery({
    EMAIL_OTP_DELIVERY: {
      async fetch(_url, init) {
        requestBody = JSON.parse(init.body);
        return Response.json({ ok: true, provider: 'TEST', deliveryRef: 'M1' });
      }
    }
  });

  const result = await delivery({
    destination: 'root@example.com',
    subject: 'VHDCHY OTP 1234',
    purpose: EMAIL_OTP_PURPOSES.ROOT_LOGIN,
    challengeId: 'C1',
    validForMs: 300000,
    code: '1234'
  });

  assert.equal(result.ok, true);
  assert.equal(requestBody.destination, 'root@example.com');
  assert.equal(requestBody.subject, 'VHDCHY OTP 1234');
  assert.equal(Object.hasOwn(requestBody, 'code'), false);
});
