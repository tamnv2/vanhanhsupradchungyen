# CHANGELOG

## 2026-09-13

### Product architecture V2 — Web + APK + Cloud Service + LAN Service
- Owner clarified the final product target: Website and PDA-optimized APK are clients of one VHDCHY business platform; Cloudflare Worker/D1 is the normal Cloud Service runtime; LAN Service is a real substitute runtime for site Internet loss, Cloud Service failure/degradation and per-client forced-LAN routing.
- Reclassified the immediately prior transport-only APK/Agent build as disposable prototype/reference evidence, not product authority.
- Persisted D-042..D-048 covering APK role, LAN substitution semantics, shared Cloud/LAN business model, offline reconciliation/conflict evidence, Google downstream behavior and legacy/prototype NON_AUTHORITY boundary.
- Added `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md` and `docs/DELIVERY_PLAN_V2.md`.
- Reworked `docs/ARCHITECTURE.md`, `docs/SERVICE_API_CONTRACT.md`, `docs/BETA_ACCEPTANCE_MATRIX.md`, `PROJECT_SCOPE.md`, `SERVICE_AUTHORITY.md`, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, and `CHECKPOINT.md` around one shared domain contract and dual Cloud/LAN runtimes.
- Superseded transport-only product authority in `docs/LAN_TRANSPORT_BETA_V1.md` while retaining reviewed low-level mechanics as reference.
- LAN autonomous mode is now defined as durable local edge state + immutable event + sync outbox with later D1 reconciliation; split-brain conflicts must be explicit and may not be silently overwritten/dropped.
- Google Sheets/Drive remain downstream/deferred during LAN autonomous operation; Sheets is never a LAN fallback database.
- Current exact implementation position moved to shared domain/API + Cloud/LAN adapter boundary before real vertical business Slice 1.

### Cloudflare D1 inspection automation
- Extended the retained Cloudflare verification script to inspect the exact verified BETA D1 read-only through Cloudflare API using the GitHub Environment `beta` secret at runner runtime.
- Inspection logs table names, `vhdchy_meta.schema_version`, and per-table row counts only; it does not read business row contents or issue INSERT/UPDATE/DELETE/DDL.
- Updated the Cloudflare verification workflow so relevant verification source/workflow changes trigger the read-only gate automatically.
- GitHub Actions run `34748247818` PASS: exact Worker/D1 identity confirmed, `schema_version=business_core_v1`, all measured business tables zero rows, bookkeeping only (`d1_migrations=1`, `vhdchy_meta=3`).
- Provider classification: `BUSINESS_CORE_V1 / ZERO_BUSINESS_ROWS / SCHEMA_MISMATCH`.

### Owner authority reconciliation
- Persisted Owner-approved 2026-09-13 business/data decisions into `DECISIONS.md` without publishing private recovery contact values.
- Marked the existing `business_core_v2` migration as stale relative to the new Owner target; it must not be applied.
- Advanced active work to complete target-schema reconciliation, local validation, pre-write reinspection, reviewed BETA migration, post-migration verification, and Worker/API integration.
- Reconciled `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, `SERVICE_AUTHORITY.md`, and `CHECKPOINT.md`.

## 2026-09-12

### AI authority/resume protocol v2
- Established `AI_ENTRYPOINT.md` as the fixed project bootstrap.
- Added `AI_OPERATING_CONTRACT.md`, `CONTEXT_INDEX.md`, and `CHECKPOINT.md` for authority, context economy, checkpoints, fail-closed execution and resume behavior.
- GitHub `main` remains the persistent project authority; memory is non-authoritative unless verified against current evidence.

### Provider foundation
- Reconciled Google Gateway BETA foundation and managed deployment.
- Verified Cloudflare BETA account, Worker `vhdchy-beta`, and D1 `vhdchy-data-beta` identities without recreating resources.
- Preserved pre-zero Android/LAN evidence under `backup/pre-zero-20260912` pending reviewed restoration.
