# SERVICE API CONTRACT — V1

Status: ACTIVE DESIGN / BETA IMPLEMENTATION
Authority: `DECISIONS.md`, `AI_OPERATING_CONTRACT.md`, D1 `business_core_v3`

## Scope

This contract is shared by Web and later APK clients. Transport/client differences do not create separate business authorities. Cloudflare Worker is the public Service layer and D1 is canonical structured authority.

## Request envelope

Protected requests use HTTPS and an `Authorization: Bearer <session-token>` header. The raw token is never stored in D1; only its SHA-256 verifier is stored in `auth_sessions.token_hash`.

Mutation requests additionally carry:
- `Idempotency-Key`: required opaque client-generated key for retry-safe commands;
- `X-Device-Id`: required when the command originates from a registered device;
- `X-Device-Seq`: monotonically increasing device sequence when offline/retry ordering applies;
- `X-App-Version`: client build/revision when available.

Cluster/module scope belongs in the command payload or route contract and is validated against the authenticated principal's effective grants. Client-supplied actor identity is never authoritative; actor user/employee come from the authenticated session.

## Standard response

Successful JSON responses use:

```json
{
  "ok": true,
  "data": {},
  "requestId": "uuid"
}
```

Errors use:

```json
{
  "ok": false,
  "error": {
    "code": "STABLE_MACHINE_CODE",
    "message": "Human-readable explanation",
    "details": {}
  },
  "requestId": "uuid"
}
```

`details` is optional and must not expose secret verifiers, raw tokens, TOTP secrets, recovery destinations, provider credentials or internal stack traces.

## Authentication/session rules

A protected route is rejected unless all required conditions pass:
1. bearer token format is valid and its hash resolves one `ACTIVE` `auth_sessions` row;
2. linked `auth_users` account is `ACTIVE`;
3. session has not expired or been revoked;
4. if a device is linked, device is `ACTIVE` and session security epoch matches current device security epoch;
5. temporary/reset-password state may access only the password-change/auth-self subset until password change completes;
6. ROOT routes additionally enforce required MFA/recovery policy at the route/command gate.

Recommended status mapping:
- 401: missing/invalid/expired/revoked authentication;
- 403: authenticated but permission/MFA/policy denied;
- 409: entity-version, resource-availability, duplicate/open-state or business conflict;
- 422: syntactically valid request that violates command validation;
- 429: rate/quota guard;
- 503: canonical dependency unavailable or schema/runtime mismatch.

## Authorization rules

Effective permission is resolved from active role grants plus active direct user grants, respecting cluster/module scope and effective time range.

- Explicit matching `DENY` overrides matching `ALLOW` for normal grant-based authority.
- ROOT has full business/all-cluster authority plus exclusive ROOT security/recovery/policy authority.
- SUPERADMIN has business/all-cluster authority equivalent to ROOT but cannot modify ROOT-exclusive security/recovery/policy resources.
- Same-level account administration requires its dedicated permission and must preserve grantor/self protection rules defined in `DECISIONS.md`.
- Historical/closed accounts remain retained and are never hard-deleted merely as an administration action.

## Mutation transaction contract

Every successful canonical business mutation is one D1 transaction/batch unit that:
1. re-validates authenticated principal, effective permission, cluster/module scope and business preconditions;
2. applies the current-state change with entity/version/availability guards;
3. appends one immutable `domain_events` record;
4. enqueues one `projection_outbox` record referencing that event;
5. returns success only after D1 commit succeeds.

Google projection is asynchronous. Google/Drive projection failure never rolls back a committed D1 business transaction.

A repeated mutation with the same valid idempotency key returns/reconciles the already committed canonical event instead of creating a second business mutation.

## Projection boundary

`projection_outbox` is the only normal handoff from canonical D1 mutations to Google Sheets projection. Outbox work is retried asynchronously and may become `DEAD` after the reviewed retry limit; canonical business data remains committed and authoritative in D1.

Closed-quarter Sheets are not directly corrected. Corrections originate as D1 adjustment/correction events and are re-projected according to projection policy.

## Route families

Foundation/public routes:
- `GET /health`
- `GET /health/deep`
- `GET /health/integrations`
- `GET /api/v1/meta`
- `GET /api/v1/capabilities`

Auth routes, once implemented and tested:
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`
- `POST /api/v1/auth/change-password`

Business/admin route families remain fail-closed until their command handlers satisfy this contract:
- `/api/v1/data/*`
- `/api/v1/admin/*`

## Client and CORS boundary

Initial Web operation is same-origin with the Service. Cross-origin API access remains disabled until an explicit client security/CORS contract is reviewed. APK transport later uses the same command/auth/event semantics with a separately reviewed device security policy.

## Acceptance minimum

Before a protected business route is BETA-live, automated tests must cover anonymous rejection, expired/revoked session rejection, security-epoch invalidation, scope filtering, explicit DENY precedence, ROOT/SUPERADMIN boundary, idempotent retry and failure of projection without rollback of canonical D1 state.
