# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Execution plan: `docs/DELIVERY_PLAN_V3.md`

## Execution rule

Default state is `CONTINUE` with dependency-aware parallel execution.

Website, APK, Cloud Service, LAN Service, Google integration and reconciliation are all active product lanes. Final company-network hardware affects only physical evidence.

## Gate 0 — V3 architecture correction — COMPLETE

Locked:
- Website + APK + Cloud Service + LAN Service are first-class deliverables;
- APK is the PDA business client of the same domain contract as Website;
- LAN is a full local Service substitute, not only transport/relay;
- LAN may continue normal business processing while Cloud is unavailable;
- LAN may write controlled Sheets projection and Drive media directly when Internet/Google is reachable;
- Cloud synchronization consumes the LAN event journal/outbox, not Google as the business source;
- LAN sync starts whenever Cloud becomes reachable, even if users remain routed through LAN;
- offline login has no duration-only expiry and uses the latest synchronized local authority snapshot;
- reconnect refresh applies new authority/configuration to future operations;
- only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy;
- automatic retry/deduplication handles technical errors; unresolved business/data conflicts go to ADMIN+.

Do not return to V2 assumptions that defer all Google work until after D1 reconciliation or treat forced-LAN as relay-only.

## Gate 1 — shared domain/API + reconciliation contract — ACTIVE NOW

1. Normalize shared command/query/event naming.
2. Lock request/response/error/idempotency/device/entity-version semantics.
3. Lock client-visible statuses for:
   - central commit;
   - LAN local acceptance pending Cloud sync;
   - Google projection/upload completed or pending;
   - synchronization conflict;
   - client-only queue.
4. Define provider-neutral business transition/event output.
5. Define LAN reconciliation envelope with stable event/command identity, edge instance/epoch, base version, authority snapshot/version and Google receipts.
6. Build shared acceptance vectors executed against both Cloud and LAN adapters.

Exit gate: Cloud and LAN cannot legally implement different business meaning for the same command.

## Gate 2 — Cloud Service runtime — PARALLEL

Existing foundation:
- D1 `business_core_v3`: PASS;
- Worker health/meta foundation: LIVE;
- auth/session/permission source foundation: present/tested;
- projection outbox foundation: present;
- Google Gateway v3: deployed fail-closed.

Next:
1. safely complete multi-module Worker packaging through an allowed path;
2. integrate protected runtime routes;
3. implement shared domain core + D1 adapter/canonical transaction helper;
4. add LAN reconciliation ingestion endpoints;
5. accept Google projection/upload receipts from LAN sync so duplicate output is avoided;
6. complete Worker -> Google sender/auth/retry/ACK;
7. implement Drive metadata/readback flow;
8. implement business commands by vertical slice.

## Gate 3 — full LAN Service runtime — PARALLEL

Build the real substitute Service:

1. no-admin portable runtime package;
2. same Service/domain API contract as Cloud;
3. local edge current-state database;
4. immutable local event journal;
5. durable Cloud synchronization/reconciliation outbox;
6. synchronized local authority/account/permission/configuration snapshot;
7. resumable online operational snapshot/delta refresh;
8. controlled direct Sheets projection when Google is reachable;
9. direct Drive upload + receipt/hash metadata when Drive is reachable;
10. queued/staged Google work when Internet/Google is unavailable;
11. continuous/opportunistic Cloud sync whenever Cloud is reachable, independent of client route;
12. explicit business conflict queue with full evidence;
13. local Web bundle hosting;
14. diagnostics/update/rollback/recovery without admin/router/firewall/DNS dependency.

The old `lan-agent/` transport prototype may donate reviewed mechanics only; it is not the LAN Service architecture.

## Gate 4 — LAN security/authority continuity — PARALLEL

1. secure local authentication/authorization snapshot storage;
2. authenticated client <-> LAN channel;
3. local authority/configuration snapshot versioning;
4. reconnect refresh for subsequent operations;
5. event evidence records which synchronized authority version accepted it;
6. SUPERADMIN/ROOT-only manual LAN activation with audit trail;
7. preserve existing ROOT-exclusive security/recovery boundary.

Important: unlimited offline operation necessarily means remote account/permission changes cannot be seen until connectivity returns. Treat this as explicit evidence/state, not an impossible guarantee.

## Gate 5 — Website + APK foundations — PARALLEL

