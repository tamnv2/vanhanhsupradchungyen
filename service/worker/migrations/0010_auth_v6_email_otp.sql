PRAGMA foreign_keys = ON;

-- V6 email one-time-password lifecycle. Readable OTP values are never persisted.
CREATE TABLE IF NOT EXISTS auth_email_otp_challenges (
  challenge_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  purpose TEXT NOT NULL CHECK (purpose IN ('ROOT_LOGIN','PASSWORD_RECOVERY')),
  destination_hash TEXT NOT NULL,
  destination_hint TEXT,
  code_hash TEXT NOT NULL,
  code_digits INTEGER NOT NULL DEFAULT 4 CHECK (code_digits = 4),
  state TEXT NOT NULL DEFAULT 'REQUESTED' CHECK (
    state IN ('REQUESTED','ISSUED','USED','EXPIRED','SUPERSEDED','DELIVERY_FAILED','REVOKED')
  ),
  requested_at TEXT NOT NULL,
  issued_at TEXT,
  expires_at TEXT,
  resend_not_before TEXT,
  used_at TEXT,
  superseded_at TEXT,
  delivery_failed_at TEXT,
  delivery_provider TEXT,
  delivery_ref_hash TEXT,
  request_id TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

-- At most one request can be in the external-delivery window for one user/purpose.
CREATE UNIQUE INDEX IF NOT EXISTS ux_auth_email_otp_requested
ON auth_email_otp_challenges(user_id, purpose)
WHERE state = 'REQUESTED';

-- Exactly one successfully issued credential is current for one user/purpose.
CREATE UNIQUE INDEX IF NOT EXISTS ux_auth_email_otp_issued
ON auth_email_otp_challenges(user_id, purpose)
WHERE state = 'ISSUED';

CREATE INDEX IF NOT EXISTS ix_auth_email_otp_cooldown
ON auth_email_otp_challenges(user_id, purpose, issued_at DESC)
WHERE issued_at IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_auth_email_otp_state_expiry
ON auth_email_otp_challenges(state, expires_at);
