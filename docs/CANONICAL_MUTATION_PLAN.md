# CANONICAL D1 MUTATION PLAN — V1

Status: REVIEWED DESIGN / SOURCE IMPLEMENTATION PENDING
Updated: 2026-09-13
Authority: `DECISIONS.md` D-004, `docs/SERVICE_API_CONTRACT.md`

## Invariant

Every successful business command must atomically produce all three effects in canonical D1:

1. apply the guarded current-state mutation;
2. append exactly one immutable `domain_events` row;
3. enqueue exactly one `projection_outbox` row for that event.

No API success may be returned if only a subset commits. Google projection remains asynchronous and is never part of the canonical transaction.

## D1 transaction primitive

Use `D1Database.batch()` with prepared statements only. The reviewed Cloudflare contract treats a batch as a transaction: statements execute sequentially and a failing statement aborts/rolls back the entire sequence.

Do not emulate a transaction with separate awaited `.run()` calls.

## Command execution order

For a normal guarded command:

1. authenticate principal and resolve effective permissions outside the mutation batch;
2. perform bounded read-only validation needed to build the command;
3. check an existing `domain_events.idempotency_key` for an ordinary retry fast-path;
4. build one D1 batch whose last canonical state statement is the primary guarded mutation;
5. immediately after the primary mutation, insert the immutable domain event with an in-SQL row-count assertion;
6. insert the projection outbox row referencing the new event;
7. return success only after `db.batch()` succeeds;
8. if the batch fails because another request won the same idempotency key race, re-read that key and reconcile only if the existing event exactly matches the requested command identity/payload.

## Guarded state mutation

The primary state write must encode the concurrency/business guard in SQL, for example through entity version, open-state, availability or expected current-state predicates.

Expected normal mutation count is exactly one row unless a command-specific reviewed contract explicitly says otherwise.

A zero-row guarded update is a business conflict, not success.

## Atomic row-count assertion

A D1 batch cannot be inspected midway and then rolled back from Worker code after it has committed. Therefore the transaction itself must turn an unexpected mutation count into a SQL failure.

The preferred V1 pattern is:

- make the guarded state write the immediately preceding INSERT/UPDATE/DELETE statement;
- in the following `domain_events` INSERT, derive the event `entity_version` with a SQL `CASE` using SQLite `changes()`;
- when `changes()` does not equal the expected row count, supply an invalid entity version such as `0`;
- `domain_events.entity_version` has `CHECK (entity_version >= 1)`, so the event INSERT fails and D1 rolls back the entire batch.

Conceptual form:

```sql
UPDATE some_state
SET ..., entity_version = entity_version + 1
WHERE entity_id = ? AND entity_version = ? AND ...;

INSERT INTO domain_events(
  event_id, event_type, entity_type, entity_id, entity_version, ...
) VALUES (
  ?, ?, ?, ?,
  CASE WHEN changes() = 1 THEN ? ELSE 0 END,
  ...
);

INSERT INTO projection_outbox(event_id, projection_target, payload_json, status, attempts)
VALUES (?, 'GOOGLE_SHEETS', ?, 'PENDING', 0);
```

This pattern must be covered by automated D1 acceptance tests before use by a business command.

## Multi-statement business mutations

If a command needs multiple state writes:

- every preliminary write must be protected by database constraints or a command-specific assertion strategy;
- the final primary guarded write must remain immediately before the domain-event assertion;
- do not accept a generic helper that silently ignores a zero-row preliminary update;
- when this cannot be expressed safely, create a command-specific transactional batch rather than weakening the invariant.

## Idempotency

`domain_events.idempotency_key` is unique when present.

Rules:

- every client mutation requires a non-empty opaque idempotency key;
- an existing key is a successful replay only when event type, entity type, entity ID, intended entity version and normalized payload are compatible with the original committed event;
- reusing one key for a materially different command returns `IDEMPOTENCY_KEY_REUSED`/409;
- a pre-read is only an optimization; the unique D1 constraint is the race-safe authority;
- if a concurrent request commits the same key first, the losing batch must roll back and then reconcile by re-reading the winning event.

## Device sequence

Where a registered device sequence applies:

- `device_id` and `device_seq` are recorded on the immutable event;
- the existing unique index on `(device_id, device_seq)` is the race-safe duplicate/order guard;
- a device sequence collision with a different command must not be treated as a successful idempotent replay merely because an idempotency key is absent or different.

## Event and outbox payloads

- actor identity comes from the authenticated principal, never trusted client actor fields;
- event payload is normalized deterministically before comparison/replay handling;
- outbox payload contains only the projection material needed by the Google Gateway contract;
- outbox creation occurs in the same D1 batch as state/event;
- no synchronous Google call occurs inside the business mutation path.

## Error mapping

- authentication failure -> 401;
- permission/policy denial -> 403;
- zero-row version/state/resource guard -> 409;
- idempotency key reused for another command -> 409;
- valid syntax but invalid business input -> 422;
- canonical D1 unavailable/schema mismatch -> 503;
- Google unavailable after D1 commit -> business command remains successful; outbox stays/re-enters retry flow.

## Required automated acceptance

Before the helper is accepted:

1. happy-path state + event + outbox all commit;
2. zero-row guarded mutation causes event assertion failure and leaves no event/outbox/state change;
3. event constraint failure rolls back state change;
4. outbox constraint failure rolls back state and event;
5. same idempotency key + same command returns the original event without second mutation;
6. same idempotency key + different command returns conflict;
7. concurrent idempotency race yields one committed event only;
8. entity-version conflict yields no partial writes;
9. device-sequence collision yields no partial writes;
10. Google unavailability after commit never rolls back canonical state and leaves projection retryable.

## Implementation boundary

The current connected GitHub path is blocking sensitive runtime-source writes by platform safety. Persist this design now; implement the helper and tests only through an approved high-level write path. Do not bypass the safety guard and do not weaken the atomic contract to make implementation easier.
