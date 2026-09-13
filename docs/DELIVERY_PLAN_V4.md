# DELIVERY PLAN V4 — BETA TO READY-STABLE

Status: ACTIVE EXECUTION PLAN
Updated: 2026-09-13
Authority: current Owner instruction, decision layers V1/V3/V4/V5/V6, V3 architecture and reviewed project evidence.

Goal: reach a complete BETA product containing Website + APK + Cloud Service + LAN Service + Google outputs, while preparing isolated STABLE infrastructure so promotion later uses the exact accepted BETA release with minimal setup work and no BETA runtime-data copy.

## Operating rule

Execution is dependency-aware and parallel by default.

Do not serialize independent work merely for convenience. Serialize only writes that touch the same resource/state or depend on an unfinished contract/migration.

One blocked lane never stops independent safe lanes. Owner interaction is requested only for a real missing permission/secret/consent or an unresolved material product/security decision.

## Dependency spine

```text
Authority/contracts
   |
Shared domain + identity/status model
   |-----------------------|-----------------------|----------------------|
Cloud runtime          LAN runtime           Web/APK clients       Stable preparation
   |                       |                       |                      |
D1 adapters            Edge adapters           shared client            isolated infra
   |                       |                       |                      |
   +----------- vertical business slices across all runtimes -----------+
                               |
                    failover/reconciliation
                               |
                       full BETA acceptance
                               |
                exact accepted release identity
                               |
                     Owner promotion approval
                               |
                         STABLE activate
```

## Phase 0 — knowledge/authority reconciliation — COMPLETE FOR CURRENT OWNER-LOCKED RULES

Outputs:
- effective decision layers V1/V3/V4/V5/V6;
- canonical Owner business-rules handbook;
- V3 shared Service API contract;
- V2 LAN edge-state contract;
- non-functional baseline;
- data-model guide;
- stale V2 wording no longer allowed to overrule current authority;
- ROOT factor/lifetime ambiguity from V5 resolved by V6.

No known ROOT factor/lifetime Owner decision gate remains open. A future `OWNER_DECISION_REQUIRED` may be raised only for a genuinely new unspecified material choice.

## Phase 1 — shared domain foundation — START NOW

### Lane D1 — command/event vocabulary

Define stable codes and versioning for the first vertical slices:
- employee/MNV;
- attendance/presence;
- work session/task;
- resource assignment/change/release/reissue/borrow/mapping;
- labor;
- dropped goods;
- document/media;
- conflict/correction;
- synchronization/receipt.

Each command defines input schema, required permission/scope, preconditions, idempotency, expected/base version, output, event intent and stable errors.

### Lane D2 — runtime-neutral result model

Shared results expose business result plus:
- Cloud/LAN commit location;
- Cloud-sync state;
- Google output state;
- conflict state;
- request/event identity.

### Lane D3 — shared acceptance vectors

Create reusable vectors that can execute against Cloud and LAN adapters and assert equivalent business meaning.

Exit: adapters cannot legally redefine business rules.

## Phase 2 — Cloud runtime foundation — PARALLEL START NOW

Current foundation already exists: D1 `business_core_v3`, Worker health/meta, auth/session/permission source modules, projection module, deployed fail-closed Google Gateway.

### C1 — Worker multi-module packaging

Replace the current single-`index.js` upload limitation with reviewed multi-module Worker packaging and validation. This is the immediate Cloud runtime blocker.

### C2 — protected route integration + V6 authentication runtime

Integrate session/permission context into runtime after C1. Keep business/admin routes fail-closed until tests pass.

Implement the locked V6 authentication contract:
- ROOT normal login by fixed-email one-time password;
- 5-minute validity and 5-minute resend cooldown;
- atomic single-use consumption and replay rejection;
- optional ROOT TOTP, enforced only when enabled;
- ROOT one-time login does not set `MUST_CHANGE_PASSWORD`;
- normal-account forgot-password uses the same one-time credential lifecycle but enters restricted `MUST_CHANGE_PASSWORD` until a different permanent password is set;
- no readable credential values in source/log/audit/business storage.

