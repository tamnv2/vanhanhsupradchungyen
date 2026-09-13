# DELIVERY PLAN V2 — WEB + APK + CLOUD SERVICE + LAN SERVICE

Status: ACTIVE EXECUTION PLAN 2026-09-13
Authority: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`, `DECISIONS.md`, current Owner instruction

## Planning rule

Do not build four disconnected products in sequence. Build one business platform in vertical slices while the following lanes advance in parallel:

- shared domain/API contracts;
- Cloud Service/D1;
- LAN Service/edge DB/reconciliation;
- Web client;
- Android APK client;
- Google Sheets/Drive downstream integration;
- CI/build/update/diagnostics/acceptance.

Every new business feature is complete only when its required Cloud, LAN, Web and APK behavior is either implemented or explicitly declared out of scope by an Owner-approved product rule.

## Phase 0 — architecture correction and authority cleanup

Current phase.

1. Lock the final deliverables: Website + APK + Cloud Service + LAN Service.
2. Reclassify the earlier transport-only DEV APK/Agent as disposable prototype/reference, not product authority.
3. Keep the legacy repository read-only NON_AUTHORITY; extract only reviewed patterns/evidence.
4. Update decisions/current-state/checkpoint/acceptance so future work cannot regress to "LAN transport only" or "APK pilot only".
5. Freeze new product code until each lane points to the same domain/API/failover contract; this is a planning correction, not a pause of Android/LAN work.

Exit gate: repository authority consistently describes the V2 product architecture.

## Phase 1 — shared domain and protocol foundation

This is the highest-priority implementation dependency for both Cloud and LAN.

### 1A. Canonical command/query contract

For every request define:
- route/command/query code;
- authentication context;
- permission and cluster/module scope;
- input schema;
- stable idempotency rules;
- device identity/sequence where applicable;
- entity/version preconditions;
- output/error machine codes;
- immutable event produced by a successful mutation.

### 1B. Provider-neutral domain core

Extract business validation/state-transition/event creation away from Cloudflare-specific and LAN-specific persistence code.

Target boundary:

```text
Client command
   -> Auth/permission context
   -> Domain core
      -> validated transition + event intent
         -> Cloud D1 adapter
         OR
         -> LAN edge adapter
