# CHECKPOINT — VHDCHY

checkpoint_version: 26
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: 58ddcd71f8152ab50138151fa13908d563dc2f02
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V4.md
context_index_ref: CONTEXT_INDEX.md

## Authority

- Fresh resume must live-read `AI_ENTRYPOINT.md`, compare `main` HEAD with this reconciliation point, then read every active decision layer listed by `CONTEXT_INDEX.md`.
- Active decisions remain `DECISIONS.md` + V3 + V4 + V5 + V6; V6 supersedes the stale V5 ROOT-factor/lifetime gate.
- Active LAN/shared contracts: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`, `docs/SERVICE_API_CONTRACT_V3.md`, `docs/LAN_EDGE_STATE_V2.md`, `docs/NON_FUNCTIONAL_BASELINE_V1.md`, `docs/LAN_HOST_DOMAIN_V1.md`, `contracts/commands.slice1.v1.json`, `contracts/acceptance.v1.json`.

## Completed / proven

### Cloud / auth foundation
- Multi-module Worker packaging and protected session boundary are already deployed/proven on BETA; business/admin handlers remain intentionally fail-closed until adapters exist.
- V6 email-OTP source foundation exists; additive D1 migration `0010` is NOT applied to BETA.
- `changePermanentPassword(...)` source exists; public password/OTP route wiring is NOT claimed live because the prior high-level write was platform action-safety blocked.
- Email/SMS provider remains unconfigured; no fabricated delivery and no unreviewed Google mail scope.
- Permission catalog migration `0011` is source-only and NOT applied to BETA.

### Shared domain
- `VHDCHY_DOMAIN_V1` machine contracts are active for first identity/employee/attendance slice.
- Common commit states include `CLOUD_COMMITTED`, `LAN_ACCEPTED_PENDING_SYNC`, `LAN_RECONCILED_CLOUD_COMMITTED`, `QUEUED_CLIENT_LOCAL`, `SYNC_CONFLICT`.
- Shared acceptance vectors enforce idempotency payload conflict, entity-version conflict, permission denial and dependency-unavailable parity.

### LAN L1/L2 durable foundation
- Portable/no-admin .NET 8 LAN runtime and durable SQLite `VHDCHY_EDGE_V2` storage are proven.
- Edge events are immutable; reconciliation state/outboxes/conflicts are separate durable groups.
- Product-foundation run `34785599214`: SUCCESS, including Windows self-contained package.

### LAN synchronized authority snapshot — PASS
- `lan-service/AuthoritySnapshotStore.cs` atomically stages/verifies/activates authority generations and preserves replaced generations.
- Environment/cluster/domain compatibility is checked; same-version/same-evidence replay is idempotent; same-version/different evidence conflicts; invalid/incompatible import preserves the previous ACTIVE generation.
- Exactly one ACTIVE authority generation is enforced.
- Original authority harness run `34785834984`: SUCCESS.

### LAN atomic local-command storage primitive — PASS
- `lan-service/LocalCommandStore.cs` implements the internal durable acceptance primitive without exposing a public mutation route.
- One SQLite transaction performs guarded `module_current_state` transition + exactly one immutable `edge_event` + `edge_reconciliation_state` + `cloud_sync_outbox` + optional Google/Drive outbox work.
- It requires an ACTIVE compatible authority snapshot and records the authority generation on every accepted edge event.
- Same idempotency key + same canonicalized command returns the original result without duplicate state/event/outbox work.
- Same idempotency + different payload => `IDEMPOTENCY_PAYLOAD_CONFLICT`.
- Reused device sequence for a different accepted command => `DEVICE_SEQUENCE_CONFLICT`.
- Wrong/missing base version for an existing entity => `ENTITY_VERSION_CONFLICT`.
- Downstream outbox constraint failure rolls back current state, event and Cloud outbox together.
- Harness/SQL-evidence workflow run `34789053619`: SUCCESS.
- Clean baseline for checkpoint/source state after the primitive, run `34788994794`: SUCCESS.
- This is a storage primitive only; it does NOT replace permission/business-rule evaluation.

### LAN operational snapshot foundation — PASS
- `lan-service/OperationalSnapshotStore.cs` atomically stages/verifies/activates operational generations on the existing EdgeStore.
- Activation requires an ACTIVE authority generation compatible with `VHDCHY_DOMAIN_V1`.
- Snapshot envelope enforces environment, cluster, compatibility, required-module scope, canonicalized scope/state evidence and conflict-safe same-version semantics.
- Invalid JSON, missing required module, wrong environment and incompatible versions do not replace the previous ACTIVE operational generation.
- Exactly one ACTIVE operational generation is enforced; replaced generations are retained.
- Operational snapshot metadata records the authority generation under which activation occurred.
- Import deliberately does NOT set `EDGE_READY`; a later readiness gate must also prove the reviewed domain/authz adapter.
- Harness + independent SQLite postflight + restart/fail-closed run `34789244223`: SUCCESS.
- Clean baseline same HEAD, run `34789244221`: SUCCESS.
- LAN restart sees `AUTH-TEST-2` + `OP-TEST-2` while readiness remains `EDGE_EMPTY` and mutation POST remains 503 `RUNTIME_DEPENDENCY_UNAVAILABLE`.

## Current provider / runtime facts

- GitHub repo: `tamnv2/vanhanhsupradchungyen`, authority branch `main`.
- BETA Worker: `vhdchy-beta`; public origin `https://beta.supra.cc.cd`.
- BETA D1: `vhdchy-data-beta`, ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, marker `business_core_v3`.
- Google Gateway remains projection-oriented/fail-closed; projection is not declared LIVE.
- `0010` and `0011` are not applied to BETA provider.
- LAN public `POST/PUT/PATCH/DELETE /api/v1/**` remains fail-closed; `businessMutationEnabled=false`.
- STABLE remains isolated/dormant for business traffic until BETA PASS + explicit Owner promotion approval.

## Immediate execution

1. Build a fail-closed LAN readiness evaluator that can explain missing prerequisites and cannot become READY from snapshots alone.
2. Build the first reviewed Slice-1 authorization/domain adapter against the shared command contract and synchronized authority evidence; DENY precedence and cluster/module scope must match Cloud semantics.
3. Adapter must validate permission/resource/action and approved command/event vocabulary before delegating to `LocalCommandStore`.
4. Add harness vectors for permission allow/deny precedence, wrong scope, disabled subject, unsupported command, stale/missing readiness dependency and successful authorized storage delegation.
5. Only after readiness + authz/domain adapter acceptance is proven may a reviewed LAN mutation route be considered. Do not open it merely because snapshot/storage layers pass.
6. Continue independent Cloud/Auth/permission/provider lanes only through allowed high-level actions; never bypass platform action-safety.

do_not_repeat:
Do not treat memory as authority. Do not replay provider migrations. Do not claim `0010`/`0011` applied. Do not claim email/SMS delivery live. Do not bypass action-safety. Do not open LAN mutation routes before readiness/authz/domain acceptance. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not promote STABLE without explicit Owner approval.
