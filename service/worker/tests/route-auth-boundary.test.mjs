import test from 'node:test';
import assert from 'node:assert/strict';
import { createPasswordRecord } from '../src/auth.js';
import { handleRequest } from '../src/index.js';

if (!globalThis.crypto?.subtle) {
  const { webcrypto } = await import('node:crypto');
  globalThis.crypto = webcrypto;
}
if (!globalThis.btoa || !globalThis.atob) {
  globalThis.btoa = value => Buffer.from(value, 'binary').toString('base64');
  globalThis.atob = value => Buffer.from(value, 'base64').toString('binary');
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

function loginDb(loginRow) {
  const state = { sessionInsertArgs: null };
  return {
    state,
    prepare(sql) {
      if (sql.includes('FROM auth_users u')) {
        return {
          bind() {
            return { first: async () => loginRow };
          }
        };
      }
      if (sql.includes('INSERT INTO auth_sessions')) {
        return {
          bind(...args) {
            return {
              run: async () => {
                state.sessionInsertArgs = args;
                return { success: true };
              }
            };
          }
        };
      }
      throw new Error(`Unexpected login SQL: ${sql.slice(0, 80)}`);
    }
  };
}

function passwordChangeDb(row = sessionRow({ metadata_json: '{"mustChangePassword":true}' })) {
  const state = { batch: null };
  return {
    state,
    prepare(sql) {
      return {
        bind(...args) {
          return {
            sql,
            args,
            async first() {
              if (sql.includes('FROM auth_sessions s')) return row;
              if (sql.includes('SELECT credential_id') && sql.includes('FROM auth_credentials')) {
                return { credential_id: 'C-OLD' };
              }
              return null;
            },
            async all() {
              return { results: [] };
            },
            async run() {
              return { success: true };
            }
          };
        }
      };
    },
    async batch(statements) {
      state.batch = statements;
      return statements.map(() => ({ success: true, results: [] }));
    }
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

test('public password login issues a bearer token while persisting only its hash', async () => {
  const record = await createPasswordRecord('Worker-Valid-2026', { username: 'operator' });
  const db = loginDb({
    user_id: 'U-1',
    username: 'operator',
    employee_id: 'E-1',
    display_name: 'Operator',
    user_status: 'ACTIVE',
    security_level: 'NORMAL',
    secret_hash: record.secretHash,
    hash_algorithm: record.hashAlgorithm,
    must_change: 0
  });
  const request = new Request('https://beta.example/api/v1/auth/login', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ username: 'operator', password: 'Worker-Valid-2026' })
  });
  const response = await handleRequest(request, { DB: db, APP_ENV: 'BETA', BUILD_SHA: 'test-build' });
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.principal.username, 'operator');
  assert.equal(body.principal.authMethodCode, 'PASSWORD');
  assert.equal(typeof body.session.token, 'string');
  assert.ok(body.session.token.length >= 32);
  assert.ok(db.state.sessionInsertArgs);
  assert.notEqual(db.state.sessionInsertArgs[3], body.session.token);
});

test('ROOT password login never creates a permanent-password session and directs to email OTP', async () => {
  const db = loginDb({
    user_id: 'ROOT-1',
    username: 'admin',
    employee_id: null,
    display_name: 'ROOT',
    user_status: 'ACTIVE',
    security_level: 'ROOT',
    secret_hash: null,
    hash_algorithm: null,
    must_change: null
  });
  const request = new Request('https://beta.example/api/v1/auth/login', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'ignored-root-password' })
  });
  const response = await handleRequest(request, { DB: db, APP_ENV: 'BETA', BUILD_SHA: 'test-build' });
  assert.equal(response.status, 401);
  const body = await response.json();
  assert.equal(body.error.code, 'ROOT_EMAIL_OTP_REQUIRED');
  assert.equal(body.error.details.requiredMethod, 'EMAIL_OTP');
  assert.equal(db.state.sessionInsertArgs, null);
});

test('must-change-password session can establish a new permanent password', async () => {
  const db = passwordChangeDb();
  const request = new Request('https://beta.example/api/v1/auth/change-password', {
    method: 'POST',
    headers: {
      authorization: bearer,
      'content-type': 'application/json'
    },
    body: JSON.stringify({ newPassword: 'New-Permanent-2026' })
  });
  const response = await handleRequest(request, { DB: db, APP_ENV: 'BETA', BUILD_SHA: 'test-build' });
  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.ok, true);
  assert.equal(body.mustChangePassword, false);
  assert.ok(Array.isArray(db.state.batch));
  assert.equal(db.state.batch.length, 4);
  assert.ok(db.state.batch.some(statement => statement.sql.includes('INSERT INTO auth_credentials')));
  assert.ok(db.state.batch.some(statement => statement.sql.includes("SET status = 'REVOKED'")));
});
