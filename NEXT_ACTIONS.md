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

### A — Live-host public CA + real-device acceptance — PRIMARY PHYSICAL/PROVIDER CHAIN

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
- portable self-contained Windows host package `0.2.2`;
- certificate-manager/host CI receives no GitHub secrets.

Evidence includes secure HTTP workflow `34811861697`, Windows DPAPI TLS workflow `34817069447`, certificate-manager workflow `34819836554`, portable-host workflow `34821013175` and the corresponding successful baseline validators recorded in `CURRENT_STATE.md`.

Cloudflare read-only provider evidence verifies the intended project account and active `supra.cc.cd` zone, but does **not** prove DNS Edit permission for the credential that will execute on the real LAN host.

Next dependency chain:

1. use a **dedicated least-privilege DNS credential on the intended ordinary-user Windows LAN host**; do not move the final private key/PFX through GitHub;
2. live-verify exact account/zone and prove TXT create/read/delete only in `_acme-challenge.lan-beta.supra.cc.cd`;
3. run Let’s Encrypt **staging** issuance on that target Windows user/machine context;
4. verify the DPAPI protected PFX, canonical SAN and LAN Service HTTPS startup/restart;
5. only after staging PASS, explicitly enable production issuance and obtain the publicly trusted BETA certificate;
6. prove canonical LAN hostname resolution/reachability + browser certificate trust from the intended company Windows/network;
7. prove real NLS-MT90/PDA HTTPS login/business/reconnection behavior including Wi-Fi/LAN reacquisition;
8. keep `EMPLOYEE_PORTRAIT_REPLACE` closed until the Owner portrait semantic gate is resolved;
9. later include the approved secure public subset in physical Internet-cut continuity evidence.

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

### C — Account security routes — PARALLEL / CURRENT ROUTE SLICE SOURCE-CI PASS

Authority requires:

- ROOT authentication through email OTP, no permanent ROOT password;
- normal-user recovery/must-change state without bypassing authorization gates.

Current source/CI PASS:

- public Cloud Worker `POST /api/v1/auth/login` for normal permanent-password login;
- ROOT login bootstrap reaches `ROOT_EMAIL_OTP_REQUIRED` using username only and never creates a permanent-password session;
- public authenticated `POST /api/v1/auth/change-password` for normal accounts, including `MUST_CHANGE_PASSWORD` sessions;
- bounded request JSON handling and route tests covering token handling, ROOT password exclusion and permanent-password establishment;
- latest auth-route source/test commits `2bffb170319f9bf6d6d5f1953655ce960115d35e` and `1f2d328f21af90d09c5e3b260ad6452af05835c6`;
- clean-baseline run `34830186926`, job/check `103931457439`, HEAD `3bb22252a53ce25d69901c3261efd2ee3c54f57d`: **SUCCESS**, including Worker syntax, auth crypto contract, Worker unit tests, clean D1 schema and schema/runtime contract.

Continue:

1. add a reviewed email-delivery adapter and secure destination provisioning boundary; do **not** fabricate delivery success and do not put readable destination/provider secrets in public source;
2. expose ROOT email-OTP request/verify route using the existing V6 OTP state machine;
3. prove expiry/cooldown/supersede/replay/attempt/failure audit behavior through the public route;
4. add normal-user forgotten-password request/verify flow that ends in `MUST_CHANGE_PASSWORD` and a different new permanent password;
5. invalidate/rebind sessions as required by credential generation/security semantics;
6. prove no credential/OTP/destination leakage in logs/diagnostics;
7. verify/apply migration `0010_auth_v6_email_otp.sql` to the exact intended provider environment through the controlled workflow before claiming live E2E PASS.

No Owner decision is required for the V6 OTP lifecycle. The remaining blocker for full E2E is provider/destination integration and verified live migration state, not an unresolved ROOT policy decision.

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

`Overall: 55% displayed / 55.4% exact | Current: Phase 6 — LAN continuity/offline/reconcile | Primary physical gate: target Windows DNS credential + ACME staging -> production CA -> company Windows/PDA HTTPS acceptance | Parallel: ROOT OTP provider/public flow + Cloud reconciliation machine-auth/network E2E + V7 Web/App`
