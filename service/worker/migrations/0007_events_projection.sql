PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS domain_events (
  event_id TEXT PRIMARY KEY,
  event_type TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  entity_version INTEGER NOT NULL CHECK (entity_version >= 1),
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
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (actor_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (actor_employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (causation_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_domain_events_idempotency
ON domain_events(idempotency_key) WHERE idempotency_key IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_domain_events_device_sequence
ON domain_events(device_id, device_seq) WHERE device_id IS NOT NULL AND device_seq IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_domain_events_cluster_time
ON domain_events(cluster_id, occurred_at);

CREATE INDEX IF NOT EXISTS ix_domain_events_entity
ON domain_events(entity_type, entity_id, entity_version);

CREATE TRIGGER IF NOT EXISTS trg_domain_events_no_update
BEFORE UPDATE ON domain_events
BEGIN SELECT RAISE(ABORT, 'domain_events are immutable'); END;

CREATE TRIGGER IF NOT EXISTS trg_domain_events_no_delete
BEFORE DELETE ON domain_events
BEGIN SELECT RAISE(ABORT, 'domain_events are immutable'); END;

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
  FOREIGN KEY (resolver_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (correction_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS business_audit_events (
  audit_event_id TEXT PRIMARY KEY,
  event_type TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT,
  actor_user_id TEXT,
  request_id TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  occurred_at TEXT NOT NULL,
  ingested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (actor_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TRIGGER IF NOT EXISTS trg_business_audit_no_update
BEFORE UPDATE ON business_audit_events
BEGIN SELECT RAISE(ABORT, 'business_audit_events are immutable'); END;

CREATE TRIGGER IF NOT EXISTS trg_business_audit_no_delete
BEFORE DELETE ON business_audit_events
BEGIN SELECT RAISE(ABORT, 'business_audit_events are immutable'); END;

CREATE TABLE IF NOT EXISTS projection_outbox (
  outbox_id INTEGER PRIMARY KEY AUTOINCREMENT,
  event_id TEXT NOT NULL UNIQUE,
  projection_target TEXT NOT NULL DEFAULT 'GOOGLE_SHEETS',
  payload_json TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING','PROCESSING','ACKED','DEAD')),
  attempts INTEGER NOT NULL DEFAULT 0 CHECK (attempts >= 0),
  next_attempt_at TEXT,
  last_error_code TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_outbox_status_retry
ON projection_outbox(status, next_attempt_at, outbox_id);

CREATE TABLE IF NOT EXISTS projection_catalog (
  projection_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  quarter_key TEXT NOT NULL,
  spreadsheet_id TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','CLOSED','REBUILDING','ERROR')),
  schema_version TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (environment, cluster_id, quarter_key),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS projection_checkpoints (
  projection_id TEXT PRIMARY KEY,
  checkpoint_event_id TEXT,
  projected_through_at TEXT,
  last_success_at TEXT,
  last_error_code TEXT,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (projection_id) REFERENCES projection_catalog(projection_id) ON UPDATE CASCADE ON DELETE CASCADE,
  FOREIGN KEY (checkpoint_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS archive_batches (
  archive_batch_id TEXT PRIMARY KEY,
  archive_type TEXT NOT NULL,
  state TEXT NOT NULL DEFAULT 'PLANNED' CHECK (state IN ('PLANNED','WRITTEN','VERIFIED','PURGED','FAILED')),
  source_from TEXT,
  source_to TEXT,
  drive_ref TEXT,
  checksum_sha256 TEXT,
  row_counts_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  verified_at TEXT,
  purged_at TEXT
);

CREATE TABLE IF NOT EXISTS snapshots (
  snapshot_id TEXT PRIMARY KEY,
  snapshot_type TEXT NOT NULL,
  state TEXT NOT NULL DEFAULT 'PLANNED' CHECK (state IN ('PLANNED','CREATED','VERIFIED','RESTORE_TEST_PASS','FAILED')),
  storage_ref TEXT,
  checksum_sha256 TEXT,
  source_schema_version TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  verified_at TEXT,
  restore_tested_at TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}'
);

CREATE TABLE IF NOT EXISTS quota_health (
  health_id TEXT PRIMARY KEY,
  provider TEXT NOT NULL,
  resource_code TEXT NOT NULL,
  measured_at TEXT NOT NULL,
  usage_json TEXT NOT NULL DEFAULT '{}',
  status TEXT NOT NULL DEFAULT 'OK' CHECK (status IN ('OK','WARN','CRITICAL','UNKNOWN'))
);

CREATE INDEX IF NOT EXISTS ix_quota_health_resource_time
ON quota_health(provider, resource_code, measured_at);

CREATE TABLE IF NOT EXISTS compatibility_telemetry (
  telemetry_id TEXT PRIMARY KEY,
  device_id TEXT,
  client_type TEXT NOT NULL,
  client_version TEXT,
  schema_family TEXT,
  pending_item_count INTEGER NOT NULL DEFAULT 0 CHECK (pending_item_count >= 0),
  last_seen_at TEXT NOT NULL,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS ix_compatibility_last_seen
ON compatibility_telemetry(schema_family, pending_item_count, last_seen_at);

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
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (actor_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);
