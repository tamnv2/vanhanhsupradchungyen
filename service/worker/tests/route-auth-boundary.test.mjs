import test from 'node:test';
import assert from 'node:assert/strict';
import { handleRequest } from '../src/index.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}

function sessionRow(overrides = {}) {
  return {
    auth_session_id: 'S-1',
    user_id: 'U-1',
    username: 'operator',
    employee_id: 'E-1',
    display_name: 'Operator',
    session_status: 'ACTIVE',
    user_status: 'ACTIVE',
    security_level: 'NORMAL',
    expires_at: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    device_id: null,
    device_status: null,
    session_security_epoch: 1,
    device_security_epoch: 1,
    metadata_json: '{"mustChangePassword":false}',
    auth_method_code: 'PASSWORD',
    ...overrides
  };
}

function makeDb(row = sessionRow()) {
  return {
    prepare(sql) {
      const isSession = sql.includes('FROM auth_sessions s');
      const isPermissions = sql.includes('FROM auth_user_role_grants urg');
      const schemaResult = { value: 'business_core_v3', schema_version: 'business_core_v3' };
      return {
        async first() {
          return schemaResult;
        },
        bind() {
          return {
            async first() {
              return isSession ? row : schemaResult;
            },
            async all() {
              return isPermissions ? { results: [] } : { results: [] };
            }
          };
        }
      };
    }
  };
}

function env(row = sessionRow()) {
  return {
    DB: makeDb(row),
    APP_ENV: 'BETA',
    BUILD_SHA: 'test-build'
  };
}

const bearer = `Bearer ${'a'.repeat(48)}`;

test('public health remains anonymous', async () => {
  const response = await handleRequest(new Request('https://beta.example/health'), env());
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.d1.schemaVersion, 'business_core_v3');
});

test('protected data and admin prefixes reject anonymous requests', async () => {
  for (const path of ['/api/v1/data/employees', '/api/v1/admin/accounts']) {
    const response = await handleRequest(new Request(`https://beta.example${path}`), env());
    assert.equal(response.status, 401);
    const body = await response.json();
    assert.equal(body.error.code, 'AUTH_TOKEN_REQUIRED');
  }
});

test('auth me returns authenticated principal and effective permission summary', async () => {
  const request = new Request('https://beta.example/api/v1/auth/me', {
    headers: { authorization: bearer }
  });
  const response = await handleRequest(request, env());
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.principal.userId, 'U-1');
  assert.equal(body.principal.username, 'operator');
  assert.equal(body.permissions.authority, 'GRANTS');
  assert.deepEqual(body.permissions.permissions, []);
});

test('authenticated business prefixes remain fail-closed until handlers exist', async () => {
  const request = new Request('https://beta.example/api/v1/data/employees', {
    headers: { authorization: bearer }
  });
  const response = await handleRequest(request, env());
  assert.equal(response.status, 404);
  const body = await response.json();
  assert.equal(body.error.code, 'ROUTE_NOT_IMPLEMENTED');
});

test('must-change-password session cannot enter ordinary business routes', async () => {
  const request = new Request('https://beta.example/api/v1/data/employees', {
    headers: { authorization: bearer }
  });
  const response = await handleRequest(request, env(sessionRow({
    metadata_json: '{"mustChangePassword":true}'
  })));
  assert.equal(response.status, 403);
  const body = await response.json();
  assert.equal(body.error.code, 'PASSWORD_CHANGE_REQUIRED');
});
