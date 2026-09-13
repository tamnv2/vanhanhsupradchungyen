# CHECKPOINT — VHDCHY

checkpoint_version: 23
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: 27bd7552638c7bad8cbf1ab59fde8ae7b5c0ef5d
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
- All active decision layers V1/V3/V4/V5/V6 are mandatory resume reads regardless of what an older checkpoint listed.
- V6 resolves the former ROOT factor/lifetime decision gate from V5.
- `docs/SERVICE_API_CONTRACT_V3.md` and `docs/DELIVERY_PLAN_V4.md` are the current implementation contract/plan for Cloud/LAN/auth work.
- Older current-state/support documents may contain stale wording and never override the active decision layers or this checkpoint.
- `CHATGPT_PROJECT_BOOTSTRAP.md` records the external ChatGPT Project instruction required to make fresh-chat live GitHub bootstrap deterministic.

## Owner scope locked through V6

- Website, APK, Cloud Service and full LAN Service are one product and are developed in parallel.
- LAN executes the same approved business model locally and synchronizes Cloud whenever Cloud is reachable.
- Authorized LAN may write controlled Google outputs directly when Google is reachable; Cloud reconciliation consumes LAN event/outbox records, not Google as business source.
- Offline login uses the latest synchronized LAN authority snapshot without duration-only expiry; refreshed information applies prospectively after reconnect.
- Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy.
- Unresolved business/data conflicts are escalated to ADMIN+ only after automatic retry/deduplication/reconciliation.
- LAN host must remain portable/no-admin and compatible with restricted company-laptop operation.
- Canonical LAN URLs are `lan-beta.supra.cc.cd` and `lan.supra.cc.cd`.
- Offline use of the canonical LAN domain is a required technical/physical acceptance gate; LAN IP/discovery is fallback, not intended normal UX.
- STABLE infrastructure remains dormant/fail-closed for business traffic until explicit Owner promotion approval.
- STABLE promotion uses the exact accepted BETA release and never copies BETA operational data into STABLE.
- Current authentication authority is `DECISIONS_V6.md`: ROOT primary login is fixed-channel email OTP; OTP is exactly four digits, single-use, 5-minute validity, 5-minute resend cooldown; ROOT TOTP is optional; normal recovery OTP requires post-login password change.

## Cloud C1 — multi-module packaging COMPLETE

- Worker deploy workflow was corrected so `enabled:false` is a successful no-op and provider steps execute only when explicitly enabled.
- The prior false failure was isolated to the public-health verifier using top-level `await` under CommonJS stdin; provider upload/postflight had already succeeded.
- Health verifier now runs as ESM and retains bounded propagation retry.
- BETA multi-module deployment verification run `34765205078`: SUCCESS.
- Dispatcher was returned to `false`; safe no-op run `34765286692`: SUCCESS.
- C1 is considered proven complete.

## Cloud C2 — protected auth boundary DEPLOYED / V6 auth PARTIAL

Source/runtime completed:
- `service/worker/src/index.js` now enforces Bearer session authentication for `/api/v1/data/*` and `/api/v1/admin/*`.
- `GET /api/v1/auth/me` returns authenticated principal plus current effective permission summary.
- `MUST_CHANGE_PASSWORD` sessions are blocked from ordinary product functions.
- Business/admin handlers remain intentionally fail-closed with `ROUTE_NOT_IMPLEMENTED`; authentication did not implicitly open business data.
- Route-boundary validation run `34765469156`: SUCCESS.
- ROOT password-flow contradiction was removed: ROOT no longer depends on a permanent password and the password path returns `ROOT_EMAIL_OTP_REQUIRED`; validation run `34765560927`: SUCCESS.
- C2 boundary deployment run `34765820883`: SUCCESS through provider preflight, upload, postflight and public health.
- BETA deploy dispatcher is `false`; post-deploy no-op run `34765885201`: SUCCESS and baseline run `34765885241`: SUCCESS.

V6 email OTP source foundation completed but not live:
- migration source `service/worker/migrations/0010_auth_v6_email_otp.sql` exists;
- `service/worker/src/email-otp.js` implements 4-digit generation, HMAC+pepper storage, 5-minute validity/cooldown, `REQUESTED/ISSUED/USED/EXPIRED/SUPERSEDED` lifecycle, atomic single-use consumption, replay rejection and code-free audit evidence;
- missing delivery adapter fails closed before DB mutation;
- OTP unit/schema validation run `34765766190`: SUCCESS;
- migration `0010` has NOT been applied to BETA D1;
- no OTP request/use public controller is live;
- no approved email or SMS delivery provider/secret/destination has been activated by this implementation.

