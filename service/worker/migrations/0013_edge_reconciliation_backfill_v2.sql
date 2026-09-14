PRAGMA foreign_keys = ON;

-- Compatibility backfill for environments that reached business_core_v3 before
-- the edge reconciliation tables were introduced. This migration intentionally
-- contains no permission catalog seeds; current permission authority is handled
-- by the dedicated permission catalog migration.

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at) VALUES
  ('domain_contract_version', 'VHDCHY_DOMAIN_V1', CURRENT_TIMESTAMP),
  ('permission_catalog_version', 'VHDCHY_PERMISSION_CATALOG_V1', CURRENT_TIMESTAMP),
  ('edge_reconciliation_model', 'edge_reconciliation_v2', CURRENT_TIMESTAMP);

CREATE TABLE IF NOT EXISTS edge_sources (
  edge_source_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  edge_instance_id TEXT NOT NULL,
  edge_epoch TEXT NOT NULL,
  domain_contract_version TEXT NOT NULL,
  edge_schema_version TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','BLOCKED','RETIRED','INCOMPATIBLE')),
  last_sync_cursor TEXT,
  last_sync_at TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (environment, edge_instance_id, edge_epoch),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_edge_sources_cluster_status
ON edge_sources(environment, cluster_id, status, updated_at);

CREATE TABLE IF NOT EXISTS edge_event_ingest (
  edge_event_id TEXT PRIMARY KEY,
  edge_source_id TEXT NOT NULL,
  request_id TEXT NOT NULL,
  idempotency_key TEXT NOT NULL,
  device_id TEXT,
  device_seq INTEGER CHECK (device_seq IS NULL OR device_seq > 0),
  command_code TEXT NOT NULL,
  event_type TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  base_entity_version INTEGER CHECK (base_entity_version IS NULL OR base_entity_version >= 1),
  resulting_entity_version INTEGER CHECK (resulting_entity_version IS NULL OR resulting_entity_version >= 1),
  payload_json TEXT NOT NULL DEFAULT '{}',
  payload_hash TEXT NOT NULL,
  actor_user_id TEXT,
  authority_snapshot_version TEXT NOT NULL,
  local_order INTEGER CHECK (local_order IS NULL OR local_order >= 1),
  local_accepted_at TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'RECEIVED' CHECK (status IN ('RECEIVED','RECONCILED','CONFLICT','REJECTED','RETRY_WAIT')),
  canonical_event_id TEXT,
  conflict_id TEXT,
  last_error_code TEXT,
  received_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (edge_source_id) REFERENCES edge_sources(edge_source_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (device_id) REFERENCES device_registry(device_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (canonical_event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (conflict_id) REFERENCES conflict_corrections(conflict_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_edge_ingest_source_idempotency
ON edge_event_ingest(edge_source_id, idempotency_key);

CREATE UNIQUE INDEX IF NOT EXISTS ux_edge_ingest_device_sequence
ON edge_event_ingest(edge_source_id, device_id, device_seq)
WHERE device_id IS NOT NULL AND device_seq IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_edge_ingest_status_order
ON edge_event_ingest(edge_source_id, status, local_order, received_at);

CREATE INDEX IF NOT EXISTS ix_edge_ingest_actor_time
ON edge_event_ingest(actor_user_id, received_at);

CREATE TABLE IF NOT EXISTS integration_receipts (
  receipt_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  edge_source_id TEXT,
  event_id TEXT,
  edge_event_id TEXT,
  target_kind TEXT NOT NULL CHECK (target_kind IN ('GOOGLE_SHEETS','GOOGLE_DRIVE')),
  logical_key TEXT NOT NULL,
  provider_ref TEXT,
  content_hash TEXT,
  checkpoint_ref TEXT,
  status TEXT NOT NULL DEFAULT 'COMPLETED' CHECK (status IN ('PENDING','COMPLETED','VERIFIED','ERROR')),
  completed_at TEXT,
  verified_at TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (environment, target_kind, logical_key),
  FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (edge_source_id) REFERENCES edge_sources(edge_source_id) ON UPDATE CASCADE ON DELETE SET NULL,
  FOREIGN KEY (event_id) REFERENCES domain_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY (edge_event_id) REFERENCES edge_event_ingest(edge_event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_integration_receipts_event
ON integration_receipts(event_id, edge_event_id, target_kind, status);

CREATE TABLE IF NOT EXISTS edge_sync_checkpoints (
  edge_source_id TEXT PRIMARY KEY,
  last_edge_event_id TEXT,
  last_local_order INTEGER CHECK (last_local_order IS NULL OR last_local_order >= 0),
  last_authority_snapshot_version TEXT,
  last_reconciled_at TEXT,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (edge_source_id) REFERENCES edge_sources(edge_source_id) ON UPDATE CASCADE ON DELETE CASCADE,
  FOREIGN KEY (last_edge_event_id) REFERENCES edge_event_ingest(edge_event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);
