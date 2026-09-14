# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 55.4% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

Phase 6 advances from 55% to 60% because the secure LAN credential transport plus public login/session -> reviewed Slice-1 HTTP business route now have real runtime E2E CI evidence. This is implementation/automation credit only; publicly trusted certificate/DNS/real company-device acceptance remains open.

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App and Cloud reconciliation transport.

## Current product authority

- VHDCHY is one platform: Online Web + LAN Web, Android/PDA App, Cloud Service/Gateway and full LAN Service substitute.
- Web/App current user-facing implementation language is **Vietnamese only**. Multilingual work is deferred until a later explicit Owner decision.
- Online Web and LAN Web share one V7 design system; LAN-critical UI assets must remain locally available without Internet.
- Pick Pack 1291 is authorized only as Android/PDA UI/UX reference; legacy business logic/data/credentials/runtime remain NON_AUTHORITY.
- DNSHE screenshots are visual-direction reference only; do not copy DNSHE branding/proprietary assets.
- Sheets/Drive remain downstream projection/storage, never canonical business authority.
- D1 remains central consolidated structured authority after synchronization; LAN edge state + immutable event journal is local operational authority for events accepted by LAN.

## Reconciled source/evidence through HEAD `63ceeb6a8db865ced1870209c7cb74d4f65baea1`

### Secure LAN HTTPS login/session -> public Slice-1 HTTP route — source/CI PASS

The reviewed transport is direct Kestrel HTTPS using a user-space PFX. Reusable passwords are never intentionally sent through the plaintext LAN mutation path merely because the paired client signs requests. Request signing remains defense-in-depth for device/request authenticity and integrity; TLS supplies credential confidentiality.

Runtime behavior:

- without `VHDCHY_LAN_TLS_PFX_PATH`, LAN remains `HTTP_READ_ONLY`, login/business routes are not registered and mutation catch-all remains 503;
- with a valid PFX, Kestrel serves HTTPS and exposes only the reviewed secure login/business routes plus existing read surfaces;
- the PFX is loaded with ephemeral user-space key handling; the design does not require company Windows certificate-store changes;
- secure-route readiness remains dependent on synchronized authority/operational snapshots, paired-client security and primary credential authority;
- `POST /api/v1/auth/login` verifies the paired signed request before normal-user credential verification and issues a durable device/security-epoch/authority-bound LAN session;
- `POST /api/v1/data/commands` requires HTTPS + Bearer session + exact signed raw body and delegates to the already-proven `LanBusinessRouteCoordinator`;
- ROOT semantics are unchanged: current password route returns `ROOT_EMAIL_OTP_REQUIRED`; no permanent ROOT password was introduced;
- `mustChangePassword` remains session evidence and ordinary business mutation stays blocked until a reviewed password-change path is implemented;
- portrait replacement remains fail-closed at the unresolved media semantic gate.

Dedicated secure HTTP workflow `34811861697`, job/check `103874646267`, at commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**.

Covered runtime vectors:

- actual LAN Service process bootstrap in HTTP read-only mode;
- actual LAN Service process restart in HTTPS mode;
- plaintext HTTP does not successfully reach the credential route on the TLS listener;
- paired signed login;
- wrong password rejection;
- signed body tamper rejection;
- login replay rejection;
- durable session issuance;
- signed authorized `EMPLOYEE_CREATE` through the public business adapter;
- business replay rejection;
- valid session reuse after LAN Service restart;
- password absent from captured service diagnostics;
- SQLite foreign-key/quick integrity checks.

Baseline workflow `34811861613` on the same HEAD: **SUCCESS**.

Detailed boundary: `docs/LAN_SECURE_HTTP_V1.md`.

This is **not yet physical/provider acceptance**. CI uses a self-signed certificate with explicit thumbprint pinning only to prove TLS/runtime behavior. Production BETA still requires a publicly trusted certificate for `lan-beta.supra.cc.cd`, canonical DNS/reachability, ordinary-user company Windows browser trust and real PDA/NLS-MT90 HTTPS/reconnect evidence without unauthorized company-admin changes.

### Cloud reconciliation ingestion — source/CI PASS foundation

The Cloud Worker source includes the reviewed LAN reconciliation ingestion module and packaging. Tests cover preservation of LAN event/actor evidence, event/idempotency/device/source collision behavior, and completed integration receipt handling without duplicate downstream output at the ingestion-core level.

Evidence:

- commit `19c3463a633389b0cd85ddcede6a4525ccaa522a`: Cloud LAN reconciliation ingestion core;
- commit `591c4083973141155530357f5bddd4ee58efbdc7`: package reconciliation ingestion module;
- workflow `34803221835` (`Validate clean baseline`) at `591c408...`: **SUCCESS**;
- workflow `34803221873` (`Build product foundations`) at `591c408...`: **SUCCESS**.

