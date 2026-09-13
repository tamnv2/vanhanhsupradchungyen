# SERVICE API CONTRACT V3 — CLOUD + LAN

Status: ACTIVE DESIGN / IMPLEMENTATION CONTRACT
Updated: 2026-09-13
Authority: effective Owner decisions through V6, `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`.

This supersedes V2 contract wording where it conflicts.

## 1. One business contract

Website, APK, Cloud Service and LAN Service share the same command/query meaning, permission semantics, validation rules, idempotency, entity-version behavior, immutable event intent and stable machine errors.

Runtime/provider differences may change persistence and commit location, not business meaning.

## 2. Request identity

Protected online requests use authenticated Service context. Actor identity in request payload is never authoritative.

Retryable mutation identity includes where applicable:
- request ID;
- idempotency key;
- device ID;
- monotonic device sequence;
- environment/cluster/module scope;
- command/event code;
- target entity identity;
- expected/base entity version;
- normalized payload/hash;
- app/schema/business-rule version.

LAN local acceptance also carries edge instance/epoch and authority-snapshot version.

Changing Cloud/LAN route does not create a new logical command.

## 3. Common response state

Successful mutations expose both business result and commit location/status.

Required commit states:
- `CLOUD_COMMITTED` — canonical Cloud/D1 commit completed;
- `LAN_ACCEPTED_PENDING_SYNC` — durable LAN local business commit completed, Cloud sync pending;
- `LAN_RECONCILED_CLOUD_COMMITTED` — LAN-originated command is now linked to canonical Cloud commit;
- `QUEUED_CLIENT_LOCAL` — client has only stored retry work; no Service has accepted the business command;
- `SYNC_CONFLICT` — LAN evidence could not auto-reconcile safely.

Google output status is separate:
- `NOT_REQUIRED`;
- `PENDING`;
- `COMPLETED`;
- `FAILED_RETRYABLE`;
- `REVIEW_REQUIRED` where automation can no longer safely continue.

UI must not collapse local acceptance, Cloud reconciliation and Google output into one ambiguous green state.

## 4. Stable errors

At minimum:
- 401 authentication failure;
- 403 permission/policy denial;
- 409 version/state/resource/idempotency/reconciliation conflict;
- 422 valid syntax but invalid business input;
- 429 rate/abuse guard;
- 503 required runtime dependency unavailable or incompatible.

Provider adapters return the same business error meaning for the same reviewed vector.

## 5. Cloud authentication/authorization

Protected Cloud routes validate active account/session/device/security state and current effective permission/scope.

DENY precedence and ROOT/SUPERADMIN boundaries follow the decision files. Temporary-password state may access only the required auth-self/change subset until completed.

### ROOT email one-time login — V6

ROOT uses the approved fixed recovery-email channel as its normal one-time-password login path.

- The email one-time password is single-use.
- It is valid for 5 minutes from issuance.
- A replacement cannot be requested until 5 minutes after the previous successful send.
- Successful ROOT login with the one-time password does not create `MUST_CHANGE_PASSWORD`.
- The fixed ROOT email channel cannot be disabled.
- TOTP is optional: if enabled, ROOT authentication must also satisfy TOTP; if disabled, the valid email one-time password is sufficient.
- Credential values and provider secrets must never be logged or persisted in readable form.

### Normal-account forgot-password — V6

For a non-ROOT account with an approved recovery email:

- request a single-use email one-time password;
- 5-minute validity and 5-minute resend cooldown apply;
- successful login with that credential enters restricted `MUST_CHANGE_PASSWORD` state;
- ordinary product functions remain blocked until a new permanent password, different from the one-time password, is established under the normal password policy.

Cloud and LAN implementations must preserve the same credential lifecycle semantics. Offline LAN must never fabricate successful email delivery.

## 6. LAN authentication/authorization

LAN maintains a protected synchronized authority snapshot sufficient for approved local operation.

Offline duration alone does not invalidate login. A disconnected LAN cannot know Cloud-side changes made after its last successful authority synchronization; accepted events therefore record the authority-snapshot version used.

After reconnect, refreshed authority applies to later operations. Previously accepted local business events are not silently deleted or rewritten.

V6 recovery/login semantics remain identical on LAN where the required approved delivery/authority capability is actually available. If email delivery cannot be completed, LAN reports the delivery dependency state rather than issuing a fabricated success.

## 7. Cloud mutation transaction

A successful Cloud business mutation atomically:
1. resolves authenticated actor and effective permission;
2. validates input/business state/version/availability;
3. updates guarded current state;
4. appends one immutable canonical event for the logical mutation;
5. enqueues required downstream work such as projection;
6. returns `CLOUD_COMMITTED` only after durable D1 commit.

Google failure never rolls back an already committed D1 business mutation.

Same idempotency identity + same logical command returns/reconciles the previous accepted result. Same key + different command/payload is conflict.

## 8. LAN autonomous transaction

