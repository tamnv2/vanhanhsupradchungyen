# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Google Gateway BETA — FOUNDATION PASS

Google Cloud/OAuth, GitHub Environment `beta`, source synchronization, runtime bootstrap, immutable versioning, managed deployment, and `/exec` verification are complete.

Canonical Google Gateway BETA authority:
- Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`
- Managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`
- Managed Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`
- Verification run: `34696139468`

The Gateway remains foundation-only: `doPost()` does not yet accept business mutations and the BETA projection workbook remains `PROVISIONED_NOT_LIVE`.

## Cloudflare BETA — IDENTITY/RESOURCE PASS

Verified by GitHub Actions run `34699120539`:
- Account-owned API token: PASS.
- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- Worker `vhdchy-beta`: FOUND.
- D1 `vhdchy-data-beta`: FOUND.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.

No resource was created or replaced during verification.

## Immediate priority — D1 pre-migration inspection

1. Inspect the confirmed BETA D1 before any migration write.
2. Classify the database as exactly one of:
   - `EMPTY`: safe candidate for clean zero-baseline migration;
   - `BUSINESS_CORE_V2`: already on the current schema, then verify protected-table row counts and schema contract;
   - `UNKNOWN_NONEMPTY` / schema mismatch: STOP and review, no migration.
3. Do not apply `service/worker/migrations/0001_initial.sql` merely because the database name/ID match.
4. After D1 inspection PASS, apply the clean schema only to database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
5. Add Worker BETA deployment configuration with:
   - Worker name `vhdchy-beta`;
   - D1 binding `DB` -> verified BETA D1;
   - `APP_ENV=BETA`;
   - build revision;
   - canonical `GAS_EXEC_URL` from Google authority.
6. Deploy `service/worker/src/index.js` only to the verified Worker `vhdchy-beta`.
7. Require `/health` PASS with D1 schema `business_core_v2`.
8. Require `/health/deep` PASS for D1 and verify Google Gateway integration; Google remains degradable/advisory while D1 core is critical.
9. Only after these gates pass, mark Worker/D1 BETA foundation live.

## Autonomous execution model

- After Owner approves a proposed plan/scope, AI executes all actions available through connected tools without delegating automatable clicks or runs back to Owner.
- Owner interaction is requested only for unavoidable UI/consent/secret/physical steps not exposed by available tools.
- Provider verification/deployment must remain fail-closed and target only verified resource identities.
- Do not silently create replacement provider resources.

## Android BETA signer

10. Verify keystore/alias/fingerprint locally when the relevant machine/material is available.
11. Do not expose keystore bytes/passwords in chat or repository.

## Integration gate

12. Add Android signer values to GitHub `beta` only after local verification.
13. Create a fresh `beta` live branch from the approved `main` baseline only when the full provider gate is ready.
14. Run BETA deploy/health checks and retain evidence.

## LAN

No physical LAN work while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending V4 physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until all retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected. Then remove the backup branch.

## STABLE

Blocked until BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