### C3 — Cloud canonical mutation adapter

Implement reusable guarded D1 current-state + immutable event + async-work transaction primitive and stable idempotency handling.

### C4 — V3 edge reconciliation storage/API

Add reviewed D1 migration/source for edge source/event linkage, reconciliation status/conflict linkage and Google output receipts.

### C5 — Google sender

Complete authenticated Worker->Gateway batching/retry/ACK, then activate projection only after failure/idempotency tests pass.

### C6 — Drive flow

Implement logical-file identity, upload/readback/hash state and document finalization integration.

## Phase 3 — LAN full Service foundation — PARALLEL START NOW

Physical company testing is not required to build/source-test the runtime.

### L1 — portable runtime skeleton

Create a normal-user portable LAN Service package with local configuration/state directories and explicit BETA/STABLE isolation.

### L2 — edge persistence

Implement V2 logical groups: metadata, authority snapshots, operational snapshots, module current state, edge events, Cloud sync queue, Google queues/receipts, conflicts and staged media.

### L3 — shared domain adapter

Run the same shared commands/acceptance vectors against the LAN persistence adapter.

### L4 — sync engine

Continuous/opportunistic Cloud reconciliation whenever Cloud is reachable, independent of client route.

### L5 — direct Google

Controlled Sheets/Drive output from LAN with durable receipts/deduplication.

### L6 — local Web hosting

Serve the compatible Website build locally for Internet-loss operation.

### L7 — diagnostics/update/recovery

No-admin health/export/update/rollback/recovery without relying on corporate infrastructure changes.

Physical LAN/domain proof remains a later gate.

## Phase 4 — Website and APK foundations — PARALLEL START AFTER SHARED CLIENT CONTRACT

### W1 — Website shell

- login/session shell;
- cluster/permission-aware navigation;
- common API client;
- runtime selector/state indicators;
- conflict/admin surface shell;
- online build and LAN-hostable build from the same compatible source;
- V6 one-time-password request/use flow and normal-account forced-password-change state.

### A1 — APK shell

- real PDA application, not transport prototype;
- same auth/domain client contract;
- scanner-first navigation;
- Cloud/LAN endpoint selection;
- durable client queue only for explicitly allowed cases;
- update/compatibility foundations;
- BETA/STABLE signing/channel separation;
- V6 one-time-password request/use flow and normal-account forced-password-change state.

Android physical scanner validation can wait for hardware; source/build/test does not.

## Phase 5 — STABLE preparation — PARALLEL START NOW

Prepare isolated STABLE resources/config while keeping business traffic fail-closed.

### S1 — source/config symmetry

Add stable deployment/config definitions derived from the same source contracts as BETA but with isolated names/IDs/secrets.

### S2 — CI safety

Create fail-closed STABLE verification/preparation workflows. Promotion workflow must require an immutable accepted BETA release identity and explicit Owner gate.

### S3 — Cloudflare STABLE

Prepare `vhdchy-stable` and `vhdchy-data-stable` when stable provider credentials are available. Do not attach/activate `supra.cc.cd` business traffic until promotion approval.

### S4 — Google STABLE

Prepare isolated GCP/OAuth/GAS/Drive projection configuration when the corresponding Owner/provider credentials are available. Do not share BETA runtime state.

### S5 — Drive STABLE

The existing `02_STABLE` skeleton remains isolated. No BETA business files/rows/queues are copied into it.

### S6 — release promotion metadata

Record exact accepted BETA commit/tag/build hashes, Worker source identity, migrations, Web build, APK build, LAN build and GAS version needed for deterministic promotion.

Exit before BETA acceptance: STABLE infrastructure may be provisioned/verified but remains dormant/fail-closed.

## Phase 6 — vertical slice 1: identity / employee / attendance — START WHEN D1/CLOUD/LAN ADAPTER BOUNDARIES EXIST

