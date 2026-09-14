import { DOMAIN_CONTRACT_VERSION } from './runtime-contract.js';

export const EDGE_SCHEMA_VERSION = 'VHDCHY_EDGE_V2';
const RECEIPT_TARGETS = new Set(['GOOGLE_SHEETS', 'GOOGLE_DRIVE']);
const REQUIRED_TEXT = [
  'eventId', 'requestId', 'idempotencyKey', 'environment', 'clusterId',
  'edgeInstanceId', 'edgeEpoch', 'commandCode', 'eventCode', 'entityType',
  'entityId', 'payloadJson', 'payloadHash', 'authoritySnapshotVersion',
  'domainContractVersion', 'edgeSchemaVersion', 'actorUserId'
];

function text(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

function safeInteger(value, minimum = 1) {
  return Number.isSafeInteger(value) && value >= minimum;
}

function objectJson(value) {
  if (!text(value)) return false;
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed);
  } catch {
    return false;
  }
}

export function validateReconciliationEnvelope(envelope, expectedEnvironment = null) {
  if (!envelope || typeof envelope !== 'object' || Array.isArray(envelope)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'ENVELOPE_OBJECT_REQUIRED' };
  }

  const missing = REQUIRED_TEXT.filter((field) => !text(envelope[field]));
  if (missing.length > 0) return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'REQUIRED_FIELDS_MISSING', missing };

  if (expectedEnvironment && envelope.environment !== expectedEnvironment) {
    return { ok: false, code: 'SCHEMA_INCOMPATIBLE', status: 409, reason: 'ENVIRONMENT_MISMATCH' };
  }
  if (envelope.domainContractVersion !== DOMAIN_CONTRACT_VERSION) {
    return { ok: false, code: 'SCHEMA_INCOMPATIBLE', status: 409, reason: 'DOMAIN_CONTRACT_MISMATCH' };
  }
  if (envelope.edgeSchemaVersion !== EDGE_SCHEMA_VERSION) {
    return { ok: false, code: 'SCHEMA_INCOMPATIBLE', status: 409, reason: 'EDGE_SCHEMA_MISMATCH' };
  }
  if (!safeInteger(envelope.resultingVersion)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'RESULTING_VERSION_INVALID' };
  }
  if (envelope.baseVersion != null && !safeInteger(envelope.baseVersion)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'BASE_VERSION_INVALID' };
  }
  if (envelope.deviceSeq != null && !safeInteger(envelope.deviceSeq)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'DEVICE_SEQUENCE_INVALID' };
  }
  if (envelope.deviceSeq != null && !text(envelope.deviceId)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'DEVICE_ID_REQUIRED' };
  }
  if (!text(envelope.acceptedAt) || Number.isNaN(Date.parse(envelope.acceptedAt))) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'ACCEPTED_AT_INVALID' };
  }
  if (!objectJson(envelope.payloadJson)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'PAYLOAD_JSON_INVALID' };
  }

  const receipts = envelope.completedIntegrationReceipts ?? [];
  if (!Array.isArray(receipts)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'RECEIPTS_INVALID' };
  }

  const receiptKeys = new Set();
  for (const receipt of receipts) {
    if (!receipt || typeof receipt !== 'object' || Array.isArray(receipt)) {
      return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'RECEIPT_INVALID' };
    }
    if (!text(receipt.receiptId) || !text(receipt.logicalKey) || !text(receipt.targetKind) ||
        !text(receipt.completedAt) || !text(receipt.status)) {
      return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'RECEIPT_FIELDS_MISSING' };
    }
    if (!RECEIPT_TARGETS.has(receipt.targetKind) || receipt.status !== 'COMPLETED') {
      return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'RECEIPT_STATE_INVALID' };
    }
    if (Number.isNaN(Date.parse(receipt.completedAt)) || !objectJson(receipt.readbackEvidenceJson ?? '{}')) {
      return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'RECEIPT_EVIDENCE_INVALID' };
    }
    const key = `${receipt.targetKind}:${receipt.logicalKey}`;
    if (receiptKeys.has(key)) return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'DUPLICATE_RECEIPT_IN_ENVELOPE' };
    receiptKeys.add(key);
  }

  return { ok: true };
}

export function reconciliationSourceId(envelope) {
  return `${envelope.environment}:${envelope.clusterId}:${envelope.edgeInstanceId}:${envelope.edgeEpoch}`;
}

function sameEvent(existing, envelope) {
  return existing &&
    existing.eventId === envelope.eventId &&
    existing.idempotencyKey === envelope.idempotencyKey &&
    existing.commandCode === envelope.commandCode &&
    existing.eventCode === envelope.eventCode &&
    existing.entityType === envelope.entityType &&
    existing.entityId === envelope.entityId &&
    existing.payloadHash === envelope.payloadHash &&
    existing.actorUserId === envelope.actorUserId &&
    existing.authoritySnapshotVersion === envelope.authoritySnapshotVersion;
}

