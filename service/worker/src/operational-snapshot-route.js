import { EDGE_SCHEMA_VERSION, reconciliationSourceId } from './reconciliation.js';
import {
  RECONCILIATION_AUTH_VERSION,
  verifyReconciliationRequestSignature
} from './reconciliation-request-auth.js';
import { DOMAIN_CONTRACT_VERSION } from './runtime-contract.js';

export const OPERATIONAL_SNAPSHOT_PATH = '/api/v1/reconciliation/operational-snapshot';
export const OPERATIONAL_SNAPSHOT_CONTRACT_VERSION = 'VHDCHY_OPERATIONAL_SNAPSHOT_V1';
export const SLICE1_OPERATIONAL_MODULE = 'IDENTITY_EMPLOYEE_ATTENDANCE';

const MAX_REQUEST_BODY_BYTES = 256 * 1024;
const MAX_COVERAGE_IDS = 10_000;
const encoder = new TextEncoder();

function reply(payload, status, requestId) {
  return Response.json(requestId ? { ...payload, requestId } : payload, {
    status,
    headers: {
      'cache-control': 'no-store',
      'x-content-type-options': 'nosniff'
    }
  });
}

function failure(code, reason, status, requestId, details = undefined) {
  const error = { code, reason };
  if (details !== undefined) error.details = details;
  return reply({ ok: false, error }, status, requestId);
}

function text(value, max = 300) {
  return typeof value === 'string' && value.trim().length > 0 && value.trim().length <= max;
}

async function readRawBody(request, requestId) {
  const declaredLength = Number(request.headers.get('content-length') || 0);
  if (Number.isFinite(declaredLength) && declaredLength > MAX_REQUEST_BODY_BYTES) {
    return { response: failure('REQUEST_BODY_TOO_LARGE', 'BODY_LIMIT_EXCEEDED', 413, requestId), rawBody: null };
  }

  let rawBody;
  try {
    rawBody = await request.text();
  } catch {
    return { response: failure('REQUEST_BODY_INVALID', 'BODY_READ_FAILED', 400, requestId), rawBody: null };
  }

  if (encoder.encode(rawBody).byteLength > MAX_REQUEST_BODY_BYTES) {
    return { response: failure('REQUEST_BODY_TOO_LARGE', 'BODY_LIMIT_EXCEEDED', 413, requestId), rawBody: null };
  }
  return { response: null, rawBody };
}

function validateSnapshotRequest(value, expectedEnvironment) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'REQUEST_OBJECT_REQUIRED' };
  }

  const required = ['environment', 'clusterId', 'edgeInstanceId', 'edgeEpoch', 'domainContractVersion', 'edgeSchemaVersion'];
  const missing = required.filter(field => !text(value[field]));
  if (missing.length) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'REQUIRED_FIELDS_MISSING', details: { missing } };
  }

  if (value.environment !== expectedEnvironment) {
    return { ok: false, code: 'SCHEMA_INCOMPATIBLE', status: 409, reason: 'ENVIRONMENT_MISMATCH' };
  }
  if (value.domainContractVersion !== DOMAIN_CONTRACT_VERSION) {
    return { ok: false, code: 'SCHEMA_INCOMPATIBLE', status: 409, reason: 'DOMAIN_CONTRACT_MISMATCH' };
  }
  if (value.edgeSchemaVersion !== EDGE_SCHEMA_VERSION) {
    return { ok: false, code: 'SCHEMA_INCOMPATIBLE', status: 409, reason: 'EDGE_SCHEMA_MISMATCH' };
  }
  if (value.mode != null && value.mode !== 'FULL') {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'SNAPSHOT_MODE_UNSUPPORTED' };
  }

  const requested = value.requestedCanonicalEventIds ?? [];
  if (!Array.isArray(requested) || requested.length > MAX_COVERAGE_IDS) {
    return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'CANONICAL_COVERAGE_REQUEST_INVALID' };
  }
  const normalized = [];
  const seen = new Set();
  for (const id of requested) {
    if (!text(id, 240) || seen.has(id)) {
      return { ok: false, code: 'INVALID_INPUT', status: 422, reason: 'CANONICAL_COVERAGE_REQUEST_INVALID' };
    }
    seen.add(id);
    normalized.push(id);
  }

  return {
    ok: true,
    request: {
      environment: value.environment,
      clusterId: value.clusterId,
      edgeInstanceId: value.edgeInstanceId,
      edgeEpoch: value.edgeEpoch,
      domainContractVersion: value.domainContractVersion,
      edgeSchemaVersion: value.edgeSchemaVersion,
      requestedCanonicalEventIds: normalized
    }
  };
}

