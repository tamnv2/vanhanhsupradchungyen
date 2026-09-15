import test from 'node:test';
import assert from 'node:assert/strict';
import { PROJECTION_SCHEDULE_LIMIT, runProjectionSchedule } from '../src/worker-entry.js';

const token = 'p'.repeat(48);

test('projection schedule fails closed when runtime dependencies are missing', async () => {
  const noDb = await runProjectionSchedule({});
  assert.equal(noDb.code, 'PROJECTION_DB_UNAVAILABLE');

  const noGateway = await runProjectionSchedule({ DB: {} });
  assert.equal(noGateway.code, 'PROJECTION_GATEWAY_URL_NOT_CONFIGURED');

  const noToken = await runProjectionSchedule({ DB: {}, GAS_EXEC_URL: 'https://example.test/exec' });
  assert.equal(noToken.code, 'PROJECTION_SHARED_TOKEN_NOT_CONFIGURED');
});

test('projection schedule passes only bounded non-secret runtime config to processor', async () => {
  let receivedDb = null;
  let received = null;
  const db = { marker: 'db' };
  const result = await runProjectionSchedule({
    DB: db,
    GAS_EXEC_URL: 'https://script.google.com/macros/s/example/exec',
    VHDCHY_PROJECTION_SHARED_TOKEN: token,
    APP_ENV: 'beta'
  }, {
    nowMs: Date.parse('2026-09-15T01:00:00Z'),
    processor: async (actualDb, options) => {
      receivedDb = actualDb;
      received = options;
      return { ok: true, code: 'PROJECTION_IDLE', loaded: 0, claimed: 0, acked: 0, failed: 0 };
    }
  });

  assert.equal(result.ok, true);
  assert.equal(receivedDb, db);
  assert.equal(received.gatewayUrl, 'https://script.google.com/macros/s/example/exec');
  assert.equal(received.environment, 'BETA');
  assert.equal(received.sharedToken, token);
  assert.equal(received.limit, PROJECTION_SCHEDULE_LIMIT);
  assert.equal(received.nowMs, Date.parse('2026-09-15T01:00:00Z'));
  assert.equal(JSON.stringify(result).includes(token), false);
});
