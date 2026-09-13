# DELIVERY PLAN V3 — WEB + APK + CLOUD SERVICE + LAN SERVICE

Status: ACTIVE EXECUTION PLAN 2026-09-13
Authority: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`, current Owner instruction, existing business decisions.

## Planning principle

Build one business platform, not four disconnected products.

Every business vertical slice advances through the same shared domain contract and is implemented across:

- Cloud Service/D1;
- LAN Service/edge DB/event journal/sync;
- Website;
- Android APK;
- Google Sheets/Drive output;
- acceptance/failover/recovery tests.

Legacy/prototype source is reference only.

## Phase 0 — architecture correction

1. Lock Website + APK + Cloud Service + LAN Service as first-class deliverables.
2. Lock LAN as a full local Service substitute, not transport-only.
3. Lock APK as the PDA business client of the same domain contract as Website.
4. Lock direct LAN -> Google behavior when Internet/Google is reachable.
5. Lock LAN -> Cloud reconciliation from the LAN event journal, not from Sheets/Drive.
6. Lock unlimited-duration offline login against the latest synchronized local authority snapshot, with explicit stale-revocation risk.
7. Lock SUPERADMIN/ROOT-only manual LAN activation while Cloud is online.
8. Lock ADMIN+ handling only for unresolved business/data conflicts; technical retries remain automatic.

Exit gate: all active authority/state/checkpoint documents point to V3.

## Phase 1 — shared domain, event and sync contracts

### 1A. Shared command/query contract

For every operation define:
- command/query code and version;
- actor/auth context;
- permission + cluster/module scope;
- request schema;
- idempotency identity;
- device ID/sequence where applicable;
- entity/base version;
- deterministic business validation;
- output/error machine codes;
- immutable event intent.

### 1B. Provider-neutral domain core

Target boundary:

```text
Client command
  -> authenticated/authorized context
  -> provider-neutral domain core
       -> validated transition + immutable event intent
          -> Cloud D1 adapter
          OR
          -> LAN edge adapter