function rows(result) {
  return Array.isArray(result?.results) ? result.results : [];
}

function mapEmployees(result) {
  return rows(result).map(row => ({
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
    currentPortraitMediaId: row.current_portrait_media_id ?? null,
    entityVersion: Number(row.entity_version)
  }));
}

function mapEmployeeCodes(result) {
  return rows(result).map(row => ({
    employeeCodeId: row.employee_code_id,
    employeeId: row.employee_id,
    employeeCode: row.employee_code,
    status: row.status,
    assignedAt: row.assigned_at,
    releasedAt: row.released_at ?? null,
    releaseReason: row.release_reason ?? null,
    entityVersion: Number(row.entity_version)
  }));
}

function mapPresence(result) {
  return rows(result).map(row => ({
    employeeId: row.employee_id,
    currentState: row.current_state,
    lastAttendanceEventId: row.last_attendance_event_id ?? null,
    clusterId: row.cluster_id ?? null,
    businessDate: row.business_date ?? null,
    updatedAt: row.updated_at,
    entityVersion: Number(row.entity_version)
  }));
}

function mapSource(result) {
  const row = rows(result)[0];
  if (!row) return null;
  return {
    sourceId: row.edge_source_id,
    environment: row.environment,
    clusterId: row.cluster_id,
    edgeInstanceId: row.edge_instance_id,
    edgeEpoch: row.edge_epoch,
    domainContractVersion: row.domain_contract_version,
    edgeSchemaVersion: row.edge_schema_version,
    status: row.status
  };
}

function sourceMatches(source, request) {
  return source &&
    source.environment === request.environment &&
    source.clusterId === request.clusterId &&
    source.edgeInstanceId === request.edgeInstanceId &&
    source.edgeEpoch === request.edgeEpoch &&
    source.domainContractVersion === request.domainContractVersion &&
    source.edgeSchemaVersion === request.edgeSchemaVersion;
}

async function sha256Hex(value) {
  const digest = await crypto.subtle.digest('SHA-256', encoder.encode(value));
  return Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, '0')).join('');
}

function canonicalSnapshotMaterial({ request, scopeJson, stateJson, coveredCanonicalEventIds }) {
  return [
    OPERATIONAL_SNAPSHOT_CONTRACT_VERSION,
    request.environment,
    request.clusterId,
    request.edgeInstanceId,
    request.edgeEpoch,
    request.domainContractVersion,
    request.edgeSchemaVersion,
    scopeJson,
    stateJson,
    JSON.stringify(coveredCanonicalEventIds)
  ].join('\n');
}

