import test from 'node:test';
import assert from 'node:assert/strict';

import {
  COMMIT_STATUS,
  GOOGLE_OUTPUT_STATUS,
  MACHINE_ERROR,
  errorEnvelope,
  successEnvelope,
  validateLanAcceptanceIdentity,
  validateMutationIdentity
} from '../src/runtime-contract.js';

const baseIdentity = {
  requestId: 'req-1',
  idempotencyKey: 'idem-1',
  environment: 'BETA',
  clusterId: 'PICK_PACK_1291',
  commandCode: 'ATTENDANCE_IN',
  entityType: 'employee',
  entityId: 'emp-1',
  payloadHash: 'abc123'
};

test('validates minimum cloud mutation identity', () => {
  assert.deepEqual(validateMutationIdentity(baseIdentity), { ok: true });
});

test('rejects device sequence without device identity', () => {
  const result = validateMutationIdentity({ ...baseIdentity, deviceSeq: 7 });
  assert.equal(result.ok, false);
  assert.equal(result.code, MACHINE_ERROR.INVALID_INPUT);
  assert.deepEqual(result.missing, ['deviceId']);
});

test('requires edge identity and authority generation for LAN acceptance', () => {
  const result = validateLanAcceptanceIdentity(baseIdentity);
  assert.equal(result.ok, false);
  assert.deepEqual(result.missing, ['edgeInstanceId', 'edgeEpoch', 'authoritySnapshotVersion']);

  assert.deepEqual(validateLanAcceptanceIdentity({
    ...baseIdentity,
    edgeInstanceId: 'edge-1',
    edgeEpoch: 'epoch-1',
    authoritySnapshotVersion: 'auth-42'
  }), { ok: true });
});

test('builds explicit success durability/output state', () => {
  assert.deepEqual(successEnvelope({
    requestId: 'req-1',
    runtime: 'LAN',
    commitStatus: COMMIT_STATUS.LAN_ACCEPTED_PENDING_SYNC,
    googleOutputStatus: GOOGLE_OUTPUT_STATUS.PENDING,
    data: { eventId: 'edge-event-1' }
  }), {
    ok: true,
    requestId: 'req-1',
    runtime: 'LAN',
    commitStatus: 'LAN_ACCEPTED_PENDING_SYNC',
    googleOutputStatus: 'PENDING',
    data: { eventId: 'edge-event-1' }
  });
});

test('builds stable machine error envelope', () => {
  const value = errorEnvelope({
    requestId: 'req-2',
    runtime: 'CLOUD',
    code: MACHINE_ERROR.RESOURCE_NOT_AVAILABLE,
    message: 'resource unavailable'
  });
  assert.equal(value.ok, false);
  assert.equal(value.error.code, 'RESOURCE_NOT_AVAILABLE');
});
