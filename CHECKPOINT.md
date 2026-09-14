# CHECKPOINT — VHDCHY

checkpoint_version: 27
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: 583ca88170faa4d0b37c48af5339068740988950
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V4.md
context_index_ref: CONTEXT_INDEX.md

## Authority

- Fresh resume must live-read `AI_ENTRYPOINT.md`, compare `main` HEAD with this reconciliation point, then read every active decision layer listed by `CONTEXT_INDEX.md`.
- Effective decisions remain `DECISIONS.md` + V3 + V4 + V5 + V6. No newer authority layer exists through this reconciliation point.
- LAN remains a full local Service runtime; Cloud/LAN share one command/event/business meaning; actor identity comes only from authenticated context; DENY precedence applies; raw events remain immutable; silent last-write-wins is forbidden.
- Employee/MNV authority: two ACTIVE people may not share one MNV; reuse is allowed only after the prior holder is inactive/left.
- Attendance authority: multiple IN/OUT events are allowed on one business date while only one current presence state exists; OUT without valid preceding IN is rejected; exact retry is idempotent.

## Proven foundations

### Cloud / auth
- Multi-module Worker packaging/session boundary foundation remains proven and protected business/admin routes remain fail-closed where handlers are not reviewed.
- V6 email-OTP source foundation exists; `0010` is NOT applied to BETA.
- Permission catalog migration `0011` is source-only and NOT applied to BETA.
- Email/SMS provider remains unconfigured. No delivery is claimed live.

### Shared domain / product build
- `VHDCHY_DOMAIN_V1`, `contracts/commands.slice1.v1.json`, `contracts/acceptance.v1.json` remain active machine contracts.
- Four-lane product build run `34791932021`: SUCCESS for Web, Cloud Service, LAN Service and Android APK.

### LAN durable storage / synchronized state
- `EdgeStore`, authority snapshot, operational snapshot, immutable edge event/outbox and `LocalCommandStore` atomic acceptance foundations are proven.
- Local command transaction SQL-evidence run `34789053619`: SUCCESS.
- Operational snapshot run `34789244223`: SUCCESS.
- Authorization evaluator with DENY precedence run `34789650267`: SUCCESS.
- Dedicated fail-closed readiness run `34789875048`: SUCCESS.
- Trusted 8-command Slice-1 command-gate run `34791998929`: SUCCESS.

### Operational snapshot materialization — PASS
- `Slice1OperationalStateMaterializer` materializes employees, employee codes and attendance/presence into `module_current_state` inside operational activation.
- Snapshot replacement is blocked by unreconciled local Cloud outbox work so synchronized refresh cannot overwrite locally accepted pending state.
- Invalid materialization rolls the activation transaction back and preserves prior active state.
- Dedicated materialization run `34793329286`: SUCCESS, including seed, rollback, pending-local guard and public mutation still 503.

### Actor evidence / replay boundary — SOURCE + COMPILE PASS
- `LocalCommandReplayResolver` recognizes exact accepted logical replay before mutable business-state validation.
- Compile defect in nullable replay fields was fixed at `023b4911ec82a2ff4dd6a7d0473e8db4602dcb71`; LAN and related checks passed afterward.
- `EdgeActorEvidenceStore` stages authenticated actor context and captures immutable event actor evidence via SQLite trigger in the same local acceptance transaction. Actor does not contaminate business payload/hash identity.

### LAN Slice-1 business adapter — SEQUENTIAL BUSINESS VECTORS PASS, NOT READY
- `Slice1BusinessAdapter` derives permission/event/entity/state authority server-side from trusted command contract; client authority fields are rejected.
- Exact replay is accepted only when immutable authenticated-actor evidence matches.
- Proven sequential behaviors include permission DENY precedence, employee create/update/status, actor evidence, exact replay, MNV active-holder guard/reuse after prior holder inactive, attendance IN, repeated IN, OUT guard, correction, and portrait fail-closed.
- Initial business harness run `34796238740` correctly FAILED on active-holder MNV reassignment (`INVALID_INPUT_NOT_REJECTED`).
- Source was corrected at `583ca88170faa4d0b37c48af5339068740988950` for prior-holder status and repeated-IN semantics.
- Business harness run `34796432170`: SUCCESS.
- Adapter remains `Ready=false` with blockers:
  - `PORTRAIT_MEDIA_LIFECYCLE_REQUIRED`;
  - `EMPLOYEE_CODE_ATOMIC_UNIQUENESS_REQUIRED`.
- Sequential pre-checks are not sufficient proof against concurrent MNV assignment races; atomic uniqueness must be enforced inside the SQLite mutation transaction before readiness can open.

## Current provider / runtime facts

- GitHub repo: `tamnv2/vanhanhsupradchungyen`, authority branch `main`.
- BETA Worker: `vhdchy-beta`; public origin `https://beta.supra.cc.cd`.
- BETA D1: `vhdchy-data-beta`, ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, marker `business_core_v3`.
- Google Gateway remains projection-oriented/fail-closed; projection is not declared LIVE.
- `0010` and `0011` are NOT applied to BETA provider.
- LAN public `POST/PUT/PATCH/DELETE /api/v1/**` remains fail-closed; `businessMutationEnabled=false`.
- Readiness remains fail-closed; source/business harness PASS is not permission to expose public mutation routes.
- STABLE remains isolated/dormant until full BETA PASS + explicit Owner promotion approval.

## Immediate execution

1. Implement atomic employee-code uniqueness in the same SQLite transaction as `module_current_state` + event + outbox so concurrent MNV assignment cannot race past sequential validation.
2. Add deterministic harness evidence for active MNV claim and one-active-code-per-employee claim; prove conflict rolls back state/event/outbox and leaves the winning claim intact.
3. Keep the business adapter/readiness blocked until atomic MNV uniqueness is proven.
4. Then implement/prove employee portrait staged-media + durable readback + prior-file deletion lifecycle required by authority.
5. Only after every Slice-1 blocker is closed may `LanReadinessEvaluator` link the adapter and consider `EDGE_READY`; public mutations remain closed until that reviewed gate passes.
6. Continue independent Cloud/Auth/provider lanes only through allowed high-level actions; never bypass platform action-safety.

do_not_repeat:
Do not treat memory as authority. Do not replay provider migrations. Do not claim `0010`/`0011` applied. Do not claim email/SMS delivery live. Do not bypass action-safety. Do not open LAN mutation routes before readiness/authz/domain acceptance. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not promote STABLE without explicit Owner approval. Do not call the Slice-1 adapter READY while portrait lifecycle or atomic MNV uniqueness remains unproven.
