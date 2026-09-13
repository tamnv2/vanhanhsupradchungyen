PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS auth_users (
  user_id TEXT PRIMARY KEY,
  username TEXT NOT NULL UNIQUE,
  employee_id TEXT,
  display_name TEXT,
  email TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','DISABLED','LOCKED','CLOSED','ARCHIVED')),
  security_level TEXT NOT NULL DEFAULT 'NORMAL' CHECK (security_level IN ('NORMAL','SUPERADMIN','ROOT')),
  closed_at TEXT,
  close_reason TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_auth_user_active_employee
ON auth_users(employee_id) WHERE employee_id IS NOT NULL AND status = 'ACTIVE';

CREATE TABLE IF NOT EXISTS auth_credentials (
  credential_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  credential_type TEXT NOT NULL DEFAULT 'PASSWORD' CHECK (credential_type IN ('PASSWORD')),
  secret_hash TEXT NOT NULL,
  hash_algorithm TEXT NOT NULL,
  must_change INTEGER NOT NULL DEFAULT 0 CHECK (must_change IN (0,1)),
  compromised_at TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  replaced_at TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','REPLACED','REVOKED')),
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_auth_active_password
ON auth_credentials(user_id, credential_type) WHERE status = 'ACTIVE';

CREATE TABLE IF NOT EXISTS auth_roles (
  role_id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  description TEXT,
  rank_no INTEGER NOT NULL DEFAULT 100,
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
  effective_from TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  effective_to TEXT,
  granted_by_user_id TEXT,
  parent_grant_id TEXT,
  source_event_id TEXT,
  note TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  revoked_at TEXT,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (role_id) REFERENCES auth_roles(role_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (granted_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (parent_grant_id) REFERENCES auth_user_role_grants(grant_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS auth_user_permission_grants (
  grant_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  permission_id TEXT NOT NULL,
  cluster_id TEXT,
  module_id TEXT,
  effect TEXT NOT NULL DEFAULT 'ALLOW' CHECK (effect IN ('ALLOW','DENY')),
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','REVOKED','EXPIRED')),
  effective_from TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  effective_to TEXT,
  granted_by_user_id TEXT,
  parent_grant_id TEXT,
  note TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  revoked_at TEXT,
  FOREIGN KEY (user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (permission_id) REFERENCES auth_permissions(permission_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (granted_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (parent_grant_id) REFERENCES auth_user_permission_grants(grant_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_auth_role_scope ON auth_user_role_grants(user_id, status, cluster_id, module_id);
CREATE INDEX IF NOT EXISTS ix_auth_permission_scope ON auth_user_permission_grants(user_id, status, cluster_id, module_id);
