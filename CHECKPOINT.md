# CHECKPOINT — VHDCHY

checkpoint_version: 55
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 9f2f7682f43215186b2b21c67683299b57be1ecc
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB_ONLINE_LAN / ANDROID_PDA / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
termination_guard_ref: AI_TERMINATION_GUARD.md
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
current_state_ref: CURRENT_STATE.md
next_actions_ref: NEXT_ACTIONS.md
context_index_ref: CONTEXT_INDEX.md

## Progress

- Evidence-weighted total: **56.2% exact / 56% displayed**.
- Phase 6 LAN continuity/offline/reconcile: **65%**.
- No weighted percentage increase is claimed from governance or source-only Android work.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare main HEAD with this checkpoint reconciliation point.
3. Read changed authority/current-state/source paths after `9f2f7682f43215186b2b21c67683299b57be1ecc` before mutation.
4. Follow current `NEXT_ACTIONS.md`; primary ready source lane is Android endpoint acquisition unless later main evidence supersedes it.

Memory/chat summaries are NON_AUTHORITY.

## Latest accepted main evidence

### Projection — LIVE BETA PASS

- isolated projection live E2E `34927511443`: SUCCESS;
- cleaned BETA deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- active Worker handlers `fetch` + `scheduled`; Cron exactly `*/2 * * * *`.

### Web employee-create — MERGED PASS

- PR #19 merged at `db746a71ed80dafd288218f599ab3e990f87e439`;
- post-merge clean baseline `34928090928`: SUCCESS;
- product foundations `34928090885`: Cloud/Web/Android/LAN SUCCESS.

### V6 email OTP source — MERGED SOURCE/CI PASS

PR #30 merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

- PR clean baseline `34930120980`: SUCCESS;
- post-merge clean baseline `34930171765`: SUCCESS;
- post-merge product foundations `34930171683`: Cloud/Web/Android/LAN SUCCESS.

Accepted source behavior:

- fixed email-OTP request/use Cloud HTTP paths;
- normal recovery uses registered email and creates restricted `MUST_CHANGE_PASSWORD` session;
- ROOT runtime-only recovery email must hash-match active allowlist;
- 4-digit, 5-minute validity/cooldown, single-use lifecycle retained;
- runtime delivery binding is injected and fail-closed when missing;
- active verified ROOT TOTP blocks completion before OTP consumption unless trusted server-side TOTP verification has already satisfied the factor;
- HTTP clients cannot self-assert TOTP success.

Not live-PASS:

- auth slice was not deployed to BETA because approved OTP runtime provider/bindings/secrets are not provisioned;
- real ROOT/normal email request/delivery/use E2E remains pending;
- current repo has no trusted TOTP verifier and authority does not lock enough operational verifier detail to invent one safely.

## Active Android source lane — IN PROGRESS / UNVERIFIED

Branch: `ai/android-endpoint-acquisition-20260915`, based on main `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

Current main Android transport foundation already has:

- HTTPS-only Cloud/LAN endpoint normalization;
- in-memory session expiry;
- bounded idempotent retry/backoff;
- same-runtime reconnect decisions;
- scanner normalization;
- authenticated request planning.

Phase 6.2 locks reusable ordering:

`cached healthy endpoint -> LAN discovery -> manual recovery`

Unverified branch work currently adds:

- package-private shared HTTPS endpoint normalization;
- pure-Java `LanEndpointAcquisitionPolicy`;
- cache-first health-probed selection to avoid discovery flapping;
- invalid/unhealthy cache fallback to discovery;
- untrusted invalid discovery candidate rejection;
- manual endpoint only after cache/discovery fail;
- no Cloud authority fallback input;
- no fabricated selection when every health probe fails;
- `EndpointAcquisitionHarness` wired into Android `preBuild`.

This Android branch has not yet been reconciled to the governance merge, opened as a PR or validated by CI. Do not report it PASS yet.

## Current READY queue

1. Merge this governance reconciliation after clean-baseline PASS.
2. Reconcile `ai/android-endpoint-acquisition-20260915` onto the resulting main without losing its intended Android-only diff.
3. Update checkpoint last on the Android branch, open PR and require clean baseline + Android product foundation PASS.
4. Merge exact tested Android head if clean; run post-merge evidence and reconcile governance.
5. Continue next Android mechanics (session persistence/expiry, scanner command handoff, reconnect/resync, HTTPS/trust, lifecycle) where authority is sufficient.
6. Keep OTP provider/TOTP-verifier, physical, STABLE and portrait gates isolated.

## Current blockers / gates

- real ROOT/normal email-OTP provider delivery/request/use E2E requires reviewed runtime provider/bindings/secrets;
- ROOT TOTP verifier crypto/runtime detail is not sufficiently locked to invent safely;
- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance require physical environment;
- final Android/PDA visual fidelity requires authorized Pick Pack reference evidence;
- portrait replacement remains `OWNER_DECISION_REQUIRED`;
- STABLE promotion requires explicit Owner approval after mandatory BETA acceptance.

## do_not_repeat

do_not_repeat:
Do not treat memory as authority. Do not replay migrations 0009 or 0014. Do not expose or infer secret values. Do not invent OTP failure-attempt limits. Do not treat TOTP as an alternative to email OTP when active. Do not invent TOTP algorithm/digits/window/secret resolution without authority. Do not deploy/claim OTP provider PASS without provider evidence. Do not reopen solved Cron incident without new failure evidence. Do not count unverified Android branch work as PASS. Do not invent Pick Pack UI details. Do not inflate progress without weighted acceptance evidence. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
