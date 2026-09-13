# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13

## Google Gateway BETA — FOUNDATION PASS

Google Cloud/OAuth, GitHub Environment `beta`, source synchronization, runtime bootstrap, immutable versioning, managed deployment, and `/exec` verification are complete. Gateway remains foundation-only: `doPost()` does not yet accept business mutations and the BETA projection workbook remains `PROVISIONED_NOT_LIVE`.

## Cloudflare BETA — PRE-WRITE GATE PASS

Latest automated pre-write GitHub Actions run `34749417468` verified:
- account-owned API token: PASS;
- exact Worker `vhdchy-beta`: FOUND;
- exact D1 `vhdchy-data-beta`: FOUND;
- D1 database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`;
- D1 schema version remains `business_core_v1`;
- all measured business tables remain zero rows;
- bookkeeping only (`d1_migrations=1`, `vhdchy_meta=3`).

Classification remains: `BUSINESS_CORE_V1 / ZERO_BUSINESS_ROWS / SCHEMA_MISMATCH`.

## Target schema — MERGED + CI PASS

The 2026-09-13 Owner-approved `business_core_v3` target has been merged to `main` through PR #7.

Evidence:
- merge commit `c4c6e43f25c78625ebd816d1bdf5c54393a0f812`;
- post-merge validation `34749181618` — SUCCESS;
- D1-compatible rebuild-guard validation `34749393544` — SUCCESS;
- final main validation `34749417525` — SUCCESS.

The migration set covers cluster/shift/position versioning, employee-code history, account retention/uniqueness, direct permissions/grant lineage, ROOT MFA/recovery structures, attendance/presence, main/extra work sessions and tasks, Pack mapping, resource assignment/reissue/cross-cluster borrowing, labor, dropped goods, documents/media, immutable events/audit, projection outbox/checkpoints, archive/snapshot/quota health and compatibility telemetry.

No Cloudflare D1 mutation or V3 Worker deployment has occurred yet.

## Immediate priority — autonomous CI D1 migration

1. Create a dedicated GitHub Actions BETA migration bridge on hosted runners; do not require Owner-local installation of Wrangler/Git/Node/provider SDKs.
2. Use GitHub Environment `beta` credentials. Keep dispatch fixed-purpose; do not accept arbitrary SQL or provider commands as input.
3. In the same migration gate, perform preflight: exact D1 name/ID, expected `business_core_v1` legacy table set, and zero business rows. Stop on any mismatch.
4. Apply only the reviewed ordered migration set `0000_beta_zero_business_rebuild.sql` through `0008_seed_core.sql` to `vhdchy-data-beta`.
5. Perform postflight and require `business_core_v3`, required target tables, required baseline catalog/cluster seeds, absence of legacy `resources`, and integrity/foreign-key checks before PASS.
6. Run an independent read-only D1 verification after the write workflow. If outcome is uncertain, verify before any retry.
7. Update `CURRENT_STATE.md`, `CHECKPOINT.md` and execution evidence after migration PASS/FAIL.
8. After D1 V3 PASS, deploy the V3 Worker only to `vhdchy-beta` with binding `DB` -> verified BETA D1, `APP_ENV=BETA`, build revision, and canonical `GAS_EXEC_URL`.
9. Require `/health` and `/health/deep` PASS, with D1 critical and Google integration degradable/advisory where designed.
10. Implement Google projection/outbox processing, then Web same-origin business flows and automated BETA acceptance scenarios from `DECISIONS.md`.
11. Mark Worker/D1/Google/Web BETA business-live only after the complete BETA gate passes.

## Autonomous execution model

Owner policy: routine project execution must not depend on installing tools on a laptop. Execution preference is direct connected action -> GitHub-hosted CI -> minimum Owner permission/UI consent -> local/physical execution only when inherently required or explicitly requested. When a provider permission is missing, request the exact least-privilege grant and resume automation after it is granted.

## Android BETA signer

Signing material verification is inherently device/key-material dependent and may require a local/physical step when that lane resumes. Never expose keystore bytes/passwords in chat or repository. Add signing values to GitHub `beta` only after verification.

## LAN

Physical LAN work remains independent and paused while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected.

## STABLE

Blocked until full BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
