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

## Immediate priority — Owner target schema reconciliation

1. Treat the existing `service/worker/migrations/0001_initial.sql` as `STALE_TARGET`; do not apply it.
2. Reconcile the 2026-09-13 Owner-approved business/data authority in `DECISIONS.md` into a complete D1 target schema covering cluster/shift/position versioning, employee-code history, auth/permission/grant lineage and ROOT security, attendance/presence/work sessions/tasks, resources/mappings/assignments/reissue/cross-cluster borrowing, labor, dropped goods, documents/media, immutable events/corrections/audit, projection outbox/catalog/checkpoints, archive/snapshot/quota health, and future device/offline compatibility fields.
3. Review schema invariants/indexes/triggers and assign the new schema version explicitly; validate the migration locally with SQLite before any provider write.
4. Because BETA D1 has zero business rows, prepare a deterministic zero-business-data rebuild/reset migration for exact D1 ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`. Preserve/fail-closed on unexpected business rows if provider state changes before execution.
5. Immediately before the first D1 write, rerun automated read-only inspection and require the same zero-business-row condition. If it changes, stop and redesign as a forward migration.
6. Apply the reviewed BETA migration through the GitHub Actions/Environment bridge; never expose the Cloudflare token to source/chat.
7. Verify post-migration table/schema contract and row invariants automatically.
8. Reconcile Worker runtime to the new schema/domain contract, then deploy only to `vhdchy-beta` with binding `DB` -> verified BETA D1, `APP_ENV=BETA`, build revision, and canonical `GAS_EXEC_URL`.
9. Require `/health` and `/health/deep` PASS, with D1 critical and Google integration degradable/advisory where designed.
10. Implement Google projection/outbox processing, then Web same-origin business flows and automated BETA acceptance scenarios from `DECISIONS.md`.
11. Mark Worker/D1/Google/Web BETA business-live only after the complete BETA gate passes.

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
