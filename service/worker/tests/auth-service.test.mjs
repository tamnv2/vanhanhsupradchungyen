import test from 'node:test';
import assert from 'node:assert/strict';
import { createPasswordRecord } from '../src/auth.js';
import { issueSession, verifyPrimaryLogin } from '../src/auth-service.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}
if (!globalThis.btoa || !globalThis.atob) {
  globalThis.btoa = value => Buffer.from(value, 'binary').toString('base64');
  globalThis.atob = value => Buffer.from(value, 'base64').toString('binary');
}

function loginDb(row) {
  return {
    prepare() {
      return {
        bind() {
          return { first: async () => row };
        }
      };
    }
  };
}

test('normal account passes password verification', async () => {
  const record = await createPasswordRecord('Worker-Valid-2026');
  const result = await verifyPrimaryLogin(loginDb({
    user_id: 'U1',
    username: 'operator',
    employee_id: 'E1',
    display_name: 'Operator',
    user_status: 'ACTIVE',
    security_level: 'NORMAL',
    secret_hash: record.secretHash,
    hash_algorithm: record.hashAlgorithm,
    must_change: 0
  }), 'operator', 'Worker-Valid-2026');
  assert.equal(result.ok, true);
  assert.equal(result.user.userId, 'U1');
});

test('wrong password and inactive account return generic invalid credentials', async () => {
  const record = await createPasswordRecord('Worker-Valid-2026');
  const wrong = await verifyPrimaryLogin(loginDb({
    user_id: 'U1', username: 'operator', user_status: 'ACTIVE', security_level: 'NORMAL',
    secret_hash: record.secretHash, hash_algorithm: record.hashAlgorithm, must_change: 0
  }), 'operator', 'wrong-password');
  assert.equal(wrong.code, 'INVALID_CREDENTIALS');

  const inactive = await verifyPrimaryLogin(loginDb({
    user_id: 'U1', username: 'operator', user_status: 'DISABLED', security_level: 'NORMAL',
    secret_hash: record.secretHash, hash_algorithm: record.hashAlgorithm, must_change: 0
  }), 'operator', 'Worker-Valid-2026');
  assert.equal(inactive.code, 'INVALID_CREDENTIALS');
});

test('ROOT primary login bypasses permanent password and requires email OTP', async () => {
  const result = await verifyPrimaryLogin(loginDb({
    user_id: 'ROOT1',
    username: 'root',
    user_status: 'ACTIVE',
    security_level: 'ROOT',
    secret_hash: null,
    hash_algorithm: null,
    must_change: null
  }), 'root', 'ignored-root-password');
  assert.equal(result.ok, false);
  assert.equal(result.code, 'ROOT_EMAIL_OTP_REQUIRED');
  assert.equal(result.challenge.requiredMethod, 'EMAIL_OTP');
  assert.equal(result.challenge.userId, 'ROOT1');
});

test('session issuance stores only token hash and returns raw token once', async () => {
  let bound = null;
  const db = {
    prepare() {
      return {
        bind(...args) {
          bound = args;
          return { run: async () => ({ success: true }) };
        }
      };
    }
  };
  const session = await issueSession(db, { userId: 'U1', mustChangePassword: false }, {
    nowMs: Date.parse('2026-09-13T10:00:00Z')
  });
  assert.equal(typeof session.token, 'string');
  assert.ok(session.token.length >= 32);
  assert.equal(bound[1], 'U1');
  assert.notEqual(bound[3], session.token);
  assert.equal(bound[4], '2026-09-13T10:00:00.000Z');
});
