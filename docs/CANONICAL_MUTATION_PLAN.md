# MUTATION + EDGE RECONCILIATION PLAN — V2

Status: REVIEWED DESIGN / SOURCE IMPLEMENTATION PENDING
Updated: 2026-09-13
Authority: `DECISIONS.md` D-004/D-042..D-047, `docs/SERVICE_API_CONTRACT.md`, `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`

## Purpose

Define one business mutation model that can be executed through:

- Cloud Service -> D1 canonical transaction; or
- LAN Service autonomous edge transaction -> later D1 reconciliation.

Cloud and LAN persistence mechanisms may differ. Business validation, command identity, event meaning, optimistic-version semantics and conflict/error behavior may not drift.

## Shared command identity

Every retryable mutation uses stable identity independent of runtime/path:

- request ID;
- idempotency key;
- authenticated/authorized actor context;
- device ID and monotonically increasing device sequence where applicable;
- command/event type;
- target entity ID/type;
- expected/base entity version where applicable;
- normalized payload + payload hash;
- cluster/module scope.

Changing Cloud direct -> LAN relay -> LAN autonomous -> reconciliation must not create a new logical business command.

## Provider-neutral domain result

Before persistence, the shared domain layer should produce a reviewed result conceptually containing:

- validated command identity;
- required current-state/base-version preconditions;
- intended state transition(s);
- immutable event type/entity/payload;
- projection intent where applicable;
- stable business error if validation fails.

Provider adapters may translate this into D1 or LAN-edge transactions, but may not alter business meaning.

# Part A — Cloud D1 transaction

## Cloud invariant

Every successful direct/reconciled canonical business command must atomically:

1. apply guarded current-state mutation(s);
2. append exactly one immutable `domain_events` row for the logical mutation;
3. enqueue exactly one `projection_outbox` row for that event where projection applies.

No Cloud success may be returned if only a subset commits. Google projection remains asynchronous and is never part of the canonical D1 transaction.

## D1 transaction primitive

Use `D1Database.batch()` with prepared statements only. Statements execute as one reviewed batch/transaction and any failing statement must abort the unit.

Do not emulate the canonical transaction with unrelated awaited `.run()` calls.

## Cloud execution order

1. authenticate/resolve effective permission;
2. bounded reads needed to construct command;
3. ordinary idempotency fast-path lookup;
4. build one guarded D1 batch;
5. apply state write(s);
6. assert guarded mutation count inside the transaction;
7. append immutable event;
8. append projection outbox;
9. return `CLOUD_COMMITTED` only after the batch succeeds;
10. on unique idempotency race, re-read the winning event and reconcile only if command identity/payload matches.

## Guarded state mutation

Use entity version/open-state/availability/current-state predicates as appropriate. A zero-row guarded update is a business conflict, not success.

For V1 D1 implementation the reviewed assertion strategy may use SQLite `changes()` in the immediately following immutable event insert, causing a constraint failure when the expected state row count is not exactly correct.

Conceptual form:

```sql
UPDATE some_state
SET ..., entity_version = entity_version + 1
WHERE entity_id = ? AND entity_version = ? AND ...;

INSERT INTO domain_events(..., entity_version, ...)
VALUES (..., CASE WHEN changes() = 1 THEN ? ELSE 0 END, ...);

INSERT INTO projection_outbox(event_id, projection_target, payload_json, status, attempts)
VALUES (?, 'GOOGLE_SHEETS', ?, 'PENDING', 0);
```

Command-specific multi-row mutations must add equivalent in-transaction guards/assertions rather than ignoring partial/zero-row writes.

# Part B — LAN autonomous edge transaction

## Edge invariant

A successful offline-capable LAN autonomous command must atomically:

1. validate the reviewed available offline auth/permission context;
2. validate local edge current-state/base-version/business preconditions;
3. apply local edge current-state mutation(s);
4. append exactly one immutable edge event for the logical command;
5. append exactly one durable sync/reconciliation outbox row;
6. return `LAN_ACCEPTED_PENDING_SYNC` only after the local durable transaction commits.

A LAN transport receipt alone is not business acceptance.

## Edge store requirements

The LAN persistence engine must provide real local transactional durability suitable for:
- current operational state for declared offline-capable modules;
- immutable edge events;
- sync outbox;
- snapshot/version metadata;
- conflict/reconciliation evidence;
- staged media metadata/files where required.

Exact implementation technology is not locked here. SQLite is a strong candidate because the legacy/no-admin environment proved it feasible, but product selection must be based on current compatibility/footprint testing rather than inheritance.

## Edge event minimum evidence

An accepted edge event must retain:
- local edge event ID;
- original request/idempotency key;
- device ID/sequence where applicable;
- edge instance ID and edge runtime epoch;
- command/event/entity identity;
- base/expected entity version;
- normalized payload hash;
- locally assigned resulting edge version/state evidence;
- local acceptance time;
- authorization evidence reference required by the later reviewed offline-auth mechanism;
- reconciliation state.

Raw bearer/provider secrets are not stored in event payloads.

## Edge idempotency

- same idempotency key + same normalized command -> return/reconcile original edge acceptance;
- same idempotency key + different payload/command -> hard conflict;
- same `(deviceId, deviceSeq)` + different command -> hard collision/conflict;
- retries do not append another edge event.

