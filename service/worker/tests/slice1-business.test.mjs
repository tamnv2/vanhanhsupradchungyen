import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
  CloudSlice1Error,
  createD1Slice1BusinessStore,
  executeCloudSlice1Command,
  validateCloudCommandEnvelope
} from '../src/slice1-business.js';

const principal = Object.freeze({
  userId: 'U1', employeeId: 'E-ACTOR', securityLevel: 'NORMAL', deviceId: 'DEV-1', mustChangePassword: false
});
const allow = async () => ({ allowed: true, reason: 'TEST_ALLOW' });

function envelope(overrides = {}) {
  return {
    requestId: 'REQ-1',
    idempotencyKey: 'IDEM-1',
    commandCode: 'EMPLOYEE_CREATE',
    entityId: 'EMP-1',
    expectedEntityVersion: null,
    payload: { employeeId: 'EMP-1', fullName: 'Nguyen Van A' },
    ...overrides
  };
}

function fakeStore(seed = {}) {
  const events = new Map(seed.events || []);
  const deviceEvents = new Map(seed.deviceEvents || []);
  const employees = new Map(seed.employees || []);
  const codes = new Map(seed.codes || []);
  const presence = new Map(seed.presence || []);
  const commits = [];
  return {
    commits,
    async getEventByIdempotency(key) { return events.get(key) || null; },
    async getEventByDeviceSequence(deviceId, deviceSeq) { return deviceEvents.get(`${deviceId}:${deviceSeq}`) || null; },
    async getEmployee(id) { return employees.get(id) || null; },
    async getEmployeeCode(id) { return codes.get(id) || null; },
    async listActiveEmployeeCodes() { return [...codes.values()].filter(item => item.status === 'ACTIVE'); },
    async getPresence(id) { return presence.get(id) || null; },
    async commit(value) {
      commits.push(value);
      const { envelope: submitted, command, plan, eventId, principal: actor, metadataJson } = value;
      if (plan.kind === 'EMPLOYEE_CREATE' || plan.kind === 'EMPLOYEE_UPDATE' || plan.kind === 'EMPLOYEE_STATUS_CHANGE') {
        employees.set(submitted.entityId, { ...plan.state, entityVersion: plan.version });
      }
      if (plan.kind === 'EMPLOYEE_CODE_ASSIGN') codes.set(submitted.entityId, { ...plan.state });
      if (plan.kind === 'ATTENDANCE' || plan.kind === 'ATTENDANCE_CORRECT') presence.set(submitted.entityId, { ...plan.state });
      events.set(submitted.idempotencyKey, {
        event_id: eventId,
        event_type: command.eventType,
        entity_type: command.entityType,
        entity_id: submitted.entityId,
        entity_version: plan.version,
        actor_user_id: actor.userId,
        device_id: actor.deviceId,
        device_seq: submitted.deviceSeq,
        idempotency_key: submitted.idempotencyKey,
        payload_json: metadataJson
      });
      if (submitted.deviceSeq != null) deviceEvents.set(`${actor.deviceId}:${submitted.deviceSeq}`, {
        event_id: eventId,
        idempotency_key: submitted.idempotencyKey
      });
    }
  };
}

function expectCode(code) {
  return error => error instanceof CloudSlice1Error && error.code === code;
}

test('envelope is strict and rejects client authority fields', () => {
  assert.equal(validateCloudCommandEnvelope(envelope()).commandCode, 'EMPLOYEE_CREATE');
  assert.throws(() => validateCloudCommandEnvelope(envelope({ extra: true })), expectCode('INVALID_INPUT'));
  assert.throws(() => validateCloudCommandEnvelope(envelope({ payload: { employeeId: 'EMP-1', fullName: 'A', actorUserId: 'spoof' } })), expectCode('CLIENT_AUTHORITY_FIELD_REJECTED'));
});

