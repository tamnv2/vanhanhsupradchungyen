# BUSINESS CORE V1 — BETA CONTRACT

Status: `DEPLOYED_BETA / RECONCILED_BASELINE`

## Design source

Business Core V1 is the deployed BETA baseline. VHDCHY scope authority is `PROJECT_SCOPE.md`; Pick Pack 1291 is reference/evidence only.

D1 is canonical authority. Google Sheets is projection/human-readable/DR surface as specified per module. Canonical mutation uses immutable events, idempotency and entity version; correction/reversal is a new event rather than raw event rewrite.

No old Pick Pack employee/resource/history rows are migrated.

## Schema baseline

Migration `worker/migrations/0001_business_core.sql` creates:

- cluster + shift definitions;
- employee + cluster membership;
- resource master;
- work session + resource bindings;
- labor records;
- dropped goods;
- document metadata;
- immutable domain events;
- conflict/correction state;
- D1 projection outbox + workbook catalog;
- import audit.

`domain_events` blocks UPDATE/DELETE with database triggers. Idempotency key and `(device_id, device_seq)` have unique indexes when present.

## Reconciliation caveat

`RECONCILE-001` found two important scope corrections that must be done additively in later migrations:

1. `resources.resource_type` in migration 0001 hard-codes `PDA`, `USER_PICK`, `BAN_PACK`, `USER_PACK`. These are Pick Pack 1291 resource types, not the global DC resource universe. Future schema must genericize resource type/catalog configuration without modifying applied migration 0001.
2. `dropped_goods` is a Pick Pack 1291 module domain, not an assumed universal DC core domain. The existing table remains for BETA compatibility while module ownership is made explicit.

See `docs/reconciliation/RECONCILE_001.md` and `docs/architecture/MODULE_MAP.md`.

## Public API currently exposed

Anonymous non-PII endpoints:

- `GET /health`
- `GET /health/deep`
- `GET /health/integrations`
- `GET /api/v1/meta`
- `GET /api/v1/capabilities`

Routes under `/api/v1/data/*` and `/api/v1/admin/*` remain `401 AUTH_REQUIRED` until session/permission contract is active. Anonymous mutation is disabled; CORS is not opened yet.

## Health semantics

D1/schema is critical to core health. Google Gateway/Sheets is an asynchronous projection/archive integration, so temporary Google failure does not invalidate canonical Service health. Projection backlog is handled through outbox/retry/ACK/checkpoint semantics.

Latest verified BETA deployment for commit `947a4feb48bc5c99867f1975b56edbb9a7309925`:

- run `34495312746`: SUCCESS;
- Worker version `f8ddf638-0829-4249-9ea0-8f2d38b03f05`;
- D1 schema `business_core_v1`: PASS;
- Worker meta `BUSINESS_CORE_V1`: PASS;
- Google Gateway advisory probe: PASS.

## Projection workbook

BETA cluster `PICK_PACK_1291` now has quarterly workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`, schema `PP1291_SHEETS_BETA_V1`. It is adapted from the old human-facing Pick Pack 1291 Sheet model; no old rows were migrated.

Actual projection transport/registration remains a separate implementation gate; workbook existence does not mean projection worker is active.

## Next

Proceed in parallel with core schema refinement, Sheet projection registration/contract, generic auth/session/permission foundation and Android client foundation. Privileged ROOT/SUPERADMIN login acceptance rules must follow the current VHDCHY Master Spec and must not be inferred from the old Pick Pack backup.
