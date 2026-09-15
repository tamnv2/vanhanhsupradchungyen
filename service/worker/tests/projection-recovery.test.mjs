import test from 'node:test';
import assert from 'node:assert/strict';
import {
  loadPendingOutbox,
  PROJECTION_PROCESSING_LEASE_MS,
  projectionLeaseStaleBefore
} from '../src/projection.js';
import { claimProjectionRows, processProjectionOutbox } from '../src/projection-processor.js';

const NOW = Date.parse('2026-09-15T07:00:00.000Z');

test('pending loader also selects expired PROCESSING leases, not fresh in-flight rows', async () => {
  let captured = null;
  const db = {
    prepare(sql) {
      return {
        bind(...args) {
          captured = { sql, args };
          return { async all() { return { results: [] }; } };
        }
      };
    }
  };
  await loadPendingOutbox(db, 25, NOW);
  assert.match(captured.sql, /status = 'PENDING'/);
  assert.match(captured.sql, /status = 'PROCESSING'/);
  assert.match(captured.sql, /updated_at <= \?/);
  assert.equal(captured.args[2], new Date(NOW - PROJECTION_PROCESSING_LEASE_MS).toISOString());
});

test('claim CAS permits stale PROCESSING recovery but remains race-safe', async () => {
  const prepared = [];
  const db = {
    prepare(sql) {
      return {
        bind(...args) {
          const statement = { sql, args };
          prepared.push(statement);
          return statement;
        }
      };
    },
    async batch() {
      return [{ results: [{ outbox_id: 7, event_id: 'EV7' }] }];
    }
  };
  const row = { outbox_id: 7, event_id: 'EV7', status: 'PROCESSING', attempts: 0 };
  const nowIso = new Date(NOW).toISOString();
  const stale = projectionLeaseStaleBefore(NOW);
  const claimed = await claimProjectionRows(db, [row], nowIso, stale);
  assert.equal(claimed.length, 1);
  assert.match(prepared[0].sql, /status = 'PROCESSING' AND updated_at <= \?/);
  assert.equal(prepared[0].args[3], stale);
});

test('materialization failure is retried independently while valid row still ACKs', async () => {
  const rows = [
    { outbox_id: 8, event_id: 'EV8', attempts: 0 },
    { outbox_id: 9, event_id: 'EV9', attempts: 0 }
  ];
  const acked = [];
  const failed = [];
  const result = await processProjectionOutbox({}, {
    nowMs: NOW,
    loadRows: async () => rows,
    claimRows: async (_db, loaded) => loaded,
    materializeRows: async (_db, claimed) => ({
      rows: [{ ...claimed[0], payload_json: '{"sheet":"RA - VÀO TRONG CA","values":{"Event ID":"EV8"}}' }],
      failures: [{ row: claimed[1], code: 'PROJECTION_EVENT_UNSUPPORTED:BAD' }]
    }),
    sendBatch: async sent => ({
      ok: true,
      receipts: sent.map(row => ({ outboxId: row.outbox_id, eventId: row.event_id, ok: true }))
    }),
    ackRow: async (_db, row) => { acked.push(row.outbox_id); },
    failRow: async (_db, row, code) => { failed.push([row.outbox_id, code]); }
  });
  assert.equal(result.ok, false);
  assert.deepEqual(acked, [8]);
  assert.deepEqual(failed, [[9, 'PROJECTION_EVENT_UNSUPPORTED:BAD']]);
  assert.equal(result.acked, 1);
  assert.equal(result.failed, 1);
});

console.log('PROJECTION_RECOVERY_PASS staleLease=PASS claimCas=PASS rowIsolation=PASS');
