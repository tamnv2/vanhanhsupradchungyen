import test from 'node:test';
import assert from 'node:assert/strict';

import {
  EDGE_SCHEMA_VERSION,
  ingestReconciliationEnvelope,
  reconciliationSourceId,
  validateReconciliationEnvelope
} from '../src/reconciliation.js';

class MemoryStore {
  constructor() {
    this.sources = new Map();
    this.events = new Map();
    this.receipts = new Map();
  }

  async getSource(sourceId) {
    return this.sources.get(sourceId) ?? null;
  }

  async getEventById(eventId) {
    return this.events.get(eventId) ?? null;
  }

  async getEventByIdempotency(sourceId, idempotencyKey) {
    return [...this.events.values()].find((event) => event.sourceId === sourceId && event.idempotencyKey === idempotencyKey) ?? null;
  }

  async getEventByDeviceSequence(sourceId, deviceId, deviceSeq) {
    return [...this.events.values()].find((event) => event.sourceId === sourceId && event.deviceId === deviceId && event.deviceSeq === deviceSeq) ?? null;
  }

  async getReceipt(environment, targetKind, logicalKey) {
    return this.receipts.get(`${environment}:${targetKind}:${logicalKey}`) ?? null;
  }

  async commitAccepted({ source, envelope, receipts }) {
    this.sources.set(source.sourceId, { ...source, status: 'ACTIVE' });
    this.events.set(envelope.eventId, {
      sourceId: source.sourceId,
      eventId: envelope.eventId,
      idempotencyKey: envelope.idempotencyKey,
      commandCode: envelope.commandCode,
      eventCode: envelope.eventCode,
      entityType: envelope.entityType,
      entityId: envelope.entityId,
      payloadHash: envelope.payloadHash,
      actorUserId: envelope.actorUserId,
      authoritySnapshotVersion: envelope.authoritySnapshotVersion,
      deviceId: envelope.deviceId,
      deviceSeq: envelope.deviceSeq,
      status: 'RECEIVED'
    });
    for (const receipt of receipts) {
      this.receipts.set(`${envelope.environment}:${receipt.targetKind}:${receipt.logicalKey}`, { ...receipt });
    }
  }
}

function envelope(overrides = {}) {
  return {
    eventId: 'edge-event-1',
    requestId: 'request-1',
    idempotencyKey: 'idem-1',
    environment: 'BETA',
    clusterId: 'PICK_PACK_1291',
    deviceId: 'PDA-001',
    deviceSeq: 7,
    edgeInstanceId: 'edge-instance-1',
    edgeEpoch: 'epoch-1',
    commandCode: 'ATTENDANCE_IN',
    eventCode: 'ATTENDANCE_IN_RECORDED',
    entityType: 'employee',
    entityId: 'employee-1',
    baseVersion: 1,
    resultingVersion: 2,
    payloadJson: '{"employeeId":"employee-1"}',
    payloadHash: 'hash-1',
    acceptedAt: '2026-09-14T03:30:00.000Z',
    authoritySnapshotVersion: 'authority-1',
    domainContractVersion: 'VHDCHY_DOMAIN_V1',
    edgeSchemaVersion: EDGE_SCHEMA_VERSION,
    actorUserId: 'user-1',
    completedIntegrationReceipts: [],
    ...overrides
  };
}

test('requires immutable actor evidence and compatible edge contract', () => {
  const missingActor = envelope({ actorUserId: '' });
  const result = validateReconciliationEnvelope(missingActor, 'BETA');
  assert.equal(result.ok, false);
  assert.equal(result.code, 'INVALID_INPUT');
  assert.deepEqual(result.missing, ['actorUserId']);

  const staleSchema = validateReconciliationEnvelope(envelope({ edgeSchemaVersion: 'OLD' }), 'BETA');
  assert.equal(staleSchema.ok, false);
  assert.equal(staleSchema.code, 'SCHEMA_INCOMPATIBLE');
  assert.equal(staleSchema.reason, 'EDGE_SCHEMA_MISMATCH');
});

