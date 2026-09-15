# CHECKPOINT — VHDCHY

checkpoint_version: 57
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: b839b8a74daa8fb677f24c6be449ba1f2d86fc21
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
- Android endpoint acquisition is now source/CI PASS, but no weighted phase threshold is moved: Android business workflows, session/scanner/lifecycle mechanics, final UI and physical PDA acceptance remain open.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare current main HEAD with this checkpoint reconciliation point.
3. Read changed authority/current-state/source paths after `b839b8a74daa8fb677f24c6be449ba1f2d86fc21` before mutation.
4. Follow current `NEXT_ACTIONS.md`; primary ready source lane is Android session persistence/expiry unless later main evidence supersedes it.

Memory/chat summaries are NON_AUTHORITY.

## Latest accepted main evidence

### Android endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`.

- PR clean baseline `34931008972`: SUCCESS;
- PR Android foundation `34931008973`: SUCCESS;
- endpoint harness: `ANDROID_ENDPOINT_ACQUISITION_PASS checks=24`;
- reconnect harness: `ANDROID_RECONNECT_BEHAVIOR_PASS checks=7`;
- transport harness: `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=52`;
- post-merge clean baseline `34933290109`: SUCCESS;
- post-merge product foundations `34933290105`: Cloud/Web/Android/LAN SUCCESS.

Accepted semantics:

- `cached healthy endpoint -> LAN discovery -> manual recovery`;
- HTTPS-only and health-probed candidates;
- cache-first anti-flapping;
- stale/invalid cache safe fallback;
- invalid discovery discarded;
- manual recovery last and fail-closed for insecure/invalid endpoints;
- probe errors are unhealthy;
- no fabricated endpoint;
- no automatic Cloud/LAN authority switch.

`.github/workflows/android-pr-foundation.yml` now gives Android-changing PRs a pre-merge APK/harness gate.

### V6 email OTP source — MERGED SOURCE/CI PASS / PROVIDER GATED

PR #30 merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

- PR clean baseline `34930120980`: SUCCESS;
- post-merge clean baseline `34930171765`: SUCCESS;
- product foundations `34930171683`: Cloud/Web/Android/LAN SUCCESS.

Real provider delivery/request/use E2E remains gated by reviewed runtime provider/bindings/secrets. TOTP-enabled ROOT remains fail-closed because current authority does not define enough verifier implementation detail to invent one safely.

### Retained accepted evidence

- Projection live E2E `34927511443`, cleaned deploy `34927869845`, observer `34927996370`: SUCCESS.
- Web employee-create PR #19 merged; clean baseline `34928090928`, product foundations `34928090885`: SUCCESS.
- LAN operational refresh/rebase integration `34851773729`, clean baseline `34851772963`: SUCCESS.

## Current primary READY — Android session persistence/expiry

Current `PdaSession` is in-memory only.

Approved implementation boundary from current architecture/security rules:

- secure local credential-verification/storage format is an implementation-security decision;
- raw passwords, email OTPs, TOTP secrets/codes and provider credentials must never be persisted;
- persisted bearer session state may contain only what is required to restore the same authenticated context: bearer token, expiry and original runtime mode;
- encrypted-at-rest persistence uses Android Keystore-backed AES-GCM and app-private storage for ciphertext/IV/version metadata;
- restore only before expiry and with structurally valid state;
- expiry/corruption/key invalidation/decrypt failure/unknown runtime clears persisted session and fails closed;
- restore must not perform endpoint discovery or switch Cloud/LAN authority.

Next implementation sequence:

1. branch from the accepted/reconciled main;
2. extend `PdaSession` with persistence-safe snapshot/restore behavior and pure-Java harness vectors;
3. add Android-specific Keystore persistence adapter outside the pure transport harness compile set;
4. do not add final UI work;
5. checkpoint last;
6. require clean baseline + Android PR foundation PASS before exact-head merge;
7. run post-merge evidence and reconcile governance.

## Following READY queue

- scanner input -> domain command handoff without direct DB writes;
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
Do not treat memory as authority. Do not replay migrations 0009 or 0014. Do not expose or infer secret values. Do not invent TOTP verifier parameters or OTP failure-attempt limits. Do not deploy/claim OTP provider PASS without evidence. Do not auto-switch Android runtime authority. Do not accept non-HTTPS LAN endpoints. Do not persist raw password/OTP/TOTP/provider secrets. Do not restore expired/corrupt session state. Do not invent Pick Pack UI details. Do not inflate progress without weighted acceptance evidence. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
