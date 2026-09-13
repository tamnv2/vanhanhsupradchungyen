# PROJECT KNOWLEDGE AUDIT — 2026-09-13

Status: ACTIVE AUDIT RECORD

This document records the result of a full review of current GitHub authority, architecture, data model, business rules, LAN/APK direction, environment handling and execution plans.

## Confirmed strengths

- Business rules D-013..D-040 are persisted in `DECISIONS.md`.
- V3 LAN/Cloud/Google/offline behavior is persisted in `DECISIONS_V3.md` and `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`.
- New LAN-host/domain and BETA-to-STABLE promotion rules are persisted in `DECISIONS_V4.md`.
- `docs/LAN_HOST_DOMAIN_V1.md` records the portable/no-admin host constraint, canonical LAN domains and offline-domain acceptance target.
- `docs/RELEASE_PROMOTION_V1.md` records exact-release promotion and no-BETA-data-copy rules.
- `CONTEXT_INDEX.md` now forces future AI to read V3/V4 overrides before stale V2 design files.
- D1 schema `business_core_v3` covers platform/configuration, identity linkage, attendance/presence, sessions/tasks, resources, labor, dropped goods, documents/media, immutable events, projection outbox, conflicts, snapshots and telemetry.
- Acceptance coverage exists in `docs/BETA_ACCEPTANCE_MATRIX.md`.

## Coverage by knowledge area

| Area | Current persistent source | Audit result |
|---|---|---|
| Product scope | `PROJECT_SCOPE.md`, V3/V4 decisions | Mostly complete; root scope file still lacks latest V4 text |
| Business rules | `DECISIONS.md` | Strong coverage |
| LAN/Cloud/offline model | `DECISIONS_V3.md`, `TARGET_PRODUCT_ARCHITECTURE_V3.md` | Strong coverage |
| LAN host/domain | `DECISIONS_V4.md`, `LAN_HOST_DOMAIN_V1.md` | Complete as design; physical proof pending |
| BETA/STABLE promotion | `DECISIONS_V4.md`, `RELEASE_PROMOTION_V1.md` | Complete as policy; implementation pending |
| Cloud D1 data model | migrations `0001`..`0008` | Strong for pre-V3 business model |
| V3 LAN data/sync additions | V3 architecture/delivery + audit notes | Design requirement recorded; schema/source not materialized |
| Business acceptance | `BETA_ACCEPTANCE_MATRIX.md` | Broad coverage but contains some stale V2 rows |
| AI resume routing | `AI_ENTRYPOINT.md`, `CONTEXT_INDEX.md`, `CHECKPOINT.md` | Reconciled to V4 authority order |
| Provider/runtime identities | `SERVICE_AUTHORITY.md` | BETA strong; LAN/STABLE wording stale |

## Drift still active after this audit

Several active documents still contain V2 assumptions that conflict with V3/V4 and must be reconciled before they become safe standalone sources:

1. `docs/SERVICE_API_CONTRACT.md` still describes old LAN relay/autonomous behavior and says LAN does not write Google directly.
2. `docs/CANONICAL_MUTATION_PLAN.md` still says LAN Google output waits for D1 reconciliation.
3. `docs/LAN_EDGE_STATE_V1.md` still references V2 and lacks local authority snapshot, Google receipt and continuous Cloud-sync requirements.
4. `SERVICE_AUTHORITY.md` still points LAN authority to V2 and treats STABLE names as reserved only.
5. `AI_OPERATING_CONTRACT.md` lists only `DECISIONS.md` in its repository-discipline summary; `CONTEXT_INDEX.md` now corrects actual read precedence.
6. `docs/ARCHITECTURE.md` still contains older V2 wording beneath a partial V3 summary.
7. `docs/BETA_ACCEPTANCE_MATRIX.md` still contains V2 expectations such as deferred LAN Google output and needs V3/V4 reconciliation.
8. STABLE CI/provider configuration is not yet provisioned in source; current workflows are BETA-focused.
9. Canonical LAN-domain offline resolution remains a required technical/physical acceptance item; it is not implemented or proven yet.

## Knowledge-structure gap

Owner decisions are not lost, but detailed business knowledge is fragmented across decision files, SQL migrations and acceptance documents. There is no single complete business-rules/data-model/business-scenario handbook yet.

Until a later consolidation pass succeeds, future AI must use:
- `DECISIONS.md` + V3/V4 overrides for rules;
- D1 migrations for exact Cloud schema;
- V3 architecture/delivery for cross-runtime behavior;
- BETA acceptance for scenario coverage;
- this audit for known drift.

Do not infer missing details from old legacy source or chat memory.

## Cloud D1 business-model findings

Current schema materially reflects the locked pre-V3 business decisions:
- employee code lifecycle is separated from employee identity;
- attendance is immutable history plus current presence state;
- one MAIN open work session is constrained;
- session tasks allow PICK/PACK under one session;
- resource ownership, mapping, assignments, daily reuse/reissue and cross-cluster borrow are separated;
- labor, dropped goods and document/media records exist;
- immutable domain events support idempotency/device sequence;
- projection outbox, conflict correction, snapshots/archive/quota/compatibility records exist.

No evidence was found that these locked business rules were removed from the active D1 model.

## V3 schema/source gaps

The deployed Cloud D1 `business_core_v3` predates the V3 LAN model. New requirements still need reviewed schema/source design for concepts such as:

- Cloud ingestion/linkage of LAN event identity;
- Google projection/upload receipts produced by LAN;
- deduplication linkage between LAN Google outputs and Cloud projection work;
- LAN sync cursors/checkpoints and authority snapshot versions;
- conflict evidence/resolution flow compatible with local-first LAN operation.

Exact table names/fields are not locked by this audit and must not be invented without implementation design review.

## STABLE findings

Policy is now clear: prepare STABLE early, keep business traffic fail-closed, promote the exact accepted BETA release, and never copy BETA runtime/business data.

Actual repo automation does not yet implement that target. Current workflow/dispatch/config inventory is BETA-focused, so STABLE preparation is a real remaining implementation lane rather than a completed item.

## Current conclusion

The repository contains the large majority of Owner-locked business/domain decisions. The main problem is not missing business intent; it is **document drift and incomplete V3/V4 implementation materialization**.

The highest-risk issue for future AI was authority routing: stale V2 files could be read without V3/V4 overrides. `CONTEXT_INDEX.md` and `CHECKPOINT.md` have now been updated so future resume must read the newer authority layers first.

Remaining work is to reconcile the stale V2 contract/design/authority files and materialize the V3 LAN sync/Google-receipt/STABLE-preparation requirements in source/schema/CI.
