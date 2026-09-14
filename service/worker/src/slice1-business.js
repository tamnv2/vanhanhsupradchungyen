import { authorizePrincipal } from './permission-store.js';

export const SLICE1_CLOUD_COMMAND_PATH = '/api/v1/data/commands';
export const SLICE1_CLUSTER_ID = 'PICK_PACK_1291';
export const SLICE1_MODULE_ID = 'PICK_PACK';
export const SLICE1_LOGICAL_MODULE = 'IDENTITY_EMPLOYEE_ATTENDANCE';

const COMMANDS = Object.freeze({
  EMPLOYEE_CREATE: { entityType: 'employee', resourceCode: 'employee', actionCode: 'create', eventType: 'EMPLOYEE_CREATED' },
  EMPLOYEE_UPDATE: { entityType: 'employee', resourceCode: 'employee', actionCode: 'edit', eventType: 'EMPLOYEE_UPDATED' },
  EMPLOYEE_STATUS_CHANGE: { entityType: 'employee', resourceCode: 'employee', actionCode: 'status', eventType: 'EMPLOYEE_STATUS_CHANGED' },
  EMPLOYEE_CODE_ASSIGN: { entityType: 'employee_code', resourceCode: 'employee', actionCode: 'edit', eventType: 'EMPLOYEE_CODE_ASSIGNED' },
  EMPLOYEE_PORTRAIT_REPLACE: { entityType: 'employee', resourceCode: 'employee', actionCode: 'portrait', eventType: 'EMPLOYEE_PORTRAIT_REPLACED', blocked: true },
  ATTENDANCE_IN: { entityType: 'attendance', resourceCode: 'attendance', actionCode: 'scan', eventType: 'ATTENDANCE_IN' },
  ATTENDANCE_OUT: { entityType: 'attendance', resourceCode: 'attendance', actionCode: 'scan', eventType: 'ATTENDANCE_OUT' },
  ATTENDANCE_CORRECT: { entityType: 'attendance', resourceCode: 'attendance', actionCode: 'correct', eventType: 'ATTENDANCE_CORRECTED' }
});

const TOP_LEVEL_FIELDS = new Set([
  'requestId', 'idempotencyKey', 'commandCode', 'entityId', 'expectedEntityVersion', 'payload', 'deviceSeq'
]);
const CLIENT_AUTHORITY_FIELDS = new Set([
  'actor', 'actorUserId', 'authenticatedUserId', 'permission', 'permissionResource',
  'permissionAction', 'eventCode', 'eventIntent', 'entityType', 'stateKey', 'authoritySnapshotVersion'
]);
const EMPLOYEE_CREATE_FIELDS = new Set([
  'employeeId', 'fullName', 'phone', 'status', 'mainPosition', 'vendor', 'department',
  'site', 'warehouse', 'startDate', 'permanentLeaveDate', 'note'
]);
const EMPLOYEE_UPDATE_FIELDS = new Set([
  'employeeId', 'fullName', 'phone', 'mainPosition', 'vendor', 'department',
  'site', 'warehouse', 'startDate', 'permanentLeaveDate', 'note'
]);
const EMPLOYEE_STATUS_FIELDS = new Set(['employeeId', 'status', 'permanentLeaveDate']);
const EMPLOYEE_CODE_FIELDS = new Set(['employeeCodeId', 'employeeId', 'employeeCode']);
const ATTENDANCE_SCAN_FIELDS = new Set(['employeeId', 'businessDate', 'occurredAt', 'source']);
const ATTENDANCE_CORRECTION_FIELDS = new Set(['employeeId', 'businessDate', 'occurredAt', 'currentState', 'reason', 'source']);
const EMPLOYEE_STATUSES = new Set(['ACTIVE', 'INACTIVE', 'LEFT', 'ARCHIVED']);
const encoder = new TextEncoder();

export class CloudSlice1Error extends Error {
  constructor(code, message, status = 422, details = undefined) {
    super(message);
    this.name = 'CloudSlice1Error';
    this.code = code;
    this.status = status;
    this.details = details;
  }
}

function fail(code, message, status = 422, details = undefined) {
  throw new CloudSlice1Error(code, message, status, details);
}

function requiredText(value, field, max = 240) {
  if (typeof value !== 'string' || !value.trim() || value.trim().length > max) {
    fail('INVALID_INPUT', `${field} is required and must be valid text.`, 422, { field });
  }
  return value.trim();
}

