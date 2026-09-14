# NEXT ACTIONS — VHDCHY

Updated: 2026-09-14
Progress: **55% displayed / 55.4% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A blocked lane does not stop unrelated ready work.

## A — Cloud operational snapshot/delta route — IMMEDIATE

Current gap: Cloud accepts signed reconciliation input but does not yet expose an accepted operational snapshot/delta response for LAN post-reconciliation refresh.

Next:

- implement machine-authenticated operational snapshot/delta route using the existing request-auth model;
- bind it to exact edge/environment/cluster identity;
- return real Slice-1 D1 current state for employees, employee codes and presence;
- return explicit canonical reconciliation coverage sufficient for LAN rebase verification;
- add route/store tests for auth failure, wrong identity, empty state, valid state and coverage semantics;
- do not fabricate/default canonical coverage.

## B — LAN post-reconciliation runtime rebase — NEXT DEPENDENCY

Current PASS:

- `PostReconciliationRebaseTracker` source/harness behavior;
- dedicated run `34844270179`: SUCCESS;
- stale snapshot rejection;
- incomplete coverage rejection;
- full coverage cursor advance;
- later canonical event reopening only new pending work.

Still required:

- LAN client/consumer for the Cloud snapshot/delta route;
- authoritative refresh import after canonical reconciliation;
- fail-closed business readiness while canonical events remain unre-based;
- readiness recovery only after verified coverage;
- restart/idempotency acceptance for the complete refresh/rebase path.

The earlier direct security-gate integration attempt was blocked by platform safety guard; do not bypass that guard.

## C — Cloud schema parity — COMPLETE FOR CURRENT NODE

Migration `0014_employee_code_entity_version.sql` is live BETA PASS.

Evidence:

- clean baseline `34844597357`: SUCCESS;
- read-only provider preflight `34844821406`: SUCCESS;
- live guarded migration `34844932823`: SUCCESS;
- final column/metadata/integrity checks PASS;
- migration dispatch returned to disabled state.

Do not replay migration 0014.

## D — Worker reconciliation credential lane — OWNER_PERMISSION_REQUIRED

This lane remains blocked on an Owner-controlled GitHub Environment setup step. Do not ask for raw credential material in chat. Continue all independent source/CI work.

After the permission/setup exists, resume the already-reviewed guarded provisioning workflow and require provider preflight/postflight evidence before any finality activation claim.

## E — Physical LAN trust/continuity — PENDING

Still required:

- intended ordinary-user Windows host acceptance;
- canonical company-LAN reachability/trust;
- real PDA HTTPS/reconnect evidence;
- >=60-minute Internet-cut continuity acceptance;
- restoration reconciliation evidence.

Do not infer physical PASS from GitHub-hosted CI.

## F — Parallel product lanes

Continue when ready and independent:

- account security: real ROOT/recovery delivery and verification flow;
- Web: authenticated Vietnamese-only business surfaces with Online/LAN parity;
- Android/PDA: endpoint/session/scanner/retry/HTTPS/reconnect and later UI fidelity from authorized Pick Pack reference evidence;
- Gateway/Google: projection/upload receipts, retry and readback behavior.

## Owner decision / release boundaries

- Portrait replacement behavior remains an Owner decision gate.
- STABLE activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall: 55% displayed / 55.4% exact | Phase 6: 60% | Immediate: Cloud operational snapshot/delta + canonical coverage | Then: LAN runtime refresh/rebase/readiness E2E | Credential lane: OWNER_PERMISSION_REQUIRED | Physical: real Windows/PDA + >=60-minute outage pending`
