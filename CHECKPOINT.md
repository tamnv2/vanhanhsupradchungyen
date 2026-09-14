# CHECKPOINT — VHDCHY

checkpoint_version: 32
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 58587a15521f4025dd268b3536eebfdfbe370747
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
- No progress increase is claimed from the post-checkpoint commits because the integrated LAN readiness gate is currently FAILED and no new physical/E2E acceptance gate has closed.

## Authority / UI / execution state

- Current Web and Android/PDA user-facing implementation is **Vietnamese only**; multilingual UI is deferred.
- Android/PDA visual direction uses actual Pick Pack 1291 UI/UX evidence only; legacy business/data/credentials/runtime remain NON_AUTHORITY.
- Online Web + LAN Web share the V7 VHDCHY design direction inspired by Owner-supplied DNSHE screenshots without copying DNSHE branding/assets.
- Ready-queue parallel execution remains mandatory. A blocked node must not stall independent safe work.
- PASS requires reproducible evidence. CI/source PASS never substitutes for physical company-network/PDA PASS.

## Reconciled evidence through `58587a15521f4025dd268b3536eebfdfbe370747`

### Cloud reconciliation ingestion — PASS foundation

Post-checkpoint source added and packaged the reviewed Cloud LAN-reconciliation ingestion core. Evidence includes preservation of LAN actor/source/event identity, collision handling and completed integration-receipt ingestion/deduplication behavior at the source-test level.

Key evidence:

- `19c3463a633389b0cd85ddcede6a4525ccaa522a` — Cloud reconciliation ingestion core;
- `591c4083973141155530357f5bddd4ee58efbdc7` — package reconciliation module;
- workflow `34803221835` — `Validate clean baseline` **SUCCESS** at `591c408...`;
- workflow `34803221873` — `Build product foundations` **SUCCESS** at `591c408...`.

Remaining: reviewed authenticated network/API route, LAN sender -> Cloud E2E, retry/restart E2E, complete cursor/rebase/conflict UX and provider/physical acceptance.

### LAN durable local foundation — PASS

Previously proven foundations remain valid: operational snapshots/materialized Slice-1, immutable local event/outbox, idempotent replay, actor evidence, employee/MNV/attendance, atomic active-code uniqueness, staged media and durable Cloud-sync queue.

Dedicated durable queue workflow `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.

### LAN client security / user session / primary credential — dedicated PASS

Post-checkpoint source now includes:

- P-256 paired-client signed request verification;
- replay defense and security-epoch fencing;
- durable LAN user-session binding;
- authority snapshot V2 synchronized primary password verifier;
- V1/V2 authorization compatibility;
- primary credential -> authenticated evidence -> session chain.

Dedicated workflow `34805395079` at `583b3d0329166804b207332dadf6d449b07c0abf`: **SUCCESS**.

This isolated PASS does not authorize public mutation routes.

### Integrated LAN readiness — FAILED / fail-closed

HEAD `609a77ee3ef48eca9b1df5d91f63d2e7d875508f` attempted to prove the evaluator advances through synchronized snapshots, client security and primary credential authority to the reviewed blocker `LAN_USER_SESSION_ROUTE_WIRING_REQUIRED`.

Workflow `34805628566` (`Validate LAN fail-closed readiness gate`) **FAILED** at `Prove readiness advances only to reviewed route-wiring gate`.

Therefore:

- route-wiring readiness is not PASS;
- public LAN business mutation remains fail-closed;
- do not relax security/readiness assertions to make CI green;
- next primary source node is diagnosis/fix of the integrated readiness invariant.

The same HEAD's baseline run `34805628608` failed only at checkpoint freshness. This checkpoint reconciles that governance gap through the current-state updates immediately preceding it.

### Web / Android foundations

- Shared V7 Online/LAN Web shell remains PASS in product-foundation workflow `34801611019` at `41565f3b2ffdca473f756e33bd71769e16d8af13`.
- Android foundation continues to build; exact final Pick Pack screen/layout evidence remains not sufficiently surfaced, so exact visual details must not be invented.

## Current dependency graph / ready queue

### Primary — integrated LAN readiness

1. add source-level diagnostic evidence around the failing readiness stage;
2. identify the exact invariant mismatch;
3. fix the integration defect without weakening fail-closed behavior;
4. prove exact transition to `LAN_USER_SESSION_ROUTE_WIRING_REQUIRED` by dedicated CI;
5. implement authenticated session -> authorization/domain -> supported Slice-1 route wiring;
6. add negative authz/session/device/epoch/unsupported-command vectors;
7. only after PASS, consider exposing the approved public LAN mutation subset.

### Parallel — reconciliation network E2E

1. wire the already-tested Cloud ingestion core behind the reviewed authenticated network/API boundary;
2. connect LAN sender;
3. prove idempotent retry/restart/result mapping;
4. prove completed Google/Drive receipts do not duplicate output;
5. add cursor/delta/rebase and conflict resolution after transport is stable.

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
Do not treat memory as authority. Do not replay provider mutations from remembered state. Do not claim provider delivery/projection live without current evidence. Do not bypass action-safety. Do not open LAN mutation routes before readiness/authz/domain acceptance. Do not weaken a failing readiness assertion merely to make CI pass. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not invent Pick Pack UI details without actual reference evidence. Do not copy DNSHE branding/assets. Do not reintroduce stale duration-only offline TTL. Do not silently resolve the portrait immediate-delete/offline-staging conflict. Do not build/expose multilingual UI in the current stage. Do not inflate progress from tool/commit activity without acceptance evidence. Do not promote STABLE without explicit Owner approval.
