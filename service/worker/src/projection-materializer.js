const SLICE1_INTENT_SCHEMA = 'VHDCHY_GOOGLE_PROJECTION_EVENT_V1';
const APP_REVISION = 'CLOUD_SLICE1_V1';

export const SLICE1_PROJECTION_HEADERS = Object.freeze({
  'DANH SÁCH NHÂN SỰ': Object.freeze([
    'Mã nhân viên', 'Họ và tên', 'Số điện thoại', 'Vị trí chính', 'Nhà cung cấp',
    'Bộ phận', 'Site', 'Kho', 'Ngày bắt đầu làm việc', 'Ghi chú',
    'Người cập nhật', 'Thời gian cập nhật'
  ]),
  'LỊCH SỬ NGHIỆP VỤ': Object.freeze([
    'Ngày', 'Session ID', 'Mã nhân viên', 'Họ tên', 'Ca', 'Loại sự kiện',
    'Nhãn sự kiện', 'Thời gian', 'Người xử lý', 'Chi tiết', 'Event ID',
    'Phạm vi', 'App Revision'
  ]),
  'RA - VÀO TRONG CA': Object.freeze([
    'Ngày', 'Ca', 'Mã nhân viên', 'Họ và tên', 'Số điện thoại', 'Nhà cung cấp',
    'Bộ phận', 'Site', 'Kho', 'Vị trí chính', 'Vị trí trong ca', 'Seri PDA',
    'User Pick', 'Bàn Pack', 'User Pack', 'Loại thao tác', 'Ghi chú',
    'Người cập nhật', 'Thời gian cập nhật', 'Event ID', 'App action', 'App revision'
  ])
});

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function parsePayload(value) {
  if (isObject(value)) return value;
  try {
    const parsed = JSON.parse(String(value || '{}'));
    return isObject(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

function cleanValues(values) {
  return Object.fromEntries(Object.entries(values).filter(([, value]) => value !== null && value !== undefined && value !== ''));
}

function isoDate(value) {
  const text = String(value || '');
  return /^\d{4}-\d{2}-\d{2}/.test(text) ? text.slice(0, 10) : '';
}

function actorLabel(intent) {
  return String(intent.actorUserId || '');
}

function details(intent) {
  try { return JSON.stringify(intent.payload || {}); }
  catch { return '{}'; }
}

function employeeValues(employee) {
  if (!employee) return {};
  return cleanValues({
    'Họ và tên': employee.full_name,
    'Số điện thoại': employee.phone,
    'Vị trí chính': employee.main_position,
    'Nhà cung cấp': employee.vendor,
    'Bộ phận': employee.department,
    'Site': employee.site,
    'Kho': employee.warehouse,
    'Ngày bắt đầu làm việc': employee.start_date,
    'Ghi chú': employee.note
  });
}

function attendanceEmployeeValues(employee) {
  if (!employee) return {};
  return cleanValues({
    'Họ và tên': employee.full_name,
    'Số điện thoại': employee.phone,
    'Nhà cung cấp': employee.vendor,
    'Bộ phận': employee.department,
    'Site': employee.site,
    'Kho': employee.warehouse,
    'Vị trí chính': employee.main_position
  });
}

function assertKnownHeaders(sheet, values) {
  const allowed = new Set(SLICE1_PROJECTION_HEADERS[sheet] || []);
  if (!allowed.size) throw new Error(`PROJECTION_SHEET_SCHEMA_UNKNOWN:${sheet}`);
  for (const key of Object.keys(values)) {
    if (!allowed.has(key)) throw new Error(`PROJECTION_COLUMN_SCHEMA_UNKNOWN:${sheet}:${key}`);
  }
}

function materialized(sheet, values) {
  const cleaned = cleanValues(values);
  assertKnownHeaders(sheet, cleaned);
  return { sheet, values: cleaned };
}

async function loadEmployeeContext(db, employeeIds) {
  const ids = [...new Set(employeeIds.filter(Boolean).map(String))];
  const employees = new Map();
  const activeCodes = new Map();
  if (!db || ids.length === 0) return { employees, activeCodes };
  const placeholders = ids.map(() => '?').join(',');
  const employeeResult = await db.prepare(`
    SELECT employee_id, full_name, phone, main_position, vendor, department, site, warehouse, start_date, note
    FROM employees
    WHERE employee_id IN (${placeholders})
  `).bind(...ids).all();
  for (const row of Array.isArray(employeeResult?.results) ? employeeResult.results : []) employees.set(String(row.employee_id), row);

  const codeResult = await db.prepare(`
    SELECT employee_id, employee_code
    FROM employee_codes
    WHERE status = 'ACTIVE' AND employee_id IN (${placeholders})
  `).bind(...ids).all();
  for (const row of Array.isArray(codeResult?.results) ? codeResult.results : []) activeCodes.set(String(row.employee_id), String(row.employee_code));
  return { employees, activeCodes };
}

function employeeIdForIntent(intent) {
  if (!isObject(intent?.payload)) return '';
  return String(intent.payload.employeeId || intent.entityId || '');
}

function needsEmployeeContext(intent) {
  return intent?.schemaVersion === SLICE1_INTENT_SCHEMA;
}

function toGatewayPayload(intent, eventId, context) {
  if (intent.schemaVersion !== SLICE1_INTENT_SCHEMA) return null;
  const eventType = String(intent.eventType || '');
  const employeeId = employeeIdForIntent(intent);
  const employee = context.employees.get(employeeId) || null;
  const activeCode = context.activeCodes.get(employeeId) || '';
  const occurredAt = String(intent.occurredAt || '');
  const payload = isObject(intent.payload) ? intent.payload : {};

  if (eventType === 'EMPLOYEE_CODE_ASSIGNED') {
    const employeeCode = String(payload.employeeCode || activeCode || '');
    if (!employeeCode) throw new Error('PROJECTION_EMPLOYEE_CODE_REQUIRED');
    return materialized('DANH SÁCH NHÂN SỰ', {
      'Mã nhân viên': employeeCode,
      ...employeeValues(employee),
      'Người cập nhật': actorLabel(intent),
      'Thời gian cập nhật': occurredAt
    });
  }

  if (eventType === 'ATTENDANCE_IN' || eventType === 'ATTENDANCE_OUT' || eventType === 'ATTENDANCE_CORRECTED') {
    return materialized('RA - VÀO TRONG CA', {
      'Ngày': String(payload.businessDate || isoDate(occurredAt)),
      'Mã nhân viên': activeCode,
      ...attendanceEmployeeValues(employee),
      'Loại thao tác': eventType,
      'Ghi chú': eventType === 'ATTENDANCE_CORRECTED' ? String(payload.reason || '') : String(payload.source || ''),
      'Người cập nhật': actorLabel(intent),
      'Thời gian cập nhật': String(payload.occurredAt || occurredAt),
      'Event ID': eventId,
      'App action': eventType,
      'App revision': APP_REVISION
    });
  }

  if (eventType.startsWith('EMPLOYEE_')) {
    return materialized('LỊCH SỬ NGHIỆP VỤ', {
      'Ngày': isoDate(occurredAt),
      'Mã nhân viên': activeCode,
      'Họ tên': employee?.full_name || payload.fullName || '',
      'Loại sự kiện': eventType,
      'Nhãn sự kiện': eventType,
      'Thời gian': occurredAt,
      'Người xử lý': actorLabel(intent),
      'Chi tiết': details(intent),
      'Event ID': eventId,
      'Phạm vi': String(intent.clusterId || 'PICK_PACK_1291'),
      'App Revision': APP_REVISION
    });
  }

  throw new Error(`PROJECTION_EVENT_UNSUPPORTED:${eventType || 'UNKNOWN'}`);
}

export async function materializeProjectionRows(db, rows) {
  const input = Array.isArray(rows) ? rows : [];
  if (!input.length) return [];
  const parsed = input.map(row => ({ row, payload: parsePayload(row?.payload_json) }));
  const employeeIds = parsed
    .filter(item => needsEmployeeContext(item.payload))
    .map(item => employeeIdForIntent(item.payload));
  const context = await loadEmployeeContext(db, employeeIds);

  return parsed.map(({ row, payload }) => {
    if (typeof payload.sheet === 'string' && isObject(payload.values)) return row;
    const gatewayPayload = toGatewayPayload(payload, String(row?.event_id || ''), context);
    if (!gatewayPayload) return row;
    return { ...row, payload_json: JSON.stringify(gatewayPayload) };
  });
}
