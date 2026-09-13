const encoder = new TextEncoder();

const COMMON_PASSWORDS = new Set([
  '12345678', '123456789', '1234567890', 'password', 'password1',
  'qwerty123', 'qwertyuiop', 'admin123', 'administrator', 'letmein123',
  'welcome123', 'iloveyou', 'abc12345', '11111111', '00000000',
  '1q2w3e4r', '1q2w3e4r5t', 'passw0rd', 'p@ssw0rd', 'changeme'
]);

function toBase64Url(bytes) {
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

function fromBase64Url(text) {
  const normalized = String(text || '').replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
  const binary = atob(padded);
  return Uint8Array.from(binary, char => char.charCodeAt(0));
}

function constantTimeEqual(left, right) {
  if (!(left instanceof Uint8Array) || !(right instanceof Uint8Array)) return false;
  if (left.length !== right.length) return false;
  let diff = 0;
  for (let i = 0; i < left.length; i += 1) diff |= left[i] ^ right[i];
  return diff === 0;
}

export function validatePasswordPolicy(password, username = '') {
  if (typeof password !== 'string') return { ok: false, code: 'PASSWORD_REQUIRED' };
  if (password.length < 8) return { ok: false, code: 'PASSWORD_TOO_SHORT' };
  if (password.length > 1024) return { ok: false, code: 'PASSWORD_TOO_LONG' };

  const normalized = password.normalize('NFKC').trim().toLowerCase();
  if (COMMON_PASSWORDS.has(normalized)) return { ok: false, code: 'PASSWORD_TOO_COMMON' };

  const normalizedUsername = String(username || '').normalize('NFKC').trim().toLowerCase();
  if (normalizedUsername && normalized === normalizedUsername) {
    return { ok: false, code: 'PASSWORD_EQUALS_USERNAME' };
  }

  return { ok: true };
}

async function derivePbkdf2(password, salt, iterations) {
  const keyMaterial = await crypto.subtle.importKey(
    'raw',
    encoder.encode(password),
    'PBKDF2',
    false,
    ['deriveBits']
  );
  const bits = await crypto.subtle.deriveBits(
    { name: 'PBKDF2', hash: 'SHA-256', salt, iterations },
    keyMaterial,
    256
  );
  return new Uint8Array(bits);
}

export async function createPasswordRecord(password, options = {}) {
  const iterations = Number(options.iterations || 100000);
  if (!Number.isSafeInteger(iterations) || iterations < 100000 || iterations > 1000000) {
    throw new Error('Invalid PBKDF2 iteration count');
  }
  const policy = validatePasswordPolicy(password, options.username || '');
  if (!policy.ok) {
    const error = new Error(policy.code);
    error.code = policy.code;
    throw error;
  }
  const salt = crypto.getRandomValues(new Uint8Array(16));
  const digest = await derivePbkdf2(password, salt, iterations);
  return {
    hashAlgorithm: `PBKDF2-SHA256$${iterations}$${toBase64Url(salt)}`,
    secretHash: toBase64Url(digest)
  };
}

export async function verifyPasswordRecord(password, hashAlgorithm, secretHash) {
  if (typeof password !== 'string' || typeof hashAlgorithm !== 'string' || typeof secretHash !== 'string') {
    return false;
  }
  const match = /^PBKDF2-SHA256\$(\d+)\$([A-Za-z0-9_-]+)$/.exec(hashAlgorithm);
  if (!match) return false;
  const iterations = Number(match[1]);
  if (!Number.isSafeInteger(iterations) || iterations < 100000 || iterations > 1000000) return false;
  let salt;
  let expected;
  try {
    salt = fromBase64Url(match[2]);
    expected = fromBase64Url(secretHash);
  } catch {
    return false;
  }
  if (salt.length < 16 || expected.length !== 32) return false;
  const actual = await derivePbkdf2(password, salt, iterations);
  return constantTimeEqual(actual, expected);
}

export function generateBearerToken(byteLength = 32) {
  if (!Number.isSafeInteger(byteLength) || byteLength < 24 || byteLength > 64) {
    throw new Error('Invalid bearer token length');
  }
  return toBase64Url(crypto.getRandomValues(new Uint8Array(byteLength)));
}

export async function hashBearerToken(token) {
  if (typeof token !== 'string' || token.length < 32 || token.length > 256) return null;
  const digest = await crypto.subtle.digest('SHA-256', encoder.encode(token));
  return toBase64Url(new Uint8Array(digest));
}

function decodeBase32(text) {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const clean = String(text || '').toUpperCase().replace(/[^A-Z2-7]/g, '');
  if (!clean) throw new Error('Invalid base32 secret');
  let buffer = 0;
  let bits = 0;
  const output = [];
  for (const char of clean) {
    const value = alphabet.indexOf(char);
    if (value < 0) throw new Error('Invalid base32 secret');
    buffer = (buffer << 5) | value;
    bits += 5;
    if (bits >= 8) {
      bits -= 8;
      output.push((buffer >> bits) & 0xff);
    }
  }
  if (!output.length) throw new Error('Invalid base32 secret');
  return Uint8Array.from(output);
}

function counterBytes(counter) {
  let value = BigInt(counter);
  const bytes = new Uint8Array(8);
  for (let i = 7; i >= 0; i -= 1) {
    bytes[i] = Number(value & 0xffn);
    value >>= 8n;
  }
  return bytes;
}

async function totpForCounter(secret, counter, digits) {
  const key = await crypto.subtle.importKey(
    'raw',
    secret,
    { name: 'HMAC', hash: 'SHA-1' },
    false,
    ['sign']
  );
  const signature = new Uint8Array(
    await crypto.subtle.sign('HMAC', key, counterBytes(counter))
  );
  const offset = signature[signature.length - 1] & 0x0f;
  const binary =
    ((signature[offset] & 0x7f) << 24) |
    ((signature[offset + 1] & 0xff) << 16) |
    ((signature[offset + 2] & 0xff) << 8) |
    (signature[offset + 3] & 0xff);
  return String(binary % (10 ** digits)).padStart(digits, '0');
}

export async function verifyTotpCode(secretBase32, code, options = {}) {
  const digits = Number(options.digits || 6);
  const period = Number(options.period || 30);
  const window = Number(options.window ?? 1);
  const nowMs = Number(options.nowMs ?? Date.now());
  if (![6, 8].includes(digits) || !Number.isSafeInteger(period) || period < 15 || period > 120) return false;
  if (!Number.isSafeInteger(window) || window < 0 || window > 2) return false;
  const normalizedCode = String(code || '').trim();
  if (!new RegExp(`^\\d{${digits}}$`).test(normalizedCode)) return false;

  let secret;
  try {
    secret = decodeBase32(secretBase32);
  } catch {
    return false;
  }

  const current = Math.floor(nowMs / 1000 / period);
  for (let delta = -window; delta <= window; delta += 1) {
    if (current + delta < 0) continue;
    const expected = await totpForCounter(secret, current + delta, digits);
    const left = encoder.encode(normalizedCode);
    const right = encoder.encode(expected);
    if (constantTimeEqual(left, right)) return true;
  }
  return false;
}