function optionalText(value, field, max = 1000) {
  if (value === null || value === undefined) return null;
  if (typeof value !== 'string' || value.length > max) fail('INVALID_INPUT', `${field} is invalid.`, 422, { field });
  return value;
}

function safeExpectedVersion(value) {
  if (value === null || value === undefined) return null;
  const number = Number(value);
  if (!Number.isSafeInteger(number) || number < 1) fail('INVALID_INPUT', 'expectedEntityVersion must be a positive integer or null.');
  return number;
}

function safeDeviceSeq(value) {
  if (value === null || value === undefined) return null;
  const number = Number(value);
  if (!Number.isSafeInteger(number) || number < 1) fail('INVALID_INPUT', 'deviceSeq must be a positive integer.');
  return number;
}

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function rejectUnknown(value, allowed, code = 'INVALID_INPUT') {
  for (const key of Object.keys(value)) {
    if (!allowed.has(key)) fail(code, `Unsupported field: ${key}.`, 422, { field: key });
  }
}

function rejectClientAuthorityFields(payload) {
  for (const field of CLIENT_AUTHORITY_FIELDS) {
    if (Object.prototype.hasOwnProperty.call(payload, field)) {
      fail('CLIENT_AUTHORITY_FIELD_REJECTED', `Client-authoritative field is prohibited: ${field}.`, 422, { field });
    }
  }
}

function requirePayloadEntity(payload, field, entityId) {
  if (requiredText(payload[field], field) !== entityId) fail('INVALID_INPUT', `${field} must match entityId.`, 422, { field });
}

function requirePayloadText(payload, field, max = 1000) {
  return requiredText(payload[field], field, max);
}

function stableValue(value) {
  if (Array.isArray(value)) return value.map(stableValue);
  if (!isObject(value)) return value;
  return Object.fromEntries(Object.keys(value).sort().map(key => [key, stableValue(value[key])]));
}

export function canonicalJson(value) {
  return JSON.stringify(stableValue(value));
}

async function sha256Hex(value) {
  const digest = await crypto.subtle.digest('SHA-256', encoder.encode(value));
  return Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, '0')).join('');
}

function eventMetadata(envelope, payloadHash, principal) {
  return {
    schemaVersion: 'VHDCHY_CLOUD_SLICE1_EVENT_V1',
    logicalModule: SLICE1_LOGICAL_MODULE,
    requestId: envelope.requestId,
    idempotencyKey: envelope.idempotencyKey,
    commandCode: envelope.commandCode,
    entityId: envelope.entityId,
    expectedEntityVersion: envelope.expectedEntityVersion,
    payload: envelope.payload,
    payloadHash,
    actorUserId: principal.userId,
    deviceSeq: envelope.deviceSeq
  };
}

