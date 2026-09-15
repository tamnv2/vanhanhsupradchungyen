# CHECKPOINT — VHDCHY

checkpoint_version: 60
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
action_mode: AUTONOMOUS_PARALLEL
reconciled_through_commit: efb49c6879efc30932ebd9924f02fb2b22b2ef62
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
- Android endpoint acquisition + secure session persistence are accepted source/CI PASS; scanner command planning is dependency-blocked until trusted MNV scan-context parity exists.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare current main HEAD with this checkpoint reconciliation point.
3. Read changed authority/current-state/source paths after `efb49c6879efc30932ebd9924f02fb2b22b2ef62` before mutation.
4. Follow current `NEXT_ACTIONS.md`; primary ready lane is Cloud+LAN attendance scan-context parity unless later main evidence supersedes it.

Memory/chat summaries are NON_AUTHORITY.

## Latest accepted main evidence

- Android session persistence PR #34 merged at `ebfb7d238268fb089a34779e2c5d53ae45693608`; PR clean `34933869114`, Android foundation `34933869144`, post-merge clean `34933981747`, product foundations `34933981735`: SUCCESS.
- Android endpoint acquisition PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`; pre/post-merge CI PASS.
- Projection live E2E `34927511443`, cleaned deploy `34927869845`, observer `34927996370`: SUCCESS.
- Web employee-create PR #19 merged with post-merge CI PASS.
- V6 email-OTP PR #30 source/CI PASS; real provider delivery remains gated.
- Governance PR #35 merged at `0f1af567cb40fd673d492e4bec76a0a79e38c880` before this dependency correction.

## Scanner identity dependency — VERIFIED

Owner authority V5-002: QR contains MNV only.

Current Cloud/LAN attendance mutations require technical `employeeId` as `entityId` and payload `employeeId`, and require current presence entity version when a presence state already exists. Actor authority is server-authenticated and is prohibited in client payload.

Therefore scanned MNV must never be treated as employeeId.

Underlying parity data is already available:

- Cloud D1 `employees`, `employee_codes`, `presence_state`;
- Cloud operational snapshot includes employees/current portrait ref, employee codes and presence;
- LAN materializes all three into `module_current_state` and validates ACTIVE employee-code uniqueness.

Missing dependency: no current public authenticated client route has been proven for MNV -> ACTIVE employee + current presence/version lookup.

## Current primary READY — Cloud+LAN scan-context read parity

Implement one read-only business meaning across Cloud and LAN.

Required behavior:

- bounded normalized MNV input only;
- current authenticated user required;
- normal password-change restriction applies;
- attendance `scan` permission/scope required;
- LAN also requires HTTPS + paired-device signed proof + matching LAN session/device/security epoch;
- resolve exactly one ACTIVE employee-code assignment and require linked employee ACTIVE;
- return `employeeCodeId`, MNV, technical `employeeId`, `fullName`, current portrait media reference or null, and current presence `{currentState,businessDate,entityVersion}` or null;
- no actor fields accepted/returned as client authority;
- no business mutation/event/outbox/Google side effect;
- stable fail-closed not-found/inactive/ambiguity/runtime errors;
- Cloud/LAN response semantics and tests must remain aligned.

Android scanner command planning stays blocked until this dependency is accepted source/CI PASS.

## Following READY queue

1. After scan-context parity: Android `ATTENDANCE_IN/OUT` command planner using resolved employee identity + presence version.
2. Android reconnect/resync/network lifecycle.
3. HTTPS/trust fail-closed and foreground/background recovery.
4. Independent safe Web/integration work where current contracts are sufficient.

## Current blockers / gates

- real ROOT/normal email-OTP provider E2E requires reviewed provider configuration/secrets;
- ROOT TOTP verifier technical contract remains insufficiently specified;
- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance require physical environment;
- final Android/PDA visual fidelity requires authorized Pick Pack reference evidence;
- portrait replacement remains `OWNER_DECISION_REQUIRED`;
- STABLE promotion requires explicit Owner approval after mandatory BETA acceptance.

## do_not_repeat

do_not_repeat:
Do not treat memory as authority. Do not replay migrations 0009 or 0014. Do not expose or infer secrets. Do not invent TOTP verifier parameters or OTP failure limits. Do not treat MNV as employeeId. Do not put actor authority in scanner/query payloads. Do not write Android scanner directly to database/Google. Do not implement scanner command planning before trusted MNV scan-context parity. Do not invent final Pick Pack UI details. Do not inflate progress without weighted acceptance evidence. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