```

Cloud and LAN must run the same acceptance vectors for business rules. Provider-specific adapters may differ, business meaning may not.

### 1C. Transport/status contract

Define exact client-visible routing/commit states:
- `CLOUD_DIRECT`
- `LAN_RELAY`
- `LAN_AUTONOMOUS`
- `LOCAL_QUEUE_ONLY`
- `CLOUD_COMMITTED`
- `LAN_ACCEPTED_PENDING_SYNC`
- `LAN_RELAYED_CLOUD_COMMITTED`
- `SYNC_CONFLICT`

### 1D. Offline/reconciliation envelope

Define the exact immutable envelope used from client -> LAN -> Cloud reconciliation, including:
- request ID;
- idempotency key;
- device ID/sequence;
- edge instance/epoch;
- command type;
- entity/base version;
- payload hash;
- local acceptance time;
- original user/session/authorization evidence according to the later offline-auth design.

Exit gate: contract tests exist before deep feature implementation.

## Phase 2 — service runtime foundations in parallel

### Lane C — Cloud Service

Continue current work:
1. multi-module Worker packaging;
2. auth/session/permission runtime integration;
3. canonical D1 mutation helper;
4. immutable `domain_events` + projection outbox;
5. health/meta/capabilities reflecting runtime mode/schema;
6. stable machine errors/idempotency;
7. Google projection authentication and sender/retry/ACK;
8. Drive upload/metadata flow.

### Lane L — LAN Service

Build a real local substitute runtime, not just discovery/echo:
1. no-admin portable host process compatible with company-laptop constraints;
2. local service API implementing the same reviewed command/query contract;
3. local edge database schema for offline-capable modules;
4. immutable local event journal;
5. local sync/reconciliation outbox;
6. Cloud snapshot/delta import while online;
7. `LAN_RELAY` path to Cloud when Cloud is reachable;
8. `LAN_AUTONOMOUS` transaction path when Cloud is unavailable;
9. reconciliation engine when Cloud returns;
10. conflict evidence/resolver queue;
11. local Web bundle hosting;
12. diagnostics, health, update and no-admin recovery.

### Lane S — security/offline authority

Design in parallel, fail-closed until locked:
1. device pairing/channel authentication;
2. offline user authorization/capability mechanism;
3. bounded stale-auth/revocation policy;
4. autonomous-mode entry/exit authorization;
5. privileged/security operation restrictions while offline;
6. key/token rotation and security epoch handling.

Do not invent exact TTLs or privileged-offline permissions before policy is reviewed.

Exit gate: Cloud and LAN can execute the same harmless test command through the same domain contract, with separate persistence adapters and identical result semantics.

## Phase 3 — client foundations in parallel

### Lane W — Website

1. same domain client contract;
2. login/session shell;
3. permission-aware navigation;
4. connection/runtime indicator;
5. endpoint selector/state machine for Cloud vs LAN;
6. support local Web hosting from LAN Service during Internet outage;
7. durable retry UI where the command contract allows it;
8. clear distinction between cloud-committed and LAN-pending-sync states.

### Lane A — Android APK

1. real VHDCHY PDA application, not diagnostics pilot;
2. same auth/domain API contract as Web;
3. PDA-optimized navigation/screens;
4. same Cloud/LAN endpoint-state machine;
5. durable permitted client queue;
6. scanner/device integration behind domain commands;
7. background execution only where required and bounded;
8. update/signing channel after source behavior stabilizes.

Web is the wider/full management surface. APK is a compact operational surface, but neither client may redefine business rules locally.

Exit gate: both clients authenticate and execute at least one shared business vertical slice through Cloud; both can use LAN endpoint routing with equivalent semantics.

## Phase 4 — business vertical slices

Implement modules vertically so Cloud/LAN/Web/APK do not drift.

### Slice 1 — identity / employee / attendance / presence

- employee lookup/MNV lifecycle;
- login/session/user context;
- IN/OUT/repeated IN rules;
- current presence;
- Cloud execution;
- LAN autonomous edge state/event/reconcile;
- Web UI;
- APK compact UI/QR flow;
- Google projection after D1 commit/reconcile.

### Slice 2 — work session / PICK / PACK / resources

- work-session concurrency;
- PICK requires PDA; optional User Pick;
- PACK table -> valid User Pack selection;
- assignment changes/history;
- release/reissue;
- cross-cluster borrow;
- LAN snapshot needs for current resource availability;
- offline conflict detection for competing resource claims;
- Web + APK operational flows.

### Slice 3 — labor / dropped goods

- labor catalog/open/finish/correction;
- cross-cluster labor;
- dropped-goods manual/QR;
- LAN autonomous events and reconciliation;
- Web/APK views.

### Slice 4 — documents/media

- DRAFT metadata;
- local capture/staging when offline;
- hash/readback;
- Cloud/Drive upload;
- FINAL only according to current durable-Drive rule;
- replacement history/portrait semantics;
- Web/APK capture/view flows.

For every slice, test the same command vectors against Cloud and LAN adapters before declaring the slice complete.

## Phase 5 — failover/recovery system

### 5A. Automatic path handling

Test:
- Cloud healthy -> direct Cloud;
- individual direct-cloud failure -> forced LAN / LAN relay;
- Cloud Service failure -> LAN autonomous if policy permits;
- total Internet loss -> LAN autonomous;
- LAN unavailable but Cloud healthy -> Cloud direct;
- both unavailable -> permitted client local queue only.

### 5B. Recovery

When Cloud returns:
1. stop creating unnecessary autonomous local commits after reviewed recovery threshold/state transition;
2. upload LAN outbox in original immutable order where domain ordering matters;
3. reconcile idempotently;
4. apply non-conflicting events once;
5. expose conflicts rather than overwrite;
6. refresh edge snapshot after reconciliation;
7. then resume normal Cloud/relay operation.

### 5C. Split-brain tests

Intentionally test conflicting Cloud/LAN activity because hard partitions can create it. Required result is evidence-preserving conflict detection/resolution, never silent last-write-wins.

## Phase 6 — Google / backup / DR integration

1. Sheets projection remains Cloud/D1-derived only.
2. LAN autonomous events project only after D1 reconciliation.
3. Drive media staged offline syncs after connectivity and durable upload acceptance.
4. outbox DEAD/retry/reconciliation tooling;
5. D1 snapshot/restore;
6. LAN edge backup/recovery for pending events and staged media;
7. closed-quarter correction rules;
8. quota/retention measurement.

## Phase 7 — packaging / update / release

### Cloud
- guarded BETA deploy;
- provider verification;
- STABLE gated after BETA PASS + Owner approval.

### LAN Service
- portable no-admin package;
- automatic update discovery + manual fallback;
- staged update + health check + rollback;
- local DB/events/staged media preserved across update.

### APK
- reviewed BETA signer;
- version/update contract;
- signer/hash verification;
- manual recovery install path.

### Web
- Cloud bundle and matching LAN-hosted bundle must identify the same compatible contract/build family.

## Phase 8 — acceptance matrix

A product BETA PASS requires evidence across all required paths, not merely compilation:

1. Cloud Service business E2E;
2. Website Cloud E2E;
3. APK Cloud E2E;
4. LAN relay E2E;
5. LAN autonomous E2E;
6. Internet-loss operation;
7. Cloud-Service-outage operation;
8. per-user forced-LAN operation;
9. LAN -> Cloud recovery/reconciliation;
10. duplicate/idempotency/device-sequence handling;
11. conflict detection/resolver evidence;
12. local Web availability during Internet outage;
13. Google deferred projection after LAN recovery;
14. offline-staged media -> Drive durability;
15. corporate-network/no-admin physical regression;
16. backup/restore/update regressions.

## Current exact position after plan reset

The project is not at "install the transport test APK" as a product milestone.

Current active position is:

```text
Provider/D1/Google foundation: largely prepared
Cloud auth/projection source: partially prepared
                    |
                    +--> NOW: lock shared domain + dual-runtime contract
                             |
                  +----------+-----------+
                  |                      |
          Cloud runtime build      LAN runtime build
                  |                      |
                  +----------+-----------+
                             |
                     vertical business slices
                  /          |            \
               Web          APK       Google/Drive
                             |
                    failover/reconciliation
                             |
                        BETA acceptance
```

The earlier green transport-only APK/Agent build is retained only as CI/no-admin feasibility evidence. It is not counted as progress toward the business APK/LAN Service feature set except where a low-level pattern is deliberately re-adopted.
