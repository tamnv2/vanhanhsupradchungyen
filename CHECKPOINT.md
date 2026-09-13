# CHECKPOINT — VHDCHY

checkpoint_version: 24
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: 70561aa96c6e6dfd7e6fe0d025a5d60c456527ac
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
product_architecture_ref: docs/TARGET_PRODUCT_ARCHITECTURE_V3.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V4.md
release_promotion_ref: docs/RELEASE_PROMOTION_V1.md
lan_host_domain_ref: docs/LAN_HOST_DOMAIN_V1.md
context_index_ref: CONTEXT_INDEX.md
external_project_bootstrap_ref: CHATGPT_PROJECT_BOOTSTRAP.md
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Resume authority state

- Fresh-chat resume must fetch `AI_ENTRYPOINT.md` from GitHub `main` in the current chat; memory/project/chat summaries are NON_AUTHORITY.
- Resume must compare current `main` HEAD with `reconciled_through_commit` before acting.
- All active decision layers V1/V3/V4/V5/V6 remain mandatory resume reads.
- V6 resolves the former ROOT factor/lifetime gate from V5; stale V5 unresolved wording must not reopen it.
- `docs/SERVICE_API_CONTRACT_V3.md`, `docs/LAN_EDGE_STATE_V2.md` and `docs/DELIVERY_PLAN_V4.md` remain the active Cloud/LAN execution contracts.
- Support/current-state files may lag and never override the active decision layers or this checkpoint.

## Owner scope retained through V6

- Website, APK, Cloud Service and full LAN Service are one product and are developed in dependency-aware parallel lanes.
- LAN executes the same approved business contract locally and synchronizes Cloud whenever Cloud is reachable.
- Authorized LAN may write controlled Google outputs directly; Google is never the canonical business source.
- Offline login uses the latest synchronized LAN authority snapshot without duration-only expiry.
- Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy.
- LAN must remain portable/no-admin; canonical LAN URLs are `lan-beta.supra.cc.cd` and `lan.supra.cc.cd`.
- Offline canonical-LAN-domain proof is still a later physical acceptance gate.
- STABLE remains isolated/dormant until explicit Owner promotion approval and never receives BETA operational data as promotion.
- ROOT normal login uses fixed-channel four-digit email OTP, single-use, 5-minute validity and 5-minute resend cooldown; TOTP is optional. Normal recovery OTP requires a different permanent password before ordinary use.

## Cloud C1 — multi-module Worker packaging COMPLETE

- BETA multi-module deployment verification run `34765205078`: SUCCESS.
- Safe dispatcher/no-op run `34765286692`: SUCCESS.
- Worker target remains `vhdchy-beta`; business origin remains `https://beta.supra.cc.cd`.

## Cloud C2 — session boundary DEPLOYED / V6 auth PARTIAL

Runtime proven:
- Bearer session protection is deployed for `/api/v1/data/*` and `/api/v1/admin/*`.
- `GET /api/v1/auth/me` returns principal + effective permission summary.
- `MUST_CHANGE_PASSWORD` sessions are blocked from ordinary product functions.
- Business/admin handlers remain intentionally fail-closed until their adapters are implemented.
- Route validation `34765469156`: SUCCESS.
- ROOT permanent-password contradiction removal `34765560927`: SUCCESS.
- BETA protected-boundary deploy `34765820883`: SUCCESS.
- post-deploy safe no-op `34765885201`: SUCCESS; baseline `34765885241`: SUCCESS.

V6 auth source progress:
- `service/worker/src/email-otp.js` implements four-digit generation, HMAC+pepper verifier storage, 5-minute validity/cooldown, single-use consumption, replay rejection and code-free audit semantics.
- `service/worker/migrations/0010_auth_v6_email_otp.sql` exists but is NOT applied to BETA D1.
- `service/worker/src/auth-service.js` now contains `changePermanentPassword(...)`; commit `e177df67ee5e7f34cdfa34e5281334fcbf641c47`, baseline run `34784873868`: SUCCESS.
- Password-login/logout/change-password/OTP public route wiring attempted through the high-level file action was blocked by platform action-safety and was NOT bypassed. Those routes therefore are not claimed live.
- Email/SMS delivery provider remains unconfigured; no fabricated delivery success and no Google mail scope was added.

## Additive D1 migration 0010 — WORKFLOW READY / PROVIDER APPLY BLOCKED

- New fixed dispatcher: `.github/dispatch/cloudflare-beta-additive-0010.json`, default `enabled:false`.
- New fail-closed workflow: `.github/workflows/cloudflare-beta-additive-0010.yml`.
- Workflow locks exact BETA account/D1 identity, current `business_core_v3`, exact migration blob SHA, rejects replay, checks exact table delta/row invariance/indexes/FK/quick-check.
- Safe-idle workflow run `34784776363`: SUCCESS with provider mutation steps skipped.
- Baseline run `34784776358`: SUCCESS.
- Attempt to change the dispatcher to `enabled:true` was blocked by platform action-safety. This was NOT bypassed using lower-level Git operations.
- Therefore BETA D1 still does NOT contain `auth_email_otp_challenges` from migration `0010`; no provider mutation occurred from this lane.

## Shared domain D1/D2/D3 foundation — MACHINE CONTRACT ACTIVE

