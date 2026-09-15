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

const rfcSecret = 'GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ';
assert(
  await verifyTotpCode(rfcSecret, '94287082', { digits: 8, period: 30, window: 0, nowMs: 59000 }),
  'RFC 6238 TOTP vector must pass'
);
assert(
  !(await verifyTotpCode(rfcSecret, '94287081', { digits: 8, period: 30, window: 0, nowMs: 59000 })),
  'invalid TOTP must fail'
);

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
assert(gatewayCode.includes('function setProjectionEnabled(enabled, expectedEnvironment)'), 'reviewed projection activation function missing');
assert(gatewayCode.includes('projectionManagementContext_(expectedEnvironment)'), 'projection activation must use owner/environment management context');
assert(gatewayCode.includes('if (enabled && !before.authConfigured) throw new Error("PROJECTION_AUTH_NOT_CONFIGURED")'), 'projection activation must require configured auth');
assert(gatewayCode.includes('PROJECTION_ENABLE_READBACK_FAILED'), 'projection activation must verify readback');
assert(gatewayCode.includes('function projectionE2EInspect(marker, expectedEnvironment)'), 'owner-only E2E readback helper missing');
assert(gatewayCode.includes('function projectionE2ECleanup(marker, expectedEnvironment)'), 'owner-only E2E cleanup helper missing');
assert(gatewayCode.includes('projectionManagementContext_(expectedEnvironment);'), 'E2E helpers must be bound to owner/environment management context');
assert(gatewayCode.includes('PROJECTION_E2E_MARKER_INVALID'), 'E2E helper must restrict marker scope');
assert(gatewayCode.includes('sheet.deleteRow(rowNumber)'), 'E2E cleanup must delete exact marker rows');

const gasSync = await readFile('.github/scripts/gas-sync.mjs', 'utf8');
assert(gasSync.includes("'provision_projection'"), 'CI projection provisioning operation missing');
assert(gasSync.includes("'provisionProjectionAuth'"), 'CI must call the verifier-only GAS function');
assert(gasSync.includes("'projectionManagementHealth'"), 'CI must perform management readback');
assert(gasSync.includes('PROJECTION_AUTH_PROVISION_PASS authConfigured=true enabled=false'), 'CI fail-closed provisioning evidence marker missing');

const managementProbe = await readFile('.github/scripts/gas-management-probe.mjs', 'utf8');
assert(managementProbe.includes("function: 'projectionManagementHealth'"), 'management E2E probe must call projectionManagementHealth');
assert(managementProbe.includes("parameters: ['BETA']"), 'management E2E probe must bind BETA environment');
assert(managementProbe.includes('activationRequested'), 'management probe must recognize explicit activation recovery mode');
assert(managementProbe.includes("mode=${mode}"), 'management probe must expose bounded state mode evidence');
assert(managementProbe.includes('PROJECTION_MANAGEMENT_E2E_PASS'), 'management E2E evidence marker missing');

const activationProbe = await readFile('.github/scripts/gas-projection-activation.mjs', 'utf8');
assert(activationProbe.includes("'setProjectionEnabled'"), 'activation CI must invoke owner-only activation function');
assert(activationProbe.includes("[true, 'BETA']"), 'activation CI must bind exact BETA environment');
assert(activationProbe.includes("'projectionManagementHealth'"), 'activation CI must read back owner-only management state');
assert(activationProbe.includes('payload?.projection?.enabled !== true'), 'activation CI must require public enabled-state readback');
assert(activationProbe.includes('Do not issue redundant'), 'activation probe must avoid redundant public POST after pre-activation auth proof');
assert(activationProbe.includes('PROJECTION_ACTIVATION_E2E_PASS'), 'activation evidence marker missing');

const gasWorkflow = await readFile('.github/workflows/gas-beta-sync.yml', 'utf8');
assert(gasWorkflow.includes('VHDCHY_PROJECTION_SHARED_TOKEN: ${{ secrets.VHDCHY_PROJECTION_SHARED_TOKEN }}'), 'projection raw token must come from GitHub secret store');
assert(gasWorkflow.includes("service/google-gateway/**"), 'Google Gateway source changes must trigger sync/deploy');
assert(gasWorkflow.includes('node .github/scripts/gas-management-probe.mjs'), 'Google sync workflow must run the owner-only management E2E probe');
assert(gasWorkflow.includes('node .github/scripts/gas-projection-activation.mjs'), 'Google sync workflow must run the separate activation gate');
assert(gasWorkflow.includes("cmd.activate_projection === true && (cmd.operation !== 'deploy' || cmd.deploy !== true)"), 'activation dispatch must be restricted to reviewed deploy operation');

const activationDispatch = JSON.parse(await readFile('.github/dispatch/gas-beta-sync.json', 'utf8'));
assert(activationDispatch.target === 'beta', 'activation dispatch must target beta');
assert(activationDispatch.operation === 'deploy', 'activation dispatch must use deploy operation');
assert(activationDispatch.deploy === true, 'activation dispatch must require deployment');
assert(activationDispatch.activate_projection === true, 'activation dispatch flag must be explicit');

const liveE2E = await readFile('.github/scripts/projection-live-e2e.mjs', 'utf8');
assert(liveE2E.includes("'GAS_SCRIPT_ID'"), 'projection live E2E must use the Apps Script management plane');
assert(liveE2E.includes("'projectionE2EInspect'"), 'projection live E2E must use owner-only Sheet readback');
assert(liveE2E.includes("'projectionE2ECleanup'"), 'projection live E2E must use owner-only Sheet cleanup');
assert(!liveE2E.includes('sheets.googleapis.com'), 'projection live E2E must not depend on Google Sheets REST API');
assert(liveE2E.includes('PROJECTION_E2E_CLEANUP_PASS'), 'projection live E2E must prove cleanup');

const liveE2EWorkflow = await readFile('.github/workflows/projection-live-e2e.yml', 'utf8');
assert(liveE2EWorkflow.includes('GAS_SCRIPT_ID: ${{ vars.GAS_SCRIPT_ID }}'), 'projection live E2E workflow must receive GAS_SCRIPT_ID');
assert(!liveE2EWorkflow.includes('GOOGLE_SHEETS_PROJECTION_ID:'), 'projection live E2E workflow must not require Sheets REST configuration');

console.log('AUTH_CONTRACT_TEST_PASS projectionManagement=PASS projectionActivation=PASS recovery=PASS projectionLiveE2E=PASS');
