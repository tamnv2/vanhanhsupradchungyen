PRAGMA foreign_keys = OFF;

-- BETA-only zero-business-row rebuild preamble.
-- Safe only behind the automated preflight in .github/scripts/cloudflare-verify.mjs.
-- Never run if any business table contains rows or if the verified D1 identity/schema differs.
DROP TABLE IF EXISTS session_resource_bindings;
DROP TABLE IF EXISTS labor_records;
DROP TABLE IF EXISTS projection_outbox;
DROP TABLE IF EXISTS projection_catalog;
DROP TABLE IF EXISTS conflict_corrections;
DROP TABLE IF EXISTS document_metadata;
DROP TABLE IF EXISTS dropped_goods;
DROP TABLE IF EXISTS import_audit;
DROP TABLE IF EXISTS work_sessions;
DROP TABLE IF EXISTS employee_cluster_memberships;
DROP TABLE IF EXISTS resources;
DROP TABLE IF EXISTS domain_events;
DROP TABLE IF EXISTS employees;
DROP TABLE IF EXISTS shift_definitions;
DROP TABLE IF EXISTS clusters;
DROP TABLE IF EXISTS d1_migrations;
DROP TABLE IF EXISTS vhdchy_meta;

PRAGMA foreign_keys = ON;