Added and CI-enforced:
- `contracts/commands.slice1.v1.json`: first vertical slice command specification for employee/identity/attendance, including permission references, event intent, version semantics and LAN reconciliation meaning.
- `contracts/acceptance.v1.json`: runtime-neutral Cloud/LAN acceptance vectors for commit state, Google state, idempotency, version conflict, permission denial and dependency failure.
- `contracts/mutation-result.v1.schema.json`: common successful mutation response shape with exact domain `commitStatus` and `googleOutputStatus` enums.
- `.github/scripts/domain-parity-test.mjs`: validates command/event catalogs, permission references, mutation-result enums and acceptance vectors.
- `service/worker/migrations/0011_permission_catalog_v1.sql`: source-only D1 seed for the 38-item dynamic permission catalog required by V5; parity test enforces exact set equality with `config/permissions.v1.json`.
- Migration `0011` is NOT applied to BETA provider yet.
- Clean-D1/baseline validations remained green, including runs `34785286434`, `34785335489`, `34785410740`, `34785546550` and `34785599207`.

## LAN L1 — PORTABLE RUNTIME FOUNDATION COMPLETE

- .NET 8 portable/no-admin LAN Service builds from `lan-service/`.
- Website is served from the package-local `wwwroot` resolved from `AppContext.BaseDirectory`, so launch working-directory no longer controls static hosting.
- The earlier 404/portable-WebRoot defect and subsequent builder-configuration defect were isolated and fixed; no stale failed run should be treated as current state.
- `VHDCHY_LAN_DATA_ROOT` supports explicit local-state placement for controlled runs while the default remains user-local application storage.
- BETA/STABLE identity collision on the same data root fails closed.
- Product foundation run `34785599214`: SUCCESS for Web contract, LAN build/smoke, self-contained Windows publish and artifact upload.
- Artifacts from that run:
  - `web-foundation`, digest `sha256:a19f8cfaac40f016b5d0647e660977e0997a8de50010662d55b0563e6b0b7a03`;
  - `lan-service-foundation`, digest `sha256:9f3c87815de1dfa2fbb7a50a464a1e724552525ff09c186381585266300f43ee`.
- Physical company-LAN/domain proof remains pending and is not implied by CI.

## LAN L2 — DURABLE EDGE STORAGE FOUNDATION PASS / BUSINESS ACCEPTANCE STILL CLOSED

`lan-service/EdgeStore.cs` now initializes SQLite `VHDCHY_EDGE_V2` with durable groups for:
- edge metadata/environment/cluster identity;
- authority snapshots;
- operational snapshots;
- module current state;
- immutable edge events;
- separate reconciliation state;
- Cloud sync outbox;
- Google projection outbox;
- Drive upload outbox/staged file references;
- integration receipts;
- explicit conflict storage.

Hardening/proof:
- `edge_events` blocks both UPDATE and DELETE; mutable reconciliation linkage is stored separately.
- SQLite uses foreign keys, WAL and bounded busy timeout.
- LAN startup initializes the store and fails fast on integrity failure.
- `/health`, `/api/v1/meta` and `/api/v1/sync/status` read actual durable-store state.
- smoke test verifies DB/table/trigger existence, `PRAGMA quick_check`, `foreign_key_check`, empty event baseline, BETA identity, restart persistence and STABLE-on-BETA-root rejection.
- `businessMutationEnabled` remains `false`; all mutation routes still return `RUNTIME_DEPENDENCY_UNAVAILABLE`.
- Product foundation run `34785599214`: SUCCESS, including durable-store smoke and Windows package publish.

Therefore L2 persistence foundation is proven, but L2/domain business acceptance is NOT complete until synchronized authority import + shared domain transaction adapter are implemented and tested.

## Provider/source state

- BETA D1: `vhdchy-data-beta`, database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, schema marker `business_core_v3`.
- Google Gateway remains projection-oriented/fail-closed; projection is not declared LIVE.
- Worker deploy dispatcher remains safe/false after reviewed deployment.
- Retired V1→V3 D1 migration path remains guarded and must never be replayed.
- `0010` and `0011` are additive source migrations not yet applied to BETA D1.
- No reviewed email or SMS provider is active for V6 recovery delivery.

## Immediate next execution

1. LAN: implement synchronized authority-snapshot import/activation and its compatibility/identity checks; keep mutations closed until authority proof exists.
2. LAN/Shared Domain: implement the first atomic local command transaction primitive: validate authority/version/idempotency -> update current state -> append immutable edge event -> append reconciliation/outbox work; execute shared acceptance vectors before opening any route.
3. Cloud/Auth: retry only allowed high-level paths for additive `0010` activation and auth route wiring when platform action-safety permits; never bypass the guard.
4. Cloud/Authz: prepare a reviewed additive apply path for `0011_permission_catalog_v1.sql`; do not imply provider catalog exists until postflight evidence passes.
5. Web/APK: continue shells against the machine-readable shared command/result/auth contracts; do not hard-code permission catalogs.
6. Continue isolated STABLE preparation and Google sender/Drive lanes where dependencies permit.
7. Physical LAN/domain regression remains deferred only until the intended company environment is available.

## do_not_repeat:

Do not use memory/project/chat summaries as project authority. Do not resume without live-fetching `AI_ENTRYPOINT.md` and comparing HEAD with this checkpoint. Do not reopen V6-resolved ROOT decisions. Do not make ROOT depend on a permanent password. Do not persist/log readable OTP values. Do not fabricate email/SMS delivery. Do not claim migrations `0010` or `0011` are applied to BETA. Do not bypass platform action-safety with lower-level Git/provider operations. Do not replay the retired V1→V3 migration. Do not open LAN business mutation before synchronized authority + shared domain transaction tests pass. Do not mutate raw edge events; reconciliation state is separate. Do not treat CI LAN proof as physical company-LAN/domain proof. Do not make Google the business source. Do not silently last-write-wins split-brain conflicts. Do not copy BETA runtime data into STABLE. Do not promote STABLE before explicit Owner approval. Do not promote accidental latest `main` instead of the exact accepted BETA release.
