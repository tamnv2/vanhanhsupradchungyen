# CHECKPOINT — VHDCHY

checkpoint_version: 58
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
action_mode: AUTONOMOUS_PARALLEL
reconciled_through_commit: 12b9d7904106289af5b611d921e336e464022e9d
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
- Android endpoint acquisition is accepted source/CI PASS; session persistence is IN PROGRESS and unverified until PR CI/build evidence succeeds.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare main HEAD against this checkpoint reconciliation point.
3. Read changed authority/current-state/source paths after `12b9d7904106289af5b611d921e336e464022e9d` before mutation.
4. If the Android session-persistence PR remains open, continue it; otherwise follow current `NEXT_ACTIONS.md`.

Memory/chat summaries are NON_AUTHORITY.

## Latest accepted main evidence

- Governance PR #33 merged at `54d623ad50be7f930255d41a618f6da7cecf6298`; checkpoint v57/current state reflect Android endpoint acquisition PASS.
- Issue #8 is synchronized to main `54d623ad50be7f930255d41a618f6da7cecf6298`.
- Android endpoint PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`; pre-merge clean `34931008972`, Android foundation `34931008973`, post-merge clean `34933290109`, product foundations `34933290105`: SUCCESS.
- Projection live E2E `34927511443`, cleaned deploy `34927869845`, observer `34927996370`: SUCCESS.
- V6 email-OTP PR #30 merged/source-CI PASS; live provider delivery remains gated.

## Active Android session persistence lane — IN PROGRESS / NOT YET PASS

Branch: `ai/android-session-persistence-20260915`, based on accepted/reconciled main `54d623ad50be7f930255d41a618f6da7cecf6298`.

Implemented through `12b9d7904106289af5b611d921e336e464022e9d`:

- `PdaSession` now has a persistence-safe in-memory `Snapshot` carrying only bearer token, expiry and original runtime mode;
- snapshot is allowed only for a currently usable session;
- snapshot `toString()` redacts the bearer token;
- restore clears any existing in-memory session first, rejects null/expired/invalid snapshots and preserves the original runtime mode only on valid restore;
- exact expiry boundary is rejected (`expiresAt <= now`);
- transport harness now tests restore-before-expiry, exact-expiry rejection, prior-session clearing on failed restore, null snapshot behavior, runtime preservation and snapshot redaction;
- Android-specific `AndroidPdaSessionStore` persists only encrypted bearer-session evidence using Android Keystore AES-GCM;
- app-private `SharedPreferences` stores only format version, IV and ciphertext;
- save removes any previous persisted session first so a failed replacement cannot leave an older bearer blob behind;
- restore never creates a replacement key when the prior Keystore key is missing;
- missing key, malformed format, corruption, decrypt failure, unknown runtime mode, invalid token or expiry causes persisted state + target session to clear and fail closed;
- passwords, email OTPs, TOTP secrets/codes and provider credentials are outside the store contract;
- Android manifest already has `android:allowBackup="false"`, so the private ciphertext prefs are not exported through app backup;
- restore has no endpoint discovery/fallback logic and cannot switch Cloud/LAN authority.

This source has not yet passed clean baseline or Android PR foundation build. Do not report it PASS before both succeed.

## Current READY queue

1. Open a focused PR from `ai/android-session-persistence-20260915`.
2. Require clean baseline + Android PR foundation SUCCESS on the exact head.
3. Diagnose/fix source/harness/Android compile failures until green.
4. Merge only the exact tested head if clean.
5. Verify post-merge clean baseline + product foundations on main.
6. Reconcile `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, `CHECKPOINT.md`, Issue #8.
7. Continue Android scanner command handoff, then reconnect/resync/network lifecycle, HTTPS/trust and foreground/background recovery as dependencies permit.
8. Continue independent safe Web/integration work where authority is sufficient; keep provider/physical/STABLE/portrait gates isolated.

## Current blockers / gates

- real ROOT/normal email-OTP provider E2E requires reviewed provider configuration/secrets;
- ROOT TOTP verifier technical contract is insufficiently specified for safe implementation;
- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance require physical environment;
- final Android/PDA visual fidelity requires authorized Pick Pack reference evidence;
- portrait replacement remains `OWNER_DECISION_REQUIRED`;
- STABLE promotion requires explicit Owner approval after mandatory BETA acceptance.

## do_not_repeat

do_not_repeat:
Do not treat memory as authority. Do not replay migrations 0009 or 0014. Do not expose or infer secret values. Do not invent TOTP verifier parameters or OTP failure-attempt limits. Do not persist raw password/OTP/TOTP/provider secrets. Do not generate a new key during restore when the original session key is unavailable. Do not restore expired/corrupt session state. Do not auto-switch Android runtime authority. Do not accept non-HTTPS LAN endpoints. Do not call the session persistence slice PASS before clean baseline + Android PR foundation evidence. Do not invent Pick Pack UI details. Do not inflate progress without weighted acceptance evidence. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
