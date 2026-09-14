export const RECONCILIATION_FINALITY_VERSION = 'VHDCHY_RECONCILIATION_FINALITY_V1';
export const FINALITY_ENABLED_VALUE = 'enabled';

const EMPLOYEE_CREATE_ALLOWED_FIELDS = new Set([
  'employeeId', 'fullName', 'phone', 'status', 'mainPosition', 'vendor',
  'department', 'site', 'warehouse', 'startDate', 'permanentLeaveDate', 'note'
]);
const EMPLOYEE_STATUSES = new Set(['ACTIVE', 'INACTIVE', 'LEFT', 'ARCHIVED']);

function text(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

function nullableText(value) {
  if (value == null) return null;
  const normalized = String(value).trim();
  return normalized.length ? normalized : null;
}

function parseObjectJson(value) {
  try {
    const parsed = JSON.parse(String(value || '{}'));
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function conflict(reason, details = {}) {
  return { ok: false, code: 'SYNC_CONFLICT', status: 409, reason, details };
}

function invalid(reason, details = {}) {
  return { ok: false, code: 'INVALID_INPUT', status: 422, reason, details };
}

function canonicalIdentityMatches(existing, envelope) {
  return existing &&
    existing.eventType === envelope.eventCode &&
    existing.entityType === envelope.entityType &&
    existing.entityId === envelope.entityId &&
    Number(existing.entityVersion) === Number(envelope.resultingVersion) &&
    (existing.actorUserId ?? null) === (envelope.actorUserId ?? null) &&
    (existing.deviceId ?? null) === (envelope.deviceId ?? null) &&
    (existing.deviceSeq == null ? null : Number(existing.deviceSeq)) === (envelope.deviceSeq == null ? null : Number(envelope.deviceSeq)) &&
    existing.idempotencyKey === envelope.idempotencyKey &&
    existing.payloadJson === envelope.payloadJson;
}

function planEmployeeCreate(envelope) {
  if (envelope.eventCode !== 'EMPLOYEE_CREATED' || envelope.entityType !== 'EMPLOYEE') {
    return invalid('EMPLOYEE_CREATE_EVENT_IDENTITY_MISMATCH');
  }
  if (envelope.baseVersion != null || Number(envelope.resultingVersion) !== 1) {
    return conflict('EMPLOYEE_CREATE_VERSION_CONFLICT', {
      baseVersion: envelope.baseVersion ?? null,
      resultingVersion: envelope.resultingVersion ?? null
    });
  }

  const payload = parseObjectJson(envelope.payloadJson);
  if (!payload) return invalid('EMPLOYEE_CREATE_PAYLOAD_INVALID');
  const unknown = Object.keys(payload).filter(key => !EMPLOYEE_CREATE_ALLOWED_FIELDS.has(key));
  if (unknown.length) return invalid('EMPLOYEE_CREATE_PAYLOAD_FIELDS_UNSUPPORTED', { fields: unknown.slice(0, 10) });

  const employeeId = nullableText(payload.employeeId);
  const fullName = nullableText(payload.fullName);
  const status = nullableText(payload.status) || 'ACTIVE';
  if (!employeeId || employeeId !== envelope.entityId) return invalid('EMPLOYEE_CREATE_EMPLOYEE_ID_MISMATCH');
  if (!fullName) return invalid('EMPLOYEE_CREATE_FULL_NAME_REQUIRED');
  if (!EMPLOYEE_STATUSES.has(status)) return invalid('EMPLOYEE_CREATE_STATUS_INVALID');

  return {
    ok: true,
    supported: true,
    commandCode: 'EMPLOYEE_CREATE',
    eventCode: 'EMPLOYEE_CREATED',
    entityType: 'EMPLOYEE',
    state: {
      employeeId,
      fullName,
      phone: nullableText(payload.phone),
      status,
      mainPosition: nullableText(payload.mainPosition),
      vendor: nullableText(payload.vendor),
      department: nullableText(payload.department),
      site: nullableText(payload.site),
      warehouse: nullableText(payload.warehouse),
      startDate: nullableText(payload.startDate),
      permanentLeaveDate: nullableText(payload.permanentLeaveDate),
      note: nullableText(payload.note),
      entityVersion: 1
    }
  };
}

export function planCanonicalReconciliation(envelope) {
  if (!envelope || typeof envelope !== 'object' || Array.isArray(envelope)) {
    return invalid('ENVELOPE_OBJECT_REQUIRED');
  }
  if (envelope.commandCode === 'EMPLOYEE_CREATE') return planEmployeeCreate(envelope);
  return {
    ok: true,
    supported: false,
    pendingReason: 'COMMAND_FINALITY_NOT_IMPLEMENTED',
    commandCode: String(envelope.commandCode || '')
  };
}

function projectionIntent(envelope, canonicalEventId) {
  return JSON.stringify({
    kind: 'VHDCHY_CANONICAL_EVENT_PROJECTION_INTENT_V1',
    canonicalEventId,
    edgeEventId: envelope.eventId,
    eventType: envelope.eventCode,
    entityType: envelope.entityType,
    entityId: envelope.entityId,
    entityVersion: envelope.resultingVersion,
    clusterId: envelope.clusterId,
    occurredAt: envelope.acceptedAt,
    payload: parseObjectJson(envelope.payloadJson) || {}
  });
}

function deterministicCanonicalEventId(edgeEventId) {
  return `cloud:${edgeEventId}`;
}

export async function finalizeReconciliationEnvelope(store, envelope, nowIso = new Date().toISOString()) {
  if (!store) throw new TypeError('Finality store is required');
  const plan = planCanonicalReconciliation(envelope);
  if (!plan.ok) return plan;

  const edge = await store.getEdgeEvent(envelope.eventId);
  if (!edge) return { ok: false, code: 'RECONCILIATION_UNAVAILABLE', status: 503, reason: 'EDGE_INGEST_NOT_FOUND' };

  if (edge.status === 'RECONCILED' && text(edge.canonicalEventId)) {
    const canonical = await store.getCanonicalEventById(edge.canonicalEventId);
    if (!canonical) return { ok: false, code: 'RECONCILIATION_UNAVAILABLE', status: 503, reason: 'CANONICAL_LINK_BROKEN' };
    return {
      ok: true,
      finalized: true,
      duplicate: true,
      reconciliationStatus: 'LAN_RECONCILED_CLOUD_COMMITTED',
      canonicalEventId: canonical.eventId,
      canonicalCommittedAt: canonical.ingestedAt
    };
  }

  if (edge.status === 'CONFLICT') {
    return conflict(edge.lastErrorCode || 'EDGE_EVENT_ALREADY_CONFLICTED', { conflictId: edge.conflictId ?? null });
  }
  if (edge.status !== 'RECEIVED') {
    return { ok: false, code: 'RECONCILIATION_UNAVAILABLE', status: 503, reason: 'EDGE_EVENT_NOT_FINALIZABLE', details: { status: edge.status } };
  }

  const existingCanonical = await store.getCanonicalEventByIdempotency(envelope.idempotencyKey);
  if (existingCanonical) {
    if (!canonicalIdentityMatches(existingCanonical, envelope)) {
      await store.recordConflict(envelope, 'IDEMPOTENCY_CANONICAL_MISMATCH', [existingCanonical.eventId], nowIso);
      return conflict('IDEMPOTENCY_CANONICAL_MISMATCH', { canonicalEventId: existingCanonical.eventId });
    }
    const hasOutbox = await store.hasProjectionOutbox(existingCanonical.eventId);
    if (!hasOutbox) {
      return { ok: false, code: 'RECONCILIATION_UNAVAILABLE', status: 503, reason: 'CANONICAL_OUTBOX_INVARIANT_BROKEN' };
    }
    await store.linkExistingCanonical(envelope, existingCanonical.eventId, nowIso);
    const linked = await store.getCanonicalEventById(existingCanonical.eventId);
    return {
      ok: true,
      finalized: true,
      duplicate: true,
      reconciliationStatus: 'LAN_RECONCILED_CLOUD_COMMITTED',
      canonicalEventId: existingCanonical.eventId,
      canonicalCommittedAt: linked?.ingestedAt || existingCanonical.ingestedAt
    };
  }

  if (!plan.supported) {
    return {
      ok: true,
      finalized: false,
      duplicate: false,
      reconciliationStatus: 'RECEIVED',
      pendingReason: plan.pendingReason
    };
  }

  if (!(await store.actorExists(envelope.actorUserId))) {
    await store.recordConflict(envelope, 'ACTOR_NOT_CANONICAL', [], nowIso);
    return conflict('ACTOR_NOT_CANONICAL');
  }
  if (!(await store.clusterExists(envelope.clusterId))) {
    return { ok: false, code: 'RECONCILIATION_UNAVAILABLE', status: 503, reason: 'CLUSTER_NOT_CANONICAL' };
  }
  if (envelope.deviceId && !(await store.deviceExists(envelope.deviceId))) {
    await store.recordConflict(envelope, 'DEVICE_NOT_CANONICAL', [], nowIso);
    return conflict('DEVICE_NOT_CANONICAL');
  }

  if (plan.commandCode === 'EMPLOYEE_CREATE') {
    const existingEmployee = await store.getEmployee(plan.state.employeeId);
    if (existingEmployee) {
      await store.recordConflict(envelope, 'EMPLOYEE_ALREADY_EXISTS', [], nowIso);
      return conflict('EMPLOYEE_ALREADY_EXISTS', { canonicalVersion: Number(existingEmployee.entityVersion) || null });
    }

    const canonicalEventId = deterministicCanonicalEventId(envelope.eventId);
    try {
      await store.commitEmployeeCreate({
        envelope,
        state: plan.state,
        canonicalEventId,
        projectionPayloadJson: projectionIntent(envelope, canonicalEventId),
        nowIso
      });
    } catch (error) {
      const winner = await store.getCanonicalEventByIdempotency(envelope.idempotencyKey);
      if (winner && canonicalIdentityMatches(winner, envelope) && await store.hasProjectionOutbox(winner.eventId)) {
        await store.linkExistingCanonical(envelope, winner.eventId, nowIso);
        const linked = await store.getCanonicalEventById(winner.eventId);
        return {
          ok: true,
          finalized: true,
          duplicate: true,
          reconciliationStatus: 'LAN_RECONCILED_CLOUD_COMMITTED',
          canonicalEventId: winner.eventId,
          canonicalCommittedAt: linked?.ingestedAt || winner.ingestedAt
        };
      }
      if (await store.getEmployee(plan.state.employeeId)) {
        await store.recordConflict(envelope, 'EMPLOYEE_CREATE_CANONICAL_RACE', winner ? [winner.eventId] : [], nowIso);
        return conflict('EMPLOYEE_CREATE_CANONICAL_RACE');
      }
      throw error;
    }

    const committed = await store.getCanonicalEventById(canonicalEventId);
    if (!committed?.ingestedAt) throw new Error('CANONICAL_EVENT_COMMIT_EVIDENCE_MISSING');
    return {
      ok: true,
      finalized: true,
      duplicate: false,
      reconciliationStatus: 'LAN_RECONCILED_CLOUD_COMMITTED',
      canonicalEventId,
      canonicalCommittedAt: committed.ingestedAt
    };
  }

  return { ok: true, finalized: false, duplicate: false, reconciliationStatus: 'RECEIVED', pendingReason: 'COMMAND_FINALITY_NOT_IMPLEMENTED' };
}

function mapCanonical(row) {
  if (!row) return null;
  return {
    eventId: row.event_id,
    eventType: row.event_type,
    entityType: row.entity_type,
    entityId: row.entity_id,
    entityVersion: Number(row.entity_version),
    actorUserId: row.actor_user_id ?? null,
    deviceId: row.device_id ?? null,
    deviceSeq: row.device_seq == null ? null : Number(row.device_seq),
    idempotencyKey: row.idempotency_key,
    payloadJson: row.payload_json,
    ingestedAt: row.ingested_at
  };
}

function mapEdge(row) {
  if (!row) return null;
  return {
    eventId: row.edge_event_id,
    status: row.status,
    canonicalEventId: row.canonical_event_id ?? null,
    conflictId: row.conflict_id ?? null,
    lastErrorCode: row.last_error_code ?? null
  };
}

export function createD1ReconciliationFinalityStore(db) {
  if (!db || typeof db.prepare !== 'function' || typeof db.batch !== 'function') throw new TypeError('D1 database binding is required');

  const canonicalSelect = `
    SELECT event_id, event_type, entity_type, entity_id, entity_version, actor_user_id,
           device_id, device_seq, idempotency_key, payload_json, ingested_at
    FROM domain_events`;

  return {
    async getEdgeEvent(eventId) {
      return mapEdge(await db.prepare(`
        SELECT edge_event_id, status, canonical_event_id, conflict_id, last_error_code
        FROM edge_event_ingest WHERE edge_event_id=? LIMIT 1
      `).bind(eventId).first());
    },
    async getCanonicalEventById(eventId) {
      return mapCanonical(await db.prepare(`${canonicalSelect} WHERE event_id=? LIMIT 1`).bind(eventId).first());
    },
    async getCanonicalEventByIdempotency(idempotencyKey) {
      return mapCanonical(await db.prepare(`${canonicalSelect} WHERE idempotency_key=? LIMIT 1`).bind(idempotencyKey).first());
    },
    async hasProjectionOutbox(eventId) {
      return Boolean(await db.prepare('SELECT 1 AS ok FROM projection_outbox WHERE event_id=? LIMIT 1').bind(eventId).first());
    },
    async actorExists(userId) {
      return Boolean(await db.prepare("SELECT 1 AS ok FROM auth_users WHERE user_id=? AND status IN ('ACTIVE','LOCKED','DISABLED') LIMIT 1").bind(userId).first());
    },
    async clusterExists(clusterId) {
      return Boolean(await db.prepare("SELECT 1 AS ok FROM clusters WHERE cluster_id=? AND status='ACTIVE' LIMIT 1").bind(clusterId).first());
    },
    async deviceExists(deviceId) {
      return Boolean(await db.prepare("SELECT 1 AS ok FROM device_registry WHERE device_id=? AND status IN ('ACTIVE','BLOCKED','RETIRED') LIMIT 1").bind(deviceId).first());
    },
    async getEmployee(employeeId) {
      const row = await db.prepare('SELECT employee_id, entity_version, status FROM employees WHERE employee_id=? LIMIT 1').bind(employeeId).first();
      return row ? { employeeId: row.employee_id, entityVersion: Number(row.entity_version), status: row.status } : null;
    },
    async recordConflict(envelope, reason, candidateEventIds, nowIso) {
      const conflictId = `sync:${envelope.eventId}`;
      const candidates = JSON.stringify(Array.isArray(candidateEventIds) ? candidateEventIds : []);
      await db.batch([
        db.prepare(`
          INSERT INTO conflict_corrections(conflict_id, cluster_id, entity_type, entity_id, candidate_event_ids_json, state, decision_reason, opened_at)
          VALUES (?, ?, ?, ?, ?, 'OPEN', ?, ?)
          ON CONFLICT(conflict_id) DO NOTHING
        `).bind(conflictId, envelope.clusterId, envelope.entityType, envelope.entityId, candidates, reason, nowIso),
        db.prepare(`
          UPDATE edge_event_ingest
          SET status='CONFLICT', conflict_id=?, last_error_code=?, updated_at=?
          WHERE edge_event_id=? AND status='RECEIVED'
        `).bind(conflictId, reason, nowIso, envelope.eventId)
      ]);
      return conflictId;
    },
    async linkExistingCanonical(envelope, canonicalEventId, nowIso) {
      await db.batch([
        db.prepare(`
          UPDATE integration_receipts SET event_id=?, updated_at=?
          WHERE edge_event_id=? AND status='COMPLETED' AND (event_id IS NULL OR event_id=?)
        `).bind(canonicalEventId, nowIso, envelope.eventId, canonicalEventId),
        db.prepare(`
          UPDATE edge_event_ingest
          SET status='RECONCILED', canonical_event_id=?, last_error_code=NULL, updated_at=?
          WHERE edge_event_id=? AND status='RECEIVED'
        `).bind(canonicalEventId, nowIso, envelope.eventId),
        db.prepare(`
          INSERT INTO edge_sync_checkpoints(edge_source_id, last_edge_event_id, last_local_order, last_authority_snapshot_version, last_reconciled_at, updated_at)
          SELECT edge_source_id, edge_event_id,
                 CASE WHEN changes()=1 THEN local_order ELSE -1 END,
                 authority_snapshot_version, ?, ?
          FROM edge_event_ingest WHERE edge_event_id=?
          ON CONFLICT(edge_source_id) DO UPDATE SET
            last_edge_event_id=excluded.last_edge_event_id,
            last_local_order=excluded.last_local_order,
            last_authority_snapshot_version=excluded.last_authority_snapshot_version,
            last_reconciled_at=excluded.last_reconciled_at,
            updated_at=excluded.updated_at
        `).bind(nowIso, nowIso, envelope.eventId)
      ]);
    },
    async commitEmployeeCreate({ envelope, state, canonicalEventId, projectionPayloadJson, nowIso }) {
      const hasGoogleReceipt = Boolean(await db.prepare(`
        SELECT 1 AS ok FROM integration_receipts
        WHERE edge_event_id=? AND target_kind='GOOGLE_SHEETS' AND status='COMPLETED' LIMIT 1
      `).bind(envelope.eventId).first());
      const projectionStatus = hasGoogleReceipt ? 'ACKED' : 'PENDING';

      await db.batch([
        db.prepare(`
          INSERT INTO employees(
            employee_id, full_name, phone, status, main_position, vendor, department, site, warehouse,
            start_date, permanent_leave_date, note, entity_version, created_at, updated_at
          )
          SELECT ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, ?, ?
          WHERE NOT EXISTS (SELECT 1 FROM employees WHERE employee_id=?)
        `).bind(
          state.employeeId, state.fullName, state.phone, state.status, state.mainPosition, state.vendor,
          state.department, state.site, state.warehouse, state.startDate, state.permanentLeaveDate,
          state.note, nowIso, nowIso, state.employeeId
        ),
        db.prepare(`
          INSERT INTO domain_events(
            event_id, event_type, entity_type, entity_id, entity_version, cluster_id, business_date,
            actor_user_id, actor_employee_id, device_id, device_seq, idempotency_key, causation_event_id,
            correlation_id, payload_json, app_version, occurred_at
          ) VALUES (?, ?, ?, ?, CASE WHEN changes()=1 THEN 1 ELSE 0 END, ?, NULL, ?, NULL, ?, ?, ?, NULL, ?, ?, ?, ?)
        `).bind(
          canonicalEventId, envelope.eventCode, envelope.entityType, envelope.entityId, envelope.clusterId,
          envelope.actorUserId, envelope.deviceId ?? null, envelope.deviceSeq ?? null, envelope.idempotencyKey,
          envelope.requestId, envelope.payloadJson, RECONCILIATION_FINALITY_VERSION, envelope.acceptedAt
        ),
        db.prepare(`
          INSERT INTO projection_outbox(event_id, projection_target, payload_json, status, attempts, next_attempt_at, created_at, updated_at)
          VALUES (?, 'GOOGLE_SHEETS', ?, ?, 0, NULL, ?, ?)
        `).bind(canonicalEventId, projectionPayloadJson, projectionStatus, nowIso, nowIso),
        db.prepare(`
          UPDATE integration_receipts SET event_id=?, updated_at=?
          WHERE edge_event_id=? AND status='COMPLETED' AND (event_id IS NULL OR event_id=?)
        `).bind(canonicalEventId, nowIso, envelope.eventId, canonicalEventId),
        db.prepare(`
          UPDATE edge_event_ingest
          SET status='RECONCILED', canonical_event_id=?, last_error_code=NULL, updated_at=?
          WHERE edge_event_id=? AND status='RECEIVED'
        `).bind(canonicalEventId, nowIso, envelope.eventId),
        db.prepare(`
          INSERT INTO edge_sync_checkpoints(edge_source_id, last_edge_event_id, last_local_order, last_authority_snapshot_version, last_reconciled_at, updated_at)
          SELECT edge_source_id, edge_event_id,
                 CASE WHEN changes()=1 THEN local_order ELSE -1 END,
                 authority_snapshot_version, ?, ?
          FROM edge_event_ingest WHERE edge_event_id=?
          ON CONFLICT(edge_source_id) DO UPDATE SET
            last_edge_event_id=excluded.last_edge_event_id,
            last_local_order=excluded.last_local_order,
            last_authority_snapshot_version=excluded.last_authority_snapshot_version,
            last_reconciled_at=excluded.last_reconciled_at,
            updated_at=excluded.updated_at
        `).bind(nowIso, nowIso, envelope.eventId)
      ]);
    }
  };
}
