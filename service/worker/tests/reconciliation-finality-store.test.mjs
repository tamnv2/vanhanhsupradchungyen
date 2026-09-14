import test from 'node:test';
import assert from 'node:assert/strict';

import { createD1ReconciliationFinalityStore } from '../src/reconciliation-finality.js';

function envelope(overrides = {}) {
  return {
    eventId: 'edge-store-1',
    requestId: 'request-store-1',
    idempotencyKey: 'idem-store-1',
    clusterId: 'PICK_PACK_1291',
    deviceId: null,
    deviceSeq: null,
    eventCode: 'EMPLOYEE_CREATED',
    entityType: 'EMPLOYEE',
    entityId: 'EMP-STORE-1',
    resultingVersion: 1,
    payloadJson: '{"employeeId":"EMP-STORE-1","fullName":"Nguyễn Văn A","status":"ACTIVE"}',
    acceptedAt: '2026-09-14T11:00:00.000Z',
    actorUserId: 'user-store-1',
    ...overrides
  };
}

function employeeState() {
  return {
    employeeId: 'EMP-STORE-1',
    fullName: 'Nguyễn Văn A',
    phone: null,
    status: 'ACTIVE',
    mainPosition: null,
    vendor: null,
    department: null,
    site: null,
    warehouse: null,
    startDate: null,
    permanentLeaveDate: null,
    note: null,
    entityVersion: 1
  };
}

function fakeD1({ hasGoogleReceipt = false } = {}) {
  const state = { batches: [] };
  return {
    state,
    prepare(sql) {
      return {
        bind(...args) {
          return {
            sql,
            args,
            async first() {
              if (sql.includes('SELECT 1 AS ok FROM integration_receipts')) {
                return hasGoogleReceipt ? { ok: 1 } : null;
              }
              return null;
            },
            async run() { return { success: true }; }
          };
        }
      };
    },
    async batch(statements) {
      state.batches.push(statements);
      return statements.map(() => ({ success: true }));
    }
  };
}

function statement(batch, fragment) {
  const found = batch.find(item => item.sql.includes(fragment));
  assert.ok(found, `missing SQL statement containing ${fragment}`);
  return found;
}

test('canonical employee commit links receipts, edge finality and checkpoint in one D1 batch', async () => {
  const db = fakeD1();
  const store = createD1ReconciliationFinalityStore(db);
  const value = envelope();

  await store.commitEmployeeCreate({
    envelope: value,
    state: employeeState(),
    canonicalEventId: 'cloud:edge-store-1',
    projectionPayloadJson: '{"kind":"projection"}',
    nowIso: '2026-09-14T11:00:05.000Z'
  });

  assert.equal(db.state.batches.length, 1);
  const batch = db.state.batches[0];
  statement(batch, 'INSERT INTO employees');
  statement(batch, 'INSERT INTO domain_events');
  statement(batch, 'INSERT INTO projection_outbox');
  statement(batch, 'UPDATE integration_receipts SET event_id');
  statement(batch, "SET status='RECONCILED'");
  statement(batch, 'INSERT INTO edge_sync_checkpoints');

  const projection = statement(batch, 'INSERT INTO projection_outbox');
  assert.equal(projection.args[0], 'cloud:edge-store-1');
  assert.equal(projection.args[2], 'PENDING');
});

test('completed LAN Google receipt suppresses duplicate Cloud projection work', async () => {
  const db = fakeD1({ hasGoogleReceipt: true });
  const store = createD1ReconciliationFinalityStore(db);

  await store.commitEmployeeCreate({
    envelope: envelope(),
    state: employeeState(),
    canonicalEventId: 'cloud:edge-store-1',
    projectionPayloadJson: '{"kind":"projection"}',
    nowIso: '2026-09-14T11:00:05.000Z'
  });

  const projection = statement(db.state.batches[0], 'INSERT INTO projection_outbox');
  assert.equal(projection.args[2], 'ACKED');
  statement(db.state.batches[0], 'UPDATE integration_receipts SET event_id');
});

test('linking an existing canonical winner updates receipt, edge and checkpoint atomically', async () => {
  const db = fakeD1();
  const store = createD1ReconciliationFinalityStore(db);

  await store.linkExistingCanonical(
    envelope(),
    'cloud-existing',
    '2026-09-14T11:00:06.000Z'
  );

  assert.equal(db.state.batches.length, 1);
  const batch = db.state.batches[0];
  assert.equal(batch.length, 3);
  statement(batch, 'UPDATE integration_receipts SET event_id');
  statement(batch, "SET status='RECONCILED'");
  statement(batch, 'INSERT INTO edge_sync_checkpoints');
});

test('conflict evidence and edge conflict state are persisted in one D1 batch', async () => {
  const db = fakeD1();
  const store = createD1ReconciliationFinalityStore(db);

  const conflictId = await store.recordConflict(
    envelope(),
    'EMPLOYEE_ALREADY_EXISTS',
    ['cloud-existing'],
    '2026-09-14T11:00:07.000Z'
  );

  assert.equal(conflictId, 'sync:edge-store-1');
  assert.equal(db.state.batches.length, 1);
  const batch = db.state.batches[0];
  assert.equal(batch.length, 2);
  statement(batch, 'INSERT INTO conflict_corrections');
  statement(batch, "SET status='CONFLICT'");
});
