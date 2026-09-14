import test from 'node:test';
import assert from 'node:assert/strict';

import { handleRequest } from '../src/index.js';
import {
  EDGE_SCHEMA_VERSION
} from '../src/reconciliation.js';
import {
  RECONCILIATION_AUTH_VERSION,
  RECONCILIATION_AUTH_MAX_SKEW_MS,
  signReconciliationRequest,
  verifyReconciliationRequestSignature
} from '../src/reconciliation-request-auth.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}

const keyId = 'lan-beta-machine-01';
const keyMaterial = 'test-only-reconciliation-key-material-2026-09-14';
const routePath = '/api/v1/reconciliation/events';

function envelope() {
  return {
    eventId: 'edge-route-event-1',
    requestId: 'edge-route-request-1',
    idempotencyKey: 'edge-route-idem-1',
    environment: 'BETA',
    clusterId: 'PICK_PACK_1291',
    deviceId: 'PDA-ROUTE-001',
    deviceSeq: 1,
    edgeInstanceId: 'edge-route-instance-1',
    edgeEpoch: 'edge-route-epoch-1',
    commandCode: 'ATTENDANCE_IN',
    eventCode: 'ATTENDANCE_IN_RECORDED',
    entityType: 'employee',
    entityId: 'employee-route-1',
    baseVersion: 1,
    resultingVersion: 2,
    payloadJson: '{"employeeId":"employee-route-1"}',
    payloadHash: 'payload-route-hash-1',
    acceptedAt: '2026-09-14T04:00:00.000Z',
    authoritySnapshotVersion: 'authority-route-1',
    domainContractVersion: 'VHDCHY_DOMAIN_V1',
    edgeSchemaVersion: EDGE_SCHEMA_VERSION,
    actorUserId: 'operator-route-1',
    completedIntegrationReceipts: []
  };
}

function fakeD1() {
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
              return null;
            },
            async run() {
              return { success: true };
            }
          };
        }
      };
    },
    async batch(statements) {
      state.batches.push(statements);
      return statements.map(() => ({ success: true, results: [] }));
    }
  };
}

function env(db = fakeD1()) {
  return {
    DB: db,
    APP_ENV: 'BETA',
    BUILD_SHA: 'test-build',
    LAN_RECONCILIATION_KEY_ID: keyId,
    LAN_RECONCILIATION_SHARED_SECRET: keyMaterial
  };
}

async function signedHeaders(rawBody, timestampMs = Date.now(), nonce = 'route_nonce_1234567890') {
  const signed = await signReconciliationRequest({
    method: 'POST',
    path: routePath,
    timestampMs,
    nonce,
    environment: 'BETA',
    keyId,
    rawBody,
    keyMaterial
  });
  return {
    'content-type': 'application/json',
    'x-vhdchy-machine-key-id': keyId,
    'x-vhdchy-machine-timestamp': String(timestampMs),
    'x-vhdchy-machine-nonce': nonce,
    'x-vhdchy-content-sha256': signed.bodyHash,
    'x-vhdchy-machine-signature': signed.signature
  };
}

test('reconciliation route rejects missing machine authentication', async () => {
  const response = await handleRequest(new Request(`https://beta.example${routePath}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(envelope())
  }), env());
  assert.equal(response.status, 401);
  const body = await response.json();
  assert.equal(body.error.code, 'MACHINE_AUTH_INVALID');
});

test('reconciliation route accepts a correctly signed LAN envelope', async () => {
  const db = fakeD1();
  const rawBody = JSON.stringify(envelope());
  const response = await handleRequest(new Request(`https://beta.example${routePath}`, {
    method: 'POST',
    headers: await signedHeaders(rawBody),
    body: rawBody
  }), env(db));

  assert.equal(response.status, 202);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.reconciliationStatus, 'RECEIVED');
  assert.equal(body.edgeEventId, 'edge-route-event-1');
  assert.equal(body.machineAuthVersion, RECONCILIATION_AUTH_VERSION);
  assert.equal(db.state.batches.length, 1);
});

test('signature binds the exact request body', async () => {
  const original = JSON.stringify(envelope());
  const signed = await signReconciliationRequest({
    method: 'POST',
    path: routePath,
    timestampMs: Date.now(),
    nonce: 'body_bind_nonce_12345',
    environment: 'BETA',
    keyId,
    rawBody: original,
    keyMaterial
  });

  const result = await verifyReconciliationRequestSignature({
    method: 'POST',
    path: routePath,
    timestampMs: Date.now(),
    nonce: 'body_bind_nonce_12345',
    environment: 'BETA',
    keyId,
    rawBody: `${original} `,
    bodyHash: signed.bodyHash,
    signature: signed.signature,
    keyMaterial
  });
  assert.equal(result.ok, false);
  assert.equal(result.reason, 'BODY_HASH_MISMATCH');
});

test('machine authentication rejects timestamps outside the accepted window', async () => {
  const rawBody = JSON.stringify(envelope());
  const timestampMs = Date.now() - RECONCILIATION_AUTH_MAX_SKEW_MS - 1000;
  const signed = await signReconciliationRequest({
    method: 'POST',
    path: routePath,
    timestampMs,
    nonce: 'stale_time_nonce_12345',
    environment: 'BETA',
    keyId,
    rawBody,
    keyMaterial
  });

  const result = await verifyReconciliationRequestSignature({
    method: 'POST',
    path: routePath,
    timestampMs,
    nonce: 'stale_time_nonce_12345',
    environment: 'BETA',
    keyId,
    rawBody,
    bodyHash: signed.bodyHash,
    signature: signed.signature,
    keyMaterial
  });
  assert.equal(result.ok, false);
  assert.equal(result.reason, 'TIMESTAMP_OUTSIDE_WINDOW');
});
