import test from 'node:test';
import assert from 'node:assert/strict';
import { PROJECTION_SCHEDULE_LIMIT, runProjectionSchedule } from '../src/worker-entry.js';

const token = 'p'.repeat(48);

test('projection schedule is a no-op while delivery activation gate is false', async () => {
  let calls = 0;
  const result = await runProjectionSchedule({
    DB: {},
    GAS_EXEC_URL: 'https://example.test/exec',
    VHDCHY_PROJECTION_SHARED_TOKEN: token,
    PROJECTION_DELIVERY_ENABLED: 'false'
  }, {
    processor: async () => { calls += 1; throw new Error('SHOULD_NOT_RUN'); }
  });
  assert.equal(result.ok, true);
  assert.equal(result.code, 'PROJECTION_DELIVERY_DISABLED');
  assert.equal(result.loaded, 0);
  assert.equal(calls, 0);
});

test('projection schedule fails closed when enabled runtime dependencies are missing', async () => {
  const enabled = { PROJECTION_DELIVERY_ENABLED: 'true' };
  const noDb = await runProjectionSchedule(enabled);
  assert.equal(noDb.code, 'PROJECTION_DB_UNAVAILABLE');

  const noGateway = await runProjectionSchedule({ ...enabled, DB: {} });
  assert.equal(noGateway.code, 'PROJECTION_GATEWAY_URL_NOT_CONFIGURED');

  const noToken = await runProjectionSchedule({ ...enabled, DB: {}, GAS_EXEC_URL: 'https://example.test/exec' });
  assert.equal(noToken.code, 'PROJECTION_SHARED_TOKEN_NOT_CONFIGURED');
});

test('projection schedule passes only bounded non-secret runtime config to processor when enabled', async () => {
  let receivedDb = null;
  let received = null;
  const db = { marker: 'db' };
  const result = await runProjectionSchedule({
    DB: db,
    GAS_EXEC_URL: 'https://script.google.com/macros/s/example/exec',
    VHDCHY_PROJECTION_SHARED_TOKEN: token,
    PROJECTION_DELIVERY_ENABLED: 'true',
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
