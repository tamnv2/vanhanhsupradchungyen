# CHECKPOINT — VHDCHY

checkpoint_version: 25
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: c79ae02bf524c078f42e0340f8b12f265681a44c
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

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

### LAN L1/L2 storage
- Portable/no-admin .NET 8 LAN runtime foundation and durable SQLite `VHDCHY_EDGE_V2` storage are proven.
- Edge events are immutable; reconciliation state/outboxes/conflicts are separate durable groups.
- `businessMutationEnabled=false`; public mutation routes still return fail-closed `RUNTIME_DEPENDENCY_UNAVAILABLE`.
- Product-foundation run `34785599214`: SUCCESS, including Windows self-contained package.

### LAN synchronized authority snapshot — PASS
- `lan-service/AuthoritySnapshotStore.cs` imports verified authority generations atomically into the existing EdgeStore.
- It checks environment, cluster and `VHDCHY_DOMAIN_V1` compatibility; same-version/same-evidence replay is idempotent; same-version/different evidence conflicts; partial/invalid/incompatible imports do not replace the previous active generation.
- Only one ACTIVE authority snapshot is allowed; replaced generations remain retained.
- Every later LAN business event is required to record the active authority version used.
- Harness run `34785834984`: SUCCESS for activation, replay, conflict, rollback, compatibility and restart persistence.
- Clean baseline for the same HEAD `c79ae02bf524c078f42e0340f8b12f265681a44c`, run `34785835026`: SUCCESS.
- Authority presence alone does not make the LAN business runtime READY; operational state + shared domain transaction proof are still required.

## Current provider / runtime facts

- GitHub repo: `tamnv2/vanhanhsupradchungyen`, authority branch `main`.
- BETA Worker: `vhdchy-beta`; public origin `https://beta.supra.cc.cd`.
- BETA D1: `vhdchy-data-beta`, ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, marker `business_core_v3`.
- Google Gateway remains projection-oriented/fail-closed; projection is not declared LIVE.
- `0010` and `0011` are not applied to BETA provider.
- STABLE remains isolated/dormant for business traffic until BETA PASS + explicit Owner promotion approval.

## Immediate execution

1. LAN/shared domain: implement an internal atomic local-command transaction primitive without opening a public mutation route.
2. Primitive must require a compatible ACTIVE authority snapshot, enforce idempotency/device/entity-version guards, update `module_current_state`, append exactly one immutable `edge_event`, append `edge_reconciliation_state` + `cloud_sync_outbox`, and append optional Google/Drive outbox work in one SQLite transaction.
3. Same idempotency + same normalized command returns the original acceptance; different payload conflicts; reused device sequence with a different logical command conflicts; any downstream insert failure rolls the whole local acceptance back.
4. Add harness acceptance vectors against the real EdgeStore. Keep tests synthetic where business rules are not yet implemented; do not invent new business fields/rules.
5. Only after authority + operational snapshot + shared local transaction acceptance pass may a reviewed LAN business route be enabled.
6. Continue independent Cloud/Auth/permission/provider lanes only through allowed high-level actions; never bypass platform action-safety.

## do_not_repeat

Do not treat memory as authority. Do not replay provider migrations. Do not claim `0010`/`0011` applied. Do not claim email/SMS delivery live. Do not bypass action-safety. Do not open LAN mutation routes before readiness/transaction acceptance. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not promote STABLE without explicit Owner approval.
