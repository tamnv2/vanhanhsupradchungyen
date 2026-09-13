# PROJECT KNOWLEDGE AUDIT — 2026-09-13

Status: ACTIVE AUDIT RECORD

This document records the result of a full review of current GitHub authority, architecture, data model, business rules, LAN/APK direction, environment handling and execution plans.

## Confirmed strengths

- Business rules D-013..D-040 are persisted in `DECISIONS.md`.
- V3 LAN/Cloud/Google/offline behavior is persisted in `DECISIONS_V3.md` and `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`.
- New LAN-host/domain and BETA-to-STABLE promotion rules are persisted in `DECISIONS_V4.md`.
- D1 schema `business_core_v3` covers identity, attendance/presence, sessions/tasks, resources, labor, dropped goods, documents/media, immutable events, projection outbox, conflicts, snapshots and telemetry.
- Acceptance coverage exists in `docs/BETA_ACCEPTANCE_MATRIX.md`.

## Drift found during audit

Several active documents still contain V2 assumptions that conflict with V3/V4 and must be reconciled before they are safe fast-resume sources:

1. `docs/SERVICE_API_CONTRACT.md` still describes old LAN relay/autonomous behavior and says LAN does not write Google directly.
2. `docs/CANONICAL_MUTATION_PLAN.md` still says LAN Google output waits for D1 reconciliation.
3. `docs/LAN_EDGE_STATE_V1.md` still references V2 and lacks local authority snapshot, Google receipt and continuous Cloud-sync requirements.
4. `SERVICE_AUTHORITY.md` still points LAN authority to V2 and treats STABLE names as reserved only.
5. `CONTEXT_INDEX.md` does not route AI to `DECISIONS_V3.md`, `DECISIONS_V4.md` or V3 architecture/delivery documents.
6. `AI_OPERATING_CONTRACT.md` lists only `DECISIONS.md` as decision authority in repository discipline.
7. `docs/ARCHITECTURE.md` still contains older V2 wording beneath a partial V3 summary.
8. STABLE CI/provider configuration is not yet provisioned in source; current workflows are BETA-focused.
9. Canonical LAN-domain offline resolution remains a required technical/physical acceptance item; it is not implemented or proven yet.

## Knowledge-structure gap

Business decisions are persisted, but detailed knowledge is fragmented across decision files, migrations and acceptance docs. There is not yet one dedicated canonical business-rules document, one data-model guide and one business-scenario guide. Until those consolidation files exist, AI must read the decision files plus relevant migrations/acceptance material for correctness.

## Schema gaps introduced by V3

The deployed Cloud D1 `business_core_v3` predates the V3 LAN model. New V3 requirements still need reviewed schema/source design for concepts such as:

- Cloud ingestion/linkage of LAN event identity;
- Google projection/upload receipts produced by LAN;
- deduplication linkage between LAN Google outputs and Cloud projection work;
- LAN sync cursors/checkpoints and authority snapshot versions;
- conflict evidence/resolution flow compatible with local-first LAN operation.

Exact table names/fields are not locked by this audit and must not be invented without design review.

## Current conclusion

The repository contains most Owner-locked business rules, but it is not yet fully internally consistent. V3/V4 decisions are present while several older design/index/authority files still describe superseded behavior. These are documentation/contract drift items, not proof that the business decisions were lost.
