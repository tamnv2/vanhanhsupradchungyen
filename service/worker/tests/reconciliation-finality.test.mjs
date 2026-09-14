import assert from 'node:assert/strict';
import test from 'node:test';
import {
  finalizeReconciliationEnvelope,
  planCanonicalReconciliation,
  RECONCILIATION_FINALITY_VERSION
} from '../src/reconciliation-finality.js';

function envelope(overrides = {}) {
  return {
    eventId: 'edge-event-1',
    requestId: 'request-1',
    idempotencyKey: 'idem-1',
    environment: 'BETA',
    clusterId: 'PICK_PACK_1291',
    deviceId: null,
    deviceSeq: null,
    edgeInstanceId: 'edge-1',
    edgeEpoch: 'epoch-1',
    commandCode: 'EMPLOYEE_CREATE',
    eventCode: 'EMPLOYEE_CREATED',
    entityType: 'EMPLOYEE',
    entityId: 'EMP001',
    baseVersion: null,
    resultingVersion: 1,
    payloadJson: JSON.stringify({ employeeId: 'EMP001', fullName: 'Nguyễn Văn A', status: 'ACTIVE' }),
    payloadHash: 'hash-1',
    acceptedAt: '2026-09-14T10:00:00.000Z',
    authoritySnapshotVersion: 'authority-1',
    domainContractVersion: 'VHDCHY_DOMAIN_V1',
    edgeSchemaVersion: 'VHDCHY_EDGE_V2',
    actorUserId: 'user-1',
    completedIntegrationReceipts: [],
    ...overrides
  };
}

function canonicalFromEnvelope(value, eventId = `cloud:${value.eventId}`, ingestedAt = '2026-09-14T10:00:05.000Z') {
  return {
    eventId,
    eventType: value.eventCode,
    entityType: value.entityType,
    entityId: value.entityId,
    entityVersion: value.resultingVersion,
    actorUserId: value.actorUserId,
    deviceId: value.deviceId,
    deviceSeq: value.deviceSeq,
    idempotencyKey: value.idempotencyKey,
    payloadJson: value.payloadJson,
    ingestedAt
  };
}

function createStore(value) {
  const state = {
    edge: { eventId: value.eventId, status: 'RECEIVED', canonicalEventId: null, conflictId: null, lastErrorCode: null },
    employee: null,
    canonicalById: new Map(),
    canonicalByIdempotency: new Map(),
    outbox: new Set(),
    conflicts: [],
    commitCount: 0,
    linkCount: 0
  };
  return {
    state,
    async getEdgeEvent() { return { ...state.edge }; },
    async getCanonicalEventById(eventId) { return state.canonicalById.get(eventId) || null; },
    async getCanonicalEventByIdempotency(key) { return state.canonicalByIdempotency.get(key) || null; },
    async hasProjectionOutbox(eventId) { return state.outbox.has(eventId); },
    async actorExists() { return true; },
    async clusterExists() { return true; },
    async deviceExists() { return true; },
    async getEmployee() { return state.employee; },
    async recordConflict(_envelope, reason, candidateEventIds) {
      state.edge.status = 'CONFLICT';
      state.edge.lastErrorCode = reason;
      state.conflicts.push({ reason, candidateEventIds });
      return `sync:${value.eventId}`;
    },
    async linkExistingCanonical(_envelope, canonicalEventId) {
      state.edge.status = 'RECONCILED';
      state.edge.canonicalEventId = canonicalEventId;
      state.linkCount += 1;
    },
    async commitEmployeeCreate({ state: employeeState, canonicalEventId }) {
      state.employee = { employeeId: employeeState.employeeId, entityVersion: 1, status: employeeState.status };
      const canonical = canonicalFromEnvelope(value, canonicalEventId);
      state.canonicalById.set(canonicalEventId, canonical);
      state.canonicalByIdempotency.set(value.idempotencyKey, canonical);
      state.outbox.add(canonicalEventId);
      state.edge.status = 'RECONCILED';
      state.edge.canonicalEventId = canonicalEventId;
      state.commitCount += 1;
    }
  };
}