export function createD1OperationalSnapshotStore(db) {
  if (!db || typeof db.prepare !== 'function' || typeof db.batch !== 'function') {
    throw new TypeError('D1 database binding is required');
  }

  return {
    async readSnapshot(request) {
      const sourceId = reconciliationSourceId(request);
      const coverageJson = JSON.stringify(request.requestedCanonicalEventIds);
      const statements = [
        db.prepare(`
          SELECT edge_source_id, environment, cluster_id, edge_instance_id, edge_epoch,
                 domain_contract_version, edge_schema_version, status
          FROM edge_sources WHERE edge_source_id=? LIMIT 1
        `).bind(sourceId),
        db.prepare("SELECT cluster_id, status FROM clusters WHERE cluster_id=? LIMIT 1").bind(request.clusterId),
        db.prepare(`
          SELECT employee_id, full_name, phone, status, main_position, vendor, department,
                 site, warehouse, start_date, permanent_leave_date, note,
                 current_portrait_media_id, entity_version
          FROM employees
          ORDER BY employee_id
        `),
        db.prepare(`
          SELECT employee_code_id, employee_id, employee_code, status, assigned_at,
                 released_at, release_reason, entity_version
          FROM employee_codes
          ORDER BY employee_code_id
        `),
        db.prepare(`
          SELECT employee_id, current_state, last_attendance_event_id, cluster_id,
                 business_date, updated_at, entity_version
          FROM presence_state
          ORDER BY employee_id
        `),
        db.prepare(`
          SELECT e.canonical_event_id, d.ingested_at AS canonical_ingested_at
          FROM edge_event_ingest e
          INNER JOIN edge_sources s ON s.edge_source_id=e.edge_source_id
          INNER JOIN domain_events d ON d.event_id=e.canonical_event_id
          INNER JOIN json_each(?) requested ON requested.value=e.canonical_event_id
          WHERE s.environment=?
            AND s.cluster_id=?
            AND s.edge_instance_id=?
            AND s.status='ACTIVE'
            AND s.domain_contract_version=?
            AND s.edge_schema_version=?
            AND e.status='RECONCILED'
            AND e.canonical_event_id IS NOT NULL
            AND d.cluster_id=?
          ORDER BY e.updated_at, e.edge_event_id
        `).bind(
          coverageJson,
          request.environment,
          request.clusterId,
          request.edgeInstanceId,
          request.domainContractVersion,
          request.edgeSchemaVersion,
          request.clusterId)
      ];

      const result = await db.batch(statements);
      if (!Array.isArray(result) || result.length !== statements.length) {
        throw new Error('OPERATIONAL_SNAPSHOT_BATCH_INVALID');
      }

      return {
        sourceId,
        source: mapSource(result[0]),
        cluster: rows(result[1])[0] ?? null,
        employees: mapEmployees(result[2]),
        employeeCodes: mapEmployeeCodes(result[3]),
        presence: mapPresence(result[4]),
        coveredCanonicalEventIds: rows(result[5]).map(row => row.canonical_event_id)
      };
    }
  };
}

export async function buildOperationalSnapshot(store, request) {
  const read = await store.readSnapshot(request);
  if (!read.cluster || read.cluster.status !== 'ACTIVE') {
    return { ok: false, code: 'SYNC_CONFLICT', status: 409, reason: 'CLUSTER_NOT_ACTIVE' };
  }

  if (read.source) {
    if (!sourceMatches(read.source, request)) {
      return { ok: false, code: 'SYNC_CONFLICT', status: 409, reason: 'EDGE_SOURCE_IDENTITY_MISMATCH' };
    }
    if (read.source.status !== 'ACTIVE') {
      return { ok: false, code: 'SYNC_CONFLICT', status: 409, reason: 'EDGE_SOURCE_NOT_ACTIVE' };
    }
  }

  const requestedSet = new Set(request.requestedCanonicalEventIds);
  const covered = read.coveredCanonicalEventIds
    .filter(id => requestedSet.has(id))
    .sort((a, b) => request.requestedCanonicalEventIds.indexOf(a) - request.requestedCanonicalEventIds.indexOf(b));

  if (request.requestedCanonicalEventIds.length > 0) {
    const coverage = new Set(covered);
    const missing = request.requestedCanonicalEventIds.filter(id => !coverage.has(id));
    if (missing.length) {
      return {
        ok: false,
        code: 'SYNC_CONFLICT',
        status: 409,
        reason: 'CANONICAL_COVERAGE_INCOMPLETE',
        details: { missingCanonicalEventIds: missing.slice(0, 100), missingCount: missing.length }
      };
    }
  }

  const scopeJson = JSON.stringify({ modules: [SLICE1_OPERATIONAL_MODULE] });
  const stateJson = JSON.stringify({
    employees: read.employees,
    employeeCodes: read.employeeCodes,
    presence: read.presence
  });
  const digest = await sha256Hex(canonicalSnapshotMaterial({
    request,
    scopeJson,
    stateJson,
    coveredCanonicalEventIds: covered
  }));

  return {
    ok: true,
    snapshot: {
      snapshotContractVersion: OPERATIONAL_SNAPSHOT_CONTRACT_VERSION,
      snapshotVersion: `OP-${request.environment}-${digest.slice(0, 32)}`,
      environment: request.environment,
      clusterId: request.clusterId,
      edgeInstanceId: request.edgeInstanceId,
      edgeEpoch: request.edgeEpoch,
      edgeSourceId: read.sourceId,
      edgeSourceRegistered: Boolean(read.source),
      sourceCheckpoint: `D1-SLICE1-SHA256:${digest}`,
      compatibilityVersion: DOMAIN_CONTRACT_VERSION,
      scopeJson,
      stateJson,
      requestedCanonicalEventIds: [...request.requestedCanonicalEventIds],
      coveredCanonicalEventIds: covered,
      counts: {
        employees: read.employees.length,
        employeeCodes: read.employeeCodes.length,
        presence: read.presence.length,
        canonicalCoverage: covered.length
      }
    }
  };
}

