PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS vhdchy_meta (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at)
VALUES
  ('schema_version', 'business_core_v2', CURRENT_TIMESTAMP),
  ('event_model', 'immutable_v1', CURRENT_TIMESTAMP),
  ('projection_model', 'd1_outbox_to_google_v1', CURRENT_TIMESTAMP),
  ('module_model', 'cluster_modules_v1', CURRENT_TIMESTAMP),
  ('resource_model', 'generic_catalog_v2', CURRENT_TIMESTAMP),
  ('auth_model', 'rbac_scope_v1', CURRENT_TIMESTAMP),
  ('session_model', 'token_hash_snapshot_v1', CURRENT_TIMESTAMP);

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
  cluster_id TEXT NOT NULL,
  shift_id TEXT NOT NULL,
  display_name TEXT NOT NULL,
  config_json TEXT NOT NULL DEFAULT '{}',
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (cluster_id, shift_id),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS employees (
  employee_id TEXT PRIMARY KEY,
  mnv TEXT NOT NULL UNIQUE,
  full_name TEXT NOT NULL,
  phone TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE',
  main_position TEXT,
  vendor TEXT,
  department TEXT,
  site TEXT,
  warehouse TEXT,
  start_date TEXT,
  permanent_leave_date TEXT,
  note TEXT,
  portrait_ref TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS employee_cluster_memberships (
  employee_id TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  membership_status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (membership_status IN ('ACTIVE','INACTIVE')),
  position TEXT,
  effective_from TEXT,
  effective_to TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (employee_id, cluster_id),
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS resource_type_catalog (
  resource_type_id TEXT PRIMARY KEY,
  module_id TEXT,
  type_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  metadata_schema_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (module_id, type_code),
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS resource_registry (
  resource_id TEXT PRIMARY KEY,
  resource_type_id TEXT NOT NULL,
  owner_cluster_id TEXT,
  display_name TEXT,
  serial TEXT,
  last5 TEXT,
  source_owner TEXT,
  state TEXT NOT NULL DEFAULT 'ACTIVE',
  availability TEXT NOT NULL DEFAULT 'AVAILABLE',
  note TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (resource_type_id) REFERENCES resource_type_catalog(resource_type_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (owner_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS position_catalog (
  cluster_id TEXT NOT NULL,
  module_id TEXT,
  position_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (cluster_id, position_code),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS labor_type_catalog (
  cluster_id TEXT NOT NULL,
  module_id TEXT,
  labor_type_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (cluster_id, labor_type_code),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS work_sessions (
  session_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  shift_id TEXT,
  shift_name_snapshot TEXT,
  employee_id TEXT NOT NULL,
  positions_json TEXT NOT NULL DEFAULT '[]',
  status TEXT NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','CLOSED','CANCELLED','CORRECTED')),
  entered_at TEXT,
  exited_at TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS session_resource_bindings (
  session_id TEXT NOT NULL,
  resource_id TEXT NOT NULL,
  assigned_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  released_at TEXT,
  source_event_id TEXT,
  note TEXT,
  PRIMARY KEY (session_id, resource_id, assigned_at),
  FOREIGN KEY (session_id) REFERENCES work_sessions(session_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (resource_id) REFERENCES resource_registry(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS labor_records (
  labor_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  session_id TEXT,
  employee_id TEXT,
  business_date TEXT NOT NULL,
  shift_id TEXT,
  labor_type TEXT NOT NULL,
  started_at TEXT,
  ended_at TEXT,
  state TEXT NOT NULL DEFAULT 'OPEN',
  note TEXT,
  deduct_staff INTEGER NOT NULL DEFAULT 0 CHECK (deduct_staff IN (0,1)),
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (session_id) REFERENCES work_sessions(session_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS dropped_goods (
  record_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  location TEXT,
  qr TEXT,
  do_code TEXT,
  package_count INTEGER NOT NULL DEFAULT 0 CHECK (package_count >= 0),
  state TEXT NOT NULL DEFAULT 'OPEN',
  note TEXT,
  actor_user_id TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS document_metadata (
  document_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  category TEXT NOT NULL,
  business_date TEXT,
  uploader_user_id TEXT,
  page_count INTEGER,
  drive_refs_json TEXT NOT NULL DEFAULT '[]',
  hash_summary TEXT,
  note TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE',
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS domain_events (
  event_id TEXT PRIMARY KEY,
  event_type TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  entity_version INTEGER NOT NULL,
  cluster_id TEXT,
  business_date TEXT,
  actor_user_id TEXT,
  actor_employee_id TEXT,
  device_id TEXT,
  device_seq INTEGER,
  idempotency_key TEXT,
  causation_event_id TEXT,
  correlation_id TEXT,
  payload_json TEXT NOT NULL DEFAULT '{}',
  app_version TEXT,
  occurred_at TEXT NOT NULL,
  ingested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_domain_events_idempotency
ON domain_events(idempotency_key)
WHERE idempotency_key IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_domain_events_device_sequence
ON domain_events(device_id, device_seq)
WHERE device_id IS NOT NULL AND device_seq IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_domain_events_cluster_time
ON domain_events(cluster_id, occurred_at);

CREATE INDEX IF NOT EXISTS ix_domain_events_entity
ON domain_events(entity_type, entity_id, entity_version);

CREATE TRIGGER IF NOT EXISTS trg_domain_events_no_update
BEFORE UPDATE ON domain_events
BEGIN
  SELECT RAISE(ABORT, 'domain_events are immutable');
END;

CREATE TRIGGER IF NOT EXISTS trg_domain_events_no_delete
BEFORE DELETE ON domain_events
BEGIN
  SELECT RAISE(ABORT, 'domain_events are immutable');
END;

CREATE TABLE IF NOT EXISTS conflict_corrections (
  conflict_id TEXT PRIMARY KEY,
  cluster_id TEXT,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  candidate_event_ids_json TEXT NOT NULL DEFAULT '[]',
  state TEXT NOT NULL DEFAULT 'OPEN' CHECK (state IN ('OPEN','RESOLVED','ESCALATED')),
  resolver_user_id TEXT,
  decision_reason TEXT,
  correction_event_id TEXT,
  opened_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  resolved_at TEXT,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (correction_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS projection_outbox (
  outbox_id INTEGER PRIMARY KEY AUTOINCREMENT,
  event_id TEXT NOT NULL UNIQUE,
  projection_target TEXT NOT NULL DEFAULT 'GOOGLE_SHEETS',
  payload_json TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING','PROCESSING','ACKED','DEAD')),
  attempts INTEGER NOT NULL DEFAULT 0,
  next_attempt_at TEXT,
  last_error_code TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS projection_catalog (
  cluster_id TEXT NOT NULL,
  quarter_key TEXT NOT NULL,
  spreadsheet_id TEXT NOT NULL,
  tab_map_json TEXT NOT NULL DEFAULT '{}',
  checkpoint_event_id TEXT,
  schema_version TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','CLOSED','REBUILDING','ERROR')),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (cluster_id, quarter_key),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (checkpoint_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS import_audit (
  import_job_id TEXT PRIMARY KEY,
  cluster_id TEXT,
  source_hash TEXT NOT NULL,
  import_domain TEXT NOT NULL,
  counts_json TEXT NOT NULL DEFAULT '{}',
  actor_user_id TEXT,
  result_file_ref TEXT,
  note TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

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

CREATE INDEX IF NOT EXISTS ix_memberships_cluster
ON employee_cluster_memberships(cluster_id, membership_status);

CREATE INDEX IF NOT EXISTS ix_cluster_modules_module
ON cluster_modules(module_id, status);

CREATE INDEX IF NOT EXISTS ix_resource_type_module
ON resource_type_catalog(module_id, status, type_code);

CREATE INDEX IF NOT EXISTS ix_resource_registry_cluster_type
ON resource_registry(owner_cluster_id, resource_type_id, availability);

CREATE INDEX IF NOT EXISTS ix_position_catalog_module
ON position_catalog(module_id, status, position_code);

CREATE INDEX IF NOT EXISTS ix_labor_type_catalog_module
ON labor_type_catalog(module_id, status, labor_type_code);

CREATE INDEX IF NOT EXISTS ix_sessions_cluster_date
ON work_sessions(cluster_id, business_date, status);

CREATE INDEX IF NOT EXISTS ix_labor_cluster_date
ON labor_records(cluster_id, business_date, state);

CREATE INDEX IF NOT EXISTS ix_dropped_goods_cluster_date
ON dropped_goods(cluster_id, business_date, state);

CREATE INDEX IF NOT EXISTS ix_outbox_status_retry
ON projection_outbox(status, next_attempt_at, outbox_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_role_scope
ON auth_user_role_grants(user_id, status, cluster_id, module_id);

CREATE INDEX IF NOT EXISTS ix_auth_sessions_user_status
ON auth_sessions(user_id, status, expires_at);

CREATE INDEX IF NOT EXISTS ix_auth_sessions_device_status
ON auth_sessions(device_id, status, expires_at);

CREATE INDEX IF NOT EXISTS ix_auth_audit_user_time
ON auth_audit_events(user_id, occurred_at);

INSERT OR IGNORE INTO clusters(cluster_id, display_name, cluster_type, status, feature_flags_json)
VALUES ('PICK_PACK_1291', 'Pick Pack 1291', 'PICK_PACK', 'ACTIVE', '{}');

INSERT OR IGNORE INTO modules(module_id, display_name, status, config_json)
VALUES ('PICK_PACK', 'Pick Pack Operations', 'ACTIVE', '{}');

INSERT OR IGNORE INTO cluster_modules(cluster_id, module_id, status, config_json)
VALUES ('PICK_PACK_1291', 'PICK_PACK', 'ACTIVE', '{}');

INSERT OR IGNORE INTO module_domain_registry(module_id, domain_code, display_name, status)
VALUES
  ('PICK_PACK', 'RESOURCE_ASSIGNMENT', 'Pick/Pack resource assignment', 'ACTIVE'),
  ('PICK_PACK', 'LABOR_SUPPORT', 'Công nhật / support work', 'ACTIVE'),
  ('PICK_PACK', 'DROPPED_GOODS', 'Nhận hàng rớt', 'ACTIVE');

INSERT OR IGNORE INTO resource_type_catalog(resource_type_id, module_id, type_code, display_name, status)
VALUES
  ('PICK_PACK:PDA', 'PICK_PACK', 'PDA', 'PDA', 'ACTIVE'),
  ('PICK_PACK:USER_PICK', 'PICK_PACK', 'USER_PICK', 'User Pick', 'ACTIVE'),
  ('PICK_PACK:BAN_PACK', 'PICK_PACK', 'BAN_PACK', 'Bàn Pack', 'ACTIVE'),
  ('PICK_PACK:USER_PACK', 'PICK_PACK', 'USER_PACK', 'User Pack', 'ACTIVE');

-- No historical employee/resource/business rows are seeded in the clean baseline.
-- No privileged user, password, role or credential is seeded here.
