# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 54.6% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

The numerical baseline is unchanged. New source/CI work below advances implementation detail but does not yet close a new product acceptance gate because the integrated LAN readiness chain is currently failing and physical/E2E evidence is still pending.

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App and reconciliation transport.

## Current product authority

- VHDCHY is one platform: Online Web + LAN Web, Android/PDA App, Cloud Service/Gateway and full LAN Service substitute.
- Web/App current user-facing implementation language is **Vietnamese only**. Multilingual work is deferred until a later explicit Owner decision.
- Online Web and LAN Web share one V7 design system; LAN-critical UI assets must remain locally available without Internet.
- Pick Pack 1291 is authorized only as Android/PDA UI/UX reference; legacy business logic/data/credentials/runtime remain NON_AUTHORITY.
- DNSHE screenshots are visual-direction reference only; do not copy DNSHE branding/proprietary assets.
- Sheets/Drive remain downstream projection/storage, never canonical business authority.
- D1 remains central consolidated structured authority after synchronization; LAN edge state + immutable event journal is local operational authority for events accepted by LAN.

## Reconciled source/evidence through pre-checkpoint HEAD `609a77ee3ef48eca9b1df5d91f63d2e7d875508f`

### Cloud reconciliation ingestion — source/CI PASS foundation

The Cloud Worker source now includes the reviewed LAN reconciliation ingestion module and packaging. Current tests cover preservation of LAN event/actor evidence, event/idempotency/device/source collision behavior, and completed integration receipt handling without duplicate downstream output at the ingestion-core level.

Evidence:

- commit `19c3463a633389b0cd85ddcede6a4525ccaa522a`: Cloud LAN reconciliation ingestion core;
- commit `4a8c6b9...`: collision/receipt vectors;
- commit `591c4083973141155530357f5bddd4ee58efbdc7`: package reconciliation ingestion module;
- workflow `34803221835` (`Validate clean baseline`) at `591c408...`: **SUCCESS**;
- workflow `34803221873` (`Build product foundations`) at `591c408...`: **SUCCESS**.

This proves the source-level ingestion foundation. It does **not** yet prove the final authenticated network route from LAN -> Cloud, full LAN sender E2E retry/restart behavior, production provider deployment, complete conflict-resolution UX or physical continuity.

### LAN durable queue / Slice-1 — PASS foundation

Current LAN source retains automated evidence for operational-state materialization, immutable local event/outbox behavior, idempotent replay, actor evidence, employee/MNV/attendance Slice-1, atomic active-code uniqueness, staged media, and the durable Cloud reconciliation queue state machine.

Dedicated queue workflow `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.

### LAN signed-client / session / primary credential — dedicated CI PASS

Since the previous checkpoint, source now includes:

- signed P-256 client pairing/request verification with replay and security-epoch fencing;
- durable LAN user-session binding;
- authority snapshot V2 primary password verifier;
- V1/V2 authorization compatibility for current synchronized permission evaluation;
- primary credential -> authenticated evidence -> LAN session chain.

Dedicated primary-auth workflow `34805395079` at commit `583b3d0329166804b207332dadf6d449b07c0abf`: **SUCCESS**.

This is source/CI evidence for the isolated authentication chain. It is not authorization to open public LAN mutations.

### Integrated LAN readiness — FAILED / fail-closed

The current readiness evaluator is designed to advance through:

1. synchronized authority + operational snapshot;
2. signed-client pairing/security;
3. executable synchronized primary-login authority;
4. reviewed login/session-to-business route wiring.

Current HEAD `609a77ee3ef48eca9b1df5d91f63d2e7d875508f` attempted to prove the final pre-route-wiring state, but workflow `34805628566` (`Validate LAN fail-closed readiness gate`) **FAILED** at `Prove readiness advances only to reviewed route-wiring gate`.

Therefore:

- route-wiring readiness is **not PASS**;
- public LAN business mutation remains fail-closed;
- do not weaken readiness conditions merely to make CI green;
- diagnose the integration invariant and add evidence before any route is enabled.

The separate baseline workflow `34805628608` at the same HEAD failed only at checkpoint freshness because `CHECKPOINT.md` still reconciled through an older commit; this governance failure is being reconciled now.

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

Primary dependency chain:

1. diagnose and repair the integrated LAN readiness test/invariant without weakening fail-closed policy;
2. prove readiness reaches exactly `LAN_USER_SESSION_ROUTE_WIRING_REQUIRED` with dedicated CI;
3. implement reviewed authenticated login/session-to-business route wiring and authorization/domain gate;
4. only then consider opening the approved public LAN business subset.

Parallel reconciliation chain:

- wire the already-tested Cloud ingestion core behind the reviewed authenticated network/API boundary;
- connect LAN sender -> Cloud ingestion;
- prove stable retry/restart/idempotency/receipt/conflict E2E semantics.

Other independent lanes continue: Web authenticated/business surfaces, Android non-visual current-contract work, broader Cloud/Gateway/Google receipt paths, and business adapter coverage.

## Physical / STABLE boundary

Final company ordinary-user Windows + company network + real NLS-MT90 regression remains physical-only. Canonical offline continuity acceptance still requires **Window 2 >=60 minutes** after Internet cut while valid LAN remains.

STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.
