# DATA MODEL GUIDE V2

Status: ACTIVE DESIGN MAP
Updated: 2026-09-13
Authority: effective Owner decisions through V5 and deployed `business_core_v3`.

## Principles

Use current-state records for fast reads and immutable events for history/retry/reconciliation. Business/display codes are not overloaded as durable technical identity. Cloud and LAN use the same domain meaning with different persistence adapters. Google remains downstream.

## Current Cloud D1 groups

Platform/configuration: `vhdchy_meta`, `clusters`, `modules`, `cluster_modules`, `module_domain_registry`, `shift_definitions`, `position_catalog`.

Employee/media: `media_objects`, `employees`, `employee_codes`, `employee_cluster_memberships`.

Accounts/authorization: `auth_users`, `auth_credentials`, `auth_roles`, `auth_permissions`, role/direct grant tables, MFA/recovery policy records, device/session/snapshot/audit records.

Attendance/session/task: `attendance_events`, `presence_state`, `extra_session_approvals`, `work_sessions`, `session_tasks`.

Resources: `resource_type_catalog`, `resource_registry`, `pack_table_user_mappings`, `cross_cluster_resource_borrows`, `resource_assignments`, `resource_daily_usage`.

Business records: `labor_type_catalog`, `labor_records`, `dropped_goods`, `documents`, `document_media`.

Events/projection/recovery: `domain_events`, conflict/audit records, `projection_outbox`, projection catalog/checkpoints, archive/snapshot/quota/compatibility/import records.

## Business mapping

- `employees` = person identity/current profile; `employee_codes` = MNV history.
- `attendance_events` = immutable IN/OUT history; `presence_state` = current presence.
- `work_sessions` = work context; `session_tasks` = PICK/PACK tasks under one session.
- `resource_assignments` = allocation history; `resource_daily_usage` = same-day lock/reissue state.
- `pack_table_user_mappings` = 1:n table-to-User-Pack configuration.
- `cross_cluster_resource_borrows` = use/approval context without changing ownership.
- documents separate metadata/lifecycle from one-or-many media objects.
- `domain_events` = immutable canonical history; `projection_outbox` = normal Cloud-to-Google asynchronous handoff.

## Cloud gaps created by the V3 LAN model

The existing Cloud schema predates full local-first LAN operation. The following logical concepts still need physical schema/source implementation:

1. trusted LAN edge source/epoch identity and sync cursor;
2. LAN event ingest/linkage from stable edge event identity to canonical Cloud event;
3. authority-snapshot version evidence carried with LAN accepted events;
4. Google Sheets/Drive completion receipts so Cloud does not duplicate output already produced by LAN;
5. explicit edge-vs-canonical reconciliation conflict evidence and resolver linkage.

Exact table/column names are implementation design, but these concepts are required by V3/V4 authority.

## LAN local logical groups

LAN persistence needs:
- runtime metadata and readiness;
- synchronized authority/configuration snapshots;
- verified operational snapshot/delta state;
- current state for offline-capable modules;
- immutable local edge events;
- Cloud synchronization outbox;
- Google projection/upload queues;
- integration receipts;
- conflict evidence;
- staged media metadata.

A LAN-accepted command is durable only after local state + immutable edge event + required sync/output work commit together.

## Stable command identity

Retry/reconciliation preserves request/idempotency identity, device ID/sequence where applicable, command/event type, target entity, expected/base version, normalized payload hash, environment/cluster scope, app/schema/business-rule version and LAN edge instance/epoch when locally accepted.

Changing Cloud/LAN route does not create a new logical business command.

## Current schema refinements still required

- seed/maintain the dynamic permission catalog used by Web/APK administration;
- represent operational device/resource condition such as a faulty PDA without destroying identity/history;
- keep ROOT factor details blocked until the two explicit Owner decisions in `DECISIONS_V5.md` are answered;
- add the V3 reconciliation/receipt concepts above through reviewed migrations.

## Google model

One workbook per environment + cluster + quarter. Use stored spreadsheet/file IDs and stable projection keys/checkpoints. Current BETA workbook schema remains `PP1291_SHEETS_BETA_V1`; new code must map to the reviewed existing tabs rather than creating an incompatible parallel schema.

## Retention and migration discipline

No fixed D1 hot-retention day count is locked. Archive/purge only after durable readback/checksum PASS and never for open/pending/unreconciled work. Snapshot/restore evidence is mandatory before STABLE.

Before any provider migration, verify exact D1 resource identity, schema and business-row state. STABLE receives accepted migration definitions but applies them only to its own isolated data.
