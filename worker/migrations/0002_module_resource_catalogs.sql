PRAGMA foreign_keys = ON;

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

CREATE TABLE IF NOT EXISTS resource_registry_v2 (
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

CREATE INDEX IF NOT EXISTS ix_cluster_modules_module
ON cluster_modules(module_id, status);

CREATE INDEX IF NOT EXISTS ix_resource_type_module
ON resource_type_catalog(module_id, status, type_code);

CREATE INDEX IF NOT EXISTS ix_resource_registry_cluster_type
ON resource_registry_v2(owner_cluster_id, resource_type_id, availability);

CREATE INDEX IF NOT EXISTS ix_position_catalog_module
ON position_catalog(module_id, status, position_code);

CREATE INDEX IF NOT EXISTS ix_labor_type_catalog_module
ON labor_type_catalog(module_id, status, labor_type_code);

-- First VHDCHY operational cluster/module. These records define ownership/configuration,
-- not the universe of DC-wide business types.
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

-- Preserve any v1 rows without mutating the historical v1 table. Future code writes v2.
INSERT OR IGNORE INTO resource_registry_v2(
  resource_id,
  resource_type_id,
  owner_cluster_id,
  display_name,
  serial,
  last5,
  source_owner,
  state,
  availability,
  note,
  metadata_json,
  entity_version,
  created_at,
  updated_at
)
SELECT
  resource_id,
  'PICK_PACK:' || resource_type,
  owner_cluster_id,
  display_name,
  serial,
  last5,
  source_owner,
  state,
  availability,
  note,
  metadata_json,
  entity_version,
  created_at,
  updated_at
FROM resources
WHERE resource_type IN ('PDA','USER_PICK','BAN_PACK','USER_PACK');

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at)
VALUES
  ('schema_version', 'business_core_v2', CURRENT_TIMESTAMP),
  ('module_model', 'cluster_modules_v1', CURRENT_TIMESTAMP),
  ('resource_model', 'generic_catalog_v2', CURRENT_TIMESTAMP);