function sameReceipt(existing, receipt) {
  return existing &&
    existing.targetKind === receipt.targetKind &&
    existing.logicalKey === receipt.logicalKey &&
    (existing.providerObjectId ?? null) === (receipt.providerObjectId ?? null) &&
    (existing.contentHash ?? null) === (receipt.contentHash ?? null) &&
    (existing.checkpoint ?? null) === (receipt.checkpoint ?? null);
}

function conflict(code, reason, extra = {}) {
  return { ok: false, code, status: 409, reason, ...extra };
}

export async function ingestReconciliationEnvelope(store, envelope, expectedEnvironment = null) {
  const validation = validateReconciliationEnvelope(envelope, expectedEnvironment);
  if (!validation.ok) return validation;

  const sourceId = reconciliationSourceId(envelope);
  const source = await store.getSource(sourceId);
  if (source) {
    if (source.status !== 'ACTIVE') return conflict('SYNC_CONFLICT', 'EDGE_SOURCE_NOT_ACTIVE', { sourceStatus: source.status });
    if (source.environment !== envelope.environment || source.clusterId !== envelope.clusterId ||
        source.edgeInstanceId !== envelope.edgeInstanceId || source.edgeEpoch !== envelope.edgeEpoch ||
        source.domainContractVersion !== envelope.domainContractVersion || source.edgeSchemaVersion !== envelope.edgeSchemaVersion) {
      return conflict('SCHEMA_INCOMPATIBLE', 'EDGE_SOURCE_IDENTITY_MISMATCH');
    }
  }

  const byEvent = await store.getEventById(envelope.eventId);
  if (byEvent) {
    return sameEvent(byEvent, envelope)
      ? { ok: true, duplicate: true, reconciliationStatus: byEvent.status, edgeEventId: byEvent.eventId, attachedReceiptCount: 0 }
      : conflict('SYNC_CONFLICT', 'EDGE_EVENT_ID_COLLISION', { existingEventId: byEvent.eventId });
  }

  const byIdempotency = await store.getEventByIdempotency(sourceId, envelope.idempotencyKey);
  if (byIdempotency) {
    return sameEvent(byIdempotency, envelope)
      ? { ok: true, duplicate: true, reconciliationStatus: byIdempotency.status, edgeEventId: byIdempotency.eventId, attachedReceiptCount: 0 }
      : conflict('IDEMPOTENCY_PAYLOAD_CONFLICT', 'IDEMPOTENCY_IDENTITY_COLLISION', { existingEventId: byIdempotency.eventId });
  }

  if (envelope.deviceId && envelope.deviceSeq != null) {
    const bySequence = await store.getEventByDeviceSequence(sourceId, envelope.deviceId, envelope.deviceSeq);
    if (bySequence && bySequence.eventId !== envelope.eventId) {
      return conflict('DEVICE_SEQUENCE_COLLISION', 'DEVICE_SEQUENCE_ALREADY_USED', { existingEventId: bySequence.eventId });
    }
  }

  const receipts = [];
  for (const receipt of envelope.completedIntegrationReceipts ?? []) {
    const existing = await store.getReceipt(envelope.environment, receipt.targetKind, receipt.logicalKey);
    if (existing) {
      if (!sameReceipt(existing, receipt)) {
        return conflict('SYNC_CONFLICT', 'INTEGRATION_RECEIPT_COLLISION', { targetKind: receipt.targetKind, logicalKey: receipt.logicalKey });
      }
    } else {
      receipts.push(receipt);
    }
  }

  await store.commitAccepted({
    source: {
      sourceId,
      environment: envelope.environment,
      clusterId: envelope.clusterId,
      edgeInstanceId: envelope.edgeInstanceId,
      edgeEpoch: envelope.edgeEpoch,
      domainContractVersion: envelope.domainContractVersion,
      edgeSchemaVersion: envelope.edgeSchemaVersion
    },
    envelope,
    receipts
  });

  return { ok: true, duplicate: false, reconciliationStatus: 'RECEIVED', edgeEventId: envelope.eventId, attachedReceiptCount: receipts.length };
}

function mapEvent(row) {
  if (!row) return null;
  return {
    eventId: row.edge_event_id,
    idempotencyKey: row.idempotency_key,
    commandCode: row.command_code,
    eventCode: row.event_type,
    entityType: row.entity_type,
    entityId: row.entity_id,
    payloadHash: row.payload_hash,
    actorUserId: row.actor_user_id,
    authoritySnapshotVersion: row.authority_snapshot_version,
    status: row.status
  };
}