# Part C — LAN relay

When Cloud Service is reachable through the LAN host:

1. preserve original command identity;
2. forward to Cloud Service;
3. do not perform an unnecessary autonomous local business commit;
4. retry uncertain Cloud response using same idempotency key;
5. return `LAN_RELAYED_CLOUD_COMMITTED` only after Cloud/D1 canonical commit/reconciliation is known.

This is the preferred forced-LAN behavior while Cloud is healthy because it solves client path problems without creating avoidable dual histories.

# Part D — reconciliation

## Reconciliation invariant

Reconciliation never means "copy current LAN tables over D1".

It replays/reconciles immutable accepted edge commands/events into the canonical Cloud command path while preserving their original identity and precondition evidence.

For each pending edge item:

1. submit original logical command/reconciliation envelope to Cloud Service;
2. Cloud checks idempotency first;
3. if already canonically committed with compatible identity/payload, link the existing canonical event and mark reconciled;
4. otherwise validate current canonical business state/version/availability;
5. if compatible, commit through the normal D1 state + event + projection-outbox transaction;
6. return/link canonical event identity;
7. mark edge outbox `RECONCILED` only after authoritative response;
8. if canonical guards conflict, retain edge evidence and move item to `SYNC_CONFLICT`;
9. never silently mutate/drop the original edge event to make the conflict disappear.

## Ordering

Commands whose domain semantics depend on order must reconcile in deterministic origin/device/event order unless a reviewed command-specific rule permits independent execution.

One conflict must not cause later dependent commands to overtake silently. Independent commands may proceed only when dependency analysis proves safety.

## Rebase/readiness after recovery

After reconciliation:
- refresh/rebase LAN edge snapshot/current state from canonical Service data;
- retain local immutable event/reconciliation history according to retention policy;
- do not claim fully current autonomous readiness until required snapshot/version checkpoints are synchronized.

# Part E — split-brain handling

A hard network partition may allow Cloud-side and LAN-side actors to create conflicting valid histories. Because uninterrupted LAN operation is an explicit requirement, this cannot be eliminated in all cases without sacrificing availability.

Required behavior:
- detect version/resource/business conflicts;
- keep both canonical and edge evidence;
- classify `SYNC_CONFLICT`;
- expose enough business context for a reviewed resolver action;
- resolver/correction produces new canonical event(s); it does not rewrite raw history.

Silent last-write-wins is prohibited.

# Part F — Google projection

- Cloud D1 canonical commit creates `projection_outbox` in the same transaction.
- LAN autonomous acceptance does NOT directly write business Sheets.
- After edge event reconciles into D1, the canonical projection outbox drives Google Gateway/Sheets.
- Google failure does not roll back D1 and does not invalidate an already durable LAN edge acceptance; it remains a downstream degraded integration.

# Part G — media/document consequence

Offline LAN may stage media locally with durable hash/metadata, but current document lifecycle still requires durable Drive upload/readback before FINAL.

Therefore:
- offline capture may exist as local staged/DRAFT evidence;
- reconciliation/upload occurs after Cloud/Internet recovery;
- FINAL is not fabricated while Drive durability has not passed unless Owner explicitly changes D-024/D-047 policy.

# Error/status mapping

Cloud/runtime business errors remain stable:
- authentication failure -> 401;
- permission/policy denial -> 403;
- version/state/resource/idempotency/reconciliation conflict -> 409;
- valid syntax but invalid business input -> 422;
- required current runtime dependency unavailable/schema mismatch -> 503.

Commit location is separate from HTTP/business error:
- `CLOUD_COMMITTED`;
- `LAN_RELAYED_CLOUD_COMMITTED`;
- `LAN_ACCEPTED_PENDING_SYNC`;
- client-only `QUEUED_CLIENT_LOCAL`;
- reconciliation `SYNC_CONFLICT`.

# Required automated acceptance

## Shared vector
1. same command vector produces same business transition/event/error on Cloud and LAN adapters.
2. actor spoofing never becomes authority.
3. same idempotency/device identity preserved across transport/runtime changes.

## Cloud
4. happy-path state + event + outbox commit atomically.
5. state guard failure leaves no event/outbox/partial state.
6. event/outbox failure rolls back state.
7. idempotency race produces one canonical event.
8. device sequence collision produces no partial writes.

## LAN edge
9. happy-path edge state + immutable edge event + sync outbox commit atomically.
10. edge state/event/outbox failure rolls back the local unit.
11. edge retry creates one event only.
12. edge device/idempotency collision is explicit.
13. restart preserves accepted pending edge events/outbox.

## Reconciliation
14. non-conflicting edge event reconciles once to D1.
15. uncertain sync response + retry does not duplicate canonical event.
16. already-reconciled idempotency maps back to existing canonical event.
17. canonical version/resource conflict becomes `SYNC_CONFLICT` with edge evidence retained.
18. no direct Sheets business write occurs before D1 reconciliation.
19. successful recovery refreshes/rebases required LAN snapshot/version state.

# Implementation boundary

Current platform safety still blocks some sensitive Cloud Worker source/workflow writes. Do not bypass that guard. The provider-neutral domain and LAN edge design/source can continue in independent safe paths, and Cloud integration resumes through approved high-level paths when available.
