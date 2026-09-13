# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- Pre-zero snapshot retained temporarily at `backup/pre-zero-20260912`.
- `AI_AUTHORITY_RESUME_V2` is active.
- Owner-approved 2026-09-13 business/data decisions are persisted in `DECISIONS.md` and supersede stale business-schema assumptions from the 2026-09-12 baseline.
- Owner execution policy is now explicit in `AI_OPERATING_CONTRACT.md`: direct connected action first; otherwise GitHub-hosted CI; request only exact missing permission/consent; do not require laptop-local tooling for routine cloud/provider operations.
- Secret values remain outside repository source.

## GitHub BETA environment

- Environment `beta` is the provider execution boundary.
- Google CI credentials/variables are configured and proven by successful Apps Script automation.
- Cloudflare credentials are configured as Environment secret/variable; raw token values are not exposed to source or chat.
- `.github/workflows/cloudflare-beta-verify.yml` provides an automated GitHub Actions bridge for Cloudflare identity verification plus read-only D1 inspection.
- A dedicated guarded D1 write/migration workflow is the current missing execution bridge. Attempted creation through the connected GitHub action was blocked by the platform action-safety guard; this was not bypassed.

## Google / Drive / Sheets / GAS

- Current Google authority: `tam95.supra@gmail.com`.
- Project root: `VẬN HÀNH DC HƯNG YÊN` with `01_BETA` and `02_STABLE` environment roots.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`.
- Google Cloud/OAuth BETA: PASS.
- GAS BETA foundation: PASS.
- Business `doPost()` remains foundation-only; Google projection is not yet business-live.

## Cloudflare BETA — final pre-write inspection PASS

Verified target resources:
- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- Worker: `vhdchy-beta` — FOUND.
- D1: `vhdchy-data-beta` — FOUND.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.

Latest automated read-only pre-write evidence:
- GitHub Actions run `34749417468` — SUCCESS.
- D1 `schema_version`: `business_core_v1`.
- User tables observed: `clusters`, `conflict_corrections`, `d1_migrations`, `document_metadata`, `domain_events`, `dropped_goods`, `employee_cluster_memberships`, `employees`, `import_audit`, `labor_records`, `projection_catalog`, `projection_outbox`, `resources`, `session_resource_bindings`, `shift_definitions`, `vhdchy_meta`, `work_sessions` (plus D1 internal `_cf_KV`).
- All measured business tables contain `0` rows.
- Bookkeeping only: `d1_migrations=1`, `vhdchy_meta=3`.
- Classification: `BUSINESS_CORE_V1 / ZERO_BUSINESS_ROWS / SCHEMA_MISMATCH`.
- No business row contents were read and no INSERT/UPDATE/DELETE/DDL was issued by the inspection.

## Source/schema status

- Owner-approved `business_core_v3` source is now on `main` after PR #7; merge commit `c4c6e43f25c78625ebd816d1bdf5c54393a0f812`.
- Ordered migration source is `0000_beta_zero_business_rebuild.sql` through `0008_seed_core.sql`.
- Post-merge validation `34749181618` — SUCCESS.
- D1-compatible rebuild-guard validation `34749393544` — SUCCESS.
- Final main validation `34749417525` — SUCCESS.
- No D1 migration/rebuild or V3 Worker deployment has been performed yet.

## Current execution gate

- Establish a fixed-purpose GitHub-hosted CI migration bridge using Environment `beta`.
- The bridge must verify exact provider identity and the V1 zero-business precondition immediately before write, apply only the reviewed V3 migration set, then verify `business_core_v3` plus target table/seed/integrity invariants.
- Dispatch must not accept arbitrary SQL/provider commands.
- Owner-local Wrangler/Git/Node installation is not part of the normal execution path.

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