export function createD1ReconciliationStore(db) {
  if (!db || typeof db.prepare !== 'function' || typeof db.batch !== 'function') throw new TypeError('D1 database binding is required');

  return {
    async getSource(sourceId) {
      const row = await db.prepare('SELECT edge_source_id, environment, cluster_id, edge_instance_id, edge_epoch, domain_contract_version, edge_schema_version, status FROM edge_sources WHERE edge_source_id=? LIMIT 1').bind(sourceId).first();
      return row ? {
        sourceId: row.edge_source_id,
        environment: row.environment,
        clusterId: row.cluster_id,
        edgeInstanceId: row.edge_instance_id,
        edgeEpoch: row.edge_epoch,
        domainContractVersion: row.domain_contract_version,
        edgeSchemaVersion: row.edge_schema_version,
        status: row.status
      } : null;
    },
    async getEventById(eventId) {
      return mapEvent(await db.prepare('SELECT edge_event_id, idempotency_key, command_code, event_type, entity_type, entity_id, payload_hash, actor_user_id, authority_snapshot_version, status FROM edge_event_ingest WHERE edge_event_id=? LIMIT 1').bind(eventId).first());
    },
    async getEventByIdempotency(sourceId, idempotencyKey) {
      return mapEvent(await db.prepare('SELECT edge_event_id, idempotency_key, command_code, event_type, entity_type, entity_id, payload_hash, actor_user_id, authority_snapshot_version, status FROM edge_event_ingest WHERE edge_source_id=? AND idempotency_key=? LIMIT 1').bind(sourceId, idempotencyKey).first());
    },
    async getEventByDeviceSequence(sourceId, deviceId, deviceSeq) {
      return mapEvent(await db.prepare('SELECT edge_event_id, idempotency_key, command_code, event_type, entity_type, entity_id, payload_hash, actor_user_id, authority_snapshot_version, status FROM edge_event_ingest WHERE edge_source_id=? AND device_id=? AND device_seq=? LIMIT 1').bind(sourceId, deviceId, deviceSeq).first());
    },
    async getReceipt(environment, targetKind, logicalKey) {
      const row = await db.prepare('SELECT target_kind, logical_key, provider_ref, content_hash, checkpoint_ref FROM integration_receipts WHERE environment=? AND target_kind=? AND logical_key=? LIMIT 1').bind(environment, targetKind, logicalKey).first();
      return row ? {
        targetKind: row.target_kind,
        logicalKey: row.logical_key,
        providerObjectId: row.provider_ref,
        contentHash: row.content_hash,
        checkpoint: row.checkpoint_ref
      } : null;
    },
    async commitAccepted({ source, envelope, receipts }) {
      const statements = [
        db.prepare("INSERT INTO edge_sources(edge_source_id, environment, cluster_id, edge_instance_id, edge_epoch, domain_contract_version, edge_schema_version, status, last_sync_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, 'ACTIVE', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) ON CONFLICT(edge_source_id) DO UPDATE SET last_sync_at=excluded.last_sync_at, updated_at=excluded.updated_at").bind(source.sourceId, source.environment, source.clusterId, source.edgeInstanceId, source.edgeEpoch, source.domainContractVersion, source.edgeSchemaVersion),
        db.prepare("INSERT INTO edge_event_ingest(edge_event_id, edge_source_id, request_id, idempotency_key, device_id, device_seq, command_code, event_type, entity_type, entity_id, base_entity_version, resulting_entity_version, payload_json, payload_hash, actor_user_id, authority_snapshot_version, local_order, local_accepted_at, status, received_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, 'RECEIVED', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)").bind(envelope.eventId, source.sourceId, envelope.requestId, envelope.idempotencyKey, envelope.deviceId ?? null, envelope.deviceSeq ?? null, envelope.commandCode, envelope.eventCode, envelope.entityType, envelope.entityId, envelope.baseVersion ?? null, envelope.resultingVersion, envelope.payloadJson, envelope.payloadHash, envelope.actorUserId, envelope.authoritySnapshotVersion, envelope.acceptedAt)
      ];

      for (const receipt of receipts) {
        const metadata = JSON.stringify({ lanReceiptId: receipt.receiptId, readbackEvidence: JSON.parse(receipt.readbackEvidenceJson ?? '{}') });
        statements.push(db.prepare("INSERT INTO integration_receipts(receipt_id, environment, cluster_id, edge_source_id, edge_event_id, target_kind, logical_key, provider_ref, content_hash, checkpoint_ref, status, completed_at, metadata_json, created_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'COMPLETED', ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)").bind(receipt.receiptId, envelope.environment, envelope.clusterId, source.sourceId, envelope.eventId, receipt.targetKind, receipt.logicalKey, receipt.providerObjectId ?? null, receipt.contentHash ?? null, receipt.checkpoint ?? null, receipt.completedAt, metadata));
      }

      await db.batch(statements);
    }
  };
}
