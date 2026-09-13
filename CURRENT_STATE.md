# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- Pre-zero snapshot retained temporarily at `backup/pre-zero-20260912`.
- `AI_AUTHORITY_RESUME_V2` is active.
- Owner-approved 2026-09-13 business/data decisions are persisted in `DECISIONS.md`.
- `D-041` is active: after approved scope is established, overall execution continues by default. Owner interaction is reserved for `OWNER_PERMISSION_REQUIRED` or `OWNER_DECISION_REQUIRED`; a blocked single lane does not stop independent safe lanes.
- Execution preference remains direct connected action -> GitHub-hosted CI -> minimum exact Owner permission/consent -> local/physical only when inherently required.
- Secret values remain outside repository source.

## GitHub BETA environment

- Environment `beta` is the provider execution boundary.
- Google CI credentials/variables are configured and proven by successful Apps Script automation.
- Cloudflare credentials are configured as Environment secret/variable; raw token values are not exposed to source or chat.
- Read-only Cloudflare inspection, guarded D1 migration and guarded Worker deployment are available through GitHub-hosted Actions.

## Google / Drive / Sheets / GAS

- Current Google authority: `tam95.supra@gmail.com`.
- Project root: `VẬN HÀNH DC HƯNG YÊN` with `01_BETA` and `02_STABLE` environment roots.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3` was re-inspected against live Sheets metadata and headers.
- Workbook schema remains `PP1291_SHEETS_BETA_V1`; canonical authority remains D1; projection status remains `PROVISIONED_NOT_LIVE`.
- Google Cloud/OAuth BETA: PASS.
- GAS managed deployment was updated in place to immutable version `3` by run `34753872034` — PASS.
- Version 3 implements fail-closed `VHDCHY_PROJECTION_V1`: fixed allowed-sheet/key mapping, bounded batches, unknown-column rejection, per-sheet key upsert and ScriptLock serialization.
- Projection writes remain intentionally disabled until the cross-service projection authentication verifier/enable state is safely provisioned and end-to-end retry/idempotency tests pass.

## Cloudflare D1 BETA — BUSINESS_CORE_V3 PASS

- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- D1: `vhdchy-data-beta`.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- Guarded migration run `34752340290` — SUCCESS.
- Independent post-migration verification run `34752381290` — SUCCESS.
- Provider schema: `business_core_v3`.
- Full V3 table contract and baseline seeds verified.
- Business rows remain zero.
- Foreign-key and quick integrity checks passed.

## Cloudflare Worker BETA — V3 DEPLOYED + HEALTH PASS

- Worker: `vhdchy-beta`.
- Public origin: `https://beta.supra.cc.cd`.
- `workers.dev`: disabled and preserved disabled.
- Binding names/types verified before and after deploy: `DB`/D1, `APP_ENV`/plain text, `BUILD_SHA`/plain text, `GAS_EXEC_URL`/plain text.
- Guarded deploy run `34752917714` — SUCCESS.
- Deployed build SHA: `6b23f7134e02c7b53571c0a151f97f27a86bcb2e`.
- `/health`, `/health/deep`, `/api/v1/meta`, `/api/v1/capabilities`: PASS at deployment verification.
- Independent post-deploy provider inspection run `34752966242` — SUCCESS.

## Parallel Service implementation state

### Auth/session/permission lane
- `service/worker/src/auth.js`: password policy/hash, bearer-token utilities and TOTP verification foundation.
- `service/worker/src/authorization.js`: scoped effective permission evaluator with explicit DENY precedence and ROOT/SUPERADMIN boundary.
- `service/worker/src/session.js`: bearer session resolution, account/session expiry/revocation checks and device security-epoch validation.
- `service/worker/src/permission-store.js`: active role/direct grant loading with effective-time and cluster/module scope.
- Unit/contract validation run `34753672108` — SUCCESS.
- Protected business/admin endpoints remain fail-closed until these modules are integrated into the deployed Worker and runtime acceptance passes.

### Projection/outbox lane
- `service/worker/src/projection.js`: `VHDCHY_PROJECTION_V1` envelope, bounded pending-outbox reads, processing/ACK/failure states, exponential retry and DEAD transition foundation.
- GAS projection batch contract version 3 is deployed but not write-live.
- D1 remains canonical if Google is degraded; no committed business mutation may be rolled back because projection fails.

### Shared API contract lane
- `docs/SERVICE_API_CONTRACT.md` defines bearer/session rules, permission/scope enforcement, same-origin boundary, idempotency requirements, standard errors and the required D1 state + immutable event + outbox transaction contract.
- No Web business surface is treated as live yet.

## Known implementation constraint

The currently deployed Worker bridge uploads the single foundation module `index.js`. New auth/session/permission/projection modules are validated source on `main` but are not yet part of deployed runtime. The multi-module deployment path must be reconciled before importing them into `index.js`; until then, existing fail-closed business route behavior is preserved.

## Target architecture

- D1 is canonical business authority.
- Google Sheets is projection/reconciliation only.
- Google Drive stores media/documents/archive while D1 stores identifiers, metadata, hashes and state.
- Web and later APK use one Service/domain contract.
- LAN remains an independent transport/fallback lane using the same command/event model, not a second backend.

## Android / LAN / STABLE

- Android BETA signer: `VERIFY_REQUIRED` when signing material/machine is available.
- Physical LAN regression remains paused while Owner is off-site; source review/restoration work may continue independently.
- STABLE remains blocked until BETA PASS and explicit Owner approval; that gate does not block independent BETA work.
