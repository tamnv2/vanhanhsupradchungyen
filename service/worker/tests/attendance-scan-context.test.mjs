import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
  ATTENDANCE_SCAN_CONTEXT_PATH,
  AttendanceScanContextError,
  handleAttendanceScanContextRoute,
  normalizeAttendanceScanContextRequest,
  resolveAttendanceScanContext
} from '../src/attendance-scan-context.js';
import { handleWorkerFetch } from '../src/worker-entry.js';

const principal = Object.freeze({
  userId: 'U-SCAN',
  securityLevel: 'NORMAL',
  mustChangePassword: false
});
const allow = async () => ({ allowed: true, reason: 'TEST_ALLOW' });

function context(overrides = {}) {
  return {
    employeeCodeId: 'EC-001',
    employeeCode: 'MNV001',
    employeeId: 'EMP-001',
    codeStatus: 'ACTIVE',
    fullName: 'Nguyễn Văn A',
    employeeStatus: 'ACTIVE',
    currentPortraitMediaId: 'MEDIA-001',
    presence: {
      currentState: 'IN',
      businessDate: '2026-09-15',
      entityVersion: 4
    },
    ...overrides
  };
}

function store(matches = [context()]) {
  return {
    seen: [],
    async findByEmployeeCode(employeeCode) {
      this.seen.push(employeeCode);
      return matches;
    }
  };
}

function expectCode(code) {
  return error => error instanceof AttendanceScanContextError && error.code === code;
}

test('scan input is MNV-only, trimmed and strict', () => {
  assert.deepEqual(normalizeAttendanceScanContextRequest({ employeeCode: '  MNV001  ' }), { employeeCode: 'MNV001' });
  assert.throws(() => normalizeAttendanceScanContextRequest({ employeeCode: 'MNV001', employeeId: 'spoof' }), expectCode('INVALID_INPUT'));
  assert.throws(() => normalizeAttendanceScanContextRequest({ employeeCode: 'BAD\nCODE' }), expectCode('INVALID_INPUT'));
  assert.throws(() => normalizeAttendanceScanContextRequest({ employeeCode: 'X'.repeat(121) }), expectCode('INVALID_INPUT'));
});

test('authorized lookup returns technical employee identity, portrait and current presence', async () => {
  const targetStore = store();
  const resolved = await resolveAttendanceScanContext({
    store: targetStore,
    db: {},
    principal,
    request: { employeeCode: ' MNV001 ' },
    authorize: allow
  });
  assert.deepEqual(targetStore.seen, ['MNV001']);
  assert.deepEqual(resolved, {
    employeeCodeId: 'EC-001',
    employeeCode: 'MNV001',
    employeeId: 'EMP-001',
    fullName: 'Nguyễn Văn A',
    currentPortraitMediaId: 'MEDIA-001',
    presence: { currentState: 'IN', businessDate: '2026-09-15', entityVersion: 4 }
  });
});

test('presence may be absent but malformed presence fails closed', async () => {
  const withoutPresence = await resolveAttendanceScanContext({
    store: store([context({ presence: null, currentPortraitMediaId: null })]),
    db: {}, principal, request: { employeeCode: 'MNV001' }, authorize: allow
  });
  assert.equal(withoutPresence.presence, null);
  assert.equal(withoutPresence.currentPortraitMediaId, null);

  await assert.rejects(
    () => resolveAttendanceScanContext({
      store: store([context({ presence: { currentState: 'IN', businessDate: null, entityVersion: 1 } })]),
      db: {}, principal, request: { employeeCode: 'MNV001' }, authorize: allow
    }),
    expectCode('SCAN_CONTEXT_CONFLICT')
  );
});

