# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 54.6% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App reference/shell preparation.

Progress is evidence-weighted, not time/commit/tool-count based. It may decrease if Owner scope expands. See `docs/PROGRESS_TRACKING_V1.md`.

## Product target

VHDCHY is one platform with four first-class product surfaces/runtime deliverables:

- Online Web + LAN Web as one shared Web product/design system;
- Android/PDA App;
- Cloud Service/Gateway/integrations;
- full LAN Service substitute with local continuity.

Web and App use the same current business/domain semantics. Cloud and LAN may use different persistence/runtime adapters but may not give the same command different business meaning.

Legacy projects, including Pick Pack 1291 and the old LAN pilot, remain NON_AUTHORITY except for explicitly authorized reference use. V7 explicitly authorizes Pick Pack 1291 as App UI/UX reference only.

## Active Owner/UI/execution direction — V7

- Current user-facing implementation language is **Vietnamese only**. Multilingual UI, language switching, locale persistence and translation-catalog work are deferred until a later explicit Owner decision.
- Android/PDA UI follows the recognizable UI/UX direction of the Owner's Pick Pack 1291 App, adapted to current VHDCHY terminology, workflows, permissions, data and runtime architecture.
- Do not invent Pick Pack screen details that have not been surfaced from accessible reference evidence.
- Online Web and LAN Web follow the visual direction of the DNSHE screenshots supplied by the Owner on 2026-09-14: dark navy navigation, light blue/white field, white rounded cards, royal-blue primary CTA, compact icon/status tiles and clean enterprise-console hierarchy.
- Do not copy DNSHE branding or proprietary assets.
- Online/LAN Web remain one product with the same navigation/visual language; core LAN UI assets must be locally available without Internet.
- Independent ready work is executed in parallel; dependency-bound work is serialized only where required.
- Long execution blocks report only evidence-backed PASS/FAIL/IN_PROGRESS/BLOCKED state and progress deltas; tool activity alone is not progress.

## Current source/runtime evidence

### LAN Slice-1 / local business foundation

Current source lineage includes:

- LAN operational-state materialization and rollback/pending-local safeguards;
- idempotent replay preflight;
- atomic authenticated actor evidence capture;
- employee/MNV/attendance business vectors;
- repeated-IN semantics;
- atomic active-MNV and one-active-code-per-employee uniqueness claims;
- race-safe rollback and stable resource-conflict mapping;
- durable staged-media local store/schema/lifecycle;
- serialized staged-media identity claims.

The dedicated staged-media workflow run `34797198225` at `cddf8dc8358dce25febf0efd6c04c4cd73f602fb` completed `SUCCESS`.

### LAN Cloud reconciliation queue — source/CI PASS

`CloudSyncQueueStore` now provides durable local reconciliation mechanics for:

- due-item claim with transactional state transition;
- retry/backoff;
- immutable edge-event reconciliation envelope construction;
- completed Google/Drive receipt attachment;
- explicit conflict evidence in `edge_conflicts`;
- reconciliation completion checkpoint metadata;
- interrupted `SYNCHRONIZING` recovery after process restart;
- race-safe single ownership of a due sync item.

LAN runtime startup now executes interrupted-claim recovery and advertises the implemented durable queue state machine separately from the still-planned Cloud network transport.

