# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`
Execution plan: `docs/DELIVERY_PLAN_V2.md`

## Execution rule

Default state is `CONTINUE` with dependency-aware parallel execution.

Owner interaction is requested only for:
- `OWNER_PERMISSION_REQUIRED`: an Owner-controlled permission/access/consent/secret-store action is genuinely required; or
- `OWNER_DECISION_REQUIRED`: a material business/security policy has multiple valid outcomes and cannot be resolved from current authority.

Android/APK, LAN Service, Cloud Service, Web and Google integration are all active product lanes. Final company-network hardware affects only physical evidence, not source/build progress.

## Gate 0 — architecture correction — COMPLETE

Completed after Owner clarification:
- locked final deliverables: Website + APK + Cloud Service + LAN Service;
- APK redefined as PDA-optimized business client of the same domain/API as Web;
- LAN redefined from transport-only fallback into a Cloud-Service substitute runtime for Internet loss, Cloud Service failure/degradation and forced-LAN clients;
- legacy repo and temporary transport prototype classified NON_AUTHORITY;
- created `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`;
- created `docs/DELIVERY_PLAN_V2.md`;
- superseded transport-only product assumptions in `docs/LAN_TRANSPORT_BETA_V1.md`;
- expanded BETA acceptance for Cloud/LAN/Web/APK/failover/reconciliation.

Do not return to the old plan of polishing the transport test APK/Agent as the final product.

## Gate 1 — shared domain/API + dual-runtime contract — ACTIVE NOW

This is the immediate common dependency for Cloud and LAN.

### 1A. Shared command/query contract

1. normalize route/command/query naming;
2. lock request/response/error envelopes;
3. lock idempotency + device sequence + entity version semantics;
4. define provider-neutral business transition/event output;
5. define commit-location/status values:
   - `CLOUD_COMMITTED`;
   - `LAN_RELAYED_CLOUD_COMMITTED`;
   - `LAN_ACCEPTED_PENDING_SYNC`;
   - `QUEUED_CLIENT_LOCAL`;
   - `SYNC_CONFLICT`.

### 1B. Shared domain-core boundary

1. separate business validation/state-transition/event intent from Cloudflare bindings;
2. define Cloud D1 adapter interface;
3. define LAN edge persistence adapter interface;
4. build shared acceptance vectors so the same business command must produce the same business result/error on both runtimes;
5. prohibit provider-specific code from silently redefining business rules.

### 1C. Reconciliation envelope

Define exact fields/state transitions for:
- edge instance/epoch;
- request/idempotency identity;
- device ID/sequence;
- command/event type;
- base/entity version;
- payload hash;
- local acceptance status/time;
- reconciliation result/conflict evidence.

Do not invent unresolved offline-auth fields/TTL policy; keep extension points explicit until security policy is reviewed.

## Gate 2 — Cloud Service runtime — PARALLEL

### Existing foundation
- D1 `business_core_v3`: PASS;
- Worker foundation health/meta: LIVE;
- auth/session/permission source foundation: PASS at source/CI level;
- projection outbox source foundation present;
- GAS projection deployment v3: PASS but NOT_LIVE.

### Next Cloud work
1. safely enable reviewed multi-module Worker packaging through an allowed high-level path;
2. packaging-only deploy with business routes still fail-closed;
3. integrate auth/session/permission runtime;
4. implement provider-neutral domain core + D1 adapter/canonical mutation helper;
5. implement business command families by vertical slice;
6. provision Worker -> GAS projection authentication;
7. implement projection sender/ACK/retry/dead-letter E2E;
8. implement Drive media/document flow.

### Current Cloud blocker

Connected platform write-safety blocks the provider-mutating deploy workflow and some sensitive runtime source changes. Do not bypass the guard with lower-level Git/API tricks. Continue every independent safe lane while this remains blocked.

## Gate 3 — LAN Service runtime — PARALLEL

Build the real substitute runtime, not the old transport pilot.

1. define local edge DB schema from current business contracts;
2. define snapshot/delta sync from Cloud/D1-derived Service data while online;
3. implement immutable local event journal;
4. implement durable reconciliation outbox;
5. implement `LAN_RELAY` to Cloud using the same command identity;
6. implement `LAN_AUTONOMOUS` transaction path for reviewed offline-capable commands;
7. implement idempotent reconnect/reconciliation into Cloud Service/D1;
8. implement explicit `SYNC_CONFLICT` evidence/resolver queue;
9. serve a compatible Web bundle locally so browser operation does not depend on public Internet during LAN autonomous mode;
10. preserve no-admin/user-mode operation, diagnostics, safe update and recovery requirements;
11. selectively adapt only useful legacy/prototype transport primitives after review.

### LAN architecture rule

A LAN receipt is not automatically global success. UI/audit must distinguish local edge acceptance from D1 reconciliation.

