# CHECKPOINT — VHDCHY

checkpoint_version: 35
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 4ecbea301d561c0b26b3eadcc413c00c44f650be
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB_ONLINE_LAN / ANDROID_PDA / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
lan_secure_http_ref: docs/LAN_SECURE_HTTP_V1.md
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
context_index_ref: CONTEXT_INDEX.md

## Current project progress

- Evidence-weighted total: **55.4% exact / 55% displayed**.
- Phase 6 LAN continuity/offline/reconcile: **60%**.
- Primary active phase: **Phase 6 — LAN continuity, local state, offline operation and reconciliation**.
- Parallel lanes: Phase 4 core business Service/API; Phase 5 Gateway/integrations; Phase 7 Online+LAN Web V7 UI; Phase 8 Android/PDA App; Cloud reconciliation machine-auth/network path.
- Delta in this execution block: **54.6% -> 55.4% exact** because a material secure LAN HTTP sub-slice reached real-runtime automated E2E PASS. No physical/provider acceptance credit was added.

## Authority / UI / execution state

- Current Web and Android/PDA user-facing implementation is **Vietnamese only**; multilingual UI is deferred.
- Android/PDA visual direction uses actual Pick Pack 1291 UI/UX evidence only; legacy business/data/credentials/runtime remain NON_AUTHORITY.
- Online Web + LAN Web share the V7 VHDCHY design direction inspired by Owner-supplied DNSHE screenshots without copying DNSHE branding/assets.
- Ready-queue parallel execution remains mandatory. A blocked node must not stall independent safe work.
- PASS requires reproducible evidence. CI/source PASS never substitutes for physical company-network/PDA PASS.

## Reconciled evidence through `4ecbea301d561c0b26b3eadcc413c00c44f650be`

### Secure LAN HTTPS login/session -> public Slice-1 HTTP route — SOURCE/CI PASS

Security decision:

- reusable passwords must not cross plaintext LAN HTTP;
- paired-client P-256 signatures provide authentication/integrity, not password confidentiality;
- reviewed transport is direct Kestrel HTTPS with a user-space PFX;
- production trust target is a publicly trusted certificate for `lan-beta.supra.cc.cd` / `lan.supra.cc.cd`;
- no company Windows certificate-store, firewall, router/AP or internal-DNS policy change is implicitly authorized;
- CI self-signed + thumbprint pinning is test-only and not production trust.

Runtime implementation:

- without TLS PFX: service is `HTTP_READ_ONLY`, secure login/business routes are not registered and mutation catch-all remains 503;
- with valid TLS PFX: Kestrel serves HTTPS and registers the reviewed secure routes;
- secure route readiness still requires synchronized authority + operational snapshots, paired-client security and executable primary credential authority;
- `POST /api/v1/auth/login` verifies paired signed request before normal-user credentials and issues a durable session bound to device/security epoch/authority generation;
- `POST /api/v1/data/commands` requires HTTPS + Bearer session + exact signed raw body and delegates to `LanBusinessRouteCoordinator`;
- ROOT remains email-OTP authority; no permanent ROOT password was introduced;
- `mustChangePassword` remains fail-closed for ordinary business mutation until reviewed change/recovery routes exist;
- `EMPLOYEE_PORTRAIT_REPLACE` remains fail-closed at the unresolved portrait lifecycle gate.

Automated evidence:

- secure HTTP workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**;
- same-HEAD clean baseline workflow `34811861613`: **SUCCESS**.

E2E vectors proven on the actual LAN Service process:

- HTTP read-only bootstrap without TLS;
- HTTPS listener startup with PFX;
- plaintext HTTP does not successfully reach the credential route on the TLS listener;
- signed paired-device normal-user login;
- wrong password rejection;
- signed-body tamper rejection;
- login replay rejection;
- durable session issuance;
- signed authorized `EMPLOYEE_CREATE` through the public business adapter;
- business replay rejection;
- valid session reuse after LAN Service restart;
- password absent from captured service diagnostics;
- SQLite foreign-key and quick-integrity checks.

Detailed boundary: `docs/LAN_SECURE_HTTP_V1.md`.

### Earlier LAN security/readiness foundations — remain PASS

- primary credential/session workflow `34805395079` at `583b3d0329166804b207332dadf6d449b07c0abf`: **SUCCESS**;
- integrated readiness workflow `34808274815`: **SUCCESS**;
- paired readiness baseline `34808274819`: **SUCCESS**;
- signed internal route-wiring workflow `34808936937` at `a469d29325ee83f8e19070234c1186ca23474a1c`: **SUCCESS**;
- route-wiring baseline `34808937077`: **SUCCESS**.

