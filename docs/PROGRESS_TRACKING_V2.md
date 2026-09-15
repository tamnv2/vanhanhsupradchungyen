# VHDCHY PROGRESS TRACKING V2

Status: ACTIVE CURRENT PROGRESS AUTHORITY
Effective: 2026-09-15
Authority: `DECISIONS_V9.md`
Supersedes: `docs/PROGRESS_TRACKING_V1.md` for current percentage reporting. V1 remains historical evidence.

## Model

The existing 12 top-level phase weights remain unchanged. Each phase now contains **20 fixed evidence credits**. One earned credit therefore equals **5 percentage points of that phase**, not 5 percentage points of the whole project.

Project completion is:

`TOTAL_PRODUCT_COMPLETION = sum(phase_weight * earned_phase_credits / 20)`

Credits are earned only from current-authority evidence. Plans, discussions, governance-only edits, mockups without acceptance, tool-call volume and legacy/reference behavior earn zero credit.

Evidence levels remain distinct: DESIGN/CONTRACT, SOURCE_PASS, CI_PASS, LIVE_PROVIDER_PASS, PHYSICAL_PASS and ACCEPTED. A credit's required level depends on the acceptance checkpoint represented by that credit; source evidence never substitutes for a provider/physical gate.

## Current total

**57.7% exact / 58% displayed**.

This is a conservative evidence rescore from V1. No architecture/scope weight changed. Only evidence already accepted before V2 moved three phase scores:

- Phase 5: 55% -> **60%** from accepted live BETA projection evidence;
- Phase 7: 25% -> **30%** from the merged Web employee-create/current shared-shell evidence;
- Phase 8: 15% -> **20%** from accepted Android endpoint-acquisition + secure-session foundation evidence.

All other phase scores are preserved from V1.

## Fixed phase weights and earned credits

| Phase | Weight | Earned / 20 | Phase completion | Weighted contribution |
|---|---:|---:|---:|---:|
| 1. Scope / Owner rules / architecture | 8% | 19 | 95% | 7.60% |
| 2. Repo / environments / provider / CI foundation | 8% | 17 | 85% | 6.80% |
| 3. Cloud data / identity / auth / Service foundation | 12% | 16 | 80% | 9.60% |
| 4. Core business Service/API/workflows | 14% | 13 | 65% | 9.10% |
| 5. Gateway / adapters / realtime / integrations | 10% | 12 | 60% | 6.00% |
| 6. LAN continuity / local state / offline / reconciliation | 16% | 13 | 65% | 10.40% |
| 7. Online + LAN Web product UI | 10% | 6 | 30% | 3.00% |
| 8. Android/PDA App | 10% | 4 | 20% | 2.00% |
| 9. Admin / reporting / operational tooling | 4% | 7 | 35% | 1.40% |
| 10. Security / observability / backup / recovery | 3% | 10 | 50% | 1.50% |
| 11. BETA physical / capacity / UAT acceptance | 3% | 2 | 10% | 0.30% |
| 12. STABLE promotion / production closure | 2% | 0 | 0% | 0.00% |
| **TOTAL** | **100%** |  |  | **57.70%** |

## Fixed subweight ledger

Capacities below are immutable unless Owner authority explicitly changes scope/weighting. `Earned` is the current evidence-backed count.

### Phase 1 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Product scope/surfaces | 4 | 4 |
| Owner/business rules | 6 | 6 |
| Online/LAN/offline architecture | 5 | 5 |
| Account/UI/language direction | 4 | 4 |
| Architecture closure / stale wording elimination | 1 | 0 |

### Phase 2 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Repo/authority/resume model | 4 | 4 |
| BETA environment/provider foundation | 5 | 5 |
| CI/build foundation | 5 | 5 |
| Isolated STABLE preparation/promotion automation | 6 | 3 |

### Phase 3 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Canonical data/schema foundation | 6 | 6 |
| Authentication/account context | 5 | 4 |
| Service/API/runtime foundation | 5 | 4 |
| Provider/permission/parity closure | 4 | 2 |

### Phase 4 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Employee + MNV lifecycle | 3 | 3 |
| Attendance/presence | 3 | 3 |
| Session + PICK/PACK + resources | 7 | 3 |
| Labor + dropped goods + borrowing/reuse | 3 | 1 |
| Documents/media business flow | 2 | 1 |
| Permissions/idempotency/audit/conflict correction | 2 | 2 |

### Phase 5 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Gateway command/event path | 4 | 3 |
| Google Sheets/Drive projection/output | 6 | 5 |
| Realtime/status/resync | 4 | 2 |
| Camera/media/provider adapters | 3 | 1 |
| Notifications/support adapters | 3 | 1 |