function parseEventMetadata(value) {
  try {
    const parsed = JSON.parse(String(value || '{}'));
    return isObject(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function mutationResult(envelope, command, eventId, version, alreadyAccepted = false) {
  return {
    ok: true,
    requestId: envelope.requestId,
    idempotencyKey: envelope.idempotencyKey,
    runtime: 'CLOUD',
    commitStatus: 'CLOUD_COMMITTED',
    googleOutputStatus: 'PENDING',
    eventId,
    entity: { type: command.entityType, id: envelope.entityId, version },
    cloud: { canonicalEventId: eventId, reconciledAt: null },
    lan: null,
    alreadyAccepted
  };
}

function normalizeEmployee(row) {
  if (!row) return null;
  return {
    employeeId: row.employee_id,
    fullName: row.full_name,
    phone: row.phone ?? null,
    status: row.status,
    mainPosition: row.main_position ?? null,
    vendor: row.vendor ?? null,
    department: row.department ?? null,
    site: row.site ?? null,
    warehouse: row.warehouse ?? null,
    startDate: row.start_date ?? null,
    permanentLeaveDate: row.permanent_leave_date ?? null,
    note: row.note ?? null,
    entityVersion: Number(row.entity_version)
  };
}

function normalizeEmployeeCode(row) {
  if (!row) return null;
  return {
    employeeCodeId: row.employee_code_id,
    employeeId: row.employee_id,
    employeeCode: row.employee_code,
    status: row.status,
    entityVersion: Number(row.entity_version)
  };
}

function normalizePresence(row) {
  if (!row) return null;
  return {
    employeeId: row.employee_id,
    currentState: row.current_state,
    businessDate: row.business_date ?? null,
    entityVersion: Number(row.entity_version)
  };
}

export function validateCloudCommandEnvelope(value) {
  if (!isObject(value)) fail('INVALID_INPUT', 'Command body must be a JSON object.');
  rejectUnknown(value, TOP_LEVEL_FIELDS);
  const commandCode = requiredText(value.commandCode, 'commandCode').toUpperCase();
  if (!COMMANDS[commandCode]) fail('UNSUPPORTED_COMMAND', 'Command is not part of the reviewed Slice-1 contract.', 422, { commandCode });
  const payload = value.payload;
  if (!isObject(payload)) fail('INVALID_INPUT', 'payload must be a JSON object.', 422, { field: 'payload' });
  rejectClientAuthorityFields(payload);
  return Object.freeze({
    requestId: requiredText(value.requestId, 'requestId'),
    idempotencyKey: requiredText(value.idempotencyKey, 'idempotencyKey'),
    commandCode,
    entityId: requiredText(value.entityId, 'entityId'),
    expectedEntityVersion: safeExpectedVersion(value.expectedEntityVersion),
    payload: structuredClone(payload),
    deviceSeq: safeDeviceSeq(value.deviceSeq)
  });
}

async function replayResult(store, envelope, principal, command, payloadHash) {
  const existing = await store.getEventByIdempotency(envelope.idempotencyKey);
  if (!existing) return null;
  const metadata = parseEventMetadata(existing.payload_json);
  const same = metadata?.schemaVersion === 'VHDCHY_CLOUD_SLICE1_EVENT_V1' &&
    metadata.commandCode === envelope.commandCode &&
    metadata.entityId === envelope.entityId &&
    (metadata.expectedEntityVersion ?? null) === (envelope.expectedEntityVersion ?? null) &&
    metadata.payloadHash === payloadHash &&
    metadata.actorUserId === principal.userId &&
    (metadata.deviceSeq ?? null) === (envelope.deviceSeq ?? null);
  if (!same) fail('IDEMPOTENCY_PAYLOAD_CONFLICT', 'The idempotency key is already bound to a different logical command.', 409);
  return mutationResult(envelope, command, existing.event_id, Number(existing.entity_version), true);
}

async function rejectDeviceSequenceCollision(store, envelope, principal) {
  if (envelope.deviceSeq == null) return;
  if (!principal.deviceId) fail('DEVICE_ID_REQUIRED', 'deviceSeq requires an authenticated device identity.', 422);
  const existing = await store.getEventByDeviceSequence(principal.deviceId, envelope.deviceSeq);
  if (existing && existing.idempotency_key !== envelope.idempotencyKey) {
    fail('DEVICE_SEQUENCE_COLLISION', 'The authenticated device sequence is already used by another command.', 409, { existingEventId: existing.event_id });
  }
}

function versionRequired(expected, current, message = 'Entity version conflict.') {
  if (expected == null || expected !== current) fail('VERSION_CONFLICT', message, 409, { expected, current });
}

function employeeCreatePlan(envelope) {
  const payload = envelope.payload;
  rejectUnknown(payload, EMPLOYEE_CREATE_FIELDS);
  requirePayloadEntity(payload, 'employeeId', envelope.entityId);
  const fullName = requirePayloadText(payload, 'fullName');
  if (envelope.expectedEntityVersion != null) fail('VERSION_CONFLICT', 'EMPLOYEE_CREATE requires no expected entity version.', 409);
  const status = payload.status == null ? 'ACTIVE' : requiredText(payload.status, 'status').toUpperCase();
  if (!EMPLOYEE_STATUSES.has(status)) fail('INVALID_INPUT', 'Employee status must be ACTIVE, INACTIVE, LEFT, or ARCHIVED.');
  return {
    kind: 'EMPLOYEE_CREATE',
    version: 1,
    state: {
      employeeId: envelope.entityId,
      fullName,
      phone: optionalText(payload.phone, 'phone'),
      status,
      mainPosition: optionalText(payload.mainPosition, 'mainPosition'),
      vendor: optionalText(payload.vendor, 'vendor'),
      department: optionalText(payload.department, 'department'),
      site: optionalText(payload.site, 'site'),
      warehouse: optionalText(payload.warehouse, 'warehouse'),
      startDate: optionalText(payload.startDate, 'startDate'),
      permanentLeaveDate: optionalText(payload.permanentLeaveDate, 'permanentLeaveDate'),
      note: optionalText(payload.note, 'note', 4000)
    }
  };
}

function mergeEmployee(current, payload) {
  const next = { ...current };
  const map = {
    fullName: 'fullName', phone: 'phone', mainPosition: 'mainPosition', vendor: 'vendor', department: 'department',
    site: 'site', warehouse: 'warehouse', startDate: 'startDate', permanentLeaveDate: 'permanentLeaveDate', note: 'note'
  };
  for (const [key, target] of Object.entries(map)) {
    if (Object.prototype.hasOwnProperty.call(payload, key)) next[target] = payload[key] == null ? null : optionalText(payload[key], key, key === 'note' ? 4000 : 1000);
  }
  if (!next.fullName || !String(next.fullName).trim()) fail('INVALID_INPUT', 'fullName cannot be empty.');
  next.fullName = String(next.fullName).trim();
  return next;
}

async function preparePlan(store, envelope) {
  const payload = envelope.payload;
  switch (envelope.commandCode) {
    case 'EMPLOYEE_CREATE': {
      const existing = await store.getEmployee(envelope.entityId);
      if (existing) fail('VERSION_CONFLICT', 'Employee already exists.', 409, { current: existing.entityVersion });
      return employeeCreatePlan(envelope);
    }
    case 'EMPLOYEE_UPDATE': {
      rejectUnknown(payload, EMPLOYEE_UPDATE_FIELDS);
      requirePayloadEntity(payload, 'employeeId', envelope.entityId);
      if (Object.keys(payload).length <= 1) fail('INVALID_INPUT', 'EMPLOYEE_UPDATE requires at least one profile field to update.');
      const current = await store.getEmployee(envelope.entityId);
      if (!current) fail('NOT_FOUND', 'Employee does not exist.', 404);
      versionRequired(envelope.expectedEntityVersion, current.entityVersion);
      return { kind: 'EMPLOYEE_UPDATE', version: current.entityVersion + 1, priorVersion: current.entityVersion, state: { ...mergeEmployee(current, payload), entityVersion: current.entityVersion + 1 } };
    }
    case 'EMPLOYEE_STATUS_CHANGE': {
      rejectUnknown(payload, EMPLOYEE_STATUS_FIELDS);
      requirePayloadEntity(payload, 'employeeId', envelope.entityId);
      const status = requiredText(payload.status, 'status').toUpperCase();
      if (!EMPLOYEE_STATUSES.has(status)) fail('INVALID_INPUT', 'Employee status must be ACTIVE, INACTIVE, LEFT, or ARCHIVED.');
      const current = await store.getEmployee(envelope.entityId);
      if (!current) fail('NOT_FOUND', 'Employee does not exist.', 404);
      versionRequired(envelope.expectedEntityVersion, current.entityVersion);
      if (current.status === status) fail('INVALID_INPUT', 'Employee already has the requested status.');
      const state = { ...current, status, entityVersion: current.entityVersion + 1 };
      if (Object.prototype.hasOwnProperty.call(payload, 'permanentLeaveDate')) state.permanentLeaveDate = optionalText(payload.permanentLeaveDate, 'permanentLeaveDate');
      return { kind: 'EMPLOYEE_STATUS_CHANGE', version: state.entityVersion, priorVersion: current.entityVersion, state };
    }
    case 'EMPLOYEE_CODE_ASSIGN': {
      rejectUnknown(payload, EMPLOYEE_CODE_FIELDS);
      requirePayloadEntity(payload, 'employeeCodeId', envelope.entityId);
      const employeeId = requirePayloadText(payload, 'employeeId');
      const employeeCode = requirePayloadText(payload, 'employeeCode');
      const employee = await store.getEmployee(employeeId);
      if (!employee || employee.status !== 'ACTIVE') fail('INVALID_INPUT', 'An ACTIVE employee is required before assigning an active employee code.');
      const current = await store.getEmployeeCode(envelope.entityId);
      if (!current) {
        if (envelope.expectedEntityVersion != null) fail('VERSION_CONFLICT', 'New employee-code identity requires no expected entity version.', 409);
      } else {
        versionRequired(envelope.expectedEntityVersion, current.entityVersion);
        if (current.employeeId !== employeeId) {
          const priorEmployee = await store.getEmployee(current.employeeId);
          if (!priorEmployee || !['INACTIVE', 'LEFT'].includes(priorEmployee.status)) {
            fail('INVALID_INPUT', 'An employee code cannot be reassigned while its previous holder is still active or not explicitly inactive/left.');
          }
        }
      }
      const active = await store.listActiveEmployeeCodes();
      for (const item of active) {
        if (item.employeeCodeId === envelope.entityId) continue;
        if (item.employeeCode === employeeCode) fail('RESOURCE_NOT_AVAILABLE', 'The requested employee code is already active.', 409);
        if (item.employeeId === employeeId) fail('RESOURCE_NOT_AVAILABLE', 'The employee already has another active employee code.', 409);
      }
      return {
        kind: 'EMPLOYEE_CODE_ASSIGN',
        version: current ? current.entityVersion + 1 : 1,
        priorVersion: current?.entityVersion ?? null,
        isCreate: !current,
        state: { employeeCodeId: envelope.entityId, employeeId, employeeCode, status: 'ACTIVE', entityVersion: current ? current.entityVersion + 1 : 1 }
      };
    }
    case 'ATTENDANCE_IN':
    case 'ATTENDANCE_OUT': {
      rejectUnknown(payload, ATTENDANCE_SCAN_FIELDS);
      requirePayloadEntity(payload, 'employeeId', envelope.entityId);
      const businessDate = requirePayloadText(payload, 'businessDate');
      const employee = await store.getEmployee(envelope.entityId);
      if (!employee || employee.status !== 'ACTIVE') fail('INVALID_INPUT', 'Attendance scan requires an ACTIVE employee.');
      const targetState = envelope.commandCode === 'ATTENDANCE_IN' ? 'IN' : 'OUT';
      const current = await store.getPresence(envelope.entityId);
      if (!current) {
        if (targetState === 'OUT') fail('INVALID_INPUT', 'ATTENDANCE_OUT requires a valid preceding IN presence state.');
        if (envelope.expectedEntityVersion != null) fail('VERSION_CONFLICT', 'No presence state exists for the supplied expected version.', 409);
      } else {
        versionRequired(envelope.expectedEntityVersion, current.entityVersion);
        if (targetState === 'OUT' && current.currentState !== 'IN') fail('INVALID_INPUT', 'ATTENDANCE_OUT requires a valid preceding IN presence state.');
      }
      return {
        kind: 'ATTENDANCE',
        version: current ? current.entityVersion + 1 : 1,
        priorVersion: current?.entityVersion ?? null,
        isCreate: !current,
        requirePriorState: targetState === 'OUT' ? 'IN' : null,
        state: {
          employeeId: envelope.entityId,
          currentState: targetState,
          businessDate,
          lastOccurredAt: payload.occurredAt == null ? null : requiredText(payload.occurredAt, 'occurredAt'),
          lastSource: payload.source == null ? 'CLOUD' : requiredText(payload.source, 'source'),
          entityVersion: current ? current.entityVersion + 1 : 1
        }
      };
    }
    case 'ATTENDANCE_CORRECT': {
      rejectUnknown(payload, ATTENDANCE_CORRECTION_FIELDS);
      requirePayloadEntity(payload, 'employeeId', envelope.entityId);
      const targetState = requirePayloadText(payload, 'currentState').toUpperCase();
      if (!['IN', 'OUT'].includes(targetState)) fail('INVALID_INPUT', 'Attendance correction currentState must be IN or OUT.');
      const reason = requirePayloadText(payload, 'reason', 2000);
      const businessDate = requirePayloadText(payload, 'businessDate');
      const employee = await store.getEmployee(envelope.entityId);
      if (!employee) fail('NOT_FOUND', 'Employee does not exist.', 404);
      const current = await store.getPresence(envelope.entityId);
      if (!current) fail('NOT_FOUND', 'Presence state does not exist.', 404);
      versionRequired(envelope.expectedEntityVersion, current.entityVersion);
      return {
        kind: 'ATTENDANCE_CORRECT',
        version: current.entityVersion + 1,
        priorVersion: current.entityVersion,
        isCreate: false,
        requirePriorState: null,
        state: {
          employeeId: envelope.entityId,
          currentState: targetState,
          businessDate,
          lastOccurredAt: payload.occurredAt == null ? null : requiredText(payload.occurredAt, 'occurredAt'),
          lastSource: payload.source == null ? 'CLOUD' : requiredText(payload.source, 'source'),
          lastCorrectionReason: reason,
          entityVersion: current.entityVersion + 1
        }
      };
    }
    case 'EMPLOYEE_PORTRAIT_REPLACE':
      fail('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Employee portrait replacement remains fail-closed until the reviewed Owner media-lifecycle gate is resolved.', 503);
    default:
      fail('UNSUPPORTED_COMMAND', 'Unsupported Slice-1 command.', 422);
  }
}

function projectionPayload({ envelope, command, eventId, version, occurredAt, principal }) {
  return JSON.stringify({
    schemaVersion: 'VHDCHY_GOOGLE_PROJECTION_EVENT_V1',
    eventId,
    eventType: command.eventType,
    entityType: command.entityType,
    entityId: envelope.entityId,
    entityVersion: version,
    clusterId: SLICE1_CLUSTER_ID,
    actorUserId: principal.userId,
    deviceId: principal.deviceId ?? null,
    occurredAt,
    payload: envelope.payload
  });
}

export function createD1Slice1BusinessStore(db) {
  if (!db || typeof db.prepare !== 'function' || typeof db.batch !== 'function') throw new TypeError('D1 database binding is required');
  return {
    async getEmployee(id) {
      return normalizeEmployee(await db.prepare(`SELECT employee_id, full_name, phone, status, main_position, vendor, department, site, warehouse, start_date, permanent_leave_date, note, entity_version FROM employees WHERE employee_id=? LIMIT 1`).bind(id).first());
    },
    async getEmployeeCode(id) {
      return normalizeEmployeeCode(await db.prepare(`SELECT employee_code_id, employee_id, employee_code, status, entity_version FROM employee_codes WHERE employee_code_id=? LIMIT 1`).bind(id).first());
    },
    async listActiveEmployeeCodes() {
      const result = await db.prepare(`SELECT employee_code_id, employee_id, employee_code, status, entity_version FROM employee_codes WHERE status='ACTIVE' ORDER BY employee_code_id`).all();
      return (Array.isArray(result?.results) ? result.results : []).map(normalizeEmployeeCode);
    },
    async getPresence(employeeId) {
      return normalizePresence(await db.prepare(`SELECT employee_id, current_state, business_date, entity_version FROM presence_state WHERE employee_id=? LIMIT 1`).bind(employeeId).first());
    },
    async getEventByIdempotency(key) {
      return db.prepare(`SELECT event_id, event_type, entity_type, entity_id, entity_version, actor_user_id, device_id, device_seq, idempotency_key, payload_json FROM domain_events WHERE idempotency_key=? LIMIT 1`).bind(key).first();
    },
    async getEventByDeviceSequence(deviceId, deviceSeq) {
      return db.prepare(`SELECT event_id, idempotency_key FROM domain_events WHERE device_id=? AND device_seq=? LIMIT 1`).bind(deviceId, deviceSeq).first();
    },
    async commit({ envelope, command, plan, eventId, attendanceEventId, principal, metadataJson, projectionJson, occurredAt }) {
      const s = [];
      if (plan.kind === 'EMPLOYEE_CREATE') {
        const v = plan.state;
        s.push(db.prepare(`INSERT INTO employees(employee_id, full_name, phone, status, main_position, vendor, department, site, warehouse, start_date, permanent_leave_date, note, entity_version, created_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, ?, ?)`).bind(v.employeeId, v.fullName, v.phone, v.status, v.mainPosition, v.vendor, v.department, v.site, v.warehouse, v.startDate, v.permanentLeaveDate, v.note, occurredAt, occurredAt));
      } else if (plan.kind === 'EMPLOYEE_UPDATE' || plan.kind === 'EMPLOYEE_STATUS_CHANGE') {
        const v = plan.state;
        s.push(db.prepare(`UPDATE employees SET full_name=?, phone=?, status=?, main_position=?, vendor=?, department=?, site=?, warehouse=?, start_date=?, permanent_leave_date=?, note=?, entity_version=?, updated_at=? WHERE employee_id=? AND entity_version=?`).bind(v.fullName, v.phone, v.status, v.mainPosition, v.vendor, v.department, v.site, v.warehouse, v.startDate, v.permanentLeaveDate, v.note, plan.version, occurredAt, envelope.entityId, plan.priorVersion));
      } else if (plan.kind === 'EMPLOYEE_CODE_ASSIGN') {
        const v = plan.state;
        if (plan.isCreate) {
          s.push(db.prepare(`INSERT INTO employee_codes(employee_code_id, employee_id, employee_code, status, assigned_at, entity_version, created_by_user_id) VALUES (?, ?, ?, 'ACTIVE', ?, 1, ?)`).bind(v.employeeCodeId, v.employeeId, v.employeeCode, occurredAt, principal.userId));
        } else {
          s.push(db.prepare(`UPDATE employee_codes SET employee_id=?, employee_code=?, status='ACTIVE', released_at=NULL, release_reason=NULL, entity_version=? WHERE employee_code_id=? AND entity_version=?`).bind(v.employeeId, v.employeeCode, plan.version, v.employeeCodeId, plan.priorVersion));
        }
      } else if (plan.kind === 'ATTENDANCE' || plan.kind === 'ATTENDANCE_CORRECT') {
        const v = plan.state;
        const attendanceMetadata = JSON.stringify({ commandCode: envelope.commandCode, correctionReason: v.lastCorrectionReason ?? null, canonicalEventId: eventId });
        s.push(db.prepare(`INSERT INTO attendance_events(attendance_event_id, employee_id, cluster_id, business_date, event_type, occurred_at, actor_user_id, device_id, source, idempotency_key, metadata_json) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`).bind(attendanceEventId, envelope.entityId, SLICE1_CLUSTER_ID, v.businessDate, v.currentState, v.lastOccurredAt || occurredAt, principal.userId, principal.deviceId ?? null, v.lastSource || 'CLOUD', envelope.idempotencyKey, attendanceMetadata));
        if (plan.isCreate) {
          s.push(db.prepare(`INSERT INTO presence_state(employee_id, current_state, last_attendance_event_id, cluster_id, business_date, updated_at, entity_version) VALUES (?, ?, ?, ?, ?, ?, 1)`).bind(envelope.entityId, v.currentState, attendanceEventId, SLICE1_CLUSTER_ID, v.businessDate, occurredAt));
        } else {
          let sql = `UPDATE presence_state SET current_state=?, last_attendance_event_id=?, cluster_id=?, business_date=?, updated_at=?, entity_version=? WHERE employee_id=? AND entity_version=?`;
          const values = [v.currentState, attendanceEventId, SLICE1_CLUSTER_ID, v.businessDate, occurredAt, plan.version, envelope.entityId, plan.priorVersion];
          if (plan.requirePriorState) { sql += ` AND current_state=?`; values.push(plan.requirePriorState); }
          s.push(db.prepare(sql).bind(...values));
        }
      }

      const businessDate = plan.state.businessDate ?? null;
      let entityGuardSql;
      let entityGuardValues;
      if (command.entityType === 'employee') {
        entityGuardSql = `SELECT ? AS event_id, ? AS event_type, 'employee' AS entity_type, employee_id AS entity_id, entity_version FROM employees WHERE employee_id=? AND entity_version=?`;
        entityGuardValues = [eventId, command.eventType, envelope.entityId, plan.version];
      } else if (command.entityType === 'employee_code') {
        entityGuardSql = `SELECT ? AS event_id, ? AS event_type, 'employee_code' AS entity_type, employee_code_id AS entity_id, entity_version FROM employee_codes WHERE employee_code_id=? AND entity_version=?`;
        entityGuardValues = [eventId, command.eventType, envelope.entityId, plan.version];
      } else {
        entityGuardSql = `SELECT ? AS event_id, ? AS event_type, 'attendance' AS entity_type, employee_id AS entity_id, entity_version FROM presence_state WHERE employee_id=? AND entity_version=?`;
        entityGuardValues = [eventId, command.eventType, envelope.entityId, plan.version];
      }
      s.push(db.prepare(`INSERT INTO domain_events(event_id, event_type, entity_type, entity_id, entity_version, cluster_id, business_date, actor_user_id, actor_employee_id, device_id, device_seq, idempotency_key, correlation_id, payload_json, app_version, occurred_at) ${entityGuardSql.replace('SELECT ? AS event_id, ? AS event_type,', `SELECT ? AS event_id, ? AS event_type,`).replace(' FROM ', `, ? AS cluster_id, ? AS business_date, ? AS actor_user_id, ? AS actor_employee_id, ? AS device_id, ? AS device_seq, ? AS idempotency_key, ? AS correlation_id, ? AS payload_json, ? AS app_version, ? AS occurred_at FROM `)}`).bind(
        entityGuardValues[0], entityGuardValues[1],
        SLICE1_CLUSTER_ID, businessDate, principal.userId, principal.employeeId ?? null, principal.deviceId ?? null, envelope.deviceSeq, envelope.idempotencyKey, envelope.requestId, metadataJson, 'WEB_V7', occurredAt,
        ...entityGuardValues.slice(2)
      ));
      s.push(db.prepare(`INSERT INTO projection_outbox(event_id, projection_target, payload_json, status, attempts, next_attempt_at, created_at, updated_at) VALUES (?, 'GOOGLE_SHEETS', ?, 'PENDING', 0, NULL, ?, ?)`).bind(eventId, projectionJson, occurredAt, occurredAt));
      await db.batch(s);
    }
  };
}

function looksLikeConstraint(error) {
  return /constraint|unique|foreign key|SQLITE_CONSTRAINT/i.test(String(error?.message || error || ''));
}

export async function executeCloudSlice1Command(options) {
  const db = options?.db;
  const principal = options?.principal;
  if (!principal?.userId) fail('AUTH_REQUIRED', 'Authentication is required.', 401);
  const envelope = validateCloudCommandEnvelope(options?.envelope);
  const command = COMMANDS[envelope.commandCode];
  if (command.blocked) fail('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Employee portrait replacement remains fail-closed until Owner media lifecycle semantics are resolved.', 503);

  const authorization = await (options?.authorizePrincipal || authorizePrincipal)(db, principal, {
    resourceCode: command.resourceCode,
    actionCode: command.actionCode
  }, { clusterId: SLICE1_CLUSTER_ID, moduleId: SLICE1_MODULE_ID });
  if (!authorization?.allowed) fail('PERMISSION_DENIED', 'The authenticated account is not permitted to execute this command.', 403, { reason: authorization?.reason || 'DENIED' });

  const store = options?.store || createD1Slice1BusinessStore(db);
  const payloadHash = await sha256Hex(canonicalJson(envelope.payload));
  const replay = await replayResult(store, envelope, principal, command, payloadHash);
  if (replay) return replay;
  await rejectDeviceSequenceCollision(store, envelope, principal);

  const plan = await preparePlan(store, envelope);
  const nowMs = Number.isFinite(Number(options?.nowMs)) ? Number(options.nowMs) : Date.now();
  const occurredAt = new Date(nowMs).toISOString();
  const uuid = options?.uuid || (() => crypto.randomUUID());
  const eventId = uuid();
  const attendanceEventId = (plan.kind === 'ATTENDANCE' || plan.kind === 'ATTENDANCE_CORRECT') ? uuid() : null;
  const metadataJson = JSON.stringify(eventMetadata(envelope, payloadHash, principal));
  const projectionJson = projectionPayload({ envelope, command, eventId, version: plan.version, occurredAt, principal });

  try {
    await store.commit({ envelope, command, plan, eventId, attendanceEventId, principal, metadataJson, projectionJson, occurredAt });
  } catch (error) {
    const racedReplay = await replayResult(store, envelope, principal, command, payloadHash);
    if (racedReplay) return racedReplay;
    await rejectDeviceSequenceCollision(store, envelope, principal);
    if (looksLikeConstraint(error) && envelope.commandCode === 'EMPLOYEE_CODE_ASSIGN') {
      fail('RESOURCE_NOT_AVAILABLE', 'Employee-code uniqueness claim is no longer available.', 409);
    }
    if (looksLikeConstraint(error)) fail('VERSION_CONFLICT', 'Concurrent state changed before the atomic commit completed.', 409);
    throw error;
  }

  return mutationResult(envelope, command, eventId, plan.version, false);
}
