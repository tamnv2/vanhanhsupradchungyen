import { hashBearerToken } from './auth.js';

const ACTIVE = 'ACTIVE';

export function parseBearerToken(request) {
  const raw = request?.headers?.get?.('authorization') || '';
  const match = /^Bearer\s+([^\s]+)$/i.exec(raw.trim());
  if (!match) return null;
  const token = match[1];
  if (token.length < 32 || token.length > 256) return null;
  return token;
}

function parseMetadata(value) {
  if (!value) return {};
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

export function evaluateSessionRecord(row, nowMs = Date.now()) {
  if (!row) return { ok: false, code: 'SESSION_NOT_FOUND' };
  if (String(row.session_status || '').toUpperCase() !== ACTIVE) {
    return { ok: false, code: 'SESSION_NOT_ACTIVE' };
  }
  if (String(row.user_status || '').toUpperCase() !== ACTIVE) {
    return { ok: false, code: 'ACCOUNT_NOT_ACTIVE' };
  }

  const expiresAtMs = Date.parse(String(row.expires_at || ''));
  if (!Number.isFinite(expiresAtMs) || expiresAtMs <= nowMs) {
    return { ok: false, code: 'SESSION_EXPIRED' };
  }

  if (row.device_id) {
    if (String(row.device_status || '').toUpperCase() !== ACTIVE) {
      return { ok: false, code: 'DEVICE_NOT_ACTIVE' };
    }
    const sessionEpoch = Number(row.session_security_epoch);
    const deviceEpoch = Number(row.device_security_epoch);
    if (!Number.isSafeInteger(sessionEpoch) || !Number.isSafeInteger(deviceEpoch) || sessionEpoch !== deviceEpoch) {
      return { ok: false, code: 'SECURITY_EPOCH_MISMATCH' };
    }
  }

  const metadata = parseMetadata(row.metadata_json);
  return {
    ok: true,
    principal: {
      sessionId: row.auth_session_id,
      userId: row.user_id,
      username: row.username,
      employeeId: row.employee_id || null,
      displayName: row.display_name || null,
      securityLevel: String(row.security_level || 'NORMAL').toUpperCase(),
      deviceId: row.device_id || null,
      securityEpoch: Number(row.session_security_epoch || 1),
      expiresAt: row.expires_at,
      mustChangePassword: metadata.mustChangePassword === true,
      authMethodCode: row.auth_method_code || null
    }
  };
}

export async function authenticateRequest(request, env, nowMs = Date.now()) {
  if (!env?.DB) return { ok: false, code: 'AUTH_DB_UNAVAILABLE' };
  const token = parseBearerToken(request);
  if (!token) return { ok: false, code: 'AUTH_TOKEN_REQUIRED' };
  const tokenHash = await hashBearerToken(token);
  if (!tokenHash) return { ok: false, code: 'AUTH_TOKEN_INVALID' };

  const row = await env.DB.prepare(`
    SELECT
      s.auth_session_id,
      s.user_id,
      s.device_id,
      s.status AS session_status,
      s.expires_at,
      s.auth_method_code,
      s.security_epoch AS session_security_epoch,
      s.metadata_json,
      u.username,
      u.employee_id,
      u.display_name,
      u.status AS user_status,
      u.security_level,
      d.status AS device_status,
      COALESCE(e.security_epoch, 1) AS device_security_epoch
    FROM auth_sessions s
    JOIN auth_users u ON u.user_id = s.user_id
    LEFT JOIN device_registry d ON d.device_id = s.device_id
    LEFT JOIN device_security_epochs e ON e.device_id = s.device_id
    WHERE s.token_hash = ?
    LIMIT 1
  `).bind(tokenHash).first();

  return evaluateSessionRecord(row, nowMs);
}

export async function revokeSession(db, sessionId, revokedAtIso = new Date().toISOString()) {
  if (!db || !sessionId) return false;
  const result = await db.prepare(`
    UPDATE auth_sessions
    SET status = 'REVOKED', revoked_at = ?
    WHERE auth_session_id = ? AND status = 'ACTIVE'
  `).bind(revokedAtIso, sessionId).run();
  return Boolean(result?.success);
}
