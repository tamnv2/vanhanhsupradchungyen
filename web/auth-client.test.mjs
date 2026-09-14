import assert from 'node:assert/strict';
import { createAuthClient } from './auth.js';

class MemoryStorage {
  constructor(initial = {}) { this.values = new Map(Object.entries(initial)); }
  getItem(key) { return this.values.has(key) ? this.values.get(key) : null; }
  setItem(key, value) { this.values.set(key, String(value)); }
  removeItem(key) { this.values.delete(key); }
}

const json = (payload, status = 200) => new Response(JSON.stringify(payload), {
  status,
  headers: { 'content-type': 'application/json' }
});
const future = new Date(Date.now() + 3600000).toISOString();

const cloudStorage = new MemoryStorage();
const cloudToken = 'c'.repeat(48);
let cloudCalls = 0;
const cloud = createAuthClient({
  storage: cloudStorage,
  fetchImpl: async (path, options = {}) => {
    cloudCalls += 1;
    if (path === '/api/v1/auth/login') {
      assert.equal(options.headers.get('Authorization'), null);
      return json({
        principal: { userId: 'U1', username: 'u1', mustChangePassword: false },
        session: { token: cloudToken, expiresAt: future, mustChangePassword: false }
      });
    }
    assert.equal(options.headers.get('Authorization'), `Bearer ${cloudToken}`);
    if (path === '/api/v1/auth/me') return json({ principal: { userId: 'U1', username: 'u1', mustChangePassword: false } });
    if (path === '/api/v1/auth/change-password') return json({ ok: true, mustChangePassword: false });
    throw new Error(`UNEXPECTED:${path}`);
  }
});
cloud.configure({ runtime: 'CLOUD' });
await cloud.login('u1', 'p');
assert.equal(cloud.token(), cloudToken);
assert.equal((await cloud.me()).principal.userId, 'U1');
await cloud.changePassword('p2');
assert.equal(cloudCalls, 3);

let unsignedCalls = 0;
const unsignedLan = createAuthClient({
  storage: new MemoryStorage(),
  getLanSigner: () => null,
  fetchImpl: async () => { unsignedCalls += 1; return json({}); }
});
unsignedLan.configure({ runtime: 'LAN' });
await assert.rejects(() => unsignedLan.login('u1', 'p'), error => error?.code === 'LAN_SIGNER_REQUIRED');
assert.equal(unsignedCalls, 0);

const proof = {
  'X-VHDCHY-Device-Id': 'D1',
  'X-VHDCHY-Security-Epoch': 'E1',
  'X-VHDCHY-Timestamp-Ms': String(Date.now()),
  'X-VHDCHY-Nonce': 'N1',
  'X-VHDCHY-Signature': 'S1'
};
let signedCalls = 0;
const signedLan = createAuthClient({
  storage: new MemoryStorage(),
  getLanSigner: () => ({ signRequest: async () => proof }),
  fetchImpl: async (path, options = {}) => {
    signedCalls += 1;
    assert.equal(path, '/api/v1/auth/login');
    for (const [name, value] of Object.entries(proof)) assert.equal(options.headers.get(name), value);
    return json({
      session: { token: 'l'.repeat(48), expiresAt: future, mustChangePassword: false },
      user: { userId: 'LU1', username: 'lu1' }
    });
  }
});
signedLan.configure({ runtime: 'LAN' });
await signedLan.login('lu1', 'p');
assert.equal((await signedLan.me()).principal.userId, 'LU1');
assert.equal(signedCalls, 1);
await assert.rejects(() => signedLan.changePassword('p2'), error => error?.code === 'PASSWORD_CHANGE_ROUTE_UNAVAILABLE');

const expiredStorage = new MemoryStorage({
  'vhdchy.web.session.token': 'x'.repeat(48),
  'vhdchy.web.session.evidence': JSON.stringify({
    principal: { userId: 'OLD' },
    session: { expiresAt: new Date(Date.now() - 1000).toISOString() }
  })
});
const expiredLan = createAuthClient({ storage: expiredStorage, fetchImpl: async () => { throw new Error('NETWORK_NOT_ALLOWED'); } });
expiredLan.configure({ runtime: 'LAN' });
assert.equal(await expiredLan.me(), null);
assert.equal(expiredLan.token(), '');

console.log('WEB_AUTH_CLIENT_PASS cloudBearer=PASS lanUnsignedNoNetwork=PASS lanSignedHeaders=PASS lanExpiry=PASS lanPasswordChangeFailClosed=PASS');
