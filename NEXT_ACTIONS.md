# NEXT ACTIONS — VHDCHY

Updated: 2026-09-14
Delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress: `docs/PROGRESS_TRACKING_V1.md`
Current overall: **55% displayed / 55.4% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with a mandatory ready-queue scheduler. Execute independent safe work in parallel where tools permit; serialize dependency-bound or same-resource writes. A failed node must not stall unrelated ready work.

Public business mutation paths remain fail-closed unless the corresponding current transport/readiness/security/domain acceptance gates are satisfied. Evidence is required before PASS.

## Ready queue NOW

### A — Publicly trusted LAN HTTPS + real-device acceptance — PRIMARY

Secure transport/source work now proven:

- user-space Kestrel HTTPS transport with PFX/private key;
- no-PFX fallback remains `HTTP_READ_ONLY` and mutation 503;
- paired signed normal-user login over HTTPS;
- durable device/security-epoch/authority-bound LAN session;
- signed session -> authorization/domain -> Slice-1 public business HTTP adapter;
- replay, signed-body tamper, wrong password and restart-session behavior;
- no password in captured service diagnostics;
- portrait mutation remains closed.

Evidence:

- secure HTTP workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1` — **SUCCESS**;
- same-HEAD clean baseline workflow `34811861613` — **SUCCESS**;
- detailed boundary: `docs/LAN_SECURE_HTTP_V1.md`.

Next dependency chain:

1. provision a publicly trusted BETA certificate for `lan-beta.supra.cc.cd` using an approach compatible with ordinary-user/no-admin operation; DNS-01/automated issuance is preferred where provider capability permits because it does not require inbound Internet reachability to the laptop;
2. keep the PFX/private key outside GitHub and define renewal/rotation handling without requiring company Windows certificate-store changes;
3. prove canonical LAN hostname resolution/reachability and certificate trust from the intended ordinary-user company Windows laptop/network;
4. prove HTTPS/login/business-route behavior from the real NLS-MT90/PDA path, including Wi-Fi/LAN reacquisition;
5. add public ROOT email-OTP flow; do not introduce a permanent ROOT password;
6. add reviewed normal-user must-change/password-change/recovery route(s) so a session marked `mustChangePassword` can recover legitimately rather than bypass the gate;
7. keep `EMPLOYEE_PORTRAIT_REPLACE` closed until the Owner portrait semantic gate is resolved;
8. include the approved secure public subset in later physical Internet-cut continuity evidence.

CI self-signed/thumbprint-pinned TLS is test evidence only. Do not use browser certificate warning click-through or install an ad-hoc company-device trust root as the production solution.

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

Continue actual authenticated login/recovery against current Service/LAN routes, then employee/attendance Slice-1 screens and later ADMIN+ conflict-resolution surfaces. Preserve Online/LAN parity, local/offline-safe critical assets and **Vietnamese-only** current UI. Do not fake login success.

LAN Web integration must respect the new HTTPS/read-only runtime distinction and must not bypass paired-device/session/readiness controls.

### D — Android/PDA App — PARALLEL WHERE NOT BLOCKED

Current APK foundation builds and visible text is Vietnamese-only.

Continue non-visual current-contract work that does not depend on missing Pick Pack visual evidence: Service/LAN endpoint abstraction, scanner-to-domain-command boundary, durable retry mechanics, network/sync state, authenticated session handling and HTTPS certificate-validating LAN access. Continue locating the actual final Pick Pack 1291 UI source/artifacts; do not invent unavailable screen details.

### E — Cloud/Gateway/Google — PARALLEL

Continue provider-neutral business coverage and controlled Google projection/upload receipt, retry and readback behavior. Sheets/Drive remain downstream only. Keep provider mutations fail-closed on identity/config mismatch and live-verify exact provider state before deployment/mutation.

## Owner decision gate — portrait only

The existing portrait conflict remains unresolved: immediate deletion of the previous portrait versus offline staging while Drive is unavailable. Continue decision-independent media infrastructure, but do not choose offline portrait-replacement semantics without explicit Owner authority.

## Physical BETA acceptance — later, when source/provider path is ready

Physical evidence remains required on the intended ordinary-user company Windows laptop/network and real NLS-MT90 devices: trusted HTTPS/reachability/discovery/reacquisition, PDA workflow/Wi-Fi recovery, **>=60-minute** Internet-cut Window 2, restoration reconciliation, host restart/update rollback, battery/background behavior, synthetic 10/25/50/100 load + soak, and Owner UAT.

## STABLE

STABLE may be prepared safely in isolation, but no production business activation/promotion occurs until mandatory BETA gates pass and the Owner explicitly approves. Promote the exact accepted BETA artifacts; do not copy BETA business/runtime data by default.

## Current execution line

`Overall: 55% displayed / 55.4% exact | Current: Phase 6 — LAN continuity/offline/reconcile | Primary next gate: public CA certificate + canonical DNS/real Windows/PDA HTTPS acceptance | Parallel: ROOT/must-change account-security routes + Cloud reconciliation machine-auth/network E2E + V7 Web/App`
