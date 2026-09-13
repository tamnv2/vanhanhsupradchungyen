# SERVICE API CONTRACT — V2

Status: ACTIVE DESIGN / BETA IMPLEMENTATION
Authority: `DECISIONS.md`, `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`, D1 `business_core_v3`

## Scope

This is the shared business contract for:
- Website;
- Android APK;
- Cloud Service runtime;
- LAN Service runtime.

Client/runtime differences do not create separate business meanings. The same business command/query must preserve actor, permission, idempotency, entity-version, event and machine-error semantics across Cloud direct, LAN relay and LAN autonomous paths.

Cloudflare Worker + D1 is the normal online runtime. LAN Service may relay to Cloud or execute approved offline-capable commands locally when Cloud/upstream is unavailable, then reconcile later.

## Request envelope

Protected online requests use an `Authorization: Bearer <session-token>` header. Raw Cloud session tokens are never stored in D1; only their SHA-256 verifier is stored in `auth_sessions.token_hash`.

Mutation requests additionally carry or resolve:
- `Idempotency-Key`: required opaque stable key for retry-safe commands;
- `X-Device-Id`: required when command originates from a registered device;
- `X-Device-Seq`: monotonically increasing device sequence when offline/retry ordering applies;
- `X-App-Version`: client build/revision when available;
- cluster/module scope in route/payload according to command contract;
- expected entity/version preconditions where required.

Actor identity supplied by a client payload is never authoritative. Actor user/employee comes from the reviewed authenticated/authorized context.

For LAN autonomous reconciliation, the same logical command additionally acquires edge evidence such as edge instance/epoch, local acceptance metadata and payload hash. Exact offline-auth evidence fields remain an explicit extension point until the offline security mechanism is reviewed.

## Standard response

Successful JSON responses use:

```json
{
  "ok": true,
  "data": {},
  "requestId": "uuid",
  "runtime": "CLOUD|LAN",
  "commitStatus": "CLOUD_COMMITTED|LAN_RELAYED_CLOUD_COMMITTED|LAN_ACCEPTED_PENDING_SYNC"
}
```

A read/query response may omit `commitStatus` when no mutation occurred.

Errors use:

```json
{
  "ok": false,
  "error": {
    "code": "STABLE_MACHINE_CODE",
    "message": "Human-readable explanation",
    "details": {}
  },
  "requestId": "uuid",
  "runtime": "CLOUD|LAN"
}
```

`details` is optional and must not expose secret verifiers, raw tokens, TOTP secrets, recovery destinations, provider credentials, pairing secrets or internal stack traces.

Client-local queueing is not a Service success response. A client that has only stored a command locally reports/records `QUEUED_CLIENT_LOCAL` in its own state and must not present it as accepted by LAN/Cloud.

## Authentication/session rules — Cloud

A protected Cloud route is rejected unless all required conditions pass:
1. bearer token format is valid and its hash resolves one `ACTIVE` `auth_sessions` row;
2. linked `auth_users` account is `ACTIVE`;
3. session has not expired or been revoked;
4. if a device is linked, device is `ACTIVE` and session security epoch matches current device security epoch;
5. temporary/reset-password state may access only the password-change/auth-self subset until password change completes;
6. ROOT routes additionally enforce required MFA/recovery policy at the route/command gate.

Recommended status mapping:
- 401: missing/invalid/expired/revoked authentication;
- 403: authenticated but permission/MFA/policy denied;
- 409: entity-version, resource-availability, duplicate/open-state, idempotency or reconciliation conflict;
- 422: syntactically valid request that violates command validation;
- 429: rate/quota guard;
- 503: required runtime dependency unavailable/schema mismatch.

## Authentication/session rules — LAN

### LAN relay

When LAN can reach Cloud and operates as `LAN_RELAY`, Cloud remains the authoritative authentication/authorization/commit path. LAN must not silently widen user authority.

### LAN autonomous

True offline autonomous operation requires a reviewed offline authentication/authorization mechanism. Current authority does NOT yet define:
- exact offline credential/capability format;
- exact offline expiry/TTL;
- which privileged/security operations may run offline;
- who may explicitly activate emergency autonomous mode.

Therefore those details must not be invented in implementation. Unresolved privileged cases remain fail-closed.

Any eventual offline mechanism must make stale/revoked authorization detectable/reconcilable and must not distribute Cloud provider credentials to PDA/LAN clients.

## Authorization rules

Effective permission is resolved from active role grants plus active direct user grants, respecting cluster/module scope and effective time range.

- Explicit matching `DENY` overrides matching `ALLOW` for normal grant-based authority.
- ROOT has full business/all-cluster authority plus exclusive ROOT security/recovery/policy authority.
- SUPERADMIN has business/all-cluster authority equivalent to ROOT but cannot modify ROOT-exclusive security/recovery/policy resources.
- Same-level account administration requires its dedicated permission and must preserve grantor/self-protection rules defined in `DECISIONS.md`.
- Historical/closed accounts remain retained and are never hard-deleted merely as an administration action.
- LAN autonomous authorization must not exceed the reviewed offline authorization snapshot/capability state.

## Provider-neutral domain rule

Business validation/state transition/event intent belongs in a provider-neutral domain layer or equivalent shared contract implementation.

Cloud D1 and LAN edge adapters may implement different persistence mechanisms, but they must not redefine:
- command validation;
- business preconditions;
- permission meaning;
- idempotency semantics;
- entity-version semantics;
- event meaning;
- stable machine error codes.

Both runtimes must pass the same business acceptance vectors.

## Cloud mutation transaction contract

