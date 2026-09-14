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

### A — Secure LAN login transport + public HTTP adapter — PRIMARY

Already proven:

- signed-client pairing/request verification, replay defense and security-epoch fencing;
- durable user-session binding;
- authority snapshot V2 primary credential verification;
- primary credential -> authenticated evidence -> LAN session chain;
- integrated readiness through the reviewed route-wiring stage;
- signed session -> authorization/domain -> Slice-1 business coordinator;
- public HTTP mutation remains deliberately closed.

Evidence:

- primary auth workflow `34805395079` at `583b3d0329166804b207332dadf6d449b07c0abf` — **SUCCESS**;
- integrated readiness workflow `34808274815` — **SUCCESS**;
- baseline paired with readiness `34808274819` — **SUCCESS**;
- signed route-wiring workflow `34808936937` at `a469d29325ee83f8e19070234c1186ca23474a1c` — **SUCCESS**;
- baseline at same route-wiring commit `34808937077` — **SUCCESS**.

Route-wiring CI covers positive execution and replay, signed-body binding, wrong target, device/session mismatch, permission DENY, unsupported command, must-change-password, stale authority and portrait fail-closed vectors.

Next dependency chain:

1. define a secure credential transport for LAN login that does not expose reusable passwords over plaintext HTTP;
2. ensure the design remains compatible with ordinary-user/no-admin host constraints and does not require changes to company certificate stores, firewall/router/AP/internal DNS or policy;
3. implement the reviewed login/session HTTP adapter only after that secure transport is proven;
4. wire public LAN business HTTP handling to `LanBusinessRouteCoordinator` so the exact signed raw body, device proof, session and current authority remain mandatory;
5. add HTTP-level negative/E2E vectors and restart/re-auth behavior;
6. keep `EMPLOYEE_PORTRAIT_REPLACE` closed until the Owner portrait semantic gate is resolved;
7. only then consider opening the approved public LAN business subset.

Do not mistake request signing for confidentiality: P-256 signatures authenticate/integrity-protect the request but do not encrypt the password.

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

1. define and prove the reviewed machine/service authentication boundary for the reconciliation route; payload actor evidence is not authentication;
2. wire the ingestion core behind that authenticated Cloud network/API boundary;
3. keep the route isolated from unrelated public mutations;
4. connect the LAN network sender to that route;
5. prove retry/restart/idempotency and stable result mapping end-to-end;
6. prove existing Google/Drive receipts do not duplicate downstream output;
7. retain explicit conflict evidence and ADMIN+ resolution boundary;
8. add sync cursor/delta/rebase behavior after the transport path is stable.

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

`Overall: 55% displayed / 54.6% exact | Current: Phase 6 — LAN continuity/offline/reconcile | Primary next gate: secure LAN login transport -> HTTP adapter -> public LAN route E2E | Parallel: Cloud reconciliation machine-auth/network E2E + Phase 4/5 + V7 Web/App`
