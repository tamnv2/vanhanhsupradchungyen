PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS auth_users (
  user_id TEXT PRIMARY KEY,
  username TEXT NOT NULL UNIQUE,
  employee_id TEXT,
  display_name TEXT,
  email TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','DISABLED','LOCKED','ARCHIVED')),
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS auth_roles (
  role_id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  description TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS auth_permissions (
  permission_id TEXT PRIMARY KEY,
  resource_code TEXT NOT NULL,
  action_code TEXT NOT NULL,
  description TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
  UNIQUE (resource_code, action_code)
);

CREATE TABLE IF NOT EXISTS auth_role_permission_grants (
  role_id TEXT NOT NULL,
  permission_id TEXT NOT NULL,
  effect TEXT NOT NULL DEFAULT 'ALLOW' CHECK (effect IN ('ALLOW','DENY')),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (role_id, permission_id),
  FOREIGN KEY (role_id) REFERENCES auth_roles(role_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (permission_id) REFERENCES auth_permissions(permission_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS auth_user_role_grants (
  grant_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  role_id TEXT NOT NULL,
  cluster_id TEXT,
  module_id TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','REVOKED','EXPIRED')),
  effective_from TEXT,
  effective_to TEXT,
  granted_by_user_id TEXT,
  source_event_id TEXT,
  note TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (role_id) REFERENCES auth_roles(role_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (granted_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (source_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
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
  entity_version INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS auth_sessions (
  session_id TEXT PRIMARY KEY,
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
  session_id TEXT,
  outcome TEXT NOT NULL,
  reason_code TEXT,
  request_id TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  occurred_at TEXT NOT NULL,
  ingested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (session_id) REFERENCES auth_sessions(session_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_auth_user_role_scope
ON auth_user_role_grants(user_id, status, cluster_id, module_id);

CREATE INDEX IF NOT EXISTS ix_auth_sessions_user_status
ON auth_sessions(user_id, status, expires_at);

CREATE INDEX IF NOT EXISTS ix_auth_sessions_device_status
ON auth_sessions(device_id, status, expires_at);

CREATE INDEX IF NOT EXISTS ix_auth_audit_user_time
ON auth_audit_events(user_id, occurred_at);

CREATE TRIGGER IF NOT EXISTS trg_auth_audit_no_update
BEFORE UPDATE ON auth_audit_events
BEGIN
  SELECT RAISE(ABORT, 'auth_audit_events are immutable');
END;

CREATE TRIGGER IF NOT EXISTS trg_auth_audit_no_delete
BEFORE DELETE ON auth_audit_events
BEGIN
  SELECT RAISE(ABORT, 'auth_audit_events are immutable');
END;

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at)
VALUES
  ('schema_version', 'business_core_v2', CURRENT_TIMESTAMP),
  ('auth_model', 'rbac_scope_v1', CURRENT_TIMESTAMP),
  ('session_model', 'token_hash_snapshot_v1', CURRENT_TIMESTAMP);

-- Intentionally no user, role, credential or privileged-login seed here.
-- Credential acceptance policy is a separate contract and must follow the current VHDCHY spec.