test('not-found, inactive, ambiguous and structurally inconsistent state are explicit', async () => {
  await assert.rejects(
    () => resolveAttendanceScanContext({ store: store([]), db: {}, principal, request: { employeeCode: 'MNV001' }, authorize: allow }),
    expectCode('SCAN_CONTEXT_NOT_FOUND')
  );
  await assert.rejects(
    () => resolveAttendanceScanContext({ store: store([context({ employeeStatus: 'INACTIVE' })]), db: {}, principal, request: { employeeCode: 'MNV001' }, authorize: allow }),
    expectCode('SCAN_CONTEXT_NOT_FOUND')
  );
  await assert.rejects(
    () => resolveAttendanceScanContext({ store: store([context(), context({ employeeCodeId: 'EC-002' })]), db: {}, principal, request: { employeeCode: 'MNV001' }, authorize: allow }),
    expectCode('SCAN_CONTEXT_CONFLICT')
  );
  await assert.rejects(
    () => resolveAttendanceScanContext({ store: store([context({ fullName: null })]), db: {}, principal, request: { employeeCode: 'MNV001' }, authorize: allow }),
    expectCode('SCAN_CONTEXT_CONFLICT')
  );
});

test('permission and password-change gates fail closed before state read', async () => {
  const deniedStore = store();
  await assert.rejects(
    () => resolveAttendanceScanContext({
      store: deniedStore,
      db: {}, principal, request: { employeeCode: 'MNV001' },
      authorize: async () => ({ allowed: false, reason: 'EXPLICIT_DENY' })
    }),
    expectCode('PERMISSION_DENIED')
  );
  assert.equal(deniedStore.seen.length, 0);

  const passwordStore = store();
  await assert.rejects(
    () => resolveAttendanceScanContext({
      store: passwordStore,
      db: {}, principal, request: { employeeCode: 'MNV001' },
      authorize: async () => ({ allowed: false, reason: 'PASSWORD_CHANGE_REQUIRED' })
    }),
    expectCode('PASSWORD_CHANGE_REQUIRED')
  );
  assert.equal(passwordStore.seen.length, 0);
});

test('Cloud HTTP route derives actor from authenticated session and returns stable envelope', async () => {
  const request = new Request(`https://beta.supra.cc.cd${ATTENDANCE_SCAN_CONTEXT_PATH}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', authorization: `Bearer ${'x'.repeat(40)}` },
    body: JSON.stringify({ employeeCode: 'MNV001' })
  });
  const response = await handleAttendanceScanContextRoute(request, { DB: {} }, 'REQ-SCAN-1', {
    authenticateRequest: async () => ({ ok: true, principal }),
    store: store(),
    authorize: allow
  });
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.runtime, 'CLOUD');
  assert.equal(body.requestId, 'REQ-SCAN-1');
  assert.equal(body.context.employeeId, 'EMP-001');
  assert.equal(Object.hasOwn(body.context, 'actorUserId'), false);
});

test('Cloud HTTP route rejects unauthenticated and invalid method requests', async () => {
  const post = new Request(`https://beta.supra.cc.cd${ATTENDANCE_SCAN_CONTEXT_PATH}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ employeeCode: 'MNV001' })
  });
  const unauthorized = await handleAttendanceScanContextRoute(post, { DB: {} }, 'REQ-SCAN-2', {
    authenticateRequest: async () => ({ ok: false, code: 'AUTH_TOKEN_REQUIRED' })
  });
  assert.equal(unauthorized.status, 401);
  assert.equal((await unauthorized.json()).error.code, 'AUTH_TOKEN_REQUIRED');

  const get = new Request(`https://beta.supra.cc.cd${ATTENDANCE_SCAN_CONTEXT_PATH}`, { method: 'GET' });
  const method = await handleAttendanceScanContextRoute(get, { DB: {} }, 'REQ-SCAN-3');
  assert.equal(method.status, 405);
  assert.equal((await method.json()).error.code, 'METHOD_NOT_ALLOWED');
});

test('Worker entry intercepts the scan-context path before generic Worker routing', async () => {
  const get = new Request(`https://beta.supra.cc.cd${ATTENDANCE_SCAN_CONTEXT_PATH}`, { method: 'GET' });
  const response = await handleWorkerFetch(get, { DB: {} }, {});
  assert.equal(response.status, 405);
  const body = await response.json();
  assert.equal(body.error.code, 'METHOD_NOT_ALLOWED');
});

console.log('ATTENDANCE_SCAN_CONTEXT_CLOUD_PASS strictMnv=PASS permission=PASS sessionActor=PASS presenceVersion=PASS route=PASS');
