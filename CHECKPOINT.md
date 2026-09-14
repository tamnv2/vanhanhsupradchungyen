# CHECKPOINT — VHDCHY

checkpoint_version: 33
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 732b14e064143413dd99e81aed390a35825a5a46
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
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
context_index_ref: CONTEXT_INDEX.md

## Current project progress

- Evidence-weighted total: **54.6% exact / 55% displayed**.
- Primary active phase: **Phase 6 — LAN continuity, local state, offline operation and reconciliation**.
- Parallel lanes: Phase 4 core business Service/API; Phase 5 Gateway/integrations; Phase 7 Online+LAN Web V7 UI; Phase 8 Android/PDA App.
- No progress increase is claimed from the newly completed LAN readiness/route-wiring source work because public LAN HTTP mutation remains closed and no additional product/physical acceptance gate has closed.

## Authority / UI / execution state

- Current Web and Android/PDA user-facing implementation is **Vietnamese only**; multilingual UI is deferred.
- Android/PDA visual direction uses actual Pick Pack 1291 UI/UX evidence only; legacy business/data/credentials/runtime remain NON_AUTHORITY.
- Online Web + LAN Web share the V7 VHDCHY design direction inspired by Owner-supplied DNSHE screenshots without copying DNSHE branding/assets.
- Ready-queue parallel execution remains mandatory. A blocked node must not stall independent safe work.
- PASS requires reproducible evidence. CI/source PASS never substitutes for physical company-network/PDA PASS.

## Reconciled evidence through `732b14e064143413dd99e81aed390a35825a5a46`

### Cloud reconciliation ingestion — PASS foundation

Cloud LAN-reconciliation ingestion core is source/CI PASS, including preservation of actor/source/event identity, collision handling and completed integration-receipt ingestion/deduplication behavior.

Key evidence:

- `19c3463a633389b0cd85ddcede6a4525ccaa522a` — Cloud reconciliation ingestion core;
- `591c4083973141155530357f5bddd4ee58efbdc7` — package reconciliation module;
- workflow `34803221835` — `Validate clean baseline` **SUCCESS** at `591c408...`;
- workflow `34803221873` — `Build product foundations` **SUCCESS** at `591c408...`.

Remaining: reviewed machine/service authenticated network/API route, LAN sender -> Cloud E2E, retry/restart E2E, complete cursor/rebase/conflict UX and provider/physical acceptance.

### LAN durable local foundation — PASS

Operational snapshots/materialized Slice-1, immutable local event/outbox, idempotent replay, actor evidence, employee/MNV/attendance, atomic active-code uniqueness, staged media and durable Cloud-sync queue remain PASS foundations.

