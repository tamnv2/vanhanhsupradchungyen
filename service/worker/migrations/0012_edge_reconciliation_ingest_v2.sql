PRAGMA foreign_keys = ON;

ALTER TABLE edge_event_ingest ADD COLUMN payload_json TEXT NOT NULL DEFAULT '{}';
ALTER TABLE edge_event_ingest ADD COLUMN actor_user_id TEXT;
ALTER TABLE edge_event_ingest ADD COLUMN resulting_entity_version INTEGER CHECK (resulting_entity_version IS NULL OR resulting_entity_version >= 1);

CREATE INDEX IF NOT EXISTS ix_edge_ingest_actor_time
ON edge_event_ingest(actor_user_id, received_at);

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at) VALUES
  ('edge_reconciliation_model', 'edge_reconciliation_v2', CURRENT_TIMESTAMP);
