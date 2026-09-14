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

### A — Live-host public CA + real-device acceptance — PRIMARY

Already SOURCE/CI PASS:

- Kestrel HTTPS transport with no-TLS `HTTP_READ_ONLY` fallback;
- paired signed normal-user login and durable LAN session;
- signed session -> authorization/domain -> reviewed Slice-1 business route;
- Windows DPAPI CurrentUser protected PFX;
- Schannel-compatible current-user key handling without certificate-store installation/admin;
- canonical SAN validation, wrong-host rejection, corrupt-protected-PFX rejection and no raw PFX disk leak;
- separate ACME DNS-01 certificate manager;
- exact Cloudflare account/zone precondition checks;
- ACME challenge write/read/cleanup logic restricted to `_acme-challenge.<canonical-host>`;
- staging-by-default ACME behavior and explicit production opt-in;
- renewal threshold + protected ACME account-key handling;
- certificate-manager CI receives no GitHub secrets.

Evidence:

- secure HTTP workflow `34811861697`, check `103874646267`: **SUCCESS**;
- Windows DPAPI TLS workflow `34817069447`, check `103889883955`: **SUCCESS**;
- certificate-manager workflow `34819836554`, check `103898656531`, commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`: **SUCCESS**;
- same-HEAD baseline validator `34819836553`, check `103898626431`: **SUCCESS**;
- detailed boundaries: `docs/LAN_SECURE_HTTP_V1.md` and `docs/LAN_CERTIFICATE_MANAGER_V1.md`.

Cloudflare read-only provider evidence:

- inspection workflow `34816004518`, check `103886701628`, commit `3588bbcc29b665be453e3988e8f5898ed35a9fa8`: **SUCCESS**;
- expected project account verified;
- zone `supra.cc.cd` verified active;
- at inspection time no `lan-beta.supra.cc.cd` or `_acme-challenge.lan-beta.supra.cc.cd` record existed.

This read evidence does **not** prove DNS Edit permission for the credential that will execute on the real LAN host.

Next dependency chain:

1. prepare/use a **dedicated least-privilege DNS credential on the intended ordinary-user Windows LAN host**; do not move the final private key/PFX through GitHub;
2. live-verify exact account/zone and prove TXT create/read/delete only in `_acme-challenge.lan-beta.supra.cc.cd`;
3. run Let’s Encrypt **staging** issuance on that target Windows user/machine context;
4. verify the DPAPI protected PFX, canonical SAN and LAN Service HTTPS startup/restart;
5. only after staging PASS, explicitly enable production issuance and obtain the publicly trusted BETA certificate;
6. prove canonical LAN hostname resolution/reachability + browser certificate trust from the intended company Windows/network;
7. prove real NLS-MT90/PDA HTTPS login/business/reconnection behavior including Wi-Fi/LAN reacquisition;
8. add public ROOT email-OTP flow; do not introduce a permanent ROOT password;
9. add reviewed normal-user must-change/password-change/recovery routes;
10. keep `EMPLOYEE_PORTRAIT_REPLACE` closed until the Owner portrait semantic gate is resolved;
11. later include the approved secure public subset in physical Internet-cut continuity evidence.

Important boundaries:

- GitHub runner DPAPI output is not deployable as the laptop’s final protected PFX; DPAPI belongs to the target Windows user/machine context.
- Do not use browser certificate-warning click-through or an ad-hoc trust root.
- Do not create a public `lan-beta` A/CNAME merely to make DNS appear complete; canonical offline/company-LAN resolution is a separate V4 acceptance problem and must match the actual network model.
- Do not assume the existing GitHub Cloudflare token has the least-privilege DNS Edit scope required for the host credential.

### B — Cloud/LAN reconciliation E2E — PARALLEL

Already PASS at source foundation level:

- durable LAN reconciliation queue;
- immutable reconciliation envelope with actor/source evidence;
- Cloud ingestion core;
- event/idempotency/device/source collision handling;
- completed integration receipt ingestion/deduplication foundation;
- Worker packaging of the reconciliation module.

Evidence: workflows `34803221835` and `34803221873` at `591c4083973141155530357f5bddd4ee58efbdc7`: **SUCCESS**.

Next dependency chain:

1. define and prove the reviewed machine/service authentication boundary for reconciliation; payload actor evidence is not authentication;
2. wire the ingestion core behind that authenticated Cloud route;
3. keep the route isolated from unrelated public mutations;
4. connect the LAN network sender;
5. prove retry/restart/idempotency and stable result mapping end-to-end;
6. prove existing Google/Drive receipts do not duplicate downstream output;
7. retain explicit conflict evidence and ADMIN+ resolution boundary;
8. add sync cursor/delta/rebase after the transport path is stable.

### C — Account security routes — PARALLEL

Authority already requires:

- ROOT authentication through email OTP, no permanent ROOT password;
- normal-user recovery/must-change state without bypassing authorization gates.

Continue:

1. public ROOT email-OTP request/verify route against current authority semantics;
2. OTP expiry/cooldown/replay/attempt-limit tests;
3. normal-user password-change route for authenticated `mustChangePassword` sessions;
4. normal-user forgotten-password recovery path that ends in mandatory password change;
5. invalidate/rebind sessions as required by current credential generation/security semantics;
6. prove no credential/OTP leakage in logs.

### D — Online Web + LAN Web — PARALLEL

Current shared V7 shell and Web contract have PASS evidence. Continue actual authenticated login/recovery against current Service/LAN routes, then employee/attendance Slice-1 screens and later ADMIN+ conflict-resolution surfaces.

Preserve:

- Online/LAN parity;
- local/offline-safe critical assets;
- **Vietnamese-only** current UI;
- HTTPS versus `HTTP_READ_ONLY` runtime distinction;
- paired-device/session/readiness controls;
- no fake login success.

### E — Android/PDA App — PARALLEL WHERE NOT BLOCKED

Continue non-visual current-contract work that does not depend on missing Pick Pack visual evidence:

- Service/LAN endpoint abstraction;
- certificate-validating HTTPS LAN access;
- authenticated session handling;
- scanner -> domain-command boundary;
- durable retry mechanics;
- network/sync state and reconnection behavior.

Continue locating actual final Pick Pack 1291 UI source/artifacts; do not invent unavailable screen details.

### F — Cloud/Gateway/Google — PARALLEL

Continue provider-neutral business coverage and controlled Google projection/upload receipt, retry and readback behavior. Sheets/Drive remain downstream only. Keep provider mutations fail-closed on identity/config mismatch and live-verify exact provider state before deployment/mutation.

## Owner decision gate — portrait only

The portrait conflict remains unresolved: immediate deletion of the previous portrait versus offline staging while Drive is unavailable. Continue decision-independent media infrastructure, but do not choose offline portrait-replacement semantics without explicit Owner authority.

## Physical BETA acceptance — later, when live trust path is ready

Physical evidence remains required on the intended ordinary-user company Windows laptop/network and real NLS-MT90 devices: trusted HTTPS/reachability/discovery/reacquisition, PDA workflow/Wi-Fi recovery, **>=60-minute** Internet-cut Window 2, restoration reconciliation, host restart/update rollback, battery/background behavior, synthetic 10/25/50/100 load + soak, and Owner UAT.

## STABLE

STABLE may be prepared safely in isolation, but no production business activation/promotion occurs until mandatory BETA gates pass and the Owner explicitly approves. Promote the exact accepted BETA artifacts; do not copy BETA business/runtime data by default.

## Current execution line

`Overall: 55% displayed / 55.4% exact | Current: Phase 6 — LAN continuity/offline/reconcile | Primary next gate: target-Windows DNS credential + ACME staging -> production CA -> company Windows/PDA HTTPS acceptance | Parallel: ROOT/must-change account-security routes + Cloud reconciliation machine-auth/network E2E + V7 Web/App`