Implement one slice across:
1. shared domain command/vector;
2. Cloud D1 adapter/API;
3. LAN edge adapter/API;
4. Website;
5. APK;
6. Google projection;
7. Cloud/LAN failover + reconciliation tests.

Coverage: employee/MNV lifecycle, account context, IN/OUT/repeated IN, current presence, QR identity verification, local authority snapshot evidence.

## Phase 7 — vertical slice 2: session / PICK / PACK / resources

Coverage:
- MAIN/EXTRA sessions;
- PICK requires PDA, optional/multiple User Pick history;
- PACK table -> compatible User Pack selection;
- change/release/reissue;
- faulty PDA behavior;
- cross-cluster borrow;
- competing Cloud/LAN resource conflicts;
- C01–C10 regression.

## Phase 8 — vertical slice 3: labor / dropped goods

Coverage:
- configurable labor catalog;
- one OPEN labor/session;
- cross-cluster labor;
- correction;
- dropped-goods manual + QR parsing;
- idempotency and Google projection parity.

## Phase 9 — vertical slice 4: documents/media

Coverage:
- DRAFT metadata;
- multi-image/media;
- Cloud Drive upload/readback/hash;
- LAN direct upload when reachable;
- LAN staged media when offline;
- later Cloud receipt attachment without duplicate upload;
- FINAL gate;
- replacement and employee-portrait semantics.

## Phase 10 — reconciliation/conflict completion

Implement/test:
- exact replay deduplication;
- version/resource conflict detection;
- stable edge->canonical linkage;
- Google receipt dedupe;
- dependency-aware ordering;
- ADMIN+ business conflict actions;
- ROOT-only security-conflict boundary;
- immutable correction/resolution events;
- post-reconcile LAN rebase.

## Phase 11 — backup/archive/update/operations

- D1 snapshot + restore test;
- archive/readback/checksum;
- LAN recovery with unsynchronized events/media;
- Website Cloud/LAN bundle compatibility;
- APK update/rollback;
- LAN update/rollback;
- quota dashboard and measured retention policy proposal.

## Phase 12 — full BETA acceptance

Use `docs/BETA_ACCEPTANCE_MATRIX.md` plus recovered non-functional targets.

Mandatory evidence includes Cloud/LAN Web/APK paths, V6 authentication/recovery cases, long offline operation, direct LAN Google output, Cloud reconciliation, conflict handling, C01–C10, documents/media, provider outages, burst/soak/quota, backup/restore/update and BETA/STABLE isolation.

Physical company laptop/network/PDA evidence is mandatory before final BETA PASS/STABLE proposal.

## Phase 13 — STABLE promotion

Only after full BETA PASS and explicit Owner approval:
1. freeze/identify the exact accepted BETA release;
2. verify STABLE isolated resources are clean/compatible;
3. apply the accepted schema/config/artifacts to STABLE's own data/resources;
4. deploy matching Cloud/Web/LAN/APK/GAS release artifacts;
5. attach/activate STABLE public traffic according to the reviewed release plan;
6. run STABLE smoke/readback checks;
7. record release/changelog/rollback identity.

Never rename BETA to STABLE and never copy BETA runtime/business data as promotion.

## Current parallel execution set

Can run immediately and in parallel:
- authority/contract reconciliation through V6;
- Worker multi-module packaging design/source;
- V6 auth state-machine/runtime/test implementation;
- shared domain-core interfaces/vectors;
- V3 reconciliation migration design/source;
- LAN runtime/edge skeleton;
- Website shared client/shell;
- APK shared client/shell/build pipeline;
- STABLE source/config/CI preparation;
- projection/Drive contract work that does not require new provider secrets.

Requires later physical environment:
- company LAN reachability/domain/no-admin regression;
- real MT90 scanner/network regression.

No known ROOT factor/lifetime Owner decision remains open after `DECISIONS_V6.md`.

Requires Owner/provider interaction only if missing when reached:
- STABLE provider credentials/interactive Google/GAS setup;
- any new provider permission not already available.
