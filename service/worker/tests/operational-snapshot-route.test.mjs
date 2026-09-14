import test from 'node:test';
import assert from 'node:assert/strict';

import { handleRequest } from '../src/index.js';
import { EDGE_SCHEMA_VERSION } from '../src/reconciliation.js';
import { signReconciliationRequest } from '../src/reconciliation-request-auth.js';
import {
  OPERATIONAL_SNAPSHOT_CONTRACT_VERSION,
  OPERATIONAL_SNAPSHOT_PATH,
  SLICE1_OPERATIONAL_MODULE
} from '../src/operational-snapshot-route.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}

const keyId = 'lan-beta-machine-01';
const keyMaterial = 'test-only-reconciliation-key-material-2026-09-14';

function snapshotRequest(overrides = {}) {
  return {
    environment: 'BETA',
    clusterId: 'PICK_PACK_1291',
    edgeInstanceId: 'edge-snapshot-instance-1',
    edgeEpoch: 'edge-snapshot-epoch-1',
    domainContractVersion: 'VHDCHY_DOMAIN_V1',
    edgeSchemaVersion: EDGE_SCHEMA_VERSION,
    mode: 'FULL',
    requestedCanonicalEventIds: [],
    ...overrides
  };
}

function source(overrides = {}) {
  return {
    edge_source_id: 'BETA:PICK_PACK_1291:edge-snapshot-instance-1:edge-snapshot-epoch-1',
    environment: 'BETA',
    cluster_id: 'PICK_PACK_1291',
    edge_instance_id: 'edge-snapshot-instance-1',
    edge_epoch: 'edge-snapshot-epoch-1',
    domain_contract_version: 'VHDCHY_DOMAIN_V1',
    edge_schema_version: EDGE_SCHEMA_VERSION,
    status: 'ACTIVE',
    ...overrides
  };
}