The fifth Google projection/output credit includes accepted live BETA projection E2E evidence. Remaining credit(s) require broader receipt/deduplication/retry/reconciliation coverage, not merely another successful row write.

### Phase 6 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| No-admin Windows host | 2 | 2 |
| Discovery/endpoint/reconnect foundation | 1 | 1 |
| Local operational state/snapshots | 3 | 2 |
| Durable events/idempotent replay/actor evidence | 3 | 2 |
| Durable staged media | 1 | 1 |
| LAN auth/pairing/security epoch | 2 | 1 |
| LAN Web local assets/domain | 1 | 1 |
| Reconciliation/conflict mechanics | 4 | 3 |
| >=60-minute outage physical acceptance | 2 | 0 |
| Target-host/device/capacity/soak | 1 | 0 |

### Phase 7 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Shared V7 design system | 3 | 2 |
| Login/authentication surface | 3 | 1 |
| Authenticated dashboard/status shell | 2 | 1 |
| Business modules | 7 | 1 |
| Online/LAN parity + network-state UX | 3 | 1 |
| Offline-safe LAN assets acceptance | 1 | 0 |
| Responsive/language closure | 1 | 0 |

The first business-module credit includes the merged `EMPLOYEE_CREATE` user flow and its accepted CI lineage. Additional display-only placeholders do not earn further module credit.

### Phase 8 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| App shell/design foundation | 2 | 1 |
| Auth/session | 3 | 1 |
| Endpoint/runtime routing | 2 | 1 |
| Attendance scanner/workflow | 4 | 0 |
| PICK/PACK/resources workflow | 4 | 0 |
| Offline/reconnect/queue lifecycle | 3 | 1 |
| Camera/update/physical PDA acceptance | 2 | 0 |

The current four credits recognize only foundations already evidenced; they do **not** imply a completed PDA business workflow. Attendance-scanner credits remain zero until the vertical path is implemented and accepted.

### Phase 9 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Account/employee administration | 6 | 3 |
| Permission administration | 5 | 2 |
| Reporting/operational views | 5 | 1 |
| Diagnostics/support tooling | 4 | 1 |

### Phase 10 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Security hardening | 6 | 4 |
| Observability/diagnostics | 4 | 2 |
| Backup/restore | 4 | 2 |
| Update/rollback | 3 | 1 |
| Recovery drills | 3 | 1 |

### Phase 11 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| Target company host/PDA/network | 4 | 1 |
| >=60-minute Internet-cut acceptance | 4 | 0 |
| Capacity/soak | 4 | 0 |
| Full BETA E2E acceptance | 4 | 1 |
| Owner UAT | 4 | 0 |

Partial environment/source preparation may support a credit only where evidence is already genuinely accepted; it does not substitute for the remaining physical gates.

### Phase 12 — 20 credits

| Subarea | Capacity | Earned |
|---|---:|---:|
| STABLE isolation/readiness | 4 | 0 |
| Exact accepted-release promotion | 5 | 0 |
| STABLE deployment/verification | 5 | 0 |
| Handover/runbooks | 4 | 0 |
| Post-release rollback/closure | 2 | 0 |

## Credit update rule

When evidence changes:

1. identify the exact acceptance checkpoint/subarea;
2. link current-source/CI/provider/physical evidence;
3. award or remove the smallest justified integer credit count;
4. recalculate the phase and total mechanically;
5. update `CURRENT_STATE.md` once at the meaningful reconciliation boundary;
6. do not copy the resulting percentage into non-owning authority files.

If evidence does not close another fixed credit, progress legitimately remains unchanged even if useful implementation occurred. This is different from the old subjective whole-phase threshold: the next increment and its owning subarea are now explicit.

## Current evidence anchors for the V2 rescore

- Projection live BETA: workflow `34927511443` SUCCESS; cleaned Worker deploy `34927869845` SUCCESS; observer `34927996370` SUCCESS.
- Web employee-create: merged PR #19 / commit `db746a71ed80dafd288218f599ab3e990f87e439`; post-merge clean `34928090928`, product foundations `34928090885` SUCCESS.
- Android endpoint acquisition: merged PR #32 / commit `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`; accepted pre/post-merge CI.
- Android secure session persistence: merged PR #34 / commit `ebfb7d238268fb089a34779e2c5d53ae45693608`; PR clean `34933869114`, Android foundation `34933869144`, post-merge clean `34933981747`, product foundations `34933981735` SUCCESS.

## Reporting

Normal user-facing progress reports use the exact percentage plus rounded displayed value from this file, followed by the evidence-backed delta since the previous credit change. Do not infer progress from elapsed time or effort.