test('EMPLOYEE_CREATE plan preserves reviewed command/event/version semantics', () => {
  const result = planCanonicalReconciliation(envelope());
  assert.equal(result.ok, true);
  assert.equal(result.supported, true);
  assert.equal(result.state.employeeId, 'EMP001');
  assert.equal(result.state.fullName, 'Nguyễn Văn A');
  assert.equal(result.state.entityVersion, 1);
  assert.equal(RECONCILIATION_FINALITY_VERSION, 'VHDCHY_RECONCILIATION_FINALITY_V1');
});

test('unsupported Slice-1 command remains RECEIVED instead of fabricating finality', async () => {
  const value = envelope({ commandCode: 'EMPLOYEE_UPDATE', eventCode: 'EMPLOYEE_UPDATED', baseVersion: 1, resultingVersion: 2 });
  const store = createStore(value);
  const result = await finalizeReconciliationEnvelope(store, value, '2026-09-14T10:00:05.000Z');
  assert.equal(result.ok, true);
  assert.equal(result.finalized, false);
  assert.equal(result.reconciliationStatus, 'RECEIVED');
  assert.equal(result.pendingReason, 'COMMAND_FINALITY_NOT_IMPLEMENTED');
  assert.equal(store.state.commitCount, 0);
});

test('EMPLOYEE_CREATE finalizes to one canonical event and retry returns the same canonical identity', async () => {
  const value = envelope();
  const store = createStore(value);

  const first = await finalizeReconciliationEnvelope(store, value, '2026-09-14T10:00:05.000Z');
  assert.equal(first.ok, true);
  assert.equal(first.finalized, true);
  assert.equal(first.duplicate, false);
  assert.equal(first.reconciliationStatus, 'LAN_RECONCILED_CLOUD_COMMITTED');
  assert.equal(first.canonicalEventId, 'cloud:edge-event-1');
  assert.equal(store.state.commitCount, 1);

  const second = await finalizeReconciliationEnvelope(store, value, '2026-09-14T10:00:06.000Z');
  assert.equal(second.ok, true);
  assert.equal(second.finalized, true);
  assert.equal(second.duplicate, true);
  assert.equal(second.canonicalEventId, first.canonicalEventId);
  assert.equal(store.state.commitCount, 1);
});

test('compatible canonical idempotency winner links without creating another canonical mutation', async () => {
  const value = envelope();
  const store = createStore(value);
  const canonical = canonicalFromEnvelope(value, 'cloud-existing');
  store.state.canonicalById.set(canonical.eventId, canonical);
  store.state.canonicalByIdempotency.set(value.idempotencyKey, canonical);
  store.state.outbox.add(canonical.eventId);

  const result = await finalizeReconciliationEnvelope(store, value, '2026-09-14T10:00:06.000Z');
  assert.equal(result.ok, true);
  assert.equal(result.duplicate, true);
  assert.equal(result.canonicalEventId, 'cloud-existing');
  assert.equal(store.state.commitCount, 0);
  assert.equal(store.state.linkCount, 1);
});

test('canonical idempotency mismatch becomes explicit SYNC_CONFLICT and retains edge evidence', async () => {
  const value = envelope();
  const store = createStore(value);
  const canonical = canonicalFromEnvelope(value, 'cloud-existing');
  canonical.payloadJson = JSON.stringify({ employeeId: 'EMP001', fullName: 'Different' });
  store.state.canonicalByIdempotency.set(value.idempotencyKey, canonical);

  const result = await finalizeReconciliationEnvelope(store, value, '2026-09-14T10:00:06.000Z');
  assert.equal(result.ok, false);
  assert.equal(result.code, 'SYNC_CONFLICT');
  assert.equal(result.reason, 'IDEMPOTENCY_CANONICAL_MISMATCH');
  assert.equal(store.state.edge.status, 'CONFLICT');
  assert.equal(store.state.conflicts.length, 1);
});

test('existing employee blocks create as explicit conflict', async () => {
  const value = envelope();
  const store = createStore(value);
  store.state.employee = { employeeId: 'EMP001', entityVersion: 3, status: 'ACTIVE' };

  const result = await finalizeReconciliationEnvelope(store, value, '2026-09-14T10:00:06.000Z');
  assert.equal(result.ok, false);
  assert.equal(result.code, 'SYNC_CONFLICT');
  assert.equal(result.reason, 'EMPLOYEE_ALREADY_EXISTS');
  assert.equal(store.state.commitCount, 0);
  assert.equal(store.state.edge.status, 'CONFLICT');
});