export async function handleOperationalSnapshotRoute(request, env, requestId) {
  if (!env?.DB) return failure('OPERATIONAL_SNAPSHOT_UNAVAILABLE', 'D1_BINDING_MISSING', 503, requestId);

  const url = new URL(request.url);
  if (url.search) return failure('MACHINE_AUTH_INVALID', 'QUERY_NOT_ALLOWED', 401, requestId);

  const expectedKeyId = String(env.LAN_RECONCILIATION_KEY_ID || '');
  const keyMaterial = String(env.LAN_RECONCILIATION_SHARED_SECRET || '');
  if (!expectedKeyId || keyMaterial.length < 32) {
    return failure('MACHINE_AUTH_UNAVAILABLE', 'MACHINE_CREDENTIAL_NOT_CONFIGURED', 503, requestId);
  }

  const body = await readRawBody(request, requestId);
  if (body.response) return body.response;

  const keyId = request.headers.get('x-vhdchy-machine-key-id') || '';
  if (keyId !== expectedKeyId) return failure('MACHINE_AUTH_INVALID', 'KEY_ID_MISMATCH', 401, requestId);

  const auth = await verifyReconciliationRequestSignature({
    method: request.method,
    path: url.pathname,
    timestampMs: request.headers.get('x-vhdchy-machine-timestamp'),
    nonce: request.headers.get('x-vhdchy-machine-nonce'),
    bodyHash: request.headers.get('x-vhdchy-content-sha256'),
    signature: request.headers.get('x-vhdchy-machine-signature'),
    environment: String(env.APP_ENV || '').toUpperCase(),
    keyId,
    rawBody: body.rawBody,
    keyMaterial
  });
  if (!auth.ok) {
    const status = auth.code === 'MACHINE_AUTH_UNAVAILABLE' ? 503 : 401;
    return failure(auth.code, auth.reason, status, requestId);
  }

  let parsed;
  try {
    parsed = JSON.parse(body.rawBody || '{}');
  } catch {
    return failure('REQUEST_JSON_INVALID', 'BODY_MUST_BE_JSON', 400, requestId);
  }

  const validation = validateSnapshotRequest(parsed, String(env.APP_ENV || '').toUpperCase());
  if (!validation.ok) {
    return failure(validation.code, validation.reason, validation.status, requestId, validation.details);
  }

  let result;
  try {
    result = await buildOperationalSnapshot(createD1OperationalSnapshotStore(env.DB), validation.request);
  } catch {
    return failure('OPERATIONAL_SNAPSHOT_UNAVAILABLE', 'SNAPSHOT_STORE_FAILURE', 503, requestId);
  }
  if (!result.ok) {
    return failure(result.code, result.reason, result.status || 409, requestId, result.details);
  }

  return reply({
    ok: true,
    ...result.snapshot,
    machineAuthVersion: auth.authVersion || RECONCILIATION_AUTH_VERSION
  }, 200, requestId);
}
