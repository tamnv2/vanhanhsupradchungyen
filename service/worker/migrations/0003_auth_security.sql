PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS auth_mfa_methods (
  mfa_method_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  method_type TEXT NOT NULL CHECK (method_type IN ('TOTP','EMAIL_OTP','SMS_OTP')),
  secret_ref TEXT,
  destination_hash TEXT,
  destination_hint TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','DISABLED','REVOKED')),
  verified_at TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS auth_recovery_challenges (
  challenge_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  channel TEXT NOT NULL CHECK (channel IN ('EMAIL','SMS')),
  destination_hash TEXT NOT NULL,
  code_hash TEXT NOT NULL,
  code_digits INTEGER NOT NULL DEFAULT 4 CHECK (code_digits = 4),
  state TEXT NOT NULL DEFAULT 'ISSUED' CHECK (state IN ('ISSUED','USED','EXPIRED','REVOKED')),
  issued_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expires_at TEXT NOT NULL,
  used_at TEXT,
  successor_challenge_id TEXT,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (successor_challenge_id) REFERENCES auth_recovery_challenges(challenge_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS root_security_policy (
  policy_id TEXT PRIMARY KEY CHECK (policy_id = 'ROOT'),
  totp_required INTEGER NOT NULL DEFAULT 1 CHECK (totp_required IN (0,1)),
  email_otp_required INTEGER NOT NULL DEFAULT 1 CHECK (email_otp_required IN (0,1)),
  sms_backup_enabled INTEGER NOT NULL DEFAULT 1 CHECK (sms_backup_enabled IN (0,1)),
  username_locked INTEGER NOT NULL DEFAULT 1 CHECK (username_locked IN (0,1)),
  recovery_allowlist_locked INTEGER NOT NULL DEFAULT 1 CHECK (recovery_allowlist_locked IN (0,1)),
  minimum_policy_locked INTEGER NOT NULL DEFAULT 1 CHECK (minimum_policy_locked IN (0,1)),
  policy_version INTEGER NOT NULL DEFAULT 1 CHECK (policy_version >= 1),
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS root_recovery_allowlist (
  allowlist_id TEXT PRIMARY KEY,
  channel TEXT NOT NULL CHECK (channel IN ('EMAIL','SMS')),
  destination_hash TEXT NOT NULL,
  destination_hint TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','REVOKED')),
  verified_at TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (channel, destination_hash)
);

CREATE TABLE IF NOT EXISTS device_registry (
  device_id TEXT PRIMARY KEY,
  device_type TEXT NOT NULL,
  platform TEXT,
  app_audience TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','BLOCKED','RETIRED')),
  public_key_ref TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  first_seen_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_seen_at TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1)
);

CREATE TABLE IF NOT EXISTS device_security_epochs (
  device_id TEXT PRIMARY KEY,
  security_epoch INTEGER NOT NULL DEFAULT 1 CHECK (security_epoch >= 1),
  reason TEXT,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS auth_sessions (
  auth_session_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  device_id TEXT,
  token_hash TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','REVOKED','EXPIRED')),
  issued_at TEXT NOT NULL,
  expires_at TEXT NOT NULL,
  revoked_at TEXT,
  last_seen_at TEXT,
  auth_method_code TEXT,
  permission_snapshot_id TEXT,
  security_epoch INTEGER NOT NULL DEFAULT 1,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS auth_permission_snapshots (
  snapshot_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  cluster_id TEXT,
  module_id TEXT,
  policy_version TEXT NOT NULL,
  permissions_json TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expires_at TEXT,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS auth_audit_events (
  auth_event_id TEXT PRIMARY KEY,
  event_type TEXT NOT NULL,
  user_id TEXT,
  username_snapshot TEXT,
  device_id TEXT,
  auth_session_id TEXT,
  outcome TEXT NOT NULL,
  reason_code TEXT,
  request_id TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  occurred_at TEXT NOT NULL,
  ingested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (auth_session_id) REFERENCES auth_sessions(auth_session_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TRIGGER IF NOT EXISTS trg_auth_audit_no_update
BEFORE UPDATE ON auth_audit_events
BEGIN SELECT RAISE(ABORT, 'auth_audit_events are immutable'); END;

CREATE TRIGGER IF NOT EXISTS trg_auth_audit_no_delete
BEFORE DELETE ON auth_audit_events
BEGIN SELECT RAISE(ABORT, 'auth_audit_events are immutable'); END;

INSERT OR IGNORE INTO root_security_policy(
  policy_id, totp_required, email_otp_required, sms_backup_enabled,
  username_locked, recovery_allowlist_locked, minimum_policy_locked, policy_version
) VALUES ('ROOT', 1, 1, 1, 1, 1, 1, 1);

-- Recovery destinations and TOTP secrets are provisioned securely at runtime and are not stored in this public repository.
