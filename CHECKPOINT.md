# CHECKPOINT — VHDCHY

checkpoint_version: 31
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 471d529b5b374d165507a7e1940e8058df08f9a4
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
- Parallel incomplete lanes: Phase 4 core business Service/API; Phase 5 Gateway/integrations; Phase 7 Online+LAN Web V7 UI; Phase 8 Android/PDA App.
- Current phase completion baseline:
  - Phase 1 Scope/rules/architecture: 95%
  - Phase 2 Repo/environments/providers/CI: 85%
  - Phase 3 Cloud data/auth/Service foundation: 80%
  - Phase 4 Core business Service/API: 65%
  - Phase 5 Gateway/adapters/integrations: 55%
  - Phase 6 LAN continuity/offline/reconcile: **55%**
  - Phase 7 Online Web + LAN Web UI: **25%**
  - Phase 8 Android/PDA App: 15%
  - Phase 9 Account/admin/reporting/support: 35%
  - Phase 10 Security/observability/recovery: 50%
  - Phase 11 BETA physical/capacity/UAT: 10%
  - Phase 12 STABLE production/handover: 0%
- Progress is evidence-weighted, never elapsed-time/commit/tool-call based. Scope expansion may legitimately reduce the percentage.

## Owner V7 UI / language / execution authority — ACTIVE

- Current user-facing Web and Android/PDA implementation is **Vietnamese only**.
- Multilingual switching, translation catalogs, locale persistence and Vietnamese/English/Chinese acceptance are deferred until a later explicit Owner decision.
- Android/PDA App UI/UX direction: actual Owner Pick Pack 1291 App reference, adapted to current VHDCHY workflows/terminology/permissions/data/runtime.
- `BACKUP PICK PACK 1291` remains NON_AUTHORITY except for this explicit UI/UX reference use. Do not inherit old business logic/data/credentials/runtime architecture.
- Exact Pick Pack visual details must come from accessible actual source/artifacts; do not invent unavailable screen details.
- Online Web + LAN Web visual direction: Owner-supplied DNSHE screenshots from 2026-09-14 — dark navy navigation, light blue/white field, white rounded cards, royal-blue primary actions, compact status/icon tiles, clean enterprise-console hierarchy.
- Do not copy DNSHE branding/proprietary assets.
- Online/LAN Web remain one product/shared design system. LAN-critical UI assets must be available locally without Internet.
- Ready-queue parallel execution is mandatory: execute all independent safe ready nodes in parallel where tools permit; serialize only real dependencies/same-resource writes; a blocked node must not stall unrelated work.
- Before ending a long tool/session block, report only evidence-backed PASS / FAILED / IN_PROGRESS / BLOCKED state, direct evidence IDs, exact/rounded progress and justified delta. Tool activity by itself is not progress.

## Current proven source foundations

### Cloud/data/auth

- D1 remains central consolidated canonical business store after synchronization under V3.
- Sheets/Drive remain projection/file-storage outputs, never canonical business authority.
- Current Google Drive/Sheets/GAS authority is `tam95.supra@gmail.com`.
- BETA provider/runtime facts in `SERVICE_AUTHORITY.md` must be live-verified before any provider mutation/deploy.
- Public business/admin/provider paths remain fail-closed where current readiness/handler/provider acceptance is incomplete.

### LAN operational state / Slice-1

Current source lineage has automated evidence for:

- LAN operational snapshot/state materialization with rollback and pending-local overwrite guard;
- idempotent local replay preflight;
- immutable authenticated actor evidence capture;
- employee/MNV/attendance Slice-1 business behavior;
- repeated-IN semantics;
- atomic active-MNV and one-active-code-per-employee uniqueness claims with race rollback;
- stable resource conflict mapping;
- durable staged-media local store/schema/lifecycle;
- serialized staged-media identity claims.

Dedicated staged-media workflow run `34797198225` at `cddf8dc8358dce25febf0efd6c04c4cd73f602fb`: **SUCCESS**.

### LAN durable Cloud reconciliation queue — PASS foundation

Current source includes `CloudSyncQueueStore` and dedicated acceptance vectors for:

- transactional due-item claim;
- retry/backoff;
- immutable edge-event reconciliation envelope construction;
- completed Google/Drive receipt attachment;
- explicit durable conflict evidence;
- reconciliation completion checkpoint metadata;
- restart recovery of interrupted `SYNCHRONIZING` claims;
- race-safe single ownership of a due item.

LAN runtime startup now executes interrupted-claim recovery and advertises implemented durable queue mechanics separately from the still-planned Cloud reconciliation network transport.