test('accepts a new edge event and preserves completed integration receipts', async () => {
  const store = new MemoryStore();
  const item = envelope({
    completedIntegrationReceipts: [{
      receiptId: 'receipt-1',
      logicalKey: 'attendance:employee-1:2026-09-14',
      targetKind: 'GOOGLE_SHEETS',
      providerObjectId: 'sheet-1',
      contentHash: 'content-1',
      checkpoint: 'row-22',
      readbackEvidenceJson: '{"verified":true}',
      completedAt: '2026-09-14T03:31:00.000Z',
      status: 'COMPLETED'
    }]
  });

  const result = await ingestReconciliationEnvelope(store, item, 'BETA');
  assert.deepEqual(result, {
    ok: true,
    duplicate: false,
    reconciliationStatus: 'RECEIVED',
    edgeEventId: 'edge-event-1',
    attachedReceiptCount: 1
  });
  assert.equal(store.events.get('edge-event-1').actorUserId, 'user-1');
  assert.equal(store.receipts.size, 1);
});

test('same event replay is idempotent', async () => {
  const store = new MemoryStore();
  const item = envelope();
  await ingestReconciliationEnvelope(store, item, 'BETA');
  const replay = await ingestReconciliationEnvelope(store, item, 'BETA');
  assert.equal(replay.ok, true);
  assert.equal(replay.duplicate, true);
  assert.equal(replay.edgeEventId, item.eventId);
  assert.equal(store.events.size, 1);
});

test('same idempotency identity with different payload is a conflict', async () => {
  const store = new MemoryStore();
  await ingestReconciliationEnvelope(store, envelope(), 'BETA');
  const conflict = await ingestReconciliationEnvelope(store, envelope({ eventId: 'edge-event-2', payloadHash: 'hash-2' }), 'BETA');
  assert.equal(conflict.ok, false);
  assert.equal(conflict.code, 'IDEMPOTENCY_PAYLOAD_CONFLICT');
  assert.equal(conflict.reason, 'IDEMPOTENCY_IDENTITY_COLLISION');
});

test('device sequence cannot be reused by another event', async () => {
  const store = new MemoryStore();
  await ingestReconciliationEnvelope(store, envelope(), 'BETA');
  const conflict = await ingestReconciliationEnvelope(store, envelope({
    eventId: 'edge-event-2',
    idempotencyKey: 'idem-2',
    payloadHash: 'hash-2'
  }), 'BETA');
  assert.equal(conflict.ok, false);
  assert.equal(conflict.code, 'DEVICE_SEQUENCE_COLLISION');
  assert.equal(conflict.reason, 'DEVICE_SEQUENCE_ALREADY_USED');
});

test('existing identical Google receipt is attached by identity without duplication', async () => {
  const store = new MemoryStore();
  const receipt = {
    receiptId: 'receipt-1',
    logicalKey: 'doc:file-1',
    targetKind: 'GOOGLE_DRIVE',
    providerObjectId: 'drive-object-1',
    contentHash: 'file-hash',
    checkpoint: 'drive-checkpoint-1',
    readbackEvidenceJson: '{"size":123}',
    completedAt: '2026-09-14T03:31:00.000Z',
    status: 'COMPLETED'
  };
  store.receipts.set('BETA:GOOGLE_DRIVE:doc:file-1', { ...receipt });

  const result = await ingestReconciliationEnvelope(store, envelope({ completedIntegrationReceipts: [receipt] }), 'BETA');
  assert.equal(result.ok, true);
  assert.equal(result.attachedReceiptCount, 0);
  assert.equal(store.receipts.size, 1);
});

test('existing receipt with conflicting provider evidence is explicit conflict', async () => {
  const store = new MemoryStore();
  store.receipts.set('BETA:GOOGLE_DRIVE:doc:file-1', {
    targetKind: 'GOOGLE_DRIVE',
    logicalKey: 'doc:file-1',
    providerObjectId: 'drive-object-old',
    contentHash: 'file-hash',
    checkpoint: 'drive-checkpoint-1'
  });

  const result = await ingestReconciliationEnvelope(store, envelope({
    completedIntegrationReceipts: [{
      receiptId: 'receipt-new',
      logicalKey: 'doc:file-1',
      targetKind: 'GOOGLE_DRIVE',
      providerObjectId: 'drive-object-new',
      contentHash: 'file-hash',
      checkpoint: 'drive-checkpoint-1',
      readbackEvidenceJson: '{}',
      completedAt: '2026-09-14T03:31:00.000Z',
      status: 'COMPLETED'
    }]
  }), 'BETA');

  assert.equal(result.ok, false);
  assert.equal(result.code, 'SYNC_CONFLICT');
  assert.equal(result.reason, 'INTEGRATION_RECEIPT_COLLISION');
});

test('source identity is stable across retry', () => {
  const item = envelope();
  assert.equal(reconciliationSourceId(item), 'BETA:PICK_PACK_1291:edge-instance-1:epoch-1');
});
