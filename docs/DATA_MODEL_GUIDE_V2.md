# DATA MODEL GUIDE V2

Status: ACTIVE DESIGN MAP
Updated: 2026-09-14
Authority: effective Owner decisions through V7 plus current reviewed source/migrations. Provider deployment state must be verified separately from source existence.

## Principles

Use current-state records for fast reads and immutable events for history/retry/reconciliation. Business/display codes are not overloaded as durable technical identity. Cloud and LAN use the same domain meaning with different persistence adapters. Google remains downstream.

## Current Cloud D1 groups

Platform/configuration: `vhdchy_meta`, `clusters`, `modules`, `cluster_modules`, `module_domain_registry`, `shift_definitions`, `position_catalog`.

Employee/media: `media_objects`, `employees`, `employee_codes`, `employee_cluster_memberships`.

Accounts/authorization: `auth_users`, `auth_credentials`, `auth_roles`, `auth_permissions`, role/direct grant tables, MFA/recovery policy records, device/session/snapshot/audit records. Current source also includes the V6 email-OTP challenge/audit model through migration `0010_auth_v6_email_otp.sql`; source presence does not prove that migration is already applied to a live provider database.

Attendance/session/task: `attendance_events`, `presence_state`, `extra_session_approvals`, `work_sessions`, `session_tasks`.

Resources: `resource_type_catalog`, `resource_registry`, `pack_table_user_mappings`, `cross_cluster_resource_borrows`, `resource_assignments`, `resource_daily_usage`.

Business records: `labor_type_catalog`, `labor_records`, `dropped_goods`, `documents`, `document_media`.

Events/projection/recovery: `domain_events`, conflict/audit records, `projection_outbox`, projection catalog/checkpoints, archive/snapshot/quota/compatibility/import records. Current source also contains additive LAN reconciliation foundations in migration `0009_edge_reconciliation.sql` and dynamic permission-catalog foundations in `0011_permission_catalog.sql`.

## Business mapping

- `employees` = person identity/current profile; `employee_codes` = MNV history.
- `attendance_events` = immutable IN/OUT history; `presence_state` = current presence.
- `work_sessions` = work context; `session_tasks` = PICK/PACK tasks under one session.
- `resource_assignments` = allocation history; `resource_daily_usage` = same-day lock/reissue state.
- `pack_table_user_mappings` = 1:n table-to-User-Pack configuration.
- `cross_cluster_resource_borrows` = use/approval context without changing ownership.
- Documents separate metadata/lifecycle from one-or-many media objects.
- `domain_events` = immutable canonical history; `projection_outbox` = normal Cloud-to-Google asynchronous handoff.

## LAN reconciliation model

The full LAN design requires stable edge identity/epoch, sync cursors, authority-snapshot evidence, stable edge-event identity, Cloud linkage, Google completion receipts and explicit reconciliation conflicts.

Current source has reviewed foundations for these concepts through migration `0009_edge_reconciliation.sql` plus LAN runtime/event/outbox implementation. Remaining work is not an unresolved product rule; it is implementation/provider/E2E completion: Cloud network ingest/transport, exact provider migration state, reconciliation execution and physical company-network/device acceptance must be demonstrated before PASS.

Central-versus-local authority is intentionally split by operating context:
- during LAN operation, the LAN edge state/event journal is operational authority for commands accepted locally under the latest synchronized authority snapshot;
- after synchronization/reconciliation, D1 is the central consolidated structured store;
- Google Sheets/Drive never become source truth for reconciliation.

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

## Authentication/account data rules

V6 resolves the previously open ROOT factor gate:
- ROOT has no permanent password login; primary ROOT login is email OTP;
- ROOT email OTP is exactly four decimal digits, single-use, valid for 5 minutes, with 5-minute resend cooldown;
- TOTP is optional and may be enabled/disabled by ROOT;
- normal-account recovery uses verified email OTP and then forces establishment of a new permanent password before ordinary product functions resume;
- secrets/readable OTP values never belong in source, Sheets, Drive business data or ordinary diagnostics.

Current Worker source contains the email-OTP primitive plus normal password/session/password-change foundations. Public ROOT OTP delivery/verification routing and a verified delivery provider remain implementation work; they are not Owner-decision blockers.

## Current schema/source refinements still required

- verify/apply reviewed additive migrations to each intended provider environment through controlled workflow rather than assuming source equals live state;
- complete and E2E-test dynamic permission-catalog use in Web/APK administration;
- represent operational device/resource condition such as a faulty PDA without destroying identity/history;
- complete Cloud/LAN network reconciliation/receipt execution and conflict resolution UI/workflow;
- complete ROOT email-OTP provider integration and public request/verify flow without exposing secrets.

## Google model

One workbook per environment + cluster + quarter. Use stored spreadsheet/file IDs and stable projection keys/checkpoints. Current BETA workbook schema remains `PP1291_SHEETS_BETA_V1`; new code must map to the reviewed existing tabs rather than creating an incompatible parallel schema.

## Retention and migration discipline

No fixed D1 hot-retention day count is locked. Archive/purge only after durable readback/checksum PASS and never for open/pending/unreconciled work. Snapshot/restore evidence is mandatory before STABLE.

Before any provider migration, verify exact D1 resource identity, schema and business-row state. STABLE receives accepted migration definitions but applies them only to its own isolated data.