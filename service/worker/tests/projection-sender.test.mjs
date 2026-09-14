import test from 'node:test';
import assert from 'node:assert/strict';
import {
  PROJECTION_SEND_MAX_ITEMS,
  buildGatewayProjectionBatch,
  sendProjectionBatch
} from '../src/projection-sender.js';

const token = 't'.repeat(40);
const row = (id, eventId = `EV-${id}`) => ({
  outbox_id: id,
  event_id: eventId,
  projection_target: 'GOOGLE_SHEETS',
  payload_json: JSON.stringify({ sheet: 'RA - VÀO TRONG CA', values: { 'Event ID': eventId, 'Mã nhân viên': 'E1' } })
});

test('projection sender builds bounded correlated gateway batch without changing business payload', () => {
  const batch = buildGatewayProjectionBatch([row(1), row(2)], { environment: 'beta', sharedToken: token });
  assert.equal(batch.protocol, 'VHDCHY_PROJECTION_V1');
  assert.equal(batch.environment, 'BETA');
  assert.equal(batch.sharedToken, token);
  assert.equal(batch.items[0].outboxId, 1);
  assert.equal(batch.items[0].eventId, 'EV-1');
  assert.equal(batch.items[0].sheet, 'RA - VÀO TRONG CA');
  assert.equal(batch.items[0].values['Event ID'], 'EV-1');
  assert.throws(() => buildGatewayProjectionBatch([], { sharedToken: token }), /PROJECTION_BATCH_SIZE_INVALID/);
  assert.throws(() => buildGatewayProjectionBatch(Array.from({ length: PROJECTION_SEND_MAX_ITEMS + 1 }, (_, i) => row(i + 1)), { sharedToken: token }), /PROJECTION_BATCH_SIZE_INVALID/);
  assert.throws(() => buildGatewayProjectionBatch([row(1, 'DUP'), row(1, 'DUP')], { sharedToken: token }), /PROJECTION_BATCH_IDENTITY_DUPLICATE/);
});

test('successful gateway response is read back into exact positional outbox receipts', async () => {
  let posted = null;
  const result = await sendProjectionBatch([row(7), row(8)], {
    gatewayUrl: 'https://script.google.com/macros/s/test/exec',
    environment: 'BETA',
    sharedToken: token,
    fetchImpl: async (_url, options) => {
      posted = JSON.parse(options.body);
      return new Response(JSON.stringify({
        ok: true,
        protocol: 'VHDCHY_PROJECTION_V1',
        environment: 'BETA',
        results: [
          { ok: true, status: 'APPENDED', sheet: 'RA - VÀO TRONG CA', key: 'EV-7' },
          { ok: true, status: 'UPDATED', sheet: 'RA - VÀO TRONG CA', key: 'EV-8' }
        ]
      }), { status: 200, headers: { 'content-type': 'application/json' } });
    }
  });
  assert.equal(posted.sharedToken, token);
  assert.equal(result.ok, true);
  assert.deepEqual(result.receipts.map(item => [item.outboxId, item.eventId, item.status]), [
    [7, 'EV-7', 'APPENDED'],
    [8, 'EV-8', 'UPDATED']
  ]);
  assert.equal(JSON.stringify(result).includes(token), false);
});

test('sender classifies retryable transport failures and rejects untrusted response identity', async () => {
  const throttled = await sendProjectionBatch([row(1)], {
    gatewayUrl: 'https://example.test/gateway', sharedToken: token,
    fetchImpl: async () => new Response('{}', { status: 429 })
  });
  assert.equal(throttled.ok, false);
  assert.equal(throttled.retryable, true);

  const badIdentity = await sendProjectionBatch([row(1)], {
    gatewayUrl: 'https://example.test/gateway', sharedToken: token,
    fetchImpl: async () => new Response(JSON.stringify({
      ok: true, protocol: 'WRONG', environment: 'BETA', results: [{ ok: true }]
    }), { status: 200, headers: { 'content-type': 'application/json' } })
  });
  assert.equal(badIdentity.code, 'PROJECTION_RESPONSE_IDENTITY_MISMATCH');
  assert.equal(badIdentity.retryable, false);

  const mismatch = await sendProjectionBatch([row(1), row(2)], {
    gatewayUrl: 'https://example.test/gateway', sharedToken: token,
    fetchImpl: async () => new Response(JSON.stringify({
      ok: true, protocol: 'VHDCHY_PROJECTION_V1', environment: 'BETA', results: [{ ok: true }]
    }), { status: 200, headers: { 'content-type': 'application/json' } })
  });
  assert.equal(mismatch.code, 'PROJECTION_RECEIPT_COUNT_MISMATCH');
  assert.equal(mismatch.retryable, true);
});
