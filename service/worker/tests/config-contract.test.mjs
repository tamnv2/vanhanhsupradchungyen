import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

async function readJson(path) {
  return JSON.parse(await readFile(new URL(`../../../${path}`, import.meta.url), 'utf8'));
}

test('domain contract exposes required runtime states and identity fields', async () => {
  const contract = await readJson('contracts/domain.v1.json');
  assert.equal(contract.contractVersion, 'VHDCHY_DOMAIN_V1');
  assert.ok(contract.commitStatuses.includes('CLOUD_COMMITTED'));
  assert.ok(contract.commitStatuses.includes('LAN_ACCEPTED_PENDING_SYNC'));
  assert.ok(contract.commitStatuses.includes('SYNC_CONFLICT'));
  for (const field of ['requestId', 'idempotencyKey', 'environment', 'clusterId', 'commandCode', 'entityType', 'entityId', 'payloadHash']) {
    assert.ok(contract.requiredMutationIdentity.includes(field), `missing mutation field ${field}`);
  }
});

test('permission catalog has unique resource/action pairs and required permissions', async () => {
  const catalog = await readJson('config/permissions.v1.json');
  assert.equal(catalog.schemaVersion, 'VHDCHY_PERMISSION_CATALOG_V1');
  const keys = catalog.permissions.map((p) => `${p.resource}:${p.action}`);
  assert.equal(new Set(keys).size, keys.length);
  for (const required of [
    'attendance:scan',
    'resource:reissue',
    'pack.mapping:manage',
    'conflict:resolve',
    'lan.routing:force'
  ]) {
    assert.ok(keys.includes(required), `missing permission ${required}`);
  }
});
