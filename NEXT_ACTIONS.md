# NEXT ACTIONS — VHDCHY

Updated: 2026-09-14
Delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress: `docs/PROGRESS_TRACKING_V1.md`
Current overall: **55% displayed / 54.6% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with a mandatory ready-queue scheduler. Execute independent safe work in parallel where tools permit; serialize dependency-bound or same-resource writes. A failed node must not stall unrelated ready work.

Public business mutation paths remain fail-closed until the corresponding current readiness/security/domain acceptance gates are proven. Evidence is required before PASS.

## Ready queue NOW

### A — LAN integrated security/readiness — PRIMARY

Already proven independently:

- signed-client pairing/request verification, replay defense and security-epoch fencing;
- durable user-session binding;
- authority snapshot V2 primary credential verification;
- primary credential -> authenticated evidence -> LAN session chain.

Evidence: dedicated workflow `34805395079` at `583b3d0329166804b207332dadf6d449b07c0abf` — **SUCCESS**.

Current integrated gate:

- workflow `34805628566` at `609a77ee3ef48eca9b1df5d91f63d2e7d875508f` — **FAILED** at the step intended to prove readiness advances only to `LAN_USER_SESSION_ROUTE_WIRING_REQUIRED`.

Next dependency chain:

1. reproduce/diagnose the exact integrated readiness invariant using source-level diagnostics;
2. fix the integration defect without relaxing fail-closed security/readiness conditions;
3. prove the evaluator reaches exactly the reviewed route-wiring blocker with dedicated CI;
4. implement authenticated login/session -> authorization/domain -> Slice-1 business route wiring;
5. add negative vectors for invalid session, wrong device/epoch, stale authority, DENY/cluster/module scope and unsupported command;
6. only after those gates PASS, expose the approved public LAN business subset.

### B — Cloud/LAN reconciliation E2E — PARALLEL

Already PASS at source foundation level:

- durable LAN reconciliation queue;
- immutable reconciliation envelope with actor/source evidence;
- Cloud ingestion core;
- event/idempotency/device/source collision handling;
- completed integration receipt ingestion/deduplication foundation;
- Worker packaging of the reconciliation module.

Evidence: workflows `34803221835` and `34803221873` at `591c4083973141155530357f5bddd4ee58efbdc7` — **SUCCESS**.

Next dependency chain:

1. wire the ingestion core behind the reviewed authenticated Cloud reconciliation network/API boundary;
2. keep the route isolated from unrelated public mutations;
3. connect the LAN network sender to that route;
4. prove retry/restart/idempotency and stable result mapping end-to-end;
5. prove existing Google/Drive receipts do not duplicate downstream output;
6. retain explicit conflict evidence and ADMIN+ resolution boundary;
7. add sync cursor/delta/rebase behavior after the transport path is stable.

### C — Online Web + LAN Web — PARALLEL

Current shared V7 shell and Web contract have PASS evidence in product-foundation workflow `34801611019`.

Continue with actual authenticated login/recovery against real Service routes, then employee/attendance Slice-1 screens and later ADMIN+ conflict-resolution surfaces. Preserve Online/LAN parity, local/offline-safe critical assets and **Vietnamese-only** current UI. Do not fake login success.

### D — Android/PDA App — PARALLEL WHERE NOT BLOCKED

Current APK foundation builds and visible text is Vietnamese-only.

Continue non-visual current-contract work that does not depend on missing Pick Pack visual evidence: Service/LAN endpoint abstraction, scanner-to-domain-command boundary, durable retry mechanics, network/sync state and authenticated session handling. Continue locating the actual final Pick Pack 1291 UI source/artifacts; do not invent unavailable screen details.

### E — Cloud/Gateway/Google — PARALLEL

Continue provider-neutral business coverage and controlled Google projection/upload receipt, retry and readback behavior. Sheets/Drive remain downstream only. Keep provider mutations fail-closed on identity/config mismatch and live-verify exact provider state before deployment/mutation.

## Owner decision gate — portrait only

The existing portrait conflict remains unresolved: immediate deletion of the previous portrait versus offline staging while Drive is unavailable. Continue decision-independent media infrastructure, but do not choose offline portrait-replacement semantics without explicit Owner authority.

## Physical BETA acceptance — later, when source is ready

Physical evidence remains required on the intended ordinary-user company Windows laptop/network and real NLS-MT90 devices: reachability/discovery/reacquisition, PDA workflow/Wi-Fi recovery, **>=60-minute** Internet-cut Window 2, restoration reconciliation, host restart/update rollback, battery/background behavior, synthetic 10/25/50/100 load + soak, and Owner UAT.

## STABLE

STABLE may be prepared safely in isolation, but no production business activation/promotion occurs until mandatory BETA gates pass and the Owner explicitly approves. Promote the exact accepted BETA artifacts; do not copy BETA business/runtime data by default.

## Current execution line

`Overall: 55% displayed / 54.6% exact | Current: Phase 6 — LAN continuity/offline/reconcile | Primary next gate: integrated LAN readiness -> reviewed route wiring | Parallel: Cloud reconciliation network E2E + Phase 4/5 + V7 Web/App`