Therefore C2 is PARTIAL, not complete. Session protection is live; V6 recovery/ROOT delivery and end-to-end login are not yet operational.

## Provider/source state retained

- BETA Worker target: `vhdchy-beta`; public origin: `https://beta.supra.cc.cd`; `workers.dev` expected disabled.
- BETA D1: `vhdchy-data-beta`, database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, schema marker `business_core_v3`.
- BETA Worker public health remained PASS after C2 deployment.
- Google Gateway remains projection-oriented/fail-closed; projection is not declared LIVE by this checkpoint.
- Google Gateway currently has no reviewed email-delivery implementation/scope for V6 OTP.
- SMS recovery/backup remains an authority requirement from V5 but no reviewed provider is configured yet.
- Worker deploy dispatcher `.github/dispatch/cloudflare-beta-deploy-v2.json` is `enabled:false`.
- The original one-shot D1 `business_core_v1 -> business_core_v3` migration path is retired and must never be replayed.
- `.github/workflows/cloudflare-beta-migrate.yml` is now a fail-closed legacy guard rather than a mutation workflow.
- `.github/dispatch/cloudflare-beta-migrate.json` is `enabled:false`; guard run `34765937456`: SUCCESS; baseline run `34765937403`: SUCCESS.
- Any later D1 change, including migration `0010`, requires a separately reviewed additive migration workflow with explicit pre/post evidence.
- No D1 mutation occurred while retiring the legacy migration path.

## Governance / validation state

- Live GitHub bootstrap and HEAD-vs-checkpoint reconciliation remain mandatory before fresh-chat project work.
- C1/C2 work used fixed dispatch files, provider preflight/postflight, bounded verification, and returned dispatchers to safe idle states.
- Main validation remained green through reconciled commit `27bd7552638c7bad8cbf1ab59fde8ae7b5c0ef5d`.
- Physical company-LAN/domain regression remains deferred until the intended company environment is available.

## Immediate next execution

1. Build a new additive BETA D1 migration path for `0010_auth_v6_email_otp.sql`; require current `business_core_v3`, exact BETA D1 identity, pre/post table/index evidence and fail-closed dispatcher state. Do not reuse the retired V1→V3 workflow.
2. Implement the remaining V6 auth service/controller path in dependency-safe order: normal password login, ROOT/recovery OTP request/use orchestration, session issuance, logout, change-password, optional ROOT TOTP composition, and abuse/rate guards.
3. Keep OTP request delivery fail-closed until an approved email provider/channel and secret handling path exists; do not add Google mail scope merely to make tests pass.
4. Resolve the required SMS backup provider/channel separately; do not silently drop the V5 requirement.
5. Continue shared domain, LAN full-service adapters, Web/APK auth UI and isolated STABLE preparation in parallel where dependencies permit.
6. Reconcile stale support/current-state documents when modifying them is useful; never let stale wording override V6.
7. Physical LAN/domain regression remains deferred only until the intended company environment is available.

## do_not_repeat:

Do not use memory/project/chat summaries as project authority. Do not resume a fresh chat without fetching `AI_ENTRYPOINT.md` and comparing GitHub HEAD with the checkpoint. Do not omit V5/V6 because an older checkpoint failed to list them. Do not reopen V6-resolved ROOT factor/lifetime decisions from stale V5 text. Do not make ROOT depend on a permanent password. Do not persist/log readable OTP values. Do not fabricate successful email/SMS delivery. Do not replay the retired V1→V3 D1 migration. Do not leave provider dispatchers enabled after reviewed work. Do not treat legacy repo or transport-only prototype as product authority. Do not build LAN as only relay/discovery. Do not make Google outputs the business source. Do not silently overwrite split-brain conflicts. Do not claim offline LAN domain PASS before physical evidence. Do not clone/rename BETA into STABLE. Do not copy BETA business/runtime data into STABLE. Do not promote/activate STABLE business traffic before explicit Owner approval. Do not promote accidental latest `main` instead of the exact accepted BETA release. Do not bypass platform action-safety guards.
