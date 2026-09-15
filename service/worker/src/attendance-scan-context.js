import { authorizePrincipal } from './permission-store.js';
import { authenticateRequest } from './session.js';

export const ATTENDANCE_SCAN_CONTEXT_PATH = '/api/v1/attendance/scan-context';
export const ATTENDANCE_SCAN_PERMISSION = Object.freeze({ resourceCode: 'ATTENDANCE', actionCode: 'SCAN' });
export const ATTENDANCE_SCAN_CLUSTER_ID = 'PICK_PACK_1291';
export const ATTENDANCE_SCAN_MODULE_ID = 'PICK_PACK';

const MAX_EMPLOYEE_CODE_LENGTH = 120;
const MAX_REQUEST_BODY_BYTES = 16 * 1024;
const ALLOWED_FIELDS = new Set(['employeeCode']);
const encoder = new TextEncoder();

export class AttendanceScanContextError extends Error {
  constructor(code, message, status = 422, details = undefined) {
    super(message);
    this.name = 'AttendanceScanContextError';
    this.code = code;
    this.status = status;
    this.details = details;
  }
}

function fail(code, message, status = 422, details = undefined) {
  throw new AttendanceScanContextError(code, message, status, details);
}

function response(payload, status, requestId) {
  return Response.json({ ...payload, requestId }, {
    status,
    headers: {
      'cache-control': 'no-store',
      'x-content-type-options': 'nosniff'
    }
  });
}

function routeError(code, message, status, requestId, details = undefined) {
  const error = { code, message };
  if (details !== undefined) error.details = details;
  return response({ ok: false, runtime: 'CLOUD', error }, status, requestId);
}

async function readJsonObject(request, requestId) {
  const declaredLength = Number(request.headers.get('content-length') || 0);
  if (Number.isFinite(declaredLength) && declaredLength > MAX_REQUEST_BODY_BYTES) {
    return { value: null, response: routeError('REQUEST_BODY_TOO_LARGE', 'Request body is too large.', 413, requestId) };
  }

  let text;
  try {
    text = await request.text();
  } catch {
    return { value: null, response: routeError('REQUEST_BODY_INVALID', 'Request body could not be read.', 400, requestId) };
  }
  if (encoder.encode(text).byteLength > MAX_REQUEST_BODY_BYTES) {
    return { value: null, response: routeError('REQUEST_BODY_TOO_LARGE', 'Request body is too large.', 413, requestId) };
  }

  let value;
  try {
    value = JSON.parse(text || '{}');
  } catch {
    return { value: null, response: routeError('REQUEST_JSON_INVALID', 'Request body must be valid JSON.', 400, requestId) };
  }
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return { value: null, response: routeError('INVALID_INPUT', 'Scan context request must be a JSON object.', 422, requestId) };
  }
  return { value, response: null };
}

export function normalizeAttendanceScanContextRequest(value) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    fail('INVALID_INPUT', 'Scan context request must be an object.');
  }
  for (const key of Object.keys(value)) {
    if (!ALLOWED_FIELDS.has(key)) fail('INVALID_INPUT', `Unsupported scan context field: ${key}.`);
  }
  if (typeof value.employeeCode !== 'string') fail('INVALID_INPUT', 'employeeCode is required.');
  const employeeCode = value.employeeCode.trim();
  if (!employeeCode || employeeCode.length > MAX_EMPLOYEE_CODE_LENGTH) {
    fail('INVALID_INPUT', 'employeeCode is missing or out of bounds.');
  }
  for (let i = 0; i < employeeCode.length; i += 1) {
    if (employeeCode.charCodeAt(i) < 0x20 || employeeCode.charCodeAt(i) === 0x7f) {
      fail('INVALID_INPUT', 'employeeCode contains control characters.');
    }
  }
  return { employeeCode };
}

function nullableString(value) {
  return value === null || value === undefined ? null : String(value);
}

function normalizeRow(row) {
  if (!row) return null;
  return {
    employeeCodeId: nullableString(row.employee_code_id),
    employeeCode: nullableString(row.employee_code),
    employeeId: nullableString(row.employee_id),
    codeStatus: nullableString(row.code_status),
    fullName: nullableString(row.full_name),
    employeeStatus: nullableString(row.employee_status),
    currentPortraitMediaId: nullableString(row.current_portrait_media_id),
    presence: row.presence_entity_version == null
      ? null
      : {
          currentState: nullableString(row.current_state),
          businessDate: nullableString(row.business_date),
          entityVersion: Number(row.presence_entity_version)
        }
  };
}