A successful LAN business mutation atomically:
1. validates the synchronized local auth/permission context;
2. validates local current state/base version/business preconditions;
3. updates local current state;
4. appends one immutable edge event;
5. appends durable Cloud-sync work;
6. appends Google projection/upload work where applicable;
7. returns `LAN_ACCEPTED_PENDING_SYNC` only after local durable commit.

A transport receipt or client queue item alone is not LAN business acceptance.

## 9. LAN with Cloud reachable

Users may remain routed through LAN while LAN synchronizes accepted events to Cloud continuously/opportunistically.

Cloud reachability does not require a client route switch before synchronization begins.

Manual forced LAN while Cloud is healthy is allowed only to SUPERADMIN/ROOT and is audited with reason/scope/time.

## 10. Direct Google from LAN

When Internet/Google is reachable, authorized LAN Service may use the controlled Google integration to:
- project quarterly Sheets output;
- upload reviewed Drive media/documents;
- retain stable projection/logical-file keys and provider receipts.

When Cloud later receives the LAN event, it attaches/recognizes a valid existing receipt instead of duplicating the logical row/file.

Sheets/Drive are never scraped as the source for Cloud reconciliation.

## 11. Reconciliation envelope

A LAN reconciliation item preserves:
- edge event stable identity;
- request/idempotency/device identity;
- edge instance/epoch;
- command/event/entity identity;
- expected/base version;
- normalized payload/hash;
- local acceptance time/order;
- authority-snapshot version;
- compatible app/schema/business-rule version;
- completed Google receipts where any.

Cloud reconciliation:
1. checks whether the same logical command/event is already canonical;
2. validates current canonical state/version if not already committed;
3. commits non-conflicting work through the normal Cloud mutation path;
4. links canonical event identity;
5. attaches valid existing Google receipts;
6. returns explicit conflict evidence if guards cannot be satisfied safely.

No silent last-write-wins and no event deletion.

## 12. Conflict handling

Technical/provider retry, duplicate replay and deterministic non-conflicting reconciliation are automatic.

Unresolved business/data conflict becomes evidence-preserving `SYNC_CONFLICT` for ADMIN+ according to permission/scope. ROOT-security/recovery conflicts remain ROOT-only.

Resolver action creates new canonical correction/resolution evidence; it never rewrites raw event history.

## 13. Core route families

Foundation:
- `GET /health`
- `GET /health/deep`
- `GET /health/integrations`
- `GET /api/v1/meta`
- `GET /api/v1/capabilities`

Auth family:
- login/logout/me/change-password;
- request/use email one-time password;
- ROOT TOTP enable/disable/verify flows under ROOT policy;
- recovery/session audit/status endpoints where approved.

Business families:
- employees/MNV;
- attendance/presence;
- sessions/tasks;
- resources/assign/release/change/reissue/borrow/mapping;
- labor;
- dropped goods;
- documents/media;
- history/conflicts/corrections;
- permission/account administration;
- projection/synchronization status;
- LAN reconciliation ingestion/receipts.

Exact endpoint paths may be versioned/refined, but both runtimes use the same reviewed business contract.

## 14. Functional rules referenced by the contract

Effective business rules are in `docs/OWNER_BUSINESS_RULES_V1.md` plus newer applicable decision overrides. Important examples:
- multiple IN/OUT but one current presence;
- IN does not open work session;
- one MAIN session plus approved EXTRA;
- PICK requires PDA;
- Pack Table -> User Pack 1:n mapping with compatible current selection;
- same-day lock/reissue for User Pick/User Pack/Pack Table, immediate normal PDA reuse;
- one OPEN labor/session;
- cross-cluster labor/resource support;
- DRAFT -> FINAL documents;
- immutable corrections/history.

## 15. Client contract

Website and APK use the same business APIs. APK scanner/device integration produces domain commands, not direct database writes.

Website must have a compatible LAN-local loading path. Both clients understand Cloud commit, LAN pending-sync, Google pending/completed and conflict states.

Both clients expose the applicable V6 one-time-password flow. A normal account in `MUST_CHANGE_PASSWORD` may access only the restricted password-establishment flow until completed; ROOT one-time login does not enter that state.

## 16. Acceptance minimum

Before one business command is BETA-live, evidence must cover:
- authentication/permission/scope;
- V6 one-time-password single-use, 5-minute expiry and 5-minute resend cooldown;
- ROOT login with TOTP enabled and disabled;
- normal-account recovery transition into and out of `MUST_CHANGE_PASSWORD`;
- idempotent retry and device-sequence collision;
- guarded version/state conflict;
- Cloud atomic state+event+async-work commit;
- LAN atomic state+edge-event+sync-work commit;
- Cloud/LAN business-vector parity;
- LAN restart durability;
- reconnect exactly-once reconciliation where non-conflicting;
- explicit conflict evidence where conflicting;
- direct LAN Google deduplication where applicable;
- Google failure without invalidating accepted business state.

Full product acceptance remains `docs/BETA_ACCEPTANCE_MATRIX.md`.
