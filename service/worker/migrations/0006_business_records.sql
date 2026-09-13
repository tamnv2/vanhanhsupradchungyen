PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS labor_type_catalog (
  labor_type_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  module_id TEXT,
  labor_type_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  default_deduct_staff INTEGER NOT NULL DEFAULT 0 CHECK (default_deduct_staff IN (0,1)),
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  config_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (cluster_id, labor_type_code),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (module_id) REFERENCES modules(module_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS labor_records (
  labor_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  session_id TEXT,
  employee_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  shift_definition_id TEXT,
  labor_type_id TEXT NOT NULL,
  source_cluster_id TEXT,
  consuming_cluster_id TEXT NOT NULL,
  started_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ended_at TEXT,
  state TEXT NOT NULL DEFAULT 'OPEN' CHECK (state IN ('OPEN','FINISHED','CANCELLED','CORRECTED')),
  note TEXT,
  deduct_staff INTEGER NOT NULL DEFAULT 0 CHECK (deduct_staff IN (0,1)),
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (session_id) REFERENCES work_sessions(session_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (shift_definition_id) REFERENCES shift_definitions(shift_definition_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (labor_type_id) REFERENCES labor_type_catalog(labor_type_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (source_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (consuming_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_labor_open_per_session
ON labor_records(session_id) WHERE session_id IS NOT NULL AND state = 'OPEN';

CREATE INDEX IF NOT EXISTS ix_labor_cluster_date
ON labor_records(consuming_cluster_id, business_date, state);

CREATE TABLE IF NOT EXISTS dropped_goods (
  record_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  do_code TEXT NOT NULL,
  package_count INTEGER NOT NULL CHECK (package_count >= 0),
  actor_user_id TEXT NOT NULL,
  input_mode TEXT NOT NULL DEFAULT 'MANUAL' CHECK (input_mode IN ('MANUAL','QR')),
  source_qr_hash TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (actor_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_dropped_goods_business
ON dropped_goods(cluster_id, business_date, do_code);

CREATE TABLE IF NOT EXISTS documents (
  document_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  category TEXT NOT NULL,
  business_date TEXT,
  uploader_user_id TEXT,
  state TEXT NOT NULL DEFAULT 'DRAFT' CHECK (state IN ('DRAFT','FINAL','REPLACED','CANCELLED')),
  replacement_of_document_id TEXT,
  final_hash_summary TEXT,
  note TEXT,
  finalized_at TEXT,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (uploader_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (replacement_of_document_id) REFERENCES documents(document_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS document_media (
  document_media_id TEXT PRIMARY KEY,
  document_id TEXT NOT NULL,
  media_id TEXT NOT NULL,
  page_no INTEGER,
  sort_order INTEGER NOT NULL DEFAULT 0,
  checksum_sha256 TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (document_id) REFERENCES documents(document_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (media_id) REFERENCES media_objects(media_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  UNIQUE (document_id, media_id)
);
