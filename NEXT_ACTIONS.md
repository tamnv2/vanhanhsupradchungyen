# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13

## Google Gateway BETA — FOUNDATION PASS

Google Cloud/OAuth, GitHub Environment `beta`, source synchronization, runtime bootstrap, immutable versioning, managed deployment, and `/exec` verification are complete. Gateway remains foundation-only: `doPost()` does not yet accept business mutations and the BETA projection workbook remains `PROVISIONED_NOT_LIVE`.

## Cloudflare BETA — IDENTITY + READ-ONLY D1 INSPECTION PASS

Automated GitHub Actions run `34748247818` verified:
- account-owned API token: PASS;
- exact Worker `vhdchy-beta`: FOUND;
- exact D1 `vhdchy-data-beta`: FOUND;
- D1 database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`;
- D1 schema version `business_core_v1`;
- all measured business tables have zero rows;
- only bookkeeping rows exist (`d1_migrations=1`, `vhdchy_meta=3`).

Classification: `BUSINESS_CORE_V1 / ZERO_BUSINESS_ROWS / SCHEMA_MISMATCH`.

## Target schema — SOURCE + CI PASS

The 2026-09-13 Owner-approved target is implemented on branch `schema/business-core-v3` as `business_core_v3`.

Validation run `34748883606` on branch head `d65f07e33b714de10a1a2a1052a5f941253ddf42` completed SUCCESS. The ordered migration set builds cleanly in SQLite, required tables and key invariants pass, `PRAGMA foreign_key_check` is clean, Worker constants match V3, and legacy table `resources` is absent.

The branch covers cluster/shift/position versioning, employee-code history, account retention/uniqueness, direct permissions/grant lineage, ROOT MFA/recovery structures, attendance/presence, main/extra work sessions and tasks, Pack mapping, resource assignment/reissue/cross-cluster borrowing, labor, dropped goods, documents/media, immutable events/audit, projection outbox/checkpoints, archive/snapshot/quota health and compatibility telemetry.

No Cloudflare D1 mutation or Worker deployment has occurred during schema reconciliation.

## Immediate priority — V3 promotion and BETA pre-write gate

1. Promote the validated `schema/business-core-v3` source to `main` through a supported GitHub path. Do not use lower-level Git object/ref operations to bypass a blocked PR/action-safety path.
2. After V3 source is authoritative on `main`, rerun the automated D1 read-only inspection immediately before any provider write.
3. Require exact D1 ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, current schema `business_core_v1`, the expected inspected V1 table set, and zero business rows. If any condition changes, stop and redesign as a forward migration.
4. Execute only a reviewed guarded zero-business-data reconciliation to `business_core_v3` through GitHub Actions/Environment `beta`; never expose Cloudflare credentials in source or chat.
5. Verify post-migration schema/table contract, required baseline catalogs and row invariants automatically.
6. Deploy the V3 Worker only to `vhdchy-beta` with binding `DB` -> verified BETA D1, `APP_ENV=BETA`, build revision, and canonical `GAS_EXEC_URL`.
7. Require `/health` and `/health/deep` PASS, with D1 critical and Google integration degradable/advisory where designed.
8. Implement Google projection/outbox processing, then Web same-origin business flows and automated BETA acceptance scenarios from `DECISIONS.md`.
9. Mark Worker/D1/Google/Web BETA business-live only after the complete BETA gate passes.

## Execution-channel constraint

The connected platform allowed schema source writes and CI validation, but rejected PR creation and rejected the proposed provider-mutation helper update through its action-safety guard. Those blocks must not be bypassed. Read-only Cloudflare/D1 automation remains operational.

## Autonomous execution model

After Owner-approved scope, AI executes available connected actions end-to-end. Owner interaction is requested only for genuinely unavailable UI/consent/private-secret/physical steps. Provider actions remain fail-closed and target verified identities only. Do not silently create replacement resources.

## Android BETA signer

Verify keystore/alias/fingerprint locally when relevant material is available. Never expose keystore bytes/passwords in chat or repository. Add signing values to GitHub `beta` only after local verification.

## LAN

Physical LAN work remains independent and paused while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected.

## STABLE

Blocked until full BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
