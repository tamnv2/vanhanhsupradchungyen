import { readFile } from 'node:fs/promises';
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

// Google projection management plane stays owner-only and fail-closed.
const gatewayManifest = JSON.parse(await readFile('service/google-gateway/appsscript.json', 'utf8'));
assert(gatewayManifest?.webapp?.access === 'ANYONE_ANONYMOUS', 'Google Gateway data-plane Web App access drifted');
assert(gatewayManifest?.webapp?.executeAs === 'USER_DEPLOYING', 'Google Gateway Web App execution identity drifted');
assert(gatewayManifest?.executionApi?.access === 'MYSELF', 'Google Gateway management API must be MYSELF only');

const gatewayCode = await readFile('service/google-gateway/Code.gs', 'utf8');
new Function(gatewayCode);
assert(gatewayCode.includes('function provisionProjectionAuth('), 'projection auth provisioning function missing');
assert(gatewayCode.includes('function projectionManagementHealth('), 'projection management readback function missing');
assert(gatewayCode.includes('VHDCHY_PROJECTION_SHARED_TOKEN_SHA256: verifierSha256'), 'GAS must store only projection verifier');
assert(gatewayCode.includes('VHDCHY_PROJECTION_ENABLED: "false"'), 'provisioning must force projection disabled');
assert(!gatewayCode.includes('function setProjectionEnabled('), 'projection activation must remain a separate not-yet-implemented gate');

const gasSync = await readFile('.github/scripts/gas-sync.mjs', 'utf8');
assert(gasSync.includes("'provision_projection'"), 'CI projection provisioning operation missing');
assert(gasSync.includes("'provisionProjectionAuth'"), 'CI must call the verifier-only GAS function');
assert(gasSync.includes("'projectionManagementHealth'"), 'CI must perform management readback');
assert(gasSync.includes('PROJECTION_AUTH_PROVISION_PASS authConfigured=true enabled=false'), 'CI fail-closed provisioning evidence marker missing');

const managementProbe = await readFile('.github/scripts/gas-management-probe.mjs', 'utf8');
assert(managementProbe.includes("function: 'projectionManagementHealth'"), 'management E2E probe must call projectionManagementHealth');
assert(managementProbe.includes("parameters: ['BETA']"), 'management E2E probe must bind BETA environment');
assert(managementProbe.includes('result?.projection?.enabled === false'), 'management E2E probe must require projection disabled');
assert(managementProbe.includes('PROJECTION_MANAGEMENT_E2E_PASS'), 'management E2E evidence marker missing');

const gasWorkflow = await readFile('.github/workflows/gas-beta-sync.yml', 'utf8');
assert(gasWorkflow.includes('VHDCHY_PROJECTION_SHARED_TOKEN: ${{ secrets.VHDCHY_PROJECTION_SHARED_TOKEN }}'), 'projection raw token must come from GitHub secret store');
assert(gasWorkflow.includes("service/google-gateway/**"), 'Google Gateway source changes must trigger sync/deploy');
assert(gasWorkflow.includes('node .github/scripts/gas-management-probe.mjs'), 'Google sync workflow must run the owner-only management E2E probe');

console.log('AUTH_CONTRACT_TEST_PASS projectionManagement=PASS');