test('employee create commits canonical cloud result and replays idempotently', async () => {
  const store = fakeStore();
  let n = 0;
  const uuid = () => `EVENT-${++n}`;
  const first = await executeCloudSlice1Command({ db: {}, principal, envelope: envelope(), store, authorizePrincipal: allow, uuid, nowMs: Date.parse('2026-09-15T00:00:00Z') });
  assert.equal(first.commitStatus, 'CLOUD_COMMITTED');
  assert.equal(first.googleOutputStatus, 'PENDING');
  assert.equal(first.entity.version, 1);
  assert.equal(first.alreadyAccepted, false);
  assert.equal(store.commits.length, 1);
  assert.equal(store.commits[0].plan.kind, 'EMPLOYEE_CREATE');
  assert.equal(store.commits[0].principal.userId, 'U1');

  const replay = await executeCloudSlice1Command({ db: {}, principal, envelope: envelope(), store, authorizePrincipal: allow, uuid, nowMs: Date.parse('2026-09-15T00:01:00Z') });
  assert.equal(replay.eventId, first.eventId);
  assert.equal(replay.alreadyAccepted, true);
  assert.equal(store.commits.length, 1);
});

test('same idempotency key with altered payload conflicts', async () => {
  const store = fakeStore();
  await executeCloudSlice1Command({ db: {}, principal, envelope: envelope(), store, authorizePrincipal: allow, uuid: () => 'EVENT-1' });
  await assert.rejects(
    () => executeCloudSlice1Command({ db: {}, principal, envelope: envelope({ payload: { employeeId: 'EMP-1', fullName: 'Changed' } }), store, authorizePrincipal: allow, uuid: () => 'EVENT-2' }),
    expectCode('IDEMPOTENCY_PAYLOAD_CONFLICT')
  );
});

test('permission denial is fail-closed before state preparation', async () => {
  const store = fakeStore();
  await assert.rejects(
    () => executeCloudSlice1Command({ db: {}, principal, envelope: envelope(), store, authorizePrincipal: async () => ({ allowed: false, reason: 'EXPLICIT_DENY' }) }),
    expectCode('PERMISSION_DENIED')
  );
  assert.equal(store.commits.length, 0);
});

test('portrait command remains fail-closed by Owner gate', async () => {
  const store = fakeStore();
  await assert.rejects(
    () => executeCloudSlice1Command({
      db: {}, principal, store, authorizePrincipal: allow,
      envelope: envelope({ commandCode: 'EMPLOYEE_PORTRAIT_REPLACE', expectedEntityVersion: 1, payload: { employeeId: 'EMP-1' } })
    }),
    expectCode('RUNTIME_DEPENDENCY_UNAVAILABLE')
  );
});

test('employee update requires exact entity version', async () => {
  const store = fakeStore({ employees: [['EMP-1', { employeeId: 'EMP-1', fullName: 'A', phone: null, status: 'ACTIVE', mainPosition: null, vendor: null, department: null, site: null, warehouse: null, startDate: null, permanentLeaveDate: null, note: null, entityVersion: 3 }]] });
  await assert.rejects(
    () => executeCloudSlice1Command({ db: {}, principal, store, authorizePrincipal: allow, envelope: envelope({ commandCode: 'EMPLOYEE_UPDATE', expectedEntityVersion: 2, payload: { employeeId: 'EMP-1', phone: '0123' } }) }),
    expectCode('VERSION_CONFLICT')
  );
  const ok = await executeCloudSlice1Command({ db: {}, principal, store, authorizePrincipal: allow, uuid: () => 'EVENT-UPD', envelope: envelope({ commandCode: 'EMPLOYEE_UPDATE', expectedEntityVersion: 3, payload: { employeeId: 'EMP-1', phone: '0123' } }) });
  assert.equal(ok.entity.version, 4);
});