Dedicated workflow run `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.
Earlier dedicated vector run `34801198095`: **SUCCESS**.

This is local source/CI evidence only. Cloud network ingestion/transport, complete sync cursor/delta/rebase behavior, full conflict resolution and physical continuity remain incomplete.

### Online/LAN Web V7 shell — PASS foundation

Current shared Web source provides:

- Vietnamese-only current UI;
- shared Online/LAN V7 shell;
- DNSHE-inspired VHDCHY design tokens without DNSHE branding/assets;
- responsive navigation/dashboard shell;
- Service, Cloud sync, Google output and conflict status surfaces;
- local/offline-safe critical UI assets;
- runtime reads for `/api/v1/meta`, `/api/v1/capabilities`, `/api/v1/sync/status` and fail-closed `/api/v1/auth/me` handling;
- no direct Google business bypass.

Product-foundation workflow run `34801611019` at `41565f3b2ffdca473f756e33bd71769e16d8af13`: **SUCCESS** for Web, Cloud Service, Android APK and LAN Service jobs, including the Web V7 contract test.

This does not claim completed login/auth flow, full business screens, ADMIN+ conflict-resolution UI or end-to-end Web product acceptance.

### Android/PDA foundation

- Current APK source builds successfully in product-foundation run `34801611019`.
- Current visible foundation text is Vietnamese-only.
- The authorized Pick Pack 1291 reference digest confirms the historical full backup and exact Beta128 APK, but the accessible current GitHub reference did not surface the actual final Android UI source/layout tree.
- Exact Pick Pack visuals remain **not surfaced**; do not invent them.
- Android product progress remains 15%; translation cleanup/build alone is not enough to advance the phase.

## Portrait semantic gate — still unresolved

One Owner-level product conflict remains open:

- current rule requires previous portrait deletion immediately;
- LAN/offline media semantics allow local staging when Drive is unavailable.

Decision-independent durable media work remains valid, but actual portrait replacement behavior while Drive is unavailable stays fail-closed until explicit Owner authority resolves the conflict.

## Current LAN/public acceptance boundary

- Public LAN business mutation readiness remains fail-closed until current auth/pairing/security-epoch/permission/domain readiness links the complete reviewed path.
- Cloud reconciliation local queue is implemented/tested; the reviewed Cloud network ingestion/transport path is the primary next dependency.
- Physical company ordinary-user Windows + real NLS-MT90 regression is pending.
- Canonical continuity acceptance: warm up connected, cut Internet while valid LAN remains, then sustain approved App + LAN Web + local business workflow for **Window 2 >=60 minutes**.
- External Internet embeds may fail during the timed window.
- Restoration sync/reconciliation is tested after the timed window.
- Host restart/power-loss is a separate recovery gate.
- Capacity gate includes synthetic 10/25/50/100 plus soak.

## Immediate ready queue

### Primary chain — Cloud/LAN reconciliation

1. Lock the Cloud reconciliation transport/API boundary against `docs/SERVICE_API_CONTRACT_V3.md` and existing D1 reconciliation schema.
2. Implement Cloud ingestion without opening unrelated public mutations.
3. Prove event/idempotency/device/source collision behavior.
4. Attach already-completed LAN integration receipts without duplicate downstream Google output.
5. Return stable reconcile/conflict/retry semantics.
6. Add CI vectors.
7. Then connect the LAN network sender and prove E2E retry/restart/reconciliation.

### Independent parallel lanes

- LAN auth/pairing/security-epoch/permission/readiness and broader current business adapters.
- Web authenticated/business surfaces where current Service routes/contracts are stable; never fake login success.
- Android non-visual endpoint/scanner/domain-command/durable-queue work while exact Pick Pack visual source remains blocked.
- Cloud/Gateway Google receipt/retry/readback and broader provider-neutral business work.

## Physical / STABLE

Final company-network/no-admin/NLS-MT90 evidence remains physical-only and cannot be replaced by CI.

STABLE infrastructure may be prepared safely in isolation, but production business activation/promotion remains blocked until full BETA acceptance plus explicit Owner approval.

do_not_repeat:
Do not treat memory as authority. Do not replay provider migrations from remembered state. Do not claim provider delivery/projection live without current evidence. Do not bypass action-safety. Do not open LAN mutation routes before readiness/authz/domain acceptance. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not invent Pick Pack UI details without actual reference evidence. Do not copy DNSHE branding/assets. Do not reintroduce stale duration-only offline TTL. Do not silently resolve the portrait immediate-delete/offline-staging conflict. Do not build/expose multilingual UI in the current stage. Do not inflate progress from tool/commit activity without acceptance evidence. Do not promote STABLE without explicit Owner approval.