## Gate 4 — security / pairing / offline authority — PARALLEL DESIGN

Need to design and then review:
1. PDA/Web-client <-> LAN Service authenticated channel/pairing;
2. device registration/security epoch integration;
3. offline user authorization/capability mechanism;
4. stale-auth/revocation handling after reconnect;
5. autonomous-mode entry/exit authorization;
6. key/token rotation and diagnostic secret exclusion.

### Explicit unresolved policy

Do not invent:
- exact offline auth/capability mechanism;
- exact offline TTL;
- privileged/security admin operations allowed offline;
- who may explicitly activate emergency autonomous mode.

Implementation for unresolved privileged cases stays fail-closed until reviewed.

## Gate 5 — Web + APK client foundations — PARALLEL

### Website
1. same domain client contract as APK;
2. auth/session shell;
3. permission-aware navigation;
4. Cloud/LAN runtime indicator and endpoint-selection logic;
5. compatible local bundle served by LAN Service;
6. clear pending-sync/conflict UI states.

### APK
1. replace transport-pilot product direction with real VHDCHY PDA client;
2. same auth/domain contract as Web;
3. PDA-optimized navigation/screens;
4. same Cloud-direct / LAN-relay / LAN-autonomous state model;
5. durable client queue only for commands explicitly safe for retry/offline;
6. scanner/QR integration through domain commands, never direct DB shortcuts;
7. bounded background retry/update behavior;
8. BETA signing/update only after product source behavior stabilizes.

The temporary `android-pilot/` source may contribute reviewed low-level mechanics but is not the product UI/architecture.

## Gate 6 — vertical business slices — BUILD ALL SURFACES TOGETHER

For each slice, implement/test in this order:

`shared contract/core -> Cloud adapter -> LAN adapter -> Web -> APK -> Cloud/LAN E2E -> reconciliation -> Google projection`

### Slice 1 — identity / employee / attendance / presence
- employee/MNV lifecycle;
- login/user context;
- IN/OUT/repeated IN;
- current presence;
- QR employee resolution;
- offline LAN acceptance + reconnect reconciliation.

### Slice 2 — work session / PICK / PACK / resources
- MAIN/EXTRA session rules;
- PICK/PDA/User Pick;
- PACK table/User Pack mapping;
- assignment changes/history;
- release/reissue;
- cross-cluster borrow;
- offline resource conflict detection/reconciliation.

### Slice 3 — labor / dropped goods
- labor catalog/open/finish/correction;
- cross-cluster labor;
- dropped-goods manual/QR;
- offline LAN/recovery path.

### Slice 4 — documents/media
- DRAFT metadata;
- offline local staging + hash;
- Drive upload/readback;
- FINAL gate;
- corrections/replacement/portrait semantics.

## Gate 7 — failover / recovery / split-brain acceptance

Required drills:
1. site Internet lost while local Wi-Fi/LAN remains;
2. Cloud Service unavailable while Internet remains;
3. one client forced to LAN while Cloud is healthy;
4. LAN host restart with pending edge events;
5. uncertain response + retry;
6. conflicting Cloud/LAN resource/entity changes during partition;
7. reconnect + exactly-once reconciliation;
8. Google remains degraded after Cloud recovery;
9. both Cloud and LAN unavailable -> eligible client-local queue;
10. local Web remains loadable/usable without public Internet.

No silent overwrite/drop is accepted.

## Gate 8 — Google / backup / DR / packaging

Parallel platform work:
- Sheets remains D1-derived projection only;
- LAN events project only after D1 reconciliation;
- offline-staged media uploads to Drive after recovery;
- D1 snapshot/restore;
- LAN edge pending-event/staged-media recovery;
- LAN no-admin update + rollback;
- APK signer/hash/update channel;
- matching Web Cloud/LAN bundle compatibility.

## Gate 9 — BETA product acceptance

BETA PASS requires evidence for:
- Cloud Service business runtime;
- Website Cloud path;
- APK Cloud path;
- LAN relay path;
- LAN autonomous path;
- all required V1 business slices;
- recovery/reconciliation/conflict handling;
- Google deferred projection and Drive staging/upload;
- no-admin company-network physical regression;
- backup/restore/update gates.

Then and only then may STABLE promotion be proposed, still requiring explicit Owner approval.

## Current exact next work

1. Reconcile `docs/SERVICE_API_CONTRACT.md` with V2 commit-location/reconciliation semantics.
2. Define provider-neutral domain core/adapters and LAN edge schema contract.
3. Extend canonical mutation design so Cloud and LAN share event/idempotency semantics.
4. In parallel, continue every unblocked Cloud auth/projection/package task.
5. Start Slice 1 only after shared contract boundaries are explicit enough to prevent Cloud/LAN divergence.

No Owner action is currently required for these design/source steps.
