# CHECKPOINT — VHDCHY

checkpoint_version: 30
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 9754550ad13a50a71ea5825f75173d3fb176a430
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB_ONLINE_LAN / ANDROID_PDA / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
context_index_ref: CONTEXT_INDEX.md

## Current project progress

- Evidence-weighted total: **53.3% exact / 53% displayed**.
- Primary active phase: **Phase 6 — LAN continuity, local state, offline operation and reconciliation**.
- Parallel incomplete lanes: Phase 4 core business Service/API; Phase 5 Gateway/integrations; Phase 7 Online+LAN Web V7 UI; Phase 8 Android/PDA App.
- Progress is not elapsed-time/commit-count based. Scope expansion may legitimately reduce the percentage.

## Owner V7 UI authority — ACTIVE

- Android/PDA App UI/UX direction: actual Owner Pick Pack 1291 App reference, adapted to current VHDCHY workflows/terminology/permissions/data/runtime.
- `BACKUP PICK PACK 1291` remains NON_AUTHORITY except for this explicit UI/UX reference use. Do not inherit old business logic/data/credentials/runtime architecture.
- Exact Pick Pack visual details must come from accessible actual source/artifacts; do not invent unavailable screen details.
- Online Web + LAN Web visual direction: Owner-supplied DNSHE screenshots from 2026-09-14 — dark navy navigation, light blue/white field, white rounded cards, royal-blue primary actions, compact status/icon tiles, clean enterprise-console hierarchy.
- Do not copy DNSHE branding/proprietary assets.
- Online/LAN Web remain one product/shared design system. LAN-critical UI assets must be available locally without Internet.
- V5 language rule remains Vietnamese / English / Chinese, default English.

## Current proven source foundations

### Cloud/data/auth

- D1 remains central consolidated canonical business store after synchronization under V3.
- Sheets/Drive remain projection/file-storage outputs, never canonical business authority.
- Current Google Drive/Sheets/GAS authority is `tam95.supra@gmail.com`.
- BETA provider/runtime facts in `SERVICE_AUTHORITY.md` must be live-verified before any provider mutation/deploy.
- Public business/admin/provider paths remain fail-closed where current readiness/handler/provider acceptance is incomplete.

### LAN operational state / Slice-1

Current source lineage has automated evidence for:

- LAN operational snapshot/state materialization with rollback and pending-local overwrite guard;
- idempotent local replay preflight;
- immutable authenticated actor evidence capture;
- employee/MNV/attendance Slice-1 business behavior;
- repeated-IN semantics;
- atomic active-MNV and one-active-code-per-employee uniqueness claims with race rollback;
- stable resource conflict mapping;
- durable staged-media local store/schema/lifecycle;
- serialized staged-media identity claims.

Dedicated staged-media workflow run `34797198225` at `cddf8dc8358dce25febf0efd6c04c4cd73f602fb`: **SUCCESS**.

This source/CI evidence is not physical company-network/PDA PASS.

## Portrait semantic gate — still unresolved

One Owner-level product conflict remains open:

- current rule requires previous portrait deletion immediately;
- LAN/offline media semantics allow local staging when Drive is unavailable.

Decision-independent durable media work is valid and now materially implemented/tested, but actual portrait replacement behavior while Drive is unavailable remains fail-closed until explicit Owner authority resolves the conflict.

Do not infer that V7 UI decisions resolve this business semantic gate.

## Current LAN acceptance boundary

- Public LAN business mutation readiness remains fail-closed until current auth/pairing/security-epoch/permission/domain readiness links the complete reviewed path.
- Physical company ordinary-user Windows + real NLS-MT90 regression is pending.
- Canonical V6 continuity acceptance: warm up connected, cut Internet while valid LAN remains, then sustain approved App + LAN Web + local business workflow for **Window 2 >=60 minutes**.
- External Internet embeds may fail during the timed window.
- Restoration sync/reconciliation is tested after the timed window.
- Host restart/power-loss is a separate recovery gate.
- Capacity gate includes synthetic 10/25/50/100 plus soak.

## Delivery plan / project position

`docs/DELIVERY_PLAN_V5.md` is now the full start-to-STABLE plan.

Current weighted phase baseline:

| Phase | Completion |
|---|---:|
| 1 Scope/rules/architecture | 95% |
| 2 Repo/environments/providers/CI | 85% |
| 3 Cloud data/auth/Service foundation | 80% |
| 4 Core business Service/API | 65% |
| 5 Gateway/adapters/integrations | 55% |
| 6 LAN continuity/offline/reconcile | 50% |
| 7 Online Web + LAN Web UI | 20% |
| 8 Android/PDA App | 15% |
| 9 Account/admin/reporting/support | 35% |
| 10 Security/observability/recovery | 50% |
| 11 BETA physical/capacity/UAT | 10% |
| 12 STABLE production/handover | 0% |

Weighted total: **53.3%**.

## Reconciled project documents in this audit

- `DECISIONS_V7.md` — new Owner UI direction.
- `docs/PROGRESS_TRACKING_V1.md` — new progress formula/weights/evidence rules.
- `docs/DELIVERY_PLAN_V5.md` — full beginning-to-final-product delivery plan.
- `CURRENT_STATE.md` — current 53% position and recent source evidence.
- `NEXT_ACTIONS.md` — current dependency-aware execution order.
- `CONTEXT_INDEX.md` — routes V7 + progress + delivery V5.
- `SERVICE_AUTHORITY.md` — reconciled stale V2 LAN/offline/Web/App authority wording to V3–V7.
- `docs/LAN_EDGE_STATE_V2.md` — removed stale V5 factor/lifetime block and added V6/V7 continuity/Web boundary.
- `PROJECT_SCOPE.md` — final deliverable/UI/60-minute scope reconciled.
- `docs/BETA_ACCEPTANCE_MATRIX.md` — V6/V7, 60-minute continuity, V7 UI, portrait fail-closed and capacity/UAT gates reconciled.
- `.github/workflows/validate.yml` — validator now requires V7, Delivery Plan V5 and Progress Tracking V1. The immediately prior baseline run failed because the validator still hard-coded V4/V6-era invariants; that validator drift is now corrected and must be rechecked by the next run.

## Immediate execution

1. **Primary:** continue Phase 6 by closing broader current LAN business/auth/reconciliation readiness without opening public mutations prematurely.
2. **Parallel:** extend Phase 4/5 business/provider coverage and current acceptance vectors.
3. **Parallel Web:** implement the shared V7 design system, login shell and dashboard shell for Online/LAN Web with local outage-safe assets.
4. **Parallel App:** surface actual Pick Pack 1291 App UI artifacts, then build the current PDA shell against stable current contracts.
5. Keep portrait replacement fail-closed at the exact unresolved offline/Drive semantic boundary until Owner decides it.
6. When source/product readiness is sufficient, execute target company-network/NLS-MT90 physical regression, >=60-minute continuity, capacity/soak and Owner UAT.
7. STABLE remains blocked until exact BETA acceptance plus explicit Owner promotion approval.

do_not_repeat:
Do not treat memory as authority. Do not replay provider migrations from remembered state. Do not claim provider delivery/projection live without current evidence. Do not bypass action-safety. Do not open LAN mutation routes before readiness/authz/domain acceptance. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not invent Pick Pack UI details without reference evidence. Do not copy DNSHE branding/assets. Do not reintroduce stale duration-only offline TTL. Do not silently resolve the portrait immediate-delete/offline-staging conflict. Do not promote STABLE without explicit Owner approval.