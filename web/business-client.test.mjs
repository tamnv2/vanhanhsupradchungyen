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
    return {
      ok: true,
      requestId: 'CLOUD-1',
      idempotencyKey: 'CLOUD-2',
      commitStatus: 'CLOUD_COMMITTED',
      googleOutputStatus: 'PENDING',
      eventId: 'EV-CLOUD-1',
      entity: { type: 'EMPLOYEE', id: 'EMP-1', version: 2 }
    };
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
assert.equal(cloudResult.commitStatus, 'CLOUD_COMMITTED');
assert.equal(cloudResult.googleOutputStatus, 'PENDING');
assert.equal(cloudResult.entity.version, 2);

let signedInput = null;
let lanCall = null;
const lanAuth = {
  runtime: 'LAN',
  async request(path, options) {
    lanCall = { path, options };
    return {
      ok: true,
      runtime: 'LAN',
      requestId: 'LAN-ID',
      result: {
        eventId: 'EDGE-1', requestId: 'LAN-ID', idempotencyKey: 'LAN-ID',
        entityType: 'ATTENDANCE_PRESENCE', entityId: 'PRESENCE:EMP-1:2026-09-15', resultingVersion: 4,
        authoritySnapshotVersion: 'AUTH-4', commitStatus: 'LAN_ACCEPTED_PENDING_SYNC', googleOutputStatus: 'PENDING',
        alreadyAccepted: false
      }
    };
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
const lanResult = await lan.submitCommand({
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
assert.equal(lanResult.commitStatus, 'LAN_ACCEPTED_PENDING_SYNC');
assert.equal(lanResult.googleOutputStatus, 'PENDING');
assert.deepEqual(lanResult.entity, { type: 'ATTENDANCE_PRESENCE', id: 'PRESENCE:EMP-1:2026-09-15', version: 4 });
assert.equal(lanResult.lan.authoritySnapshotVersion, 'AUTH-4');

let unsignedNetworkCalls = 0;
const unsigned = createBusinessClient({
  runtime: 'LAN',
  async request() { unsignedNetworkCalls += 1; return {}; }
}, { getLanSigner: () => null, randomUUID: () => 'U1' });
await assert.rejects(() => unsigned.submitCommand({
  commandCode: 'ATTENDANCE_OUT', entityId: 'PRESENCE:EMP-1:2026-09-15',
  payload: { employeeId: 'EMP-1', businessDate: '2026-09-15', occurredAt: '2026-09-15T08:00:00+07:00', source: 'WEB' }
}), error => error?.code === 'LAN_SIGNER_REQUIRED');
assert.equal(unsignedNetworkCalls, 0);

await assert.rejects(() => cloud.submitCommand({
  commandCode: 'EMPLOYEE_PORTRAIT_REPLACE', entityId: 'EMP-1', payload: { employeeId: 'EMP-1' }
}), error => error?.code === 'EMPLOYEE_PORTRAIT_REPLACE_OWNER_DECISION_REQUIRED');

const invalidResultClient = createBusinessClient({
  runtime: 'CLOUD',
  async request() { return { ok: true, commitStatus: 'MADE_UP', googleOutputStatus: 'PENDING' }; }
}, { randomUUID: () => 'BAD' });
await assert.rejects(() => invalidResultClient.submitCommand({
  commandCode: 'EMPLOYEE_CREATE', entityId: 'EMP-2', payload: { employeeId: 'EMP-2', fullName: 'B' }
}), error => error?.code === 'MUTATION_RESULT_INVALID');

await import('./slice1-ui.test.mjs');

console.log('WEB_SLICE1_CLIENT_PASS cloudContractShape=PASS lanContractNormalized=PASS lanSignedExactBody=PASS lanUnsignedNoNetwork=PASS portraitGate=PASS invalidResultFailClosed=PASS surfaceStates=PASS');
