PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS resource_type_catalog (
  resource_type_id TEXT PRIMARY KEY,
  module_id TEXT,
  type_code TEXT NOT NULL,
  display_name TEXT NOT NULL,
  daily_reuse_policy TEXT NOT NULL DEFAULT 'REUSABLE' CHECK (daily_reuse_policy IN ('REUSABLE','LOCK_AFTER_RELEASE_UNTIL_REISSUE')),
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
  owner_cluster_id TEXT NOT NULL,
  resource_code TEXT NOT NULL,
  display_name TEXT,
  serial TEXT,
  last5 TEXT,
  source_owner TEXT,
  state TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (state IN ('ACTIVE','INACTIVE','RETIRED','ARCHIVED')),
  note TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (resource_type_id, resource_code),
  FOREIGN KEY (resource_type_id) REFERENCES resource_type_catalog(resource_type_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (owner_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_resource_registry_owner_type
ON resource_registry(owner_cluster_id, resource_type_id, state);

CREATE TABLE IF NOT EXISTS pack_table_user_mappings (
  mapping_id TEXT PRIMARY KEY,
  cluster_id TEXT NOT NULL,
  pack_table_resource_id TEXT NOT NULL,
  pack_user_resource_id TEXT NOT NULL,
  shift_definition_id TEXT,
  effective_from TEXT NOT NULL,
  effective_to TEXT,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','ARCHIVED')),
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (pack_table_resource_id) REFERENCES resource_registry(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (pack_user_resource_id) REFERENCES resource_registry(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (shift_definition_id) REFERENCES shift_definitions(shift_definition_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_pack_mapping_effective
ON pack_table_user_mappings(cluster_id, pack_table_resource_id, status, effective_from, effective_to);

CREATE TABLE IF NOT EXISTS cross_cluster_resource_borrows (
  borrow_id TEXT PRIMARY KEY,
  resource_id TEXT NOT NULL,
  source_cluster_id TEXT NOT NULL,
  consuming_cluster_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  reason TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'APPROVED' CHECK (status IN ('PENDING','APPROVED','REJECTED','CLOSED','REVOKED')),
  requested_by_user_id TEXT,
  approved_by_user_id TEXT,
  requested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  approved_at TEXT,
  closed_at TEXT,
  FOREIGN KEY (resource_id) REFERENCES resource_registry(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (source_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (consuming_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (requested_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (approved_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS resource_assignments (
  assignment_id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL,
  task_id TEXT,
  resource_id TEXT NOT NULL,
  resource_role TEXT NOT NULL,
  consuming_cluster_id TEXT NOT NULL,
  borrow_id TEXT,
  assigned_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  released_at TEXT,
  release_reason TEXT,
  source_event_id TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  FOREIGN KEY (session_id) REFERENCES work_sessions(session_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (task_id) REFERENCES session_tasks(task_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (resource_id) REFERENCES resource_registry(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (consuming_cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (borrow_id) REFERENCES cross_cluster_resource_borrows(borrow_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_resource_assignment_active
ON resource_assignments(resource_id, released_at, consuming_cluster_id);

CREATE TABLE IF NOT EXISTS resource_daily_usage (
  resource_id TEXT NOT NULL,
  business_date TEXT NOT NULL,
  usage_state TEXT NOT NULL DEFAULT 'AVAILABLE' CHECK (usage_state IN ('AVAILABLE','IN_USE','USED_LOCKED','REISSUED_AVAILABLE')),
  current_assignment_id TEXT,
  first_used_at TEXT,
  last_released_at TEXT,
  reissue_count INTEGER NOT NULL DEFAULT 0 CHECK (reissue_count >= 0),
  updated_by_user_id TEXT,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1),
  PRIMARY KEY (resource_id, business_date),
  FOREIGN KEY (resource_id) REFERENCES resource_registry(resource_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (current_assignment_id) REFERENCES resource_assignments(assignment_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (updated_by_user_id) REFERENCES auth_users(user_id) ON UPDATE CASCADE ON DELETE RESTRICT
);