### Website
1. same domain client contract as APK;
2. login/session shell;
3. permission-aware navigation;
4. Cloud/LAN endpoint selector;
5. local Web build served by LAN Service;
6. runtime/sync/Google-output indicators;
7. ADMIN+ conflict-resolution surface.

### APK
1. real VHDCHY PDA business app;
2. same auth/domain contract as Website;
3. PDA-optimized screens/workflow;
4. Cloud/LAN endpoint selector;
5. QR/scanner integration through domain commands;
6. durable retry queue only where contract permits;
7. bounded background work;
8. BETA signing/update after product behavior stabilizes.

## Gate 6 — vertical business slices

For each slice:

`shared contract/core -> Cloud adapter -> LAN adapter -> Website -> APK -> Google outputs -> failover/reconciliation acceptance`

### Slice 1 — identity / employee / attendance / presence
- employee/MNV lifecycle;
- user context;
- IN/OUT/repeated IN;
- current presence;
- QR employee resolution;
- local authority snapshot behavior;
- LAN event -> Cloud reconciliation;
- Google projection from Cloud or LAN with deduplication.

### Slice 2 — work session / PICK / PACK / resources
- MAIN/EXTRA sessions;
- PICK/PDA/User Pick;
- PACK table/User Pack;
- assignment changes/history;
- release/reissue;
- cross-cluster borrow;
- edge resource snapshot;
- competing Cloud/LAN conflict detection and ADMIN+ resolution.

### Slice 3 — labor / dropped goods
- labor catalog/open/finish/correction;
- cross-cluster labor;
- dropped-goods manual/QR;
- Cloud/LAN parity;
- Google projection parity.

### Slice 4 — documents/media
- DRAFT metadata;
- direct LAN Drive upload when reachable;
- offline staging when unreachable;
- hash/readback receipts;
- later Cloud/D1 attachment without duplicate upload;
- FINAL gate;
- replacement/portrait semantics.

## Gate 7 — failover / recovery acceptance

Required drills:
1. normal Cloud direct;
2. SUPERADMIN/ROOT forced LAN while Cloud healthy;
3. Cloud unavailable but Internet/Google available;
4. full Internet loss with LAN available;
5. Cloud returns while users remain on LAN;
6. account/permission/configuration changed remotely during outage;
7. LAN restart with pending events/Google work;
8. duplicate/uncertain response retry;
9. split-brain business conflict;
10. both Cloud and LAN unavailable -> permitted client-local queue.

## Gate 8 — reconciliation/conflict engine

1. automatic duplicate/idempotent replay handling;
2. automatic Google receipt deduplication;
3. automatic non-conflicting Cloud ingestion;
4. version/resource conflict detection;
5. evidence-preserving conflict queue;
6. ADMIN+ business conflict decision actions;
7. ROOT-only security conflict boundary;
8. immutable post-resolution audit event.

## Gate 9 — backup / DR / packaging

- D1 snapshot/restore;
- LAN edge backup/recovery for unsynced events and staged media;
- Google projection/readback/checksum controls;
- LAN no-admin update/rollback;
- APK signer/hash/update channel;
- matching Cloud-hosted and LAN-hosted Website bundle versions.

## Gate 10 — BETA product acceptance

BETA PASS requires evidence for:
- Website Cloud path;
- APK Cloud path;
- Website LAN path;
- APK LAN path;
- forced LAN while Cloud healthy;
- LAN local business execution with Cloud down;
- direct LAN Google output with Internet available;
- full offline LAN operation;
- long-duration offline login against the last synchronized authority snapshot;
- authority/config refresh after reconnect;
- LAN -> Cloud reconciliation;
- Google row/file deduplication;
- ADMIN+ unresolved business conflict resolution;
- all required business slices;
- final company-network/no-admin/PDA regression;
- backup/restore/update gates.

STABLE remains blocked until full BETA PASS + explicit Owner approval.

## Current exact next work

1. Extend `docs/SERVICE_API_CONTRACT.md` for V3 local-acceptance/sync/Google-receipt semantics.
2. Extend LAN edge-state design for authority snapshots, Google receipts and Cloud sync cursors.
3. Define shared domain-core/adapters and runtime-neutral acceptance vectors.
4. Continue every unblocked Cloud packaging/auth/projection task in parallel.
5. Build the real LAN Service skeleton against the shared contract.
6. Build Website/APK foundations against the same contract.
7. Start Slice 1 once the parity boundaries are explicit enough to prevent divergence.
