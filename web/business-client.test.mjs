import assert from 'node:assert/strict';
import { createBusinessClient, BUSINESS_COMMAND_PATH } from './business.js';

const proof = {
  'X-VHDCHY-Device-Id': 'D1',
  'X-VHDCHY-Security-Epoch': '3',
  'X-VHDCHY-Timestamp-Ms': '1000',
  'X-VHDCHY-Nonce': 'N1',
  'X-VHDCHY-Signature': 'S1'
};

let cloudCall = null;
const cloudAuth = {
  runtime: 'CLOUD',
  async request(path, options) {
    cloudCall = { path, options };
    return { ok: true, result: { commitStatus: 'CLOUD_COMMITTED', resultingVersion: 2 } };
  }
};
const cloud = createBusinessClient(cloudAuth, { randomUUID: (() => { let i = 0; return () => `CLOUD-${++i}`; })() });
const cloudResult = await cloud.submitCommand({
  commandCode: 'EMPLOYEE_UPDATE',
  entityId: 'EMP-1',
  expectedEntityVersion: 1,
  payload: { employeeId: 'EMP-1', fullName: 'Nguyễn Văn A' }
});
assert.equal(cloudCall.path, BUSINESS_COMMAND_PATH);
assert.equal(cloudCall.options.method, 'POST');
assert.equal(cloudCall.options.headers.get('X-VHDCHY-Signature'), null);
assert.equal(JSON.parse(cloudCall.options.body).idempotencyKey, 'CLOUD-2');
assert.equal(cloudResult.runtime, 'CLOUD');
assert.equal(cloudResult.result.commitStatus, 'CLOUD_COMMITTED');

let signedInput = null;
let lanCall = null;
const lanAuth = {
  runtime: 'LAN',
  async request(path, options) {
    lanCall = { path, options };
    return { ok: true, runtime: 'LAN', result: { commitStatus: 'LAN_ACCEPTED_PENDING_SYNC', resultingVersion: 4 } };
  }
};
const lan = createBusinessClient(lanAuth, {
  randomUUID: () => 'LAN-ID',
  getLanSigner: () => ({
    async signRequest(input) {
      signedInput = input;
      return proof;
    }
  })
});
await lan.submitCommand({
  commandCode: 'ATTENDANCE_IN',
  entityId: 'PRESENCE:EMP-1:2026-09-15',
  payload: { employeeId: 'EMP-1', businessDate: '2026-09-15', occurredAt: '2026-09-15T01:00:00+07:00', source: 'WEB' },
  deviceSeq: 7
});
assert.equal(signedInput.method, 'POST');
assert.equal(signedInput.target, BUSINESS_COMMAND_PATH);
assert.equal(signedInput.body, lanCall.options.body);
for (const [name, value] of Object.entries(proof)) assert.equal(lanCall.options.headers.get(name), value);
assert.equal(JSON.parse(lanCall.options.body).deviceSeq, 7);

let unsignedNetworkCalls = 0;
const unsigned = createBusinessClient({
  runtime: 'LAN',
  async request() { unsignedNetworkCalls += 1; return {}; }
}, { getLanSigner: () => null, randomUUID: () => 'U1' });
await assert.rejects(() => unsigned.submitCommand({
  commandCode: 'ATTENDANCE_OUT',
  entityId: 'PRESENCE:EMP-1:2026-09-15',
  payload: { employeeId: 'EMP-1', businessDate: '2026-09-15', occurredAt: '2026-09-15T08:00:00+07:00', source: 'WEB' }
}), error => error?.code === 'LAN_SIGNER_REQUIRED');
assert.equal(unsignedNetworkCalls, 0);

await assert.rejects(() => cloud.submitCommand({
  commandCode: 'EMPLOYEE_PORTRAIT_REPLACE',
  entityId: 'EMP-1',
  payload: { employeeId: 'EMP-1' }
}), error => error?.code === 'EMPLOYEE_PORTRAIT_REPLACE_OWNER_DECISION_REQUIRED');

await assert.rejects(() => cloud.submitCommand({
  commandCode: 'UNKNOWN_COMMAND',
  entityId: 'EMP-1',
  payload: {}
}), error => error?.code === 'COMMAND_NOT_SUPPORTED');

console.log('WEB_SLICE1_CLIENT_PASS cloudEnvelope=PASS lanSignedExactBody=PASS lanUnsignedNoNetwork=PASS portraitGate=PASS unsupportedFailClosed=PASS');