This proves the source-level ingestion foundation. It does **not** yet prove the final authenticated network route from LAN -> Cloud, full LAN sender E2E retry/restart behavior, production provider deployment, complete conflict-resolution UX or physical continuity.

### LAN durable queue / Slice-1 — PASS foundation

Current LAN source retains automated evidence for operational-state materialization, immutable local event/outbox behavior, idempotent replay, actor evidence, employee/MNV/attendance Slice-1, atomic active-code uniqueness, staged media, and the durable Cloud reconciliation queue state machine.

Dedicated queue workflow `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.

### LAN signed-client / session / primary credential — dedicated CI PASS

Source includes signed P-256 client pairing/request verification with replay/security-epoch fencing, durable LAN user-session binding, authority snapshot V2 primary password verifier, V1/V2 authorization compatibility and the primary credential -> authenticated evidence -> LAN session chain.

Dedicated primary-auth workflow `34805395079` at commit `583b3d0329166804b207332dadf6d449b07c0abf`: **SUCCESS**.

### Integrated LAN readiness + internal business coordinator — PASS

Readiness and route-wiring foundations remain proven without weakening fail-closed safeguards.

Evidence:

- readiness workflow `34808274815`: **SUCCESS**;
- paired baseline `34808274819`: **SUCCESS**;
- signed route-wiring workflow `34808936937` at `a469d29325ee83f8e19070234c1186ca23474a1c`: **SUCCESS**;
- route-wiring baseline `34808937077`: **SUCCESS**.

The coordinator binds signed paired-device request -> device-bound session -> current authority -> authorization/domain/command gate -> Slice-1 adapter. Authenticated actor evidence is server-derived and the coordinator consumes the exact signed raw body.

### Cloud reconciliation network boundary — still pending

The Cloud ingestion core is service-to-service. Payload actor evidence must not be treated as authentication. Before exposing the LAN -> Cloud reconciliation route, a reviewed machine/service authentication boundary is required; then LAN sender -> Cloud E2E retry/restart/idempotency/result mapping can be proven.

### Web V7 shell — PASS foundation

Product-foundation workflow `34801611019` at `41565f3b2ffdca473f756e33bd71769e16d8af13`: **SUCCESS** for the current shared Web shell plus Cloud/LAN/App foundations. Remaining Web work includes actual authenticated login/recovery, business modules, ADMIN+ conflict surfaces and Cloud/LAN E2E.

### Android/PDA — build foundation only

Current Android source builds and visible foundation text is Vietnamese-only. Exact final Pick Pack 1291 screen/layout artifacts are still not sufficiently surfaced in current accessible reference evidence, so exact visual details must not be invented. Real NLS-MT90, scanner, background/battery and signed release acceptance remain pending.

## Portrait semantic gate — OWNER_DECISION_REQUIRED only when implementation reaches it

The existing rule conflict remains unresolved:

- one rule requires immediate deletion of the previous portrait;
- LAN/offline media semantics permit staging when Drive is unavailable.

Decision-independent media work may continue. Portrait replacement while Drive is unavailable remains fail-closed until explicit Owner authority resolves the conflict.

## Immediate execution direction

Primary LAN chain now moves from source/CI transport implementation to real trust/acceptance:

1. provision/automate a publicly trusted BETA certificate for `lan-beta.supra.cc.cd` without relying on company-admin certificate-store changes;
2. prove canonical DNS/reachability and HTTPS trust on the intended ordinary-user company Windows environment;
3. prove real PDA/NLS-MT90 HTTPS/reconnect behavior;
4. implement/prove the remaining public account-security surfaces required for actual use, especially ROOT email OTP and normal-user must-change/password recovery/change flows;
5. keep portrait replacement closed until the Owner semantic decision exists;
6. then include the approved secure public subset in the physical continuity window.

Parallel reconciliation chain:

- define/prove the reviewed machine/service authentication boundary for Cloud reconciliation;
- wire the already-tested Cloud ingestion core behind that boundary;
- connect LAN sender -> Cloud ingestion;
- prove retry/restart/idempotency/receipt/conflict E2E semantics.

Other independent lanes continue: Web authenticated/business surfaces, Android non-visual current-contract work, broader Cloud/Gateway/Google receipt paths, and business adapter coverage.

## Physical / STABLE boundary

Final company ordinary-user Windows + company network + real NLS-MT90 regression remains physical-only. Canonical offline continuity acceptance still requires **Window 2 >=60 minutes** after Internet cut while valid LAN remains.

STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.
