import test from 'node:test';
import assert from 'node:assert/strict';
import { claimProjectionRows, processProjectionOutbox } from '../src/projection-processor.js';

const rows = [
  { outbox_id: 1, event_id: 'EV1', attempts: 0 },
  { outbox_id: 2, event_id: 'EV2', attempts: 1 }
];

test('claim returns only rows actually transitioned from PENDING', async () => {
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
    async batch(statements) {
      assert.equal(statements.length, 2);
      return [
        { success: true, results: [{ outbox_id: 1, event_id: 'EV1' }] },
        { success: true, results: [] }
      ];
    }
  };
  const claimed = await claimProjectionRows(db, rows, '2026-09-15T00:00:00.000Z');
  assert.deepEqual(claimed.map(row => row.outbox_id), [1]);
  assert.match(prepared[0].sql, /status = 'PROCESSING'/);
  assert.match(prepared[0].sql, /status = 'PENDING'/);
  assert.match(prepared[0].sql, /RETURNING outbox_id, event_id/);
});

test('processor ACKs exact successful receipts and retries rejected rows', async () => {
  const acked = [];
  const failed = [];
  const result = await processProjectionOutbox({}, {
    nowMs: Date.parse('2026-09-15T00:00:00Z'),
    loadRows: async () => rows,
    claimRows: async (_db, loaded) => loaded,
    sendBatch: async () => ({
      ok: false,
      code: 'PROJECTION_GATEWAY_REJECTED',
      receipts: [
        { outboxId: 1, eventId: 'EV1', ok: true, status: 'UPDATED' },
        { outboxId: 2, eventId: 'EV2', ok: false, code: 'GATEWAY_ROW_INVALID' }
      ]
    }),
    ackRow: async (_db, row) => { acked.push(row.outbox_id); },
    failRow: async (_db, row, code) => { failed.push([row.outbox_id, code]); }
  });
  assert.equal(result.ok, false);
  assert.deepEqual(acked, [1]);
  assert.deepEqual(failed, [[2, 'GATEWAY_ROW_INVALID']]);
  assert.equal(result.acked, 1);
  assert.equal(result.failed, 1);
});

test('processor fails every claimed row on network failure with no receipts', async () => {
  const failed = [];
  const result = await processProjectionOutbox({}, {
    loadRows: async () => rows,
    claimRows: async (_db, loaded) => loaded,
    sendBatch: async () => ({ ok: false, code: 'PROJECTION_NETWORK_ERROR', retryable: true }),
    ackRow: async () => { throw new Error('SHOULD_NOT_ACK'); },
    failRow: async (_db, row, code) => { failed.push([row.outbox_id, code]); }
  });
  assert.equal(result.ok, false);
  assert.deepEqual(failed, [[1, 'PROJECTION_NETWORK_ERROR'], [2, 'PROJECTION_NETWORK_ERROR']]);
});

test('processor is idle without touching sender when no rows are pending', async () => {
  let sends = 0;
  const result = await processProjectionOutbox({}, {
    loadRows: async () => [],
    sendBatch: async () => { sends += 1; return { ok: true }; }
  });
  assert.equal(result.code, 'PROJECTION_IDLE');
  assert.equal(sends, 0);
});