Production fail-closed safeguards remain intact.

### Cloud reconciliation foundation — PASS, network auth pending

Cloud LAN-reconciliation ingestion core remains source/CI PASS, including actor/source/event evidence preservation, collision handling, completed integration receipt ingestion/deduplication and packaged Worker module.

Evidence:

- commit `19c3463a633389b0cd85ddcede6a4525ccaa522a` — ingestion core;
- commit `591c4083973141155530357f5bddd4ee58efbdc7` — packaged module;
- workflows `34803221835` and `34803221873`: **SUCCESS**.

A reviewed machine/service authentication mechanism is still required before the LAN -> Cloud network route is exposed. Payload actor evidence is not authentication.

### LAN durable local foundation — PASS

Operational snapshots/materialized Slice-1, immutable local event/outbox, idempotent replay, actor evidence, employee/MNV/attendance, atomic active-code uniqueness, staged media and durable Cloud-sync queue remain PASS foundations.

Dedicated durable queue workflow `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.

### Web / Android foundations

- Shared V7 Online/LAN Web shell remains PASS in product-foundation workflow `34801611019` at `41565f3b2ffdca473f756e33bd71769e16d8af13`.
- Android foundation continues to build; exact final Pick Pack screen/layout evidence remains insufficiently surfaced, so exact visual details must not be invented.

## Current dependency graph / ready queue

### Primary — production-trusted LAN HTTPS + real-device acceptance

1. provision/automate a publicly trusted BETA certificate for `lan-beta.supra.cc.cd`; DNS-01 is preferred where provider capability permits because inbound Internet reachability to the laptop is unnecessary;
2. keep private key/PFX outside source control and define safe renewal/rotation;
3. prove canonical hostname resolution/reachability + browser certificate trust on intended ordinary-user company Windows/network;
4. prove real NLS-MT90/PDA HTTPS login/business/reconnection behavior;
5. implement/prove public ROOT email-OTP flow;
6. implement/prove normal-user must-change/password-change/recovery flow;
7. keep portrait replacement closed until explicit Owner semantic authority;
8. then include the approved secure public subset in physical Internet-cut acceptance.

### Parallel — reconciliation network E2E

1. define/prove machine/service authentication for Cloud reconciliation;
2. wire tested Cloud ingestion core behind that boundary;
3. connect LAN sender;
4. prove retry/restart/idempotency/result mapping E2E;
5. prove Google/Drive receipts do not duplicate output;
6. add cursor/delta/rebase/conflict-resolution behavior after transport stabilizes.

### Other independent lanes

- Web authenticated/business surfaces against real Service/LAN routes;
- Android endpoint/scanner/session/durable-queue/HTTPS work not blocked by missing visual evidence;
- broader Cloud/Gateway/Google receipt/retry/readback work;
- broader business adapter coverage.

## Portrait semantic gate

The portrait rule conflict remains unresolved: immediate deletion of the previous remote portrait versus offline staging while Drive is unavailable. Decision-independent media infrastructure may continue. Actual offline portrait-replacement semantics remain fail-closed until explicit Owner authority.

## Physical / STABLE boundary

- Publicly trusted BETA certificate/DNS/company-device HTTPS evidence is not yet PASS.
- Physical company ordinary-user Windows + target network + real NLS-MT90 regression remains pending.
- Canonical offline continuity acceptance remains **Window 2 >=60 minutes** after Internet cut while valid LAN remains.
- STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

do_not_repeat:
Do not treat memory as authority. Do not replay provider mutations from remembered state. Do not claim provider delivery/projection live without current evidence. Do not bypass action-safety. Do not send reusable passwords over plaintext LAN HTTP. Do not confuse signed requests with encryption. Do not treat CI self-signed certificates as production trust. Do not require or silently modify company certificate stores/firewall/router/AP/internal DNS. Do not weaken security/readiness assertions merely to make CI pass. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not invent Pick Pack UI details without actual reference evidence. Do not copy DNSHE branding/assets. Do not reintroduce stale duration-only offline TTL. Do not silently resolve the portrait immediate-delete/offline-staging conflict. Do not build/expose multilingual UI in the current stage. Do not inflate progress from tool/commit activity without acceptance evidence. Do not promote STABLE without explicit Owner approval.