export function createD1AttendanceScanContextStore(db) {
  if (!db || typeof db.prepare !== 'function') throw new TypeError('D1 database binding is required');
  return {
    async findByEmployeeCode(employeeCode) {
      const result = await db.prepare(`
        SELECT
          ec.employee_code_id,
          ec.employee_code,
          ec.employee_id,
          ec.status AS code_status,
          e.full_name,
          e.status AS employee_status,
          e.current_portrait_media_id,
          p.current_state,
          p.business_date,
          p.entity_version AS presence_entity_version
        FROM employee_codes ec
        INNER JOIN employees e ON e.employee_id = ec.employee_id
        LEFT JOIN presence_state p ON p.employee_id = e.employee_id
        WHERE ec.employee_code = ? AND ec.status = 'ACTIVE'
        ORDER BY ec.employee_code_id
        LIMIT 3
      `).bind(employeeCode).all();
      return (Array.isArray(result?.results) ? result.results : []).map(normalizeRow);
    }
  };
}

export async function resolveAttendanceScanContext({
  store,
  db,
  principal,
  request,
  authorize = authorizePrincipal
}) {
  if (!principal?.userId) fail('AUTH_REQUIRED', 'Authentication is required.', 401);
  if (!store || typeof store.findByEmployeeCode !== 'function') {
    fail('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Scan context store is unavailable.', 503);
  }

  const normalized = normalizeAttendanceScanContextRequest(request);
  const permission = await authorize(
    db,
    principal,
    ATTENDANCE_SCAN_PERMISSION,
    { clusterId: ATTENDANCE_SCAN_CLUSTER_ID, moduleId: ATTENDANCE_SCAN_MODULE_ID }
  );
  if (!permission?.allowed) {
    const reason = permission?.reason || 'DENIED';
    if (reason === 'PASSWORD_CHANGE_REQUIRED') {
      fail('PASSWORD_CHANGE_REQUIRED', 'The account must change its password before attendance scan lookup.', 403);
    }
    fail('PERMISSION_DENIED', 'Attendance scan permission is required.', 403, { reason });
  }

  let matches;
  try {
    matches = await store.findByEmployeeCode(normalized.employeeCode);
  } catch (error) {
    if (error instanceof AttendanceScanContextError) throw error;
    fail('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Scan context state could not be read.', 503);
  }

  if (!Array.isArray(matches) || matches.length === 0) {
    fail('SCAN_CONTEXT_NOT_FOUND', 'No active employee assignment exists for the supplied MNV.', 404);
  }
  if (matches.length !== 1) {
    fail('SCAN_CONTEXT_CONFLICT', 'Active MNV resolution is ambiguous.', 409);
  }

  const match = matches[0];
  if (match.codeStatus !== 'ACTIVE' || match.employeeStatus !== 'ACTIVE') {
    fail('SCAN_CONTEXT_NOT_FOUND', 'No active employee assignment exists for the supplied MNV.', 404);
  }
  if (!match.employeeCodeId || !match.employeeId || !match.fullName || match.employeeCode !== normalized.employeeCode) {
    fail('SCAN_CONTEXT_CONFLICT', 'Resolved scan context is structurally inconsistent.', 409);
  }
  if (match.currentPortraitMediaId !== null && !match.currentPortraitMediaId) {
    fail('SCAN_CONTEXT_CONFLICT', 'Resolved portrait reference is invalid.', 409);
  }
  if (match.presence) {
    if (!['IN', 'OUT'].includes(match.presence.currentState) ||
        !match.presence.businessDate ||
        !Number.isInteger(match.presence.entityVersion) ||
        match.presence.entityVersion < 1) {
      fail('SCAN_CONTEXT_CONFLICT', 'Resolved presence state is invalid.', 409);
    }
  }

  return {
    employeeCodeId: match.employeeCodeId,
    employeeCode: match.employeeCode,
    employeeId: match.employeeId,
    fullName: match.fullName,
    currentPortraitMediaId: match.currentPortraitMediaId ?? null,
    presence: match.presence ?? null
  };
}

export async function handleAttendanceScanContextRoute(
  request,
  env,
  requestId = crypto.randomUUID(),
  options = {}
) {
  if (request.method !== 'POST') {
    return routeError('METHOD_NOT_ALLOWED', 'Attendance scan context requires POST.', 405, requestId);
  }
  if (!env?.DB) {
    return routeError('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Attendance scan context is unavailable.', 503, requestId);
  }

  const authenticate = options.authenticateRequest || authenticateRequest;
  const auth = await authenticate(request, env);
  if (!auth.ok) {
    return routeError(auth.code || 'AUTH_FAILED', 'Authentication failed.', 401, requestId);
  }

  const body = await readJsonObject(request, requestId);
  if (body.response) return body.response;

  try {
    const context = await resolveAttendanceScanContext({
      store: options.store || createD1AttendanceScanContextStore(env.DB),
      db: env.DB,
      principal: auth.principal,
      request: body.value,
      authorize: options.authorize || authorizePrincipal
    });
    return response({ ok: true, runtime: 'CLOUD', context }, 200, requestId);
  } catch (error) {
    if (error instanceof AttendanceScanContextError) {
      return routeError(error.code, error.message, error.status, requestId, error.details);
    }
    console.error('ATTENDANCE_SCAN_CONTEXT_UNEXPECTED', error?.stack || error);
    return routeError('RUNTIME_DEPENDENCY_UNAVAILABLE', 'Attendance scan context could not be resolved.', 503, requestId);
  }
}
