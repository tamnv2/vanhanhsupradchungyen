# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- Pre-zero snapshot retained temporarily at `backup/pre-zero-20260912`.
- `AI_AUTHORITY_RESUME_V2` is active.
- Owner-approved 2026-09-13 business/data decisions are persisted in `DECISIONS.md` and supersede stale business-schema assumptions from the 2026-09-12 baseline.
- Secret values remain outside repository source.

## GitHub BETA environment

- Environment `beta` is the provider execution boundary.
- Google CI credentials/variables are configured and proven by successful Apps Script automation.
- Cloudflare credentials are configured as Environment secret/variable; raw token values are not exposed to source or chat.
- `.github/workflows/cloudflare-beta-verify.yml` now provides an automated GitHub Actions bridge for Cloudflare identity verification plus read-only D1 inspection.

## Google / Drive / Sheets / GAS

- Current Google authority: `tam95.supra@gmail.com`.
- Project root: `VẬN HÀNH DC HƯNG YÊN` with `01_BETA` and `02_STABLE` environment roots.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`.
- Google Cloud/OAuth BETA: PASS.
- GAS BETA foundation: PASS.
- Business `doPost()` remains foundation-only; Google projection is not yet business-live.

## Cloudflare BETA — identity and D1 inspection PASS

Verified target resources:
- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- Worker: `vhdchy-beta` — FOUND.
- D1: `vhdchy-data-beta` — FOUND.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.

Automated read-only inspection evidence:
- GitHub Actions run `34748247818` — SUCCESS.
- D1 `schema_version`: `business_core_v1`.
- User tables observed: `clusters`, `conflict_corrections`, `d1_migrations`, `document_metadata`, `domain_events`, `dropped_goods`, `employee_cluster_memberships`, `employees`, `import_audit`, `labor_records`, `projection_catalog`, `projection_outbox`, `resources`, `session_resource_bindings`, `shift_definitions`, `vhdchy_meta`, `work_sessions` (plus D1 internal `_cf_KV`).
- All measured business tables contain `0` rows.
- Bookkeeping only: `d1_migrations=1`, `vhdchy_meta=3`.
- Classification: `BUSINESS_CORE_V1 / ZERO_BUSINESS_ROWS / SCHEMA_MISMATCH`.
- No business row contents were read and no INSERT/UPDATE/DELETE/DDL was issued by the inspection.

## Source/schema status

- Existing `service/worker/migrations/0001_initial.sql` represents the earlier `business_core_v2` clean-baseline design and does not match the current D1 v1 schema.
- More importantly, the 2026-09-13 Owner-approved target contract materially expands/changes the earlier v2 design. Therefore the existing migration is `STALE_TARGET` and must not be applied.
- Because the verified BETA D1 contains zero business rows, a reviewed zero-business-data rebuild/reset path is eligible, but only after the new target schema/migration is reconciled from current Owner authority.
- No D1 migration/rebuild or Worker deployment has been performed for the new target yet.

## Target architecture

- D1 is canonical business authority.
- Google Sheets is projection/reconciliation only.
- Google Drive stores media/documents/archive while D1 stores identifiers, metadata, hashes and state.
- Web and later APK use one Service/domain contract.
- LAN remains an independent transport/fallback lane using the same command/event model, not a second backend.

## Android / LAN / STABLE

- Android BETA signer: `VERIFY_REQUIRED` when signing material/machine is available.
- Physical LAN regression remains paused while Owner is off-site; Service work continues independently.
- STABLE remains blocked until BETA PASS and explicit Owner approval.
