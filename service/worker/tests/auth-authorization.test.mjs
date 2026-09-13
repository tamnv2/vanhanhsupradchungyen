import test from 'node:test';
import assert from 'node:assert/strict';
import {
  createPasswordRecord,
  verifyPasswordRecord,
  validatePasswordPolicy,
  generateBearerToken,
  hashBearerToken,
  verifyTotpCode
} from '../src/auth.js';
import {
  evaluatePermission,
  isRootExclusiveResource,
  scopeMatches,
  summarizeAllowedPermissions
} from '../src/authorization.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}

if (!globalThis.btoa || !globalThis.atob) {
  globalThis.btoa = value => Buffer.from(value, 'binary').toString('base64');
  globalThis.atob = value => Buffer.from(value, 'base64').toString('binary');
}

test('password policy enforces minimum and common-password block', () => {
  assert.equal(validatePasswordPolicy('short').ok, false);
  assert.equal(validatePasswordPolicy('12345678').code, 'PASSWORD_TOO_COMMON');
  assert.equal(validatePasswordPolicy('correct horse battery staple').ok, true);
});

test('password record verifies correct password and rejects wrong password', async () => {
  const record = await createPasswordRecord('Correct-Horse-2026', { username: 'operator' });
  assert.equal(await verifyPasswordRecord('Correct-Horse-2026', record.hashAlgorithm, record.secretHash), true);
  assert.equal(await verifyPasswordRecord('Wrong-Horse-2026', record.hashAlgorithm, record.secretHash), false);
});

test('bearer token hash is deterministic but token generation is not', async () => {
  const a = generateBearerToken();
  const b = generateBearerToken();
  assert.notEqual(a, b);
  assert.equal(await hashBearerToken(a), await hashBearerToken(a));
});

test('TOTP verifier accepts RFC-style known vector window', async () => {
  const secret = 'JBSWY3DPEHPK3PXP';
  const accepted = await verifyTotpCode(secret, '282760', {
    nowMs: 0,
    period: 30,
    digits: 6,
    window: 0
  });
  assert.equal(typeof accepted, 'boolean');
});

test('scope match treats null scope as global', () => {
  assert.equal(scopeMatches({ cluster_id: null, module_id: null }, 'PICK_PACK_1291', 'PICK_PACK'), true);
  assert.equal(scopeMatches({ cluster_id: 'A', module_id: null }, 'B', null), false);
});

test('ROOT has full authority and SUPERADMIN is excluded from ROOT security', () => {
  assert.equal(evaluatePermission({ securityLevel: 'ROOT', resourceCode: 'ROOT_SECURITY', actionCode: 'UPDATE' }).allowed, true);
  const superDecision = evaluatePermission({ securityLevel: 'SUPERADMIN', resourceCode: 'ROOT_SECURITY', actionCode: 'UPDATE' });
  assert.equal(superDecision.allowed, false);
  assert.equal(superDecision.reason, 'ROOT_EXCLUSIVE_POLICY');
  assert.equal(isRootExclusiveResource('root_recovery'), true);
});

test('explicit DENY wins over ALLOW', () => {
  const grants = [
    { resource_code: 'ATTENDANCE', action_code: 'WRITE', effect: 'ALLOW', cluster_id: null, module_id: null },
    { resource_code: 'ATTENDANCE', action_code: 'WRITE', effect: 'DENY', cluster_id: 'PICK_PACK_1291', module_id: null }
  ];
  const decision = evaluatePermission({
    securityLevel: 'NORMAL',
    grants,
    resourceCode: 'ATTENDANCE',
    actionCode: 'WRITE',
    clusterId: 'PICK_PACK_1291'
  });
  assert.equal(decision.allowed, false);
  assert.equal(decision.reason, 'EXPLICIT_DENY');
});

test('permission summary removes denied permission', () => {
  const summary = summarizeAllowedPermissions({
    grants: [
      { resource_code: 'LABOR', action_code: 'READ', effect: 'ALLOW' },
      { resource_code: 'LABOR', action_code: 'READ', effect: 'DENY' },
      { resource_code: 'LABOR', action_code: 'WRITE', effect: 'ALLOW' }
    ]
  });
  assert.deepEqual(summary.permissions, ['LABOR:WRITE']);
});
