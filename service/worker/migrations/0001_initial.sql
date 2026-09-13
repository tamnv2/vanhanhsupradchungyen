PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS vhdchy_meta (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at) VALUES
  ('schema_version', 'business_core_v3', CURRENT_TIMESTAMP),
  ('owner_contract', 'owner_approved_2026_09_13', CURRENT_TIMESTAMP),
  ('event_model', 'immutable_v1', CURRENT_TIMESTAMP),
  ('projection_model', 'd1_outbox_to_google_v1', CURRENT_TIMESTAMP),
  ('auth_model', 'rbac_direct_grants_mfa_v2', CURRENT_TIMESTAMP),
  ('resource_model', 'versioned_assignment_daily_lock_v3', CURRENT_TIMESTAMP),
  ('document_model', 'draft_final_replacement_v1', CURRENT_TIMESTAMP),
  ('compatibility_model', 'telemetry_guarded_retirement_v1', CURRENT_TIMESTAMP);

CREATE TABLE IF NOT EXISTS clusters (
  cluster_id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  cluster_type TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  feature_flags_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS modules (
  module_id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS cluster_modules (
  cluster_id TEXT NOT NULL,
  module_id TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (cluster_id, module_id),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS module_domain_registry (
  module_id TEXT NOT NULL,
  domain_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
  config_json TEXT NOT NULL DEFAULT '{}',
  PRIMARY KEY (module_id, domain_code),
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS shift_definitions (
  shift_definition_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  shift_code TEXT NOT NULL,
  version_no INTEGER NOT NULL CHECK (version_no >= 1),
  display_name TEXT NOT NULL,
  start_local TEXT,
  end_local TEXT,
  effective_from TEXT NOT NULL,
  effective_to TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  created_by_user_id TEXT,
  UNIQUE (cluster_id, shift_code, version_no),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_shift_effective
ON shift_definitions(cluster_id, shift_code, effective_from, effective_to, status);

CREATE TABLE IF NOT EXISTS position_catalog (
  position_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  module_id TEXT,
  position_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (cluster_id, position_code),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS media_objects (
  media_id TEXT PRIMARY KEY,
  storage_provider TEXT NOT NULL DEFAULT 'GOOGLE_DRIVE',
  storage_ref TEXT NOT NULL,
  media_kind TEXT NOT NULL,
  mime_type TEXT,
  checksum_sha256 TEXT,
  byte_size INTEGER CHECK (byte_size IS NULL OR byte_size >= 0),
  durable_state TEXT NOT NULL DEFAULT 'PENDING' CHECK (durable_state IN ('PENDING','DURABLE','DELETE_PENDING','DELETED','ERROR')),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  durable_at TEXT,
  deleted_at TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}'
);

CREATE TABLE IF NOT EXISTS employees (
  employee_id TEXT PRIMARY KEY,
  full_name TEXT NOT NULL,
  phone TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','LEFT','ARCHIVED')),
  main_position TEXT,
  vendor TEXT,
  department TEXT,
  site TEXT,
  warehouse TEXT,
  start_date TEXT,
  permanent_leave_date TEXT,
  note TEXT,
  current_portrait_media_id TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (current_portrait_media_id) REFERENCES media_objects(media_id) ON UPDATE CASCADE ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS employee_codes (
  employee_code_id TEXT PRIMARY KEY,
  employee_id TEXT NOT NULL,
  employee_code TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','RELEASED','ARCHIVED')),
  assigned_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  released_at TEXT,
  release_reason TEXT,
  created_by_user_id TEXT,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_employee_code_active ON employee_codes(employee_code) WHERE status = 'ACTIVE';
CREATE UNIQUE INDEX IF NOT EXISTS ux_employee_single_active_code ON employee_codes(employee_id) WHERE status = 'ACTIVE';
CREATE INDEX IF NOT EXISTS ix_employee_code_lookup ON employee_codes(employee_code, status, assigned_at);

CREATE TABLE IF NOT EXISTS employee_cluster_memberships (
  membership_id TEXT PRIMARY KEY,
  employee_id TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  membership_status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (membership_status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  position_id TEXT,
  effective_from TEXT NOT NULL,
  effective_to TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (position_id) REFERENCES position_catalog(position_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_memberships_employee_cluster
ON employee_cluster_memberships(employee_id, cluster_id, membership_status, effective_from);
