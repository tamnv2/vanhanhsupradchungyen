import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

async function readJson(path) {
  return JSON.parse(await readFile(new URL(`../../../${path}`, import.meta.url), 'utf8'));
}

async function readText(path) {
  return readFile(new URL(`../../../${path}`, import.meta.url), 'utf8');
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

test('BETA Worker deployment manifest includes every local JavaScript dependency', async () => {
  const deploy = await readJson('service/worker/deploy.beta.json');
  assert.equal(deploy.target_worker, 'vhdchy-beta');
  assert.equal(deploy.source, 'service/worker/src/index.js');
  assert.ok(Array.isArray(deploy.modules) && deploy.modules.length > 0);

  const modules = new Set(deploy.modules);
  assert.ok(modules.has('index.js'));
  for (const moduleName of modules) {
    assert.match(moduleName, /^[A-Za-z0-9._-]+\.js$/);
    const source = await readText(`service/worker/src/${moduleName}`);
    for (const match of source.matchAll(/from\s+['"]\.\/([^'"]+\.js)['"]/g)) {
      assert.ok(modules.has(match[1]), `${moduleName} imports ${match[1]} but deploy.beta.json does not package it`);
    }
  }

  for (const required of ['LAN_RECONCILIATION_KEY_ID', 'LAN_RECONCILIATION_SHARED_SECRET']) {
    assert.ok(deploy.inherit_bindings.includes(required), `missing inherited Worker binding ${required}`);
  }
});
