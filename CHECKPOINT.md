# CHECKPOINT — VHDCHY

checkpoint_version: 61
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V9_BETA
action_mode: VERTICAL_SLICE_WIP3
reconciled_through_commit: 385c0f3db859b536536284ff2e90b6248a696139
active_wip: LANE_A_ATTENDANCE / LANE_B_WEB / LANE_C_SESSION_PICK_PACK_RESOURCES
paused_gates: PROVIDER_OTP_E2E / PHYSICAL_CORPORATE_LAN_REGRESSION / FINAL_PICK_PACK_VISUAL_EVIDENCE / PORTRAIT_OWNER_DECISION / STABLE_PROMOTION

authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
authority_v9_ref: DECISIONS_V9.md
termination_guard_ref: AI_TERMINATION_GUARD.md
delivery_plan_ref: docs/DELIVERY_PLAN_V6.md
execution_model_ref: docs/EXECUTION_MODEL_V1.md
progress_ref: docs/PROGRESS_TRACKING_V2.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
current_state_ref: CURRENT_STATE.md
next_actions_ref: NEXT_ACTIONS.md
context_index_ref: CONTEXT_INDEX.md

## V9 operating-model migration

Owner approved the full execution optimization on 2026-09-15.

Persisted changes through the reconciliation commit:

- vertical acceptance slice is the default delivery unit;
- normal WIP is capped at one integrating slice plus two independent client/preparation lanes;
- governance/checkpoints are batched at meaningful slice/security/provider/release boundaries rather than after every mechanic;
- volatile truth has single-source owners;
- current progress uses the fixed 20-credit-per-phase V2 evidence ledger;
- CI is explicitly tiered and reuses existing focused workflows/harnesses;
- current delivery sequencing is Attendance -> Session/PICK/PACK/resources -> labor/dropped goods -> documents/media -> product/physical/release closure.

Current exact/displayed progress is read only from `docs/PROGRESS_TRACKING_V2.md`; do not copy it from this checkpoint.

## Current WIP

### Lane A — Attendance golden path — PRIMARY READY

Immediate node is Cloud + LAN authenticated MNV scan-context parity.

Required invariant: QR contains MNV only; MNV is never treated as technical `employeeId`. Resolve one ACTIVE code/employee plus current presence/version. Actor identity remains authenticated Service context. Lookup is read-only and creates no event/outbox/Google write.

After scan-context parity, continue within the same vertical slice to Android ATTENDANCE_IN/OUT planning, minimal Vietnamese scanner/result/status UI, dispatch and retry/restart/idempotency acceptance.

### Lane B — Web — READY PARALLEL

Advance usable current-contract business actions and shared Online/LAN network/sync UX. Do not invent missing server contracts or unsafe version semantics.

### Lane C — Session/PICK/PACK/resources — READY PARALLEL PREPARATION

Lock the next vertical slice command/acceptance contract and focused core tests from current Owner rules. Do not build speculative generic infrastructure.

## Accepted evidence carried forward

- Projection live BETA: `34927511443`, cleaned deploy `34927869845`, observer `34927996370`: SUCCESS.
- Web employee-create PR #19 merge `db746a71ed80dafd288218f599ab3e990f87e439`; post-merge clean `34928090928`, product foundations `34928090885`: SUCCESS.
- V6 email OTP PR #30 merge `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`: SOURCE/CI PASS; real provider delivery gated.
- Android endpoint acquisition PR #32 merge `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`: SOURCE/CI PASS.
- Android secure session PR #34 merge `ebfb7d238268fb089a34779e2c5d53ae45693608`; `34933869114`, `34933869144`, `34933981747`, `34933981735`: SUCCESS.
- Pre-V9 main clean baseline `34934745772`: SUCCESS.

## Current gates

- real ROOT/normal email-OTP provider E2E requires reviewed provider configuration/secrets;
- ROOT TOTP verifier technical parameters remain insufficiently locked; do not invent;
- physical company host/network/PDA/public trust, >=60-minute outage, reconnect/restart/network-change and capacity/soak/UAT require target physical environment;
- final Android visual fidelity requires accessible authorized Pick Pack reference evidence;
- portrait replacement conflicting behavior remains `OWNER_DECISION_REQUIRED`;
- STABLE promotion requires explicit Owner approval after BETA acceptance.

None of these gates stop the current independent source WIP.

## Fresh-chat resume anchor

1. Live-fetch `AI_ENTRYPOINT.md` from GitHub `main` and execute its bootstrap.
2. Compare current main HEAD with this reconciliation point.
3. Read any changed active authority/current-state/source paths after `385c0f3db859b536536284ff2e90b6248a696139` before mutation.
4. Rebuild V9 WIP from `NEXT_ACTIONS.md` and continue all safe READY nodes within the WIP limit.

Memory/chat summaries are NON_AUTHORITY.

## do_not_repeat

do_not_repeat:
Do not treat memory as authority. Do not restore V1 progress or V5 sequencing as current authority. Do not copy current percentage/provider liveness into non-owning files. Do not open more than the V9 normal WIP without closing/blocking/replacing a lane. Do not create governance-only PRs after every helper. Do not replay migrations 0009 or 0014. Do not expose or infer secrets. Do not invent TOTP verifier parameters or OTP failure limits. Do not treat MNV as employeeId. Do not put actor authority in scanner/query payloads. Do not write Android scanner directly to database/Google. Do not invent final Pick Pack UI details. Do not promote STABLE without explicit Owner approval. Do not voluntarily final while approved READY work remains; run `PRE_FINAL_TERMINATION_GUARD` first.
