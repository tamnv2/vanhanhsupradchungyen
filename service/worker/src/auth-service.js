import {
  generateBearerToken,
  hashBearerToken,
  verifyPasswordRecord
} from './auth.js';

const DEFAULT_SESSION_TTL_MS = 8 * 60 * 60 * 1000;
const ROOT_LEVEL = 'ROOT';

function safeSessionTtl(value) {
  const parsed = Number(value || DEFAULT_SESSION_TTL_MS);
  if (!Number.isSafeInteger(parsed)) return DEFAULT_SESSION_TTL_MS;
  return Math.max(15 * 60 * 1000, Math.min(parsed, 24 * 60 * 60 * 1000));
}

export async function loadLoginRecord(db, username) {
  const normalized = String(username || '').trim();
  if (!db || !normalized) return null;
  return db.prepare(`
    SELECT
      u.user_id,
      u.username,
      u.employee_id,
      u.display_name,
      u.status AS user_status,
      u.security_level,
      c.credential_id,
      c.secret_hash,
      c.hash_algorithm,
      c.must_change
    FROM auth_users u
    LEFT JOIN auth_credentials c
      ON c.user_id = u.user_id
      AND c.credential_type = 'PASSWORD'
      AND c.status = 'ACTIVE'
    WHERE u.username = ? COLLATE NOCASE
    LIMIT 1
  `).bind(normalized).first();
}

export async function verifyPrimaryLogin(db, username, password) {
  const row = await loadLoginRecord(db, username);
  if (!row || String(row.user_status || '').toUpperCase() !== 'ACTIVE') {
    return { ok: false, code: 'INVALID_CREDENTIALS' };
  }

  const securityLevel = String(row.security_level || 'NORMAL').toUpperCase();
  if (securityLevel === ROOT_LEVEL) {
    return {
      ok: false,
      code: 'ROOT_EMAIL_OTP_REQUIRED',
      challenge: {
        userId: row.user_id,
        username: row.username,
        requiredMethod: 'EMAIL_OTP'
      }
    };
  }

  if (!row.secret_hash || !row.hash_algorithm) {
    return { ok: false, code: 'INVALID_CREDENTIALS' };
  }
  const passwordOk = await verifyPasswordRecord(password, row.hash_algorithm, row.secret_hash);
  if (!passwordOk) return { ok: false, code: 'INVALID_CREDENTIALS' };

  return {
    ok: true,
    user: {
      userId: row.user_id,
      username: row.username,
      employeeId: row.employee_id || null,
      displayName: row.display_name || null,
      securityLevel,
      mustChangePassword: Number(row.must_change) === 1
    }
  };
}

export async function issueSession(db, user, options = {}) {
  if (!db || !user?.userId) throw new Error('SESSION_ISSUE_CONTEXT_REQUIRED');
  const token = generateBearerToken();
  const tokenHash = await hashBearerToken(token);
  const sessionId = crypto.randomUUID();
  const issuedAtMs = Number(options.nowMs ?? Date.now());
  const issuedAt = new Date(issuedAtMs).toISOString();
  const expiresAt = new Date(issuedAtMs + safeSessionTtl(options.ttlMs)).toISOString();
  const deviceId = options.deviceId || null;
  const securityEpoch = Math.max(1, Number(options.securityEpoch || 1));
  const authMethodCode = String(options.authMethodCode || 'PASSWORD');
  const metadata = {
    mustChangePassword: user.mustChangePassword === true
  };

  const result = await db.prepare(`
    INSERT INTO auth_sessions(
      auth_session_id, user_id, device_id, token_hash, status,
      issued_at, expires_at, auth_method_code, security_epoch, metadata_json
    ) VALUES (?, ?, ?, ?, 'ACTIVE', ?, ?, ?, ?, ?)
  `).bind(
    sessionId,
    user.userId,
    deviceId,
    tokenHash,
    issuedAt,
    expiresAt,
    authMethodCode,
    securityEpoch,
    JSON.stringify(metadata)
  ).run();

  if (result?.success !== true) throw new Error('SESSION_INSERT_FAILED');
  return {
    sessionId,
    token,
    issuedAt,
    expiresAt,
    mustChangePassword: metadata.mustChangePassword
  };
}

export async function loginWithPassword(db, username, password, options = {}) {
  const primary = await verifyPrimaryLogin(db, username, password);
  if (!primary.ok) return primary;
  const session = await issueSession(db, primary.user, {
    deviceId: options.deviceId || null,
    securityEpoch: options.securityEpoch || 1,
    ttlMs: options.ttlMs,
    nowMs: options.nowMs,
    authMethodCode: 'PASSWORD'
  });
  return { ok: true, user: primary.user, session };
}
