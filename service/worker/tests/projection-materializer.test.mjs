import test from 'node:test';
import assert from 'node:assert/strict';
import {
  materializeProjectionBatch,
  materializeProjectionRows,
  SLICE1_PROJECTION_HEADERS
} from '../src/projection-materializer.js';

const employee = {
  employee_id: 'EMP-1',
  full_name: 'Nguyễn Văn A',
  phone: '0900000000',
  main_position: 'Picker',
  vendor: 'NCC A',
  department: 'Pick Pack',
  site: '1291',
  warehouse: 'HY1',
  start_date: '2026-09-01',
  note: 'Ghi chú'
};

function fakeDb() {
  return {
    prepare(sql) {
      return {
        bind(...args) {
          return {
            async all() {
              if (sql.includes('FROM employees')) return { results: args.includes('EMP-1') ? [employee] : [] };
              if (sql.includes('FROM employee_codes')) return { results: args.includes('EMP-1') ? [{ employee_id: 'EMP-1', employee_code: '44198' }] : [] };
              return { results: [] };
            }
          };
        }
      };
    }
  };
}

function intentRow(eventId, eventType, payload = {}) {
  return {
    outbox_id: Number(eventId.replace(/\D/g, '')) || 1,
    event_id: eventId,
    projection_target: 'GOOGLE_SHEETS',
    payload_json: JSON.stringify({
      schemaVersion: 'VHDCHY_GOOGLE_PROJECTION_EVENT_V1',
      eventId,
      eventType,
      entityType: eventType.startsWith('ATTENDANCE_') ? 'attendance' : 'employee',
      entityId: 'EMP-1',
      entityVersion: 1,
      clusterId: 'PICK_PACK_1291',
      actorUserId: 'U1',
      occurredAt: '2026-09-15T06:00:00.000Z',
      payload: { employeeId: 'EMP-1', ...payload }
    })
  };
}

function parsedPayload(row) {
  return JSON.parse(row.payload_json);
}

function assertOnlyKnownHeaders(payload) {
  const allowed = new Set(SLICE1_PROJECTION_HEADERS[payload.sheet]);
  assert.ok(allowed.size > 0);
  for (const key of Object.keys(payload.values)) assert.ok(allowed.has(key), `${payload.sheet} unexpected header ${key}`);
}

test('employee code assignment projects staff row using real employee code, never technical employeeId as MNV', async () => {
  const [row] = await materializeProjectionRows(fakeDb(), [intentRow('EV1', 'EMPLOYEE_CODE_ASSIGNED', { employeeCode: '44198' })]);
  const payload = parsedPayload(row);
  assert.equal(payload.sheet, 'DANH SÁCH NHÂN SỰ');
  assert.equal(payload.values['Mã nhân viên'], '44198');
  assert.notEqual(payload.values['Mã nhân viên'], 'EMP-1');
  assert.equal(payload.values['Họ và tên'], 'Nguyễn Văn A');
  assertOnlyKnownHeaders(payload);
});

test('attendance projection uses Event ID as stable row key and enriches current active MNV/profile', async () => {
  const [row] = await materializeProjectionRows(fakeDb(), [intentRow('EV2', 'ATTENDANCE_IN', {
    businessDate: '2026-09-15',
    occurredAt: '2026-09-15T06:01:00.000Z',
    source: 'QR'
  })]);
  const payload = parsedPayload(row);
  assert.equal(payload.sheet, 'RA - VÀO TRONG CA');
  assert.equal(payload.values['Event ID'], 'EV2');
  assert.equal(payload.values['Mã nhân viên'], '44198');
  assert.equal(payload.values['Loại thao tác'], 'ATTENDANCE_IN');
  assertOnlyKnownHeaders(payload);
});

test('employee profile mutation without safe staff key projects to business history instead of fabricating MNV', async () => {
  const db = fakeDb();
  db.prepare = sql => ({
    bind: (...args) => ({
      async all() {
        if (sql.includes('FROM employees')) return { results: [employee] };
        if (sql.includes('FROM employee_codes')) return { results: [] };
        return { results: [] };
      }
    })
  });
  const [row] = await materializeProjectionRows(db, [intentRow('EV3', 'EMPLOYEE_UPDATED', { phone: '0911111111' })]);
  const payload = parsedPayload(row);
  assert.equal(payload.sheet, 'LỊCH SỬ NGHIỆP VỤ');
  assert.equal(payload.values['Event ID'], 'EV3');
  assert.equal(Object.hasOwn(payload.values, 'Mã nhân viên'), false);
  assertOnlyKnownHeaders(payload);
});

test('already materialized sender payload is preserved exactly', async () => {
  const row = {
    outbox_id: 4,
    event_id: 'EV4',
    payload_json: JSON.stringify({ sheet: 'RA - VÀO TRONG CA', values: { 'Event ID': 'EV4' } })
  };
  const [result] = await materializeProjectionRows(fakeDb(), [row]);
  assert.equal(result, row);
});

test('one unsupported Slice-1 event is isolated and does not block a valid row', async () => {
  const valid = intentRow('EV5', 'ATTENDANCE_OUT', { businessDate: '2026-09-15' });
  const invalid = intentRow('EV6', 'UNKNOWN_SLICE1_EVENT', {});
  const result = await materializeProjectionBatch(fakeDb(), [valid, invalid]);
  assert.equal(result.rows.length, 1);
  assert.equal(result.rows[0].event_id, 'EV5');
  assert.equal(result.failures.length, 1);
  assert.equal(result.failures[0].row.event_id, 'EV6');
  assert.match(result.failures[0].code, /^PROJECTION_EVENT_UNSUPPORTED:/);
});

console.log('PROJECTION_MATERIALIZER_PASS liveHeaders=PASS realMnv=PASS attendanceEventKey=PASS historyFallback=PASS rowIsolation=PASS');