function fakeD1(data = {}) {
  function statement(sql, args = []) {
    return {
      sql,
      args,
      bind(...bound) {
        return statement(sql, bound);
      }
    };
  }

  return {
    prepare(sql) {
      return statement(sql);
    },
    async batch(statements) {
      return statements.map(item => {
        const sql = item.sql.replace(/\s+/g, ' ');
        if (sql.includes('FROM edge_event_ingest e')) {
          if (data.assertCoverageInstanceScope) {
            assert.match(sql, /INNER JOIN edge_sources s ON s\.edge_source_id=e\.edge_source_id/);
            assert.match(sql, /s\.environment=\?/);
            assert.match(sql, /s\.cluster_id=\?/);
            assert.match(sql, /s\.edge_instance_id=\?/);
            assert.doesNotMatch(sql, /s\.edge_epoch=\?/);
            assert.deepEqual(item.args.slice(1, 6), [
              'BETA',
              'PICK_PACK_1291',
              'edge-snapshot-instance-1',
              'VHDCHY_DOMAIN_V1',
              EDGE_SCHEMA_VERSION
            ]);
          }
          return { success: true, results: data.coverage ?? [] };
        }
        if (sql.includes('FROM edge_sources')) return { success: true, results: data.source ? [data.source] : [] };
        if (sql.includes('FROM clusters')) return { success: true, results: [data.cluster ?? { cluster_id: 'PICK_PACK_1291', status: 'ACTIVE' }] };
        if (sql.includes('FROM employees')) return { success: true, results: data.employees ?? [] };
        if (sql.includes('FROM employee_codes')) return { success: true, results: data.employeeCodes ?? [] };
        if (sql.includes('FROM presence_state')) return { success: true, results: data.presence ?? [] };
        throw new Error(`Unexpected SQL in fake D1: ${sql}`);
      });
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

async function signedHeaders(rawBody, nonce = 'snapshot_nonce_1234567890') {
  const timestampMs = Date.now();
  const signed = await signReconciliationRequest({
    method: 'POST',
    path: OPERATIONAL_SNAPSHOT_PATH,
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

async function post(value, db = fakeD1(), headers = null, nonce = 'snapshot_nonce_1234567890') {
  const rawBody = JSON.stringify(value);
  return handleRequest(new Request(`https://beta.example${OPERATIONAL_SNAPSHOT_PATH}`, {
    method: 'POST',
    headers: headers ?? await signedHeaders(rawBody, nonce),
    body: rawBody
  }), env(db));
}

test('operational snapshot rejects missing machine authentication', async () => {
  const response = await post(snapshotRequest(), fakeD1(), { 'content-type': 'application/json' });
  assert.equal(response.status, 401);
  const body = await response.json();
  assert.equal(body.error.code, 'MACHINE_AUTH_INVALID');
});

test('operational snapshot allows an authenticated empty initial baseline before edge source registration', async () => {
  const response = await post(snapshotRequest(), fakeD1(), null, 'snapshot_empty_nonce_12345');
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.snapshotContractVersion, OPERATIONAL_SNAPSHOT_CONTRACT_VERSION);
  assert.equal(body.environment, 'BETA');
  assert.equal(body.clusterId, 'PICK_PACK_1291');
  assert.equal(body.edgeSourceRegistered, false);
  assert.deepEqual(body.coveredCanonicalEventIds, []);
  assert.deepEqual(body.counts, { employees: 0, employeeCodes: 0, presence: 0, canonicalCoverage: 0 });
  assert.deepEqual(JSON.parse(body.scopeJson), { modules: [SLICE1_OPERATIONAL_MODULE] });
  assert.deepEqual(JSON.parse(body.stateJson), { employees: [], employeeCodes: [], presence: [] });
  assert.match(body.snapshotVersion, /^OP-BETA-[0-9a-f]{32}$/);
  assert.match(body.sourceCheckpoint, /^D1-SLICE1-SHA256:[0-9a-f]{64}$/);
});

test('operational snapshot returns real Slice-1 D1 state and explicit canonical coverage for the exact current edge source', async () => {
  const db = fakeD1({
    source: source(),
    employees: [{
      employee_id: 'EMP-001', full_name: 'Employee 001', phone: null, status: 'ACTIVE',
      main_position: 'PICK', vendor: null, department: null, site: null, warehouse: null,
      start_date: null, permanent_leave_date: null, note: null, current_portrait_media_id: null,
      entity_version: 2
    }],
    employeeCodes: [{
      employee_code_id: 'MNV-ROW-001', employee_id: 'EMP-001', employee_code: 'MNV001', status: 'ACTIVE',
      assigned_at: '2026-09-14T00:00:00Z', released_at: null, release_reason: null, entity_version: 3
    }],
    presence: [{
      employee_id: 'EMP-001', current_state: 'IN', last_attendance_event_id: 'ATT-001',
      cluster_id: 'PICK_PACK_1291', business_date: '2026-09-14', updated_at: '2026-09-14T01:00:00Z',
      entity_version: 4
    }],
    coverage: [{ canonical_event_id: 'cloud:edge-event-1', canonical_ingested_at: '2026-09-14T01:01:00Z' }]
  });

  const response = await post(snapshotRequest({ requestedCanonicalEventIds: ['cloud:edge-event-1'] }), db, null, 'snapshot_valid_nonce_12345');
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.edgeSourceRegistered, true);
  assert.deepEqual(body.requestedCanonicalEventIds, ['cloud:edge-event-1']);
  assert.deepEqual(body.coveredCanonicalEventIds, ['cloud:edge-event-1']);
  assert.deepEqual(body.counts, { employees: 1, employeeCodes: 1, presence: 1, canonicalCoverage: 1 });

  const state = JSON.parse(body.stateJson);
  assert.equal(state.employees[0].employeeId, 'EMP-001');
  assert.equal(state.employees[0].entityVersion, 2);
  assert.equal(state.employeeCodes[0].employeeCode, 'MNV001');
  assert.equal(state.employeeCodes[0].entityVersion, 3);
  assert.equal(state.presence[0].currentState, 'IN');
  assert.equal(state.presence[0].entityVersion, 4);
});

test('operational snapshot rejects mismatched persisted current edge identity', async () => {
  const db = fakeD1({ source: source({ edge_instance_id: 'different-edge-instance' }) });
  const response = await post(snapshotRequest(), db, null, 'snapshot_identity_nonce_12345');
  assert.equal(response.status, 409);
  const body = await response.json();
  assert.equal(body.error.code, 'SYNC_CONFLICT');
  assert.equal(body.error.reason, 'EDGE_SOURCE_IDENTITY_MISMATCH');
});

test('operational snapshot fails closed when requested canonical coverage is not fully proven', async () => {
  const db = fakeD1({
    source: source(),
    coverage: [{ canonical_event_id: 'cloud:edge-event-1', canonical_ingested_at: '2026-09-14T01:01:00Z' }]
  });
  const response = await post(snapshotRequest({
    requestedCanonicalEventIds: ['cloud:edge-event-1', 'cloud:edge-event-2']
  }), db, null, 'snapshot_coverage_nonce_12345');

  assert.equal(response.status, 409);
  const body = await response.json();
  assert.equal(body.error.code, 'SYNC_CONFLICT');
  assert.equal(body.error.reason, 'CANONICAL_COVERAGE_INCOMPLETE');
  assert.equal(body.error.details.missingCount, 1);
  assert.deepEqual(body.error.details.missingCanonicalEventIds, ['cloud:edge-event-2']);
});

test('operational snapshot can prove historical reconciled coverage for the same persistent edge instance after epoch restart', async () => {
  const response = await post(snapshotRequest({
    edgeEpoch: 'new-runtime-epoch-after-restart',
    requestedCanonicalEventIds: ['cloud:old-epoch-edge-event-1']
  }), fakeD1({
    source: null,
    assertCoverageInstanceScope: true,
    coverage: [{ canonical_event_id: 'cloud:old-epoch-edge-event-1', canonical_ingested_at: '2026-09-14T01:01:00Z' }]
  }), null, 'snapshot_restart_nonce_12345');

  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.edgeSourceRegistered, false);
  assert.deepEqual(body.coveredCanonicalEventIds, ['cloud:old-epoch-edge-event-1']);
  assert.equal(body.edgeEpoch, 'new-runtime-epoch-after-restart');
});
