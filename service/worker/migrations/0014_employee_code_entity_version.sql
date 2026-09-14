PRAGMA foreign_keys = ON;

-- LAN/Cloud command parity requires employee-code identities to participate in
-- guarded entity-version semantics when an existing identity is updated.
-- Existing rows become version 1; later reviewed mutations must increment the
-- version atomically with their canonical state/event commit.
ALTER TABLE employee_codes
ADD COLUMN entity_version INTEGER NOT NULL DEFAULT 1 CHECK (entity_version >= 1);

INSERT OR REPLACE INTO vhdchy_meta(key, value, updated_at) VALUES
  ('employee_code_version_model', 'entity_version_v1', CURRENT_TIMESTAMP);
