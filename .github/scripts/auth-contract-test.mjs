import {
  createPasswordRecord,
  generateBearerToken,
  hashBearerToken,
  validatePasswordPolicy,
  verifyPasswordRecord,
  verifyTotpCode
} from '../../service/worker/src/auth.js';

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

assert(validatePasswordPolicy('short').ok === false, 'short password must fail');
assert(validatePasswordPolicy('password').code === 'PASSWORD_TOO_COMMON', 'common password must fail');
assert(validatePasswordPolicy('warehouse long passphrase 2026').ok === true, 'long passphrase should pass');
assert(validatePasswordPolicy('operator01', 'operator01').code === 'PASSWORD_EQUALS_USERNAME', 'password=username must fail');

const record = await createPasswordRecord('warehouse long passphrase 2026', {
  username: 'operator01',
  iterations: 100000
});
assert(record.hashAlgorithm.startsWith('PBKDF2-SHA256$100000$'), 'PBKDF2 metadata mismatch');
assert(await verifyPasswordRecord('warehouse long passphrase 2026', record.hashAlgorithm, record.secretHash), 'valid password must verify');
assert(!(await verifyPasswordRecord('wrong password', record.hashAlgorithm, record.secretHash)), 'wrong password must fail');

const tokenA = generateBearerToken();
const tokenB = generateBearerToken();
assert(tokenA !== tokenB, 'tokens must differ');
const hashA1 = await hashBearerToken(tokenA);
const hashA2 = await hashBearerToken(tokenA);
assert(hashA1 && hashA1 === hashA2, 'token hash must be deterministic');
assert(hashA1 !== await hashBearerToken(tokenB), 'different tokens must hash differently');

// RFC 6238 SHA-1 test vector: secret "12345678901234567890", T=59, 8 digits => 94287082.
const rfcSecret = 'GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ';
assert(
  await verifyTotpCode(rfcSecret, '94287082', { digits: 8, period: 30, window: 0, nowMs: 59000 }),
  'RFC 6238 TOTP vector must pass'
);
assert(
  !(await verifyTotpCode(rfcSecret, '94287081', { digits: 8, period: 30, window: 0, nowMs: 59000 })),
  'invalid TOTP must fail'
);

console.log('AUTH_CONTRACT_TEST_PASS');