Every successful Cloud canonical business mutation is one D1 transaction/batch unit that:
1. re-validates authenticated principal, effective permission, cluster/module scope and business preconditions;
2. applies the current-state change with entity/version/availability guards;
3. appends one immutable `domain_events` record;
4. enqueues one `projection_outbox` record referencing that event;
5. returns success only after D1 commit succeeds.

Google projection is asynchronous. Google/Drive projection failure never rolls back a committed D1 business transaction.

A repeated mutation with the same valid idempotency key returns/reconciles the already committed canonical event instead of creating a second mutation.

Successful direct Cloud commit returns `CLOUD_COMMITTED`.

## LAN relay contract

`LAN_RELAY` is preferred when a client is forced to LAN but LAN Service can still reach Cloud Service.

Rules:
1. preserve original request/idempotency/device identity;
2. do not allocate a new business command because transport changed;
3. Cloud Service performs authoritative auth/business/D1 commit;
4. uncertain relay responses are retried with the same idempotency identity;
5. LAN reports success only when authoritative Cloud response is known.

Successful relay commit returns `LAN_RELAYED_CLOUD_COMMITTED`.

A local LAN receipt before Cloud commit is not enough to delete a business pending command.

## LAN autonomous transaction contract

When Cloud/upstream is genuinely unavailable and the command is approved for offline operation, LAN Service may accept it locally.

One successful LAN autonomous transaction must atomically:
1. validate the available reviewed offline auth/permission context;
2. validate local edge state/business preconditions/base version;
3. update local edge current state;
4. append one immutable local edge event;
5. append one durable reconciliation/sync outbox record;
6. return success only after local durable commit succeeds.

Successful local acceptance returns `LAN_ACCEPTED_PENDING_SYNC`, not `CLOUD_COMMITTED`.

The edge event must preserve enough evidence to reconcile later, including stable idempotency/device identity and relevant base/entity version. It must not be silently rewritten after acceptance.

## Reconciliation contract

When Cloud becomes reachable:
1. LAN submits pending edge events/commands in deterministic order where domain ordering matters;
2. original idempotency/device identity is preserved;
3. Cloud re-validates against current canonical state and current reconciliation rules;
4. if the same command was already canonically committed, reconciliation returns the existing canonical event;
5. non-conflicting pending events commit once into D1 using the canonical Cloud mutation path;
6. successful reconciliation marks the LAN outbox item reconciled and links the canonical event identity;
7. version/resource/business conflicts become explicit `SYNC_CONFLICT` evidence;
8. conflicting edge evidence is retained and is never silently dropped or overwritten;
9. edge snapshot/state is refreshed/rebased after reconciliation before full autonomous readiness is claimed again.

A hard network partition can create genuine concurrent histories; the API contract must expose conflict rather than claim impossible strict consistency.

## Commit status semantics

- `CLOUD_COMMITTED`: direct Cloud/D1 commit completed.
- `LAN_RELAYED_CLOUD_COMMITTED`: client used LAN front door; authoritative Cloud/D1 commit completed.
- `LAN_ACCEPTED_PENDING_SYNC`: local edge business commit completed; D1 reconciliation pending.
- `QUEUED_CLIENT_LOCAL`: not accepted by LAN/Cloud; client-only state, not a Service commit.
- `SYNC_CONFLICT`: edge event could not be automatically reconciled under current canonical/business guards.

UI, logs and diagnostics must not collapse these states into a generic success that hides durability/authority location.

## Projection boundary

`projection_outbox` is the normal handoff from canonical D1 mutations to Google Sheets projection.

LAN autonomous mode does not directly multi-write business Sheets. After LAN events reconcile into D1, the normal canonical outbox/Gateway path performs projection.

Closed-quarter Sheets are not directly corrected. Corrections originate as D1 adjustment/correction events and re-project according to policy.

## Drive/media boundary

Drive durability remains a Cloud/downstream concern.

When Internet/Cloud is unavailable, reviewed clients/LAN Service may stage files durably with hashes/metadata for later upload. The current document rule remains `DRAFT -> FINAL`, with FINAL requiring the current durable-Drive/readback gate unless Owner explicitly changes that policy.

## Route families

Foundation/public Cloud routes:
- `GET /health`
- `GET /health/deep`
- `GET /health/integrations`
- `GET /api/v1/meta`
- `GET /api/v1/capabilities`

Auth routes, once implemented/tested:
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`
- `POST /api/v1/auth/change-password`

Business/admin route families remain fail-closed until handlers satisfy this contract:
- `/api/v1/data/*`
- `/api/v1/admin/*`

LAN Service should expose a compatible reviewed business route family rather than inventing a separate business API. Provider/runtime health/diagnostic routes may differ where necessary.

## Client boundary

Website and APK use the same reviewed business/domain contract.

Website must support both the normal public Cloud bundle and a compatible LAN-local loading path so browser use can continue when public Internet is unavailable.

APK may use PDA-specific UI/scanner integration, but scanner/device input must enter the same domain commands rather than bypass Service rules.

Cross-origin/CORS/security exposure remains explicit and reviewed; local LAN availability does not imply anonymous business endpoints.

## Acceptance minimum

Before a protected business command is BETA-live:
- anonymous rejection;
- expired/revoked auth rejection where online;
- reviewed offline auth rule where autonomous LAN is enabled;
- scope filtering/DENY precedence;
- ROOT/SUPERADMIN boundary;
- idempotent retry;
- device-sequence collision handling;
- Cloud atomic state/event/outbox transaction;
- LAN atomic edge state/event/sync-outbox transaction for offline-capable commands;
- Cloud/LAN shared business vector parity;
- LAN relay identity preservation;
- LAN autonomous pending-sync status;
- reconnect exactly-once reconciliation;
- explicit sync-conflict evidence;
- projection failure without rollback of canonical D1 state;
- no direct Sheets authority in LAN mode.