test('attendance OUT requires preceding IN and correction appends a command plan', async () => {
  const employees = [['EMP-1', { employeeId: 'EMP-1', fullName: 'A', status: 'ACTIVE', entityVersion: 1 }]];
  const empty = fakeStore({ employees });
  await assert.rejects(
    () => executeCloudSlice1Command({ db: {}, principal, store: empty, authorizePrincipal: allow, envelope: envelope({ commandCode: 'ATTENDANCE_OUT', payload: { employeeId: 'EMP-1', businessDate: '2026-09-15' } }) }),
    expectCode('INVALID_INPUT')
  );

  const store = fakeStore({ employees, presence: [['EMP-1', { employeeId: 'EMP-1', currentState: 'IN', businessDate: '2026-09-15', entityVersion: 4 }]] });
  const correction = await executeCloudSlice1Command({
    db: {}, principal, store, authorizePrincipal: allow,
    uuid: (() => { let n = 0; return () => `CORR-${++n}`; })(),
    envelope: envelope({ commandCode: 'ATTENDANCE_CORRECT', expectedEntityVersion: 4, payload: { employeeId: 'EMP-1', businessDate: '2026-09-15', currentState: 'OUT', reason: 'Điều chỉnh theo bằng chứng' } })
  });
  assert.equal(correction.entity.version, 5);
  assert.equal(store.commits[0].plan.kind, 'ATTENDANCE_CORRECT');
  assert.equal(store.commits[0].plan.state.lastCorrectionReason, 'Điều chỉnh theo bằng chứng');
});

test('device sequence collision is explicit', async () => {
  const store = fakeStore({ deviceEvents: [['DEV-1:7', { event_id: 'OLD', idempotency_key: 'OTHER' }]] });
  await assert.rejects(
    () => executeCloudSlice1Command({ db: {}, principal, store, authorizePrincipal: allow, envelope: envelope({ deviceSeq: 7 }) }),
    expectCode('DEVICE_SEQUENCE_COLLISION')
  );
});

test('D1 commit keeps guarded state, immutable event, and outbox in one batch', async () => {
  const batches = [];
  const db = {
    prepare(sql) {
      return {
        sql,
        args: [],
        bind(...args) { this.args = args; return this; },
        async first() { return null; },
        async all() { return { results: [] }; },
        async run() { return { success: true }; }
      };
    },
    async batch(statements) { batches.push(statements); return statements.map(() => ({ success: true })); }
  };
  const store = createD1Slice1BusinessStore(db);
  await store.commit({
    envelope: envelope({ commandCode: 'EMPLOYEE_UPDATE', expectedEntityVersion: 3, payload: { employeeId: 'EMP-1', phone: '0123' } }),
    command: { entityType: 'employee', eventType: 'EMPLOYEE_UPDATED' },
    plan: {
      kind: 'EMPLOYEE_UPDATE', priorVersion: 3, version: 4,
      state: { employeeId: 'EMP-1', fullName: 'A', phone: '0123', status: 'ACTIVE', mainPosition: null, vendor: null, department: null, site: null, warehouse: null, startDate: null, permanentLeaveDate: null, note: null, entityVersion: 4 }
    },
    eventId: 'EVENT-ATOMIC', attendanceEventId: null, principal,
    metadataJson: '{}', projectionJson: '{}', occurredAt: '2026-09-15T00:00:00.000Z'
  });
  assert.equal(batches.length, 1);
  assert.equal(batches[0].length, 3);
  assert.match(batches[0][0].sql, /UPDATE employees[\s\S]*WHERE employee_id=\? AND entity_version=\?/);
  assert.match(batches[0][1].sql, /INSERT INTO domain_events[\s\S]*SELECT[\s\S]*FROM employees WHERE employee_id=\? AND entity_version=\?/);
  assert.match(batches[0][2].sql, /INSERT INTO projection_outbox/);
});

console.log('CLOUD_SLICE1_BUSINESS_PASS strictEnvelope=PASS actorAuthority=PASS permission=PASS idempotency=PASS versionConflict=PASS attendanceGuard=PASS portraitGate=PASS deviceSequence=PASS');
