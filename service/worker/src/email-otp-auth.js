import {
  EMAIL_OTP_PURPOSES,
  consumeEmailOtp,
  hashOtpDestination,
  issueEmailOtp
} from './email-otp.js';
import { issueSession } from './auth-service.js';

export const EMAIL_OTP_REQUEST_PATH = '/api/v1/auth/email-otp/request';
export const EMAIL_OTP_USE_PATH = '/api/v1/auth/email-otp/use';

const ACTIVE = 'ACTIVE';
const ROOT = 'ROOT';

function normalizeUsername(value) {
  return String(value || '').trim();
}

function maskEmail(value) {
  const email = String(value || '').trim().toLowerCase();
  const at = email.indexOf('@');
  if (at <= 0 || at === email.length - 1) return null;
  const local = email.slice(0, at);
  const domain = email.slice(at + 1);
  const first = local.slice(0, 1);
  return `${first || '*'}***@${domain}`;
}

function safePepper(value) {
  const pepper = String(value || '');
  return pepper.length >= 32 && pepper.length <= 512 ? pepper : null;
}

function expectedPurpose(securityLevel) {
  return String(securityLevel || '').toUpperCase() === ROOT
    ? EMAIL_OTP_PURPOSES.ROOT_LOGIN
    : EMAIL_OTP_PURPOSES.PASSWORD_RECOVERY;
}

async function loadAccountByUsername(db, username) {
  return db.prepare(`
    SELECT
      user_id,
      username,
      employee_id,
      display_name,
      email,
      status AS user_status,
      security_level
    FROM auth_users
    WHERE username = ? COLLATE NOCASE
    LIMIT 1
  `).bind(username).first();
}

async function loadRootAllowlistMatch(db, destinationHash) {
  return db.prepare(`
    SELECT destination_hash, destination_hint
    FROM root_recovery_allowlist
    WHERE channel = 'EMAIL'
      AND destination_hash = ?
      AND status = 'ACTIVE'
    LIMIT 1
  `).bind(destinationHash).first();
}

async function loadIssuedChallenge(db, username, challengeId, nowIso) {
  return db.prepare(`
    SELECT
      c.challenge_id,
      c.user_id,
      c.purpose,
      c.state,
      c.expires_at,
      u.username,
      u.employee_id,
      u.display_name,
      u.status AS user_status,
      u.security_level
    FROM auth_email_otp_challenges c
    JOIN auth_users u ON u.user_id = c.user_id
    WHERE c.challenge_id = ?
      AND u.username = ? COLLATE NOCASE
      AND c.state = 'ISSUED'
      AND c.expires_at > ?
    LIMIT 1
  `).bind(challengeId, username, nowIso).first();
}

async function hasActiveTotp(db, userId) {
  const row = await db.prepare(`
    SELECT mfa_method_id
    FROM auth_mfa_methods
    WHERE user_id = ?
      AND method_type = 'TOTP'
      AND status = 'ACTIVE'
      AND verified_at IS NOT NULL
    LIMIT 1
  `).bind(userId).first();
  return Boolean(row?.mfa_method_id);
}

function deliverySubject(code) {
  return `VHDCHY OTP ${String(code || '').trim()}`;
}

export function createRuntimeEmailOtpDelivery(env) {
  const binding = env?.EMAIL_OTP_DELIVERY;
  if (!binding || typeof binding.fetch !== 'function') return null;

  return async payload => {
    const response = await binding.fetch('https://email-otp.internal/send', {
      method: 'POST',
      headers: {
        'content-type': 'application/json',
        'cache-control': 'no-store'
      },
      body: JSON.stringify({
        destination: payload.destination,
        subject: payload.subject,
        purpose: payload.purpose,
        challengeId: payload.challengeId,
        validForMs: payload.validForMs
      })
    });

    let result = null;
    try {
      result = await response.json();
    } catch {
      return { ok: false, reasonCode: 'DELIVERY_RESPONSE_INVALID' };
    }

    if (!response.ok || result?.ok !== true) {
      return {
        ok: false,
        reasonCode: String(result?.reasonCode || 'DELIVERY_REJECTED').slice(0, 80)
      };
    }

    return {
      ok: true,
      provider: result?.provider ? String(result.provider).slice(0, 64) : 'EMAIL_OTP_DELIVERY',
      deliveryRef: result?.deliveryRef ? String(result.deliveryRef).slice(0, 256) : null
    };
  };
}