```

Cloud and LAN run the same acceptance vectors.

### 1C. Runtime/commit state contract

Define client-visible states for:
- Cloud accepted/committed;
- LAN accepted/pending Cloud sync;
- Google projection/upload completed/pending;
- sync conflict;
- local-client queue only.

### 1D. Reconciliation envelope

Each LAN-accepted event needs stable fields sufficient for deterministic replay/conflict detection:
- event/request/idempotency identity;
- device identity/sequence;
- edge instance/epoch;
- command/event code;
- entity/base version;
- normalized payload hash;
- local acceptance time/order;
- authority snapshot/version;
- Google projection/upload receipts where already completed;
- reconciliation result/conflict evidence.

Exit gate: runtime-neutral contract tests exist before deep feature coding.

## Phase 2 — dual Service foundations in parallel

### Lane C — Cloud Service

1. complete multi-module Worker packaging;
2. integrate auth/session/permission runtime;
3. implement provider-neutral domain core boundary;
4. implement D1 transaction/event/outbox adapter;
5. expose stable command/query APIs;
6. complete Worker -> Google Gateway authentication;
7. complete Sheets projection sender/retry/ACK;
8. implement Drive media/document flow;
9. add reconciliation endpoints for LAN event ingestion and Google receipt attachment.

### Lane L — LAN Service

1. choose/verify no-admin portable runtime packaging compatible with company laptop;
2. implement the same Service API/domain contract locally;
3. implement edge current-state database;
4. implement immutable local event journal;
5. implement durable Cloud sync/reconciliation outbox;
6. implement synchronized local authority/authentication snapshot;
7. implement online master/config/state snapshot + resumable delta refresh;
8. implement direct Google Gateway projection when Internet/Google is available;
9. implement direct Drive upload + local receipt/hash metadata;
10. implement offline Google projection/upload queues;
11. implement Cloud sync daemon that starts whenever Cloud becomes reachable, regardless of client route;
12. implement explicit conflict queue + evidence package;
13. serve the compatible Web bundle locally;
14. retain diagnostics, update/rollback and recovery without admin/router/firewall/DNS dependency.

### Lane S — security and operational authority

1. protect local authentication verifier/security snapshot at rest;
2. authenticated client <-> LAN channel/pairing;
3. security/account snapshot versioning;
4. refresh new authority immediately after reconnect for future operations;
5. audit manual LAN activation;
6. enforce SUPERADMIN/ROOT-only forced LAN control while Cloud is healthy;
7. preserve ROOT-exclusive security/recovery boundaries.

Exit gate: Cloud and LAN execute the same harmless domain command with equivalent business semantics, and LAN can authenticate from a synchronized local snapshot.

## Phase 3 — client foundations in parallel

### Website

1. login/session shell;
2. permission-aware navigation;
3. shared domain client library;
4. Cloud/LAN endpoint selector;
5. locally loadable Web build served by LAN Service;
6. runtime/sync/projection indicators;
7. conflict-management UI for ADMIN+;
8. retry handling without duplicate business mutations.

### Android APK

1. real VHDCHY PDA application;
2. same auth/domain client library as Website where practical;
3. PDA-optimized operational navigation/screens;
4. Cloud/LAN endpoint selector;
5. QR/scanner integration through domain commands;
6. durable client queue only where contract permits;
7. bounded background sync/update work;
8. BETA signing/update after source behavior stabilizes.

Exit gate: Website and APK can authenticate and execute the same initial business command against both Cloud and LAN test runtimes.

## Phase 4 — business vertical slices

Each slice follows:

`shared domain contract -> Cloud adapter -> LAN adapter -> Web -> APK -> Google outputs -> failover/reconciliation acceptance`

### Slice 1 — identity / employee / attendance / presence

- employee/MNV lifecycle;
- account/user context;
- IN/OUT/repeated IN;
- current presence;
- QR employee resolution;
- local auth/permission snapshot behavior;
- LAN event sync to Cloud;
- Sheets projection from Cloud or LAN when permitted;
- reconnect authority refresh.

### Slice 2 — work session / PICK / PACK / resources

- MAIN/EXTRA work-session rules;
- PICK/PDA/User Pick;
- PACK table/User Pack mapping;
- assignment changes/history;
- release/reissue;
- cross-cluster borrow;
- edge snapshot of current resource state;
- competing Cloud/LAN resource conflict detection;
- ADMIN+ conflict resolution.

### Slice 3 — labor / dropped goods

- labor catalog/open/finish/correction;
- cross-cluster labor;
- dropped-goods manual/QR;
- Cloud/LAN parity;
- Google projection parity/deduplication.

### Slice 4 — documents/media

- DRAFT metadata;
- direct LAN Drive upload when Internet/Drive reachable;
- offline local staging when Drive unavailable;
- hashes/readback receipts;
- later D1 attachment without duplicate file creation;
- FINAL gate;
- replacement/portrait history semantics;
- Website/APK capture/view flows.

## Phase 5 — failover, online-LAN and recovery

Required mode drills:

1. Cloud healthy -> CLOUD_DIRECT.
2. SUPERADMIN/ROOT forces LAN while Cloud healthy -> LAN_PRIMARY_ONLINE.
3. Internet available, Cloud unavailable, Google available -> LAN local commit + direct Google + pending Cloud sync.
4. Internet unavailable, LAN available -> full local business operation + local auth snapshot + queued Google work.
5. Cloud returns while clients remain on LAN -> background Cloud sync begins immediately.
6. Cloud authority/permission changed during outage -> refreshed authority applies to new operations; historical LAN events reconcile with evidence.
7. both Cloud and LAN unavailable -> only permitted client-local queue.
8. LAN host restart with pending events/media.
9. uncertain responses/retries -> idempotency prevents duplicates.

## Phase 6 — conflict and reconciliation system

1. automatic duplicate detection;
2. automatic already-projected/already-uploaded receipt reconciliation;
3. automatic non-conflicting event ingestion;
4. base-version/resource/state conflict detection;
5. evidence-preserving `SYNC_CONFLICT` queue;
6. ADMIN+ business conflict resolution actions;
7. ROOT-only handling for ROOT-security/recovery conflicts;
8. post-resolution event trail; never raw history overwrite.

## Phase 7 — Google / backup / DR

1. Sheets projection accepts authorized Cloud and LAN writers through one controlled contract;
2. stable projection keys prevent duplicates;
3. Drive logical-file identity/hash prevents duplicate uploads;
4. LAN edge backup for unsynced events and staged media;
5. D1 snapshot/restore;
6. edge restore/recovery;
7. closed-quarter correction flow;
8. quota/retention measurement.

## Phase 8 — packaging and update

### Cloud
- guarded BETA deploy + provider verification.

### LAN Service
- portable no-admin package;
- self-check/diagnostics;
- safe update + rollback;
- local Web bundle version compatibility.

### APK
- BETA signer;
- immutable build/version/hash;
- controlled update channel.

### Website
- matching Cloud-hosted and LAN-hosted build/version contract.

## Phase 9 — BETA acceptance

BETA PASS requires evidence for:

- Website Cloud path;
- APK Cloud path;
- Website LAN path;
- APK LAN path;
- LAN_PRIMARY_ONLINE;
- LAN with Cloud unavailable but Google reachable;
- LAN with no Internet;
- unlimited-duration offline login behavior against last synchronized authority snapshot;
- authority refresh after reconnect;
- LAN -> Cloud event reconciliation;
- direct LAN Sheets/Drive deduplication;
- ADMIN+ business conflict resolution;
- all required business slices;
- no-admin company-network/PDA physical regression;
- backup/restore/update gates.

Only after full BETA PASS may STABLE promotion be proposed, and explicit Owner approval is still required.

## Immediate execution order

1. Reconcile active docs/checkpoint to V3.
2. Extend the Service API contract with LAN local acceptance + Google receipt + authority snapshot fields.
3. Extend LAN edge schema design with auth snapshot, projection receipt, Drive receipt and Cloud-sync cursor tables/state.
4. Define shared domain-core interfaces and cross-runtime acceptance vectors.
5. Continue unblocked Cloud packaging/auth/projection work in parallel.
6. Build the real LAN Service skeleton against the shared API/core, not the transport prototype.
7. Build Website/APK client foundations against the same contract.
8. Start Slice 1 only after the shared runtime contracts are explicit enough to prevent drift.
