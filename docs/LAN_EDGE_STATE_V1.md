# LAN EDGE STATE / RECONCILIATION CONTRACT V1

Status: ACTIVE DESIGN 2026-09-13
Authority: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`, `docs/SERVICE_API_CONTRACT.md`, `docs/CANONICAL_MUTATION_PLAN.md`

## Purpose

Define the minimum local state model required for LAN Service to act as a real substitute runtime during Cloud/upstream loss without becoming an uncontrolled second global database.

This contract is infrastructure-level. Business module tables/fields must come from current domain contracts and are not copied from the legacy repository by default.

## 1. Edge database responsibilities

The LAN edge store must persist five categories:

1. runtime/snapshot metadata;
2. the current operational state required by enabled offline-capable modules;
3. immutable locally accepted edge events;
4. durable sync/reconciliation outbox;
5. conflict and staged-media evidence.

It must survive normal LAN Service restart/update and must not depend on Administrator privileges.

## 2. Required logical records

Exact SQL/table names may change during implementation, but the following logical records are required.

### `edge_meta`

Minimum concepts:
- environment;
- cluster/module scope;
- local edge instance ID;
- current runtime epoch;
- edge schema version;
- compatible shared-domain/API version;
- current snapshot generation/checkpoint;
- last successful Cloud sync time/checkpoint;
- readiness state.

### `edge_snapshot_state`

Tracks Cloud-derived baseline/delta import:
- snapshot generation ID;
- module/scope;
- source canonical checkpoint/version;
- import started/completed time;
- row/domain counters needed for verification;
- checksum/hash where applicable;
- status/error evidence.

### module current-state tables

Contain only the operational state required for the modules declared offline-capable.

Examples of categories may include employee identity/status, shifts, current presence/open work, resources/configuration and other module state, but exact fields must be derived from current business schemas/contracts.

Do not mirror every D1 table automatically merely because it exists.

### `edge_events`

Immutable accepted LAN business events.

Required concepts:
- edge event ID;
- request ID;
- idempotency key;
- device ID/sequence when applicable;
- edge instance/epoch;
- command/event/entity identity;
- base/expected entity version;
- normalized payload or reviewed event payload;
- payload hash;
- local acceptance time;
- local resulting version/state evidence;
- authorization evidence reference defined by the later offline-auth contract;
- reconciliation status/reference.

Raw provider credentials/session secrets do not belong here.

### `edge_sync_outbox`

One durable pending reconciliation record per accepted edge event.

Minimum states:
- `PENDING`;
- `SYNCING`;
- `RECONCILED`;
- `CONFLICT`;
- `RETRY_WAIT`;
- `REVIEW_REQUIRED` where automatic retry is no longer safe.

Exact retry limits/backoff are implementation parameters and must be measured/reviewed rather than guessed here.

### `edge_conflicts`

Retains reconciliation conflict evidence:
- edge event reference;
- canonical entity/version/event reference when available;
- stable conflict code;
- business/context snapshot sufficient for resolver UI;
- detected time;
- resolver state;
- eventual correction/resolution canonical event reference.

Conflict evidence is retained; resolver actions create new events/corrections rather than rewriting original edge/canonical history.

### `staged_media`

For files/images captured while Drive is unavailable:
- local file identity/path reference;
- business/document/entity reference;
- SHA-256 or reviewed hash;
- content type/size;
- capture time;
- upload state;
- eventual Drive identity/readback result.

Do not mark a document FINAL merely because a local file exists when current policy requires Drive durability.

## 3. Edge readiness states

LAN health must make readiness explicit.

Recommended logical states:

- `EDGE_EMPTY` — no valid operational snapshot;
- `EDGE_SYNCING` — snapshot/delta import in progress;
- `EDGE_READY` — required state/checkpoint for declared offline-capable modules is present and verified under current compatibility rules;
- `EDGE_STALE` — last sync is outside the later reviewed readiness policy or required checkpoint validation failed;
- `EDGE_CONFLICTED` — unresolved condition prevents safe autonomous claims for affected scope/module;
- `EDGE_DEGRADED` — partial non-critical function unavailable but evidence remains explicit.

Exact staleness time thresholds are NOT defined here and must not be invented.

## 4. Snapshot/delta sync rules

While Cloud is healthy, LAN Service prepares for possible outage by maintaining reviewed operational state.

Rules:
1. sync is through a reviewed Service/API snapshot/delta contract, not direct use of Cloud provider admin credentials;
2. snapshot generation/checkpoint is versioned;
3. import is atomic per reviewed scope or uses a staging/swap method so partial refresh is not reported READY;
4. resume/retry must be deterministic;
5. source schema/domain contract compatibility must be checked;
6. readiness must identify which modules/scopes are actually available offline;
7. after reconciliation, refresh/rebase required state before reporting full autonomous readiness again.

## 5. Autonomous transaction rules

For one accepted offline-capable command, local persistence must commit as one durable unit:

`local current-state change + immutable edge event + edge sync outbox`

If any part fails, none is reported accepted.

Return `LAN_ACCEPTED_PENDING_SYNC` only after this local transaction is durable.

## 6. Reconciliation rules

Reconciliation works from immutable edge events/outbox, never by overwriting D1 with a dump of edge current-state tables.

For each pending item:
- preserve original idempotency/device identity;
- submit to the canonical Cloud command/reconciliation path;
- link existing canonical event for a valid replay;
- mark `RECONCILED` only after authoritative Cloud result;
- on canonical/business version conflict, mark `CONFLICT` and create conflict evidence;
- retry transient failure without creating a new business identity.

## 7. Ordering/dependency

Default reconciliation preserves origin/device/event order where business semantics depend on it.

Later commands must not silently overtake an unresolved earlier dependency.

Independent events may be parallelized only after dependency analysis proves that their entities/business invariants do not interact.

## 8. Relay vs autonomous persistence

### `LAN_RELAY`

When Cloud is reachable, LAN should forward the command to Cloud and avoid creating an unnecessary edge business event. Transport/diagnostic metadata may be recorded separately.

### `LAN_AUTONOMOUS`

When Cloud/upstream is unavailable and policy permits the command, create the durable edge transaction described above.

This separation reduces avoidable split-brain while still satisfying true offline continuity.

## 9. Security/storage rules

- no raw Cloudflare/Google/GitHub provider credentials in edge DB;
- no raw bearer/session token inside business event payload;
- pairing/offline-auth secrets use a later reviewed secure local storage design;
- diagnostics export identifiers/status/hashes, not raw credentials/business payload by default;
- BETA/STABLE local state must be isolated;
- filesystem permissions and encryption-at-rest choices are implementation/security design work and must respect company no-admin constraints.

## 10. Retention

Do not invent destructive retention yet.

At minimum:
- never delete unreconciled edge events/outbox;
- never delete unresolved conflict evidence;
- never delete staged media before durable target upload/readback is verified;
- reconciled history retention/compaction policy is TBD and must preserve audit/recovery needs.

## 11. Acceptance minimum

Before one module is declared LAN-autonomous-capable:

1. valid snapshot import reaches verified `EDGE_READY`;
2. partial/corrupt snapshot never reports READY;
3. autonomous state + event + outbox transaction is atomic;
4. restart preserves pending accepted events;
5. same idempotency retry does not duplicate edge event;
6. device-sequence collision is explicit;
7. relay mode does not create duplicate autonomous event when Cloud commit succeeds;
8. non-conflicting edge event reconciles once;
9. conflicting edge event becomes explicit conflict evidence;
10. post-recovery snapshot rebase completes before full READY claim;
11. no direct Sheets fallback write;
12. no secrets leak through DB diagnostics/export.