export async function requestEmailOtpLogin(db, usernameValue, options = {}) {
  const username = normalizeUsername(usernameValue);
  const pepper = safePepper(options.pepper);
  if (!db || !username) return { ok: false, code: 'OTP_REQUEST_CONTEXT_REQUIRED' };
  if (!pepper) return { ok: false, code: 'OTP_RUNTIME_NOT_CONFIGURED' };
  if (typeof options.deliver !== 'function') return { ok: false, code: 'OTP_DELIVERY_NOT_CONFIGURED' };

  const account = await loadAccountByUsername(db, username);
  if (!account || String(account.user_status || '').toUpperCase() !== ACTIVE) {
    return { ok: false, code: 'OTP_RECOVERY_NOT_AVAILABLE' };
  }

  const securityLevel = String(account.security_level || 'NORMAL').toUpperCase();
  const purpose = expectedPurpose(securityLevel);
  let destination;
  let destinationHash;
  let destinationHint;

  try {
    if (securityLevel === ROOT) {
      destination = String(options.rootRecoveryEmail || '').trim().toLowerCase();
      if (!destination) return { ok: false, code: 'OTP_ROOT_DESTINATION_NOT_CONFIGURED' };
      destinationHash = await hashOtpDestination(destination, pepper);
      if (!destinationHash) return { ok: false, code: 'OTP_ROOT_DESTINATION_NOT_CONFIGURED' };
      const allowlist = await loadRootAllowlistMatch(db, destinationHash);
      if (!allowlist) return { ok: false, code: 'OTP_RECOVERY_NOT_AVAILABLE' };
      destinationHint = allowlist.destination_hint || maskEmail(destination);
    } else {
      destination = String(account.email || '').trim().toLowerCase();
      if (!destination) return { ok: false, code: 'OTP_RECOVERY_NOT_AVAILABLE' };
      destinationHash = await hashOtpDestination(destination, pepper);
      if (!destinationHash) return { ok: false, code: 'OTP_RECOVERY_NOT_AVAILABLE' };
      destinationHint = maskEmail(destination);
    }
  } catch {
    return { ok: false, code: 'OTP_RUNTIME_NOT_CONFIGURED' };
  }

  const issue = options.issueOtp || issueEmailOtp;
  const issued = await issue(db, {
    userId: account.user_id,
    purpose,
    destinationHash,
    destinationHint,
    pepper,
    nowMs: options.nowMs,
    requestId: options.requestId || null,
    deliver: async payload => options.deliver({
      ...payload,
      destination,
      subject: deliverySubject(payload.code)
    })
  });

  if (!issued.ok) return issued;
  return {
    ok: true,
    challengeId: issued.challengeId,
    expiresAt: issued.expiresAt,
    resendNotBefore: issued.resendNotBefore,
    destinationHint: issued.destinationHint || destinationHint,
    purpose
  };
}

export async function useEmailOtpLogin(db, input = {}, options = {}) {
  const username = normalizeUsername(input.username);
  const challengeId = String(input.challengeId || '').trim();
  const code = String(input.code || '').trim();
  const pepper = safePepper(options.pepper);
  const nowMs = Number(options.nowMs ?? Date.now());
  const nowIso = new Date(nowMs).toISOString();

  if (!db || !username || !challengeId || !/^\d{4}$/.test(code)) {
    return { ok: false, code: 'OTP_INVALID_OR_EXPIRED' };
  }
  if (!pepper) return { ok: false, code: 'OTP_RUNTIME_NOT_CONFIGURED' };

  const challenge = await loadIssuedChallenge(db, username, challengeId, nowIso);
  if (!challenge || String(challenge.user_status || '').toUpperCase() !== ACTIVE) {
    return { ok: false, code: 'OTP_INVALID_OR_EXPIRED' };
  }

  const securityLevel = String(challenge.security_level || 'NORMAL').toUpperCase();
  const purpose = expectedPurpose(securityLevel);
  if (challenge.purpose !== purpose) return { ok: false, code: 'OTP_INVALID_OR_EXPIRED' };

  if (securityLevel === ROOT) {
    const totpEnabled = await hasActiveTotp(db, challenge.user_id);
    if (totpEnabled && options.totpSatisfied !== true) {
      return {
        ok: false,
        code: 'ROOT_TOTP_REQUIRED',
        challenge: {
          challengeId,
          userId: challenge.user_id,
          username: challenge.username,
          requiredMethod: 'TOTP'
        }
      };
    }
  }

  const consume = options.consumeOtp || consumeEmailOtp;
  const consumed = await consume(db, {
    challengeId,
    userId: challenge.user_id,
    purpose,
    code,
    pepper,
    requestId: options.requestId || null,
    nowMs
  });
  if (!consumed.ok) return consumed;

  const mustChangePassword = securityLevel !== ROOT;
  const authMethodCode = securityLevel === ROOT
    ? (options.totpSatisfied === true ? 'EMAIL_OTP_TOTP' : 'EMAIL_OTP')
    : 'EMAIL_OTP_RECOVERY';
  const sessionIssuer = options.issueSessionFn || issueSession;
  const user = {
    userId: challenge.user_id,
    username: challenge.username,
    employeeId: challenge.employee_id || null,
    displayName: challenge.display_name || null,
    securityLevel,
    mustChangePassword
  };
  const session = await sessionIssuer(db, user, {
    nowMs,
    ttlMs: options.ttlMs,
    securityEpoch: options.securityEpoch || 1,
    deviceId: options.deviceId || null,
    authMethodCode
  });

  return {
    ok: true,
    user,
    session,
    authMethodCode
  };
}