Dedicated durable queue workflow `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.

### LAN client security / user session / primary credential — PASS

Source includes P-256 paired-client signed request verification, replay defense/security-epoch fencing, durable LAN user-session binding, authority snapshot V2 primary password verifier, V1/V2 authorization compatibility and primary credential -> authenticated evidence -> session chain.

Dedicated workflow `34805395079` at `583b3d0329166804b207332dadf6d449b07c0abf`: **SUCCESS**.

### Integrated LAN readiness — PASS

The earlier readiness failures were test-coupling defects, not production security defects:

- one harness reused a DB containing deliberately unreconciled local work, correctly triggering `OPERATIONAL_PENDING_LOCAL_WORK`;
- after DB isolation, the readiness fixture still depended on authority/operational rows seeded by another harness.

The readiness harness was isolated and made self-seeding. Production fail-closed safeguards were preserved.

Evidence:

- workflow `34808274815` — integrated LAN readiness **SUCCESS**;
- workflow `34808274819` — baseline at the same readiness state **SUCCESS**.

The chain now proves synchronized authority + operational state -> signed client security -> primary credential authority -> reviewed route-wiring boundary and restart fail-closed behavior.

### Signed LAN session -> Slice-1 business coordinator — PASS

`LanBusinessRouteCoordinator` is implemented and CI-proven for an already-issued LAN user session.

Mandatory gates:

- exact reviewed POST target `/api/v1/data/commands`;
- P-256 signed paired-device request;
- exact raw-body SHA-256 binding;
- nonce/replay and security-epoch checks;
- session bound to device + security epoch;
- current authority snapshot/session freshness;
- `MUST_CHANGE_PASSWORD` blocks ordinary mutation;
- authorization/domain/command gate precedes business execution;
- authenticated actor comes only from server-side session evidence;
- coordinator parses the exact signed raw body itself;
- portrait replacement remains fail-closed.

Evidence:

- workflow `34808936937` at `a469d29325ee83f8e19070234c1186ca23474a1c`: **SUCCESS**;
- baseline workflow `34808937077` at the same commit: **SUCCESS**.

Covered vectors include positive business execution plus replay, signed-body mismatch, wrong route target, device/session mismatch, permission DENY, unsupported command, must-change-password, stale authority and portrait-closed behavior.

### Public LAN HTTP mutation — CLOSED / proven fail-closed

The route-wiring workflow explicitly confirms the current public HTTP mutation endpoint still returns closed/503 behavior. Coordinator PASS does not authorize public mutation exposure.

Primary blocker now: **secure LAN login transport + HTTP adapter**.

Important security rule:

- P-256 request signing provides authentication/integrity but **not confidentiality**;
- do not transmit reusable user passwords over plaintext LAN HTTP;
- do not silently require changes to company certificate stores, firewall, router/AP, internal DNS or corporate security policy because the LAN host must remain ordinary-user/no-admin.

Next implementation must prove a secure credential transport compatible with those constraints before login/public business HTTP routes are enabled.

### Cloud reconciliation network boundary — pending

Cloud reconciliation ingestion is service-to-service. Actor fields carried in reconciliation payloads are evidence, not authentication. A reviewed machine/service authentication mechanism is required before LAN -> Cloud ingestion is exposed over a network route.

### Web / Android foundations

- Shared V7 Online/LAN Web shell remains PASS in product-foundation workflow `34801611019` at `41565f3b2ffdca473f756e33bd71769e16d8af13`.
- Android foundation continues to build; exact final Pick Pack screen/layout evidence remains not sufficiently surfaced, so exact visual details must not be invented.

## Current dependency graph / ready queue

### Primary — secure LAN login/public HTTP path

1. design/prove secure LAN credential transport without plaintext reusable passwords;
2. preserve no-admin/company-policy constraints;
3. implement reviewed login/session HTTP adapter;
4. wire public LAN business HTTP route to the already-proven `LanBusinessRouteCoordinator`;
5. prove HTTP-level positive/negative/restart/re-auth E2E vectors;
6. keep portrait command closed;
7. only after PASS, consider opening the reviewed public LAN business subset.

### Parallel — reconciliation network E2E

1. define/prove machine/service authentication for Cloud reconciliation;
2. wire tested Cloud ingestion core behind that boundary;
3. connect LAN sender;
4. prove retry/restart/idempotency/result mapping E2E;
5. prove Google/Drive receipts do not duplicate output;
6. add cursor/delta/rebase/conflict-resolution behavior after transport stabilizes.

### Other independent lanes

- Web authenticated/business surfaces against real Service routes;
- Android non-visual endpoint/scanner/session/durable-queue work while visual reference remains blocked;
- broader Cloud/Gateway/Google receipt/retry/readback work;
- broader business adapter coverage.

## Portrait semantic gate

The portrait rule conflict remains unresolved: immediate deletion of the previous remote portrait versus offline staging while Drive is unavailable. Decision-independent media infrastructure may continue. Actual offline portrait-replacement semantics remain fail-closed until explicit Owner authority.

## Physical / STABLE boundary

- Physical company ordinary-user Windows + target network + real NLS-MT90 regression remains pending.
- Canonical offline continuity acceptance remains **Window 2 >=60 minutes** after Internet cut while valid LAN remains.
- STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

do_not_repeat:
Do not treat memory as authority. Do not replay provider mutations from remembered state. Do not claim provider delivery/projection live without current evidence. Do not bypass action-safety. Do not open LAN mutation routes before secure login/HTTP/readiness/authz/domain acceptance. Do not send reusable passwords over plaintext LAN HTTP. Do not weaken security/readiness assertions merely to make CI pass. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not invent Pick Pack UI details without actual reference evidence. Do not copy DNSHE branding/assets. Do not reintroduce stale duration-only offline TTL. Do not silently resolve the portrait immediate-delete/offline-staging conflict. Do not build/expose multilingual UI in the current stage. Do not inflate progress from tool/commit activity without acceptance evidence. Do not promote STABLE without explicit Owner approval.
