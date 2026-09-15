import { authorizePrincipal } from './permission-store.js';

export const ATTENDANCE_SCAN_CONTEXT_PATH = '/api/v1/attendance/scan-context';
export const ATTENDANCE_SCAN_PERMISSION = Object.freeze({ resourceCode: 'ATTENDANCE', actionCode: 'SCAN' });
export const ATTENDANCE_SCAN_CLUSTER_ID = 'PICK_PACK_1291';
export const ATTENDANCE_SCAN_MODULE_ID = 'PICK_PACK';

const MAX_EMPLOYEE_CODE_LENGTH = 120;
const ALLOWED_FIELDS = new Set(['employeeCode']);

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

function normalizeRow(row) {
  if (!row) return null;
  return {
    employeeCodeId: String(row.employee_code_id),
    employeeCode: String(row.employee_code),
    employeeId: String(row.employee_id),
    codeStatus: String(row.code_status),
    fullName: String(row.full_name),
    employeeStatus: String(row.employee_status),
    currentPortraitMediaId: row.current_portrait_media_id == null ? null : String(row.current_portrait_media_id),
    presence: row.presence_entity_version == null
      ? null
      : {
          currentState: String(row.current_state),
          businessDate: row.business_date == null ? null : String(row.business_date),
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
  if (match.presence) {
    if (!['IN', 'OUT'].includes(match.presence.currentState) ||
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