Dedicated workflow `34801533266` at commit `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS** after runtime integration. Earlier dedicated vector run `34801198095`: **SUCCESS**.

This proves durable local queue behavior. It does **not** prove Cloud network ingestion/transport, full conflict resolution, complete sync cursor/delta behavior or physical company-network continuity.

### Portrait semantic gate — still open

A specific Owner-rule conflict remains unresolved for portrait replacement:

- one rule requires deletion of the previous portrait immediately;
- current LAN/offline media semantics permit staging when Drive is unavailable.

Durable staged-media primitives can progress independently, but the product must not silently redefine whether an existing remote portrait may be replaced while Drive is unavailable. Portrait mutation remains fail-closed until the authority conflict is explicitly resolved and implemented/tested.

### LAN public readiness — still fail-closed

- Passing source/business/reconciliation queue harnesses does not authorize opening public LAN business mutation routes.
- LAN auth/pairing/security-epoch and broader business/Cloud-ingestion coverage remain incomplete.
- Current physical regression is still pending.

## Cloud / canonical data / Google

- D1 remains central consolidated canonical business storage after synchronization under current architecture.
- LAN edge current-state + immutable event journal is local operational authority for operations accepted locally.
- Sheets/Drive remain projection/storage outputs, never canonical business authority.
- Google operational authority remains `tam95.supra@gmail.com` for current project Drive/Sheets/GAS scope.
- Google projection/upload paths still require complete current receipt/deduplication/retry/reconciliation acceptance.
- Current Cloud reconciliation D1 schema exists, but the reviewed Cloud network ingestion/transport path is not yet product-PASS.

## Cloud/LAN behavior

### CLOUD_DIRECT
`Web/App -> Cloud Service -> canonical data -> controlled outputs`

### LAN_PRIMARY_ONLINE
`Web/App -> LAN Service -> local state/event journal`, while Cloud synchronization and controlled Google work proceed whenever the corresponding provider is reachable.

### LAN_PRIMARY_CLOUD_UNAVAILABLE
LAN continues normal local business processing; allowed Google work may continue if Internet/Google is reachable; Cloud sync waits for Cloud recovery.

### LAN_OFFLINE
Public Internet is unavailable while LAN remains usable. LAN continues current approved local workflows using the synchronized local authority snapshot; external Google/provider work is queued/staged locally.

### LOCAL_QUEUE_ONLY
Only explicitly retry-safe client work may remain when neither Service is reachable.

## Canonical offline acceptance

Final acceptance requires a minute-level continuity drill with **Window 2 >= 60 minutes** after warmup and Internet cut while valid LAN connectivity remains.

During that timed window:

- App/LAN Web/local workflow must remain usable for the approved local scope;
- external Internet embeds may fail without failing LAN continuity;
- restoration sync/reconciliation is tested after the timed window;
- host restart/power loss is a separate recovery test.

## Website current state — tested V7 foundation

The shared Web source now has:

- Vietnamese-only current UI;
- shared Online/LAN V7 shell;
- dark navy navigation, light field, white rounded cards and blue primary-action design tokens;
- responsive navigation/dashboard layout;
- Service, Cloud-sync, Google-output and conflict status surfaces;
- current runtime reads for `/api/v1/meta`, `/api/v1/capabilities`, `/api/v1/sync/status` and fail-closed `/api/v1/auth/me` handling;
- only local/system critical UI assets for LAN continuity;
- no DNSHE branding/assets and no direct Google business bypass.

Product-foundation workflow `34801611019` at commit `41565f3b2ffdca473f756e33bd71769e16d8af13`: **SUCCESS** for Web, Cloud Service, Android APK and LAN Service jobs. The Web-specific V7 contract test passed inside that run.

Remaining major Web work includes actual authenticated login flow, full business modules, ADMIN+ conflict-resolution surfaces, complete account/admin views and Cloud/LAN E2E acceptance.

## Android/PDA App current state

The current Android source builds as part of product-foundation workflow `34801611019`. Current visible foundation text has been changed to Vietnamese only.

The authorized Pick Pack 1291 reference digest confirms an exact Beta128 APK and a full recovery backup existed, but the current accessible GitHub reference index/digest does not contain the actual final App screen source/layouts. The legacy `beta` branch inspected in this session contains Service/Gateway source but no final Android UI tree. Therefore exact Pick Pack visual details remain **not yet surfaced** and must not be invented.

Legacy mechanics for endpoint discovery, durable queue/device sequence, ACK-driven deletion, reconnect/resync and bounded background work remain reference patterns only and must be adapted to current contracts.

Final current App business UI, actual reference-faithful visuals, current Service/LAN integration, real NLS-MT90 acceptance, background/battery and final signed release remain incomplete.

## Current exact project position

```text
Scope / architecture / authority ..................... mostly complete
Repo / provider / CI foundation ...................... substantially complete
Cloud data/auth/Service foundation ................... substantially complete
Core business Service/API ............................ mid implementation
Gateway/integrations ................................. mid implementation
LAN continuity/offline/reconcile ..................... PRIMARY ACTIVE PHASE; durable queue advanced
Online Web + LAN Web V7 UI ........................... working shared shell
Android/PDA final App ................................ early implementation/reference blocked
Physical BETA/UAT/capacity ........................... mostly pending
STABLE production promotion/handover ................. pending
```

Current numerical baseline: **54.6% (display 55%)**.

## Immediate direction

Primary next dependency is the reviewed Cloud side of LAN reconciliation: ingest the reconciliation envelope safely into the existing Cloud/D1 reconciliation model, preserve idempotency/device/event identity and attach already-completed integration receipts without duplicate downstream output. Public LAN mutations remain fail-closed until full readiness.

In parallel:

- continue Web authenticated/business surfaces against the stable shared contract;
- continue Cloud/Gateway/provider work that is independent of reconciliation transport;
- continue locating the actual Pick Pack 1291 final App UI artifact/source before visual implementation;
- keep Android current source/build Vietnamese-only and contract-compatible.

Final company-network/no-admin/PDA evidence still requires the intended physical environment. STABLE business activation/promotion remains blocked until full BETA acceptance and explicit Owner approval.