# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 54.6% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

The numerical baseline remains unchanged. LAN readiness and session-to-business route wiring now have dedicated CI PASS evidence, but public LAN HTTP mutation is still intentionally closed and no additional product/physical acceptance gate has closed yet.

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App and reconciliation transport.

## Current product authority

- VHDCHY is one platform: Online Web + LAN Web, Android/PDA App, Cloud Service/Gateway and full LAN Service substitute.
- Web/App current user-facing implementation language is **Vietnamese only**. Multilingual work is deferred until a later explicit Owner decision.
- Online Web and LAN Web share one V7 design system; LAN-critical UI assets must remain locally available without Internet.
- Pick Pack 1291 is authorized only as Android/PDA UI/UX reference; legacy business logic/data/credentials/runtime remain NON_AUTHORITY.
- DNSHE screenshots are visual-direction reference only; do not copy DNSHE branding/proprietary assets.
- Sheets/Drive remain downstream projection/storage, never canonical business authority.
- D1 remains central consolidated structured authority after synchronization; LAN edge state + immutable event journal is local operational authority for events accepted by LAN.

## Reconciled source/evidence through HEAD `a469d29325ee83f8e19070234c1186ca23474a1c`

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

Source includes:

- signed P-256 client pairing/request verification with replay and security-epoch fencing;
- durable LAN user-session binding;
- authority snapshot V2 primary password verifier;
- V1/V2 authorization compatibility for current synchronized permission evaluation;
- primary credential -> authenticated evidence -> LAN session chain.

Dedicated primary-auth workflow `34805395079` at commit `583b3d0329166804b207332dadf6d449b07c0abf`: **SUCCESS**.

### Integrated LAN readiness — PASS through reviewed route-wiring gate

The previous readiness failures were traced to test coupling, not production safeguards:

1. readiness reused a DB containing intentionally unreconciled local work, so `OperationalSnapshotStore` correctly rejected replacement;
2. after DB isolation, the harness still depended on authority/operational fixtures created by the base harness.

The readiness harness was isolated and made self-seeding. Production fail-closed safeguards were not weakened.

Evidence:

- workflow `34808274815` (`Validate LAN fail-closed readiness gate`): **SUCCESS**;
- same-chain baseline workflow `34808274819`: **SUCCESS**.

This proves synchronized snapshots -> signed-client security -> primary credential authority -> reviewed route-wiring blocker/restart fail-closed behavior.

### Signed LAN session -> Slice-1 business coordinator — PASS

`LanBusinessRouteCoordinator` now binds an already-authenticated LAN session to the Slice-1 business adapter with all of the following gates:

- signed paired-device P-256 request;
- signed method/route target;
- SHA-256 binding to the exact raw request body;
- nonce/replay and security-epoch checks;
- session token bound to device + security epoch;
- current authority snapshot/session freshness;
- `MUST_CHANGE_PASSWORD` blocks ordinary business mutation;
- authorization/domain/command gate before business execution;
- authenticated actor comes only from server-side session evidence, never client actor fields;
- portrait command remains fail-closed at its unresolved media lifecycle gate.

The coordinator parses the exact signed raw body itself, preventing a mismatch between signed bytes and separately supplied parsed business fields.

Dedicated workflow `34808936937` at commit `a469d29325ee83f8e19070234c1186ca23474a1c`: **SUCCESS**.

Covered vectors include positive business execution plus replay rejection, signed-body mismatch, wrong route target, wrong device/session binding, permission DENY precedence, unsupported command, `MUST_CHANGE_PASSWORD`, stale authority requiring re-authentication and portrait fail-closed behavior.

Baseline workflow `34808937077` at the same commit: **SUCCESS**.

### Public LAN HTTP mutation — intentionally CLOSED

The same route-wiring workflow explicitly proves the public HTTP mutation endpoint remains closed/503. Coordinator PASS is therefore **not** equivalent to public mutation readiness.

The next material blocker is secure login transport / HTTP adapter wiring. P-256 request signing authenticates device/request integrity but does not encrypt a user's password. No public LAN login route may send a reusable password over plaintext HTTP merely to advance readiness.

Current no-admin host constraints also prohibit silently depending on changes to company certificate stores, firewall, router/AP, internal DNS or other corporate policy.

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

Primary LAN dependency chain:

1. design/prove a secure LAN login transport compatible with normal-user/no-admin host constraints and without plaintext reusable passwords;
2. wire the reviewed HTTP login/session adapter only after that transport is safe;
3. wire public LAN business HTTP handling to the already-proven signed-session coordinator;
4. re-run readiness/E2E negative vectors and keep portrait closed;
5. only then consider opening the approved public LAN business subset.

Parallel reconciliation chain:

- define/prove the reviewed machine/service authentication boundary for Cloud reconciliation;
- wire the already-tested Cloud ingestion core behind that boundary;
- connect LAN sender -> Cloud ingestion;
- prove retry/restart/idempotency/receipt/conflict E2E semantics.

Other independent lanes continue: Web authenticated/business surfaces, Android non-visual current-contract work, broader Cloud/Gateway/Google receipt paths, and business adapter coverage.

## Physical / STABLE boundary

Final company ordinary-user Windows + company network + real NLS-MT90 regression remains physical-only. Canonical offline continuity acceptance still requires **Window 2 >=60 minutes** after Internet cut while valid LAN remains.

STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.
