# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 53% | Exact weighted baseline: 53.3% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App reference/shell preparation.

Progress is evidence-weighted, not time/commit-count based. It may decrease if Owner scope expands. See `docs/PROGRESS_TRACKING_V1.md`.

## Product target

VHDCHY is one platform with four first-class product surfaces/runtime deliverables:

- Online Web + LAN Web as one shared Web product/design system;
- Android/PDA App;
- Cloud Service/Gateway/integrations;
- full LAN Service substitute with local continuity.

Web and App use the same current business/domain semantics. Cloud and LAN may use different persistence/runtime adapters but may not give the same command different business meaning.

Legacy projects, including Pick Pack 1291 and the old LAN pilot, remain NON_AUTHORITY except for explicitly authorized reference use. V7 explicitly authorizes Pick Pack 1291 as App UI/UX reference only.

## Active Owner/UI direction — V7

- Android/PDA UI follows the recognizable UI/UX direction of the Owner's Pick Pack 1291 App, adapted to current VHDCHY terminology, workflows, permissions, data and runtime architecture.
- Do not invent Pick Pack screen details that have not been surfaced from accessible reference evidence.
- Online Web and LAN Web follow the visual direction of the DNSHE screenshots supplied by the Owner on 2026-09-14: dark navy navigation, light blue/white field, white rounded cards, royal-blue primary CTA, compact icon/status tiles and clean enterprise-console hierarchy.
- Do not copy DNSHE branding or proprietary assets.
- Online/LAN Web must remain one product with the same navigation/visual language; core LAN UI assets must be locally available without Internet.
- V5 language rule remains: Vietnamese / English / Chinese, default English.

## Current source/runtime evidence

### LAN Slice-1 / business core — materially implemented

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

This is current-source automated evidence. It is **not** physical company-network/PDA acceptance.

### Portrait semantic gate — still open

A specific Owner-rule conflict remains unresolved for portrait replacement:

- one rule requires deletion of the previous portrait immediately;
- current LAN/offline media semantics permit staging when Drive is unavailable.

Durable staged-media primitives can progress independently, but the product must not silently redefine whether an existing remote portrait may be replaced while Drive is unavailable. Portrait mutation remains fail-closed until the authority conflict is explicitly resolved and implemented/tested.

### LAN public readiness — still fail-closed

- Passing source/business harnesses does not authorize opening public LAN business mutation routes.
- LAN auth/pairing/security-epoch and broader business/reconciliation coverage remain incomplete.
- Current physical regression is still pending.

## Cloud / canonical data / Google

- D1 remains central consolidated canonical business storage after synchronization under current architecture.
- LAN edge current-state + immutable event journal is local operational authority for operations accepted locally.
- Sheets/Drive remain projection/storage outputs, never canonical business authority.
- Google operational authority remains `tam95.supra@gmail.com` for current project Drive/Sheets/GAS scope.
- The locked/unavailable separate domain Google account does not block source/LAN/Web/App implementation and can be revisited later if recovered.
- Google projection/upload paths still require complete current receipt/deduplication/retry/reconciliation acceptance.

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

The V6 target is no longer a sub-minute failover experiment. Final acceptance requires a minute-level continuity drill with **Window 2 >= 60 minutes** after warmup and Internet cut while valid LAN connectivity remains.

During that timed window:

- App/LAN Web/local workflow must remain usable for the approved local scope;
- external Internet embeds may fail without failing LAN continuity;
- restoration sync/reconciliation is tested after the timed window;
- host restart/power loss is a separate recovery test.

## Website current state

Some operator/service UI foundations and account/provider action flows exist in source lineage, including signed-in staged actions, tab navigation and language-label hardening. They do not count as V7 visual completion automatically.

Remaining major work:

- shared DNSHE-inspired VHDCHY design tokens/shell;
- login and authenticated dashboard shell;
- business modules on the shared shell;
- Online/LAN state UX parity;
- local-only critical assets for LAN outage;
- full Vietnamese/English/Chinese acceptance.

## Android/PDA App current state

Legacy source provides useful proven mechanics for endpoint discovery, hysteresis, durable queue/device sequence, ACK-driven deletion, reconnect/resync and bounded background work. These are reference patterns only and must be adapted to current contracts.

V7 now fixes App UI direction to Pick Pack 1291 adapted for VHDCHY. The backup container is accessible in Drive, but the exact App UI source/evidence has not yet been surfaced sufficiently to claim a faithful visual implementation.

Final current App business UI, current Service/LAN integration, real NLS-MT90 acceptance, background/battery and final signed release remain incomplete.

## Current exact project position

```text
Scope / architecture / authority ..................... mostly complete
Repo / provider / CI foundation ...................... substantially complete
Cloud data/auth/Service foundation ................... substantially complete
Core business Service/API ............................ mid implementation
Gateway/integrations ................................. mid implementation
LAN continuity/offline/reconcile ..................... PRIMARY ACTIVE PHASE
Online Web + LAN Web V7 UI ........................... early implementation
Android/PDA final App ................................ early implementation
Physical BETA/UAT/capacity ........................... mostly pending
STABLE production promotion/handover ................. pending
```

Current numerical baseline: **53.3% (display 53%)**.

## Immediate direction

Primary work remains to extend the current LAN/business slice through broader auth/reconciliation/provider-safe behavior without opening fail-closed public mutations prematurely. In parallel, begin the shared V7 Web design shell and surface the Pick Pack 1291 App UI reference so the current App shell can be built against stable Service/LAN contracts.

Final company-network/no-admin/PDA evidence still requires the intended physical environment. STABLE business activation/promotion remains blocked until full BETA acceptance and explicit Owner approval.