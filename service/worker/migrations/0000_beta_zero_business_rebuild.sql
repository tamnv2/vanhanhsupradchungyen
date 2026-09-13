PRAGMA defer_foreign_keys = ON;

-- BETA-only zero-business-row rebuild preamble.
-- Safe only after an exact read-only preflight confirms the verified BETA D1,
-- schema business_core_v1, the expected legacy table set, and zero business rows.
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

PRAGMA defer_foreign_keys = OFF;
