PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS vhdchy_meta (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at)
VALUES
  ('schema_version', 'business_core_v1', CURRENT_TIMESTAMP),
  ('event_model', 'immutable_v1', CURRENT_TIMESTAMP),
  ('projection_model', 'd1_outbox_to_google_v1', CURRENT_TIMESTAMP);

CREATE TABLE IF NOT EXISTS clusters (
  cluster_id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  cluster_type TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  feature_flags_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
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

CREATE TABLE IF NOT EXISTS resources (
  resource_id TEXT PRIMARY KEY,
  resource_type TEXT NOT NULL CHECK (resource_type IN ('PDA','USER_PICK','BAN_PACK','USER_PACK')),
  display_name TEXT,
  serial TEXT,
  last5 TEXT,
  source_owner TEXT,
  owner_cluster_id TEXT,
  state TEXT NOT NULL DEFAULT 'ACTIVE',
  availability TEXT NOT NULL DEFAULT 'AVAILABLE',
  note TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  entity_version INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (owner_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
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
  FOREIGN KEY (resource_id) REFERENCES resources(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT
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

CREATE INDEX IF NOT EXISTS ix_memberships_cluster
ON employee_cluster_memberships(cluster_id, membership_status);

CREATE INDEX IF NOT EXISTS ix_resources_cluster_type
ON resources(owner_cluster_id, resource_type, availability);

CREATE INDEX IF NOT EXISTS ix_sessions_cluster_date
ON work_sessions(cluster_id, business_date, status);

CREATE INDEX IF NOT EXISTS ix_labor_cluster_date
ON labor_records(cluster_id, business_date, state);

CREATE INDEX IF NOT EXISTS ix_dropped_goods_cluster_date
ON dropped_goods(cluster_id, business_date, state);

CREATE INDEX IF NOT EXISTS ix_outbox_status_retry
ON projection_outbox(status, next_attempt_at, outbox_id);
