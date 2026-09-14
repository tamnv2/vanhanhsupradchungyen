import {
  loadPendingOutbox,
  markOutboxAcked,
  markOutboxFailed
} from './projection.js';
import {
  PROJECTION_SEND_MAX_ITEMS,
  sendProjectionBatch
} from './projection-sender.js';

function keyOf(row) {
  return `${Number(row?.outbox_id)}:${String(row?.event_id || '')}`;
}

export async function claimProjectionRows(db, rows, nowIso = new Date().toISOString()) {
  if (!db || !Array.isArray(rows) || rows.length === 0) return [];
  const statements = rows.map(row => db.prepare(`
    UPDATE projection_outbox
    SET status = 'PROCESSING', updated_at = ?
    WHERE outbox_id = ? AND event_id = ? AND status = 'PENDING'
    RETURNING outbox_id, event_id
  `).bind(nowIso, Number(row.outbox_id), String(row.event_id)));
  const results = await db.batch(statements);
  const claimedKeys = new Set();
  for (const result of Array.isArray(results) ? results : []) {
    for (const claimed of Array.isArray(result?.results) ? result.results : []) claimedKeys.add(keyOf(claimed));
  }
  return rows.filter(row => claimedKeys.has(keyOf(row)));
}

export async function processProjectionOutbox(db, options = {}) {
  if (!db) return { ok: false, code: 'PROJECTION_DB_UNAVAILABLE', loaded: 0, claimed: 0, acked: 0, failed: 0 };
  const limit = Math.max(1, Math.min(PROJECTION_SEND_MAX_ITEMS, Number(options.limit) || PROJECTION_SEND_MAX_ITEMS));
  const nowMs = Number(options.nowMs ?? Date.now());
  const nowIso = new Date(nowMs).toISOString();
  const loadRows = options.loadRows || loadPendingOutbox;
  const claimRows = options.claimRows || claimProjectionRows;
  const sendBatch = options.sendBatch || sendProjectionBatch;
  const ackRow = options.ackRow || markOutboxAcked;
  const failRow = options.failRow || markOutboxFailed;

  const loaded = await loadRows(db, limit, nowMs);
  if (!loaded.length) return { ok: true, code: 'PROJECTION_IDLE', loaded: 0, claimed: 0, acked: 0, failed: 0 };
  const claimed = await claimRows(db, loaded, nowIso);
  if (!claimed.length) return { ok: true, code: 'PROJECTION_CLAIM_RACE', loaded: loaded.length, claimed: 0, acked: 0, failed: 0 };

  const delivery = await sendBatch(claimed, {
    gatewayUrl: options.gatewayUrl,
    environment: options.environment,
    sharedToken: options.sharedToken,
    fetchImpl: options.fetchImpl,
    timeoutMs: options.timeoutMs
  });

  const receiptByKey = new Map((delivery?.receipts || []).map(receipt => [
    `${Number(receipt.outboxId)}:${String(receipt.eventId || '')}`,
    receipt
  ]));
  let acked = 0;
  let failed = 0;

  for (const row of claimed) {
    const receipt = receiptByKey.get(keyOf(row));
    if (delivery?.ok === true || receipt?.ok === true) {
      await ackRow(db, row, nowIso);
      acked += 1;
      continue;
    }
    const code = receipt?.code || delivery?.code || 'PROJECTION_DELIVERY_FAILED';
    await failRow(db, row, code, nowMs);
    failed += 1;
  }

  return {
    ok: failed === 0,
    code: failed === 0 ? 'PROJECTION_BATCH_ACKED' : 'PROJECTION_BATCH_INCOMPLETE',
    loaded: loaded.length,
    claimed: claimed.length,
    acked,
    failed,
    deliveryCode: delivery?.code || null
  };
}
