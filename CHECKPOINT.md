# CHECKPOINT — VHDCHY

checkpoint_version: 59
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
action_mode: AUTONOMOUS_PARALLEL
reconciled_through_commit: 14715e55ee6996422b3aeeb3ca1849d7d2733e55
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

- Evidence-weighted total remains **56.2% exact / 56% displayed**.
- Phase 6 LAN continuity/offline/reconcile remains **65%**.
- Android endpoint acquisition + secure session persistence are accepted source/CI PASS, but broader App workflow/UI/physical gates remain open, so no weighted phase threshold moves.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare current main HEAD with this checkpoint reconciliation point.
3. Read changed authority/current-state/source paths after `14715e55ee6996422b3aeeb3ca1849d7d2733e55` before mutation.
4. Follow current `NEXT_ACTIONS.md`; primary ready source lane is Android scanner -> attendance domain command handoff unless later main evidence supersedes it.

Memory/chat summaries are NON_AUTHORITY.

## Latest accepted main evidence

### Android secure session persistence — MERGED SOURCE/CI PASS

PR #34 merged at `ebfb7d238268fb089a34779e2c5d53ae45693608`.

- PR clean baseline `34933869114`: SUCCESS;
- PR Android foundation `34933869144`: SUCCESS;
- transport harness `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=66` with `sessionRestore=PASS`;
- Android `assembleDebug`: SUCCESS;
- post-merge clean baseline `34933981747`: SUCCESS;
- post-merge product foundations `34933981735`: Cloud/Web/Android/LAN SUCCESS.

Accepted semantics:

- persistence snapshot carries only bearer token, expiry and original runtime mode;
- snapshot text output redacts bearer material;
- exact-expiry/null/invalid restore clears prior state and fails closed;
- Android Keystore AES-GCM encrypts persisted session evidence;
- app-private storage contains format version, IV and ciphertext only;
- replacement save clears old persisted state first;
- missing/invalid original key is never silently replaced during restore;
- corruption/decrypt failure/unknown runtime/expiry clears state and fails closed;
- raw password, email OTP, TOTP secret/code and provider credentials are never persisted;
- restore preserves original runtime authority and performs no discovery/fallback.

### Android endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`; pre/post-merge clean baselines and Android/product foundations are SUCCESS.

### Retained accepted evidence

- Projection live E2E `34927511443`, cleaned deploy `34927869845`, observer `34927996370`: SUCCESS.
- Web employee-create PR #19 merged; post-merge clean/product foundations: SUCCESS.
- V6 email-OTP PR #30 source/CI PASS; provider live delivery remains gated.
- LAN refresh/rebase integration `34851773729` and clean baseline `34851772963`: SUCCESS.

## Current primary READY — Android scanner -> attendance domain command handoff

Current Android only normalizes scanner text; there is no current-product command handoff.

Authority boundary:

- first slice is only `ATTENDANCE_IN` / `ATTENDANCE_OUT`, because they exist in the active Slice-1 command contract;
- normalized scanner value is business input, not actor authority;
- actor/permission identity comes from authenticated Service context only;
- same logical command identity must survive Cloud/LAN route selection;
- Android scanner layer must never write D1/SQLite/Sheet/Drive directly;
- command planner must reject unsupported command codes, invalid scan values and malformed client command identity before dispatch.

Next implementation sequence:

1. identify the exact current Service/LAN command envelope/route source used by Slice-1;
2. build a pure-Java attendance scan command plan compatible with that envelope;
3. add harness vectors for IN/OUT, normalization, stable identity across route planning, invalid command rejection and no client actor fields;
4. checkpoint last;
5. require clean baseline + Android PR foundation before exact-head merge;
6. post-merge evidence + governance reconciliation.

## Following READY queue

- reconnect/resync/network lifecycle;
- HTTPS/trust fail-closed behavior;
- foreground/background recovery/stale-session handling;
- independent safe Web/integration work where current contracts are sufficient.

## Current blockers / gates

- real ROOT/normal email-OTP provider E2E requires reviewed provider configuration/secrets;
- ROOT TOTP verifier technical contract is insufficiently specified for safe implementation;
- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance require physical environment;
- final Android/PDA visual fidelity requires authorized Pick Pack reference evidence;
- portrait replacement remains `OWNER_DECISION_REQUIRED`;
- STABLE promotion requires explicit Owner approval after mandatory BETA acceptance.

## do_not_repeat

do_not_repeat:
Do not treat memory as authority. Do not replay migrations 0009 or 0014. Do not expose or infer secret values. Do not invent TOTP verifier parameters or OTP failure-attempt limits. Do not persist raw password/OTP/TOTP/provider secrets. Do not restore expired/corrupt session state. Do not regenerate command identity merely because Cloud/LAN route changes. Do not put actor authority in scanner payloads. Do not write directly from Android scanner code to database/Google. Do not invent unsupported Pick/Pack command contracts or final Pick Pack UI details. Do not inflate progress without weighted acceptance evidence. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
