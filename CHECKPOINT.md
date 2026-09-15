# CHECKPOINT — VHDCHY

checkpoint_version: 54
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 75b4c467ff4bfe41e883beefe405521f32fd1567
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
- No progress increase is claimed from the current auth source slice before CI/provider/acceptance evidence.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare main HEAD with this checkpoint reconciliation point.
3. Read changed authority/current-state/source paths after `75b4c467ff4bfe41e883beefe405521f32fd1567` before mutation.
4. Continue the V6 email-OTP HTTP slice if it remains open; otherwise follow current `NEXT_ACTIONS.md`.

Memory/chat summaries are NON_AUTHORITY.

## Latest accepted main evidence

- Governance PR #29 merged at `e46325d762ce792200e574aace824bb865b9c94b`; post-merge clean baseline `34929584409`: SUCCESS.
- Projection live E2E `34927511443`: SUCCESS; cleaned BETA deploy `34927869845`: SUCCESS; post-deploy Cron observer `34927996370`: SUCCESS.
- Web employee-create PR #19 merged at `db746a71ed80dafd288218f599ab3e990f87e439`; post-merge clean baseline `34928090928` and product foundations `34928090885`: SUCCESS.
- Issue #8 has been synchronized to main `e46325d762ce792200e574aace824bb865b9c94b`.

## Active AUTH V6 source slice — IN PROGRESS / NOT YET PASS

Branch: `ai/auth-v6-email-otp-http-20260915`.

Authority correction already made in `NEXT_ACTIONS.md`:

- do not invent an OTP failure-attempt limit; V6 does not define one;
- ROOT TOTP is optional, but when an active verified TOTP is enrolled, ROOT authentication must satisfy TOTP in addition to the email OTP.

Implemented on the branch through `75b4c467ff4bfe41e883beefe405521f32fd1567`:

- new `service/worker/src/email-otp-auth.js` composition service;
- normal-account recovery uses the registered `auth_users.email`;
- ROOT actual recovery email remains runtime-only and must hash-match an ACTIVE `root_recovery_allowlist` entry before issuance;
- fixed OTP request/use HTTP paths are defined;
- runtime email delivery is an injected binding and fails closed when absent;
- OTP remains four digits, 5-minute validity, 5-minute resend cooldown and single-use through the existing V6 OTP core;
- normal OTP login creates `MUST_CHANGE_PASSWORD` session state;
- ROOT without active TOTP may use email OTP alone;
- ROOT with active verified TOTP returns `ROOT_TOTP_REQUIRED` before OTP consumption unless trusted server-side TOTP verification has already succeeded;
- HTTP request body cannot assert its own TOTP success;
- deploy module manifest packages `email-otp-auth.js` but does not provision OTP secrets/provider bindings;
- new unit tests cover normal request, ROOT allowlist, normal/ROOT use, TOTP gate and delivery adapter behavior.

This branch has not yet passed CI and has not been merged or deployed. Real email provider delivery remains a separate gate.

## Current READY queue

1. Open/auth PR from `ai/auth-v6-email-otp-http-20260915`, run clean baseline, diagnose/fix until green, then merge exact tested head if clean.
2. Verify post-merge main CI/product foundations as triggered.
3. Reconcile auth state/governance and identify the next provider-independent V6 gap, including the trusted TOTP verification path.
4. Continue Android endpoint/session/scanner/retry/HTTPS/reconnect mechanics in parallel where independent.
5. Keep physical/STABLE/portrait gates isolated.

## Current blockers / gates

- Real ROOT/normal email-OTP delivery E2E requires approved runtime delivery provider/bindings and secrets; do not fake provider PASS.
- Physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance require the physical environment.
- Android/PDA final visual fidelity requires authorized Pick Pack reference evidence.
- Portrait replacement semantics remain `OWNER_DECISION_REQUIRED`.
- STABLE promotion requires explicit Owner approval after mandatory BETA acceptance.

## do_not_repeat

do_not_repeat:
Do not treat memory as authority. Do not replay migrations 0009 or 0014. Do not expose or infer secret values. Do not invent OTP failure-attempt limits. Do not treat TOTP as an alternative to email OTP when TOTP is actively enrolled for ROOT. Do not let a client-supplied flag bypass TOTP verification. Do not provision or deploy unverified OTP provider secrets as part of this source-only slice. Do not reopen the solved Cron incident without new failing evidence. Do not inflate progress without acceptance-backed weighted evidence. Do not invent Pick Pack UI details. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
