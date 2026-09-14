# VHDCHY PROJECT PROGRESS TRACKING V1

Status: ACTIVE
Effective date: 2026-09-14
Purpose: provide one evidence-based completion percentage from project start to final production acceptance.

## 1. Primary progress metric

`TOTAL_PRODUCT_COMPLETION` measures completion of the entire target product: Android/PDA App + Online Web + LAN Web + Service/Gateway/LAN continuity + operational/release readiness.

It is not a measure of elapsed time, number of commits, number of documents, or developer effort. A design decision alone cannot be counted as if the corresponding product behavior were already implemented.

Formula:

`TOTAL_PRODUCT_COMPLETION = sum(phase_weight × phase_completion) / 100`

Phase weights are fixed by this V1 baseline and total 100%. Phase completion is evidence-based and is recalculated when implementation/test/acceptance evidence changes.

## 2. Evidence scale

Use the following scale as a scoring guide, then choose the nearest justified percentage for the phase or substep:

| Evidence state | Guide | Meaning |
|---|---:|---|
| NOT_STARTED | 0% | No accepted design or implementation evidence. |
| DECIDED_DESIGNED | 25% | Owner decision/architecture/contract exists, but target implementation is not materially working. |
| WORKING_SLICE | 50% | A real subset works and is testable; important target coverage is still missing. |
| IMPLEMENTED_AUTOMATED | 80% | Target behavior is materially implemented and automated tests/CI support it; physical/provider/UAT evidence may still be missing. |
| ACCEPTED | 100% | Required implementation plus the acceptance evidence appropriate to that phase is complete. |

Intermediate values are allowed only when repository/provider/physical evidence makes them defensible.

## 3. Fixed phase weights

| # | Delivery phase | Weight |
|---|---|---:|
| 1 | Product scope, Owner rules and target architecture | 8% |
| 2 | Repository, environments, provider/runtime and CI foundation | 8% |
| 3 | Cloud data, identity/auth and Service foundation | 12% |
| 4 | Core business Service/API/workflows | 14% |
| 5 | Gateway, adapters, realtime and external integrations | 10% |
| 6 | LAN continuity, local state, offline operation and reconciliation | 16% |
| 7 | Online Web + LAN Web product UI | 10% |
| 8 | Android/PDA App | 10% |
| 9 | Account/admin/reporting/support operational surfaces | 4% |
| 10 | Security, observability, backup/recovery and update safety | 3% |
| 11 | BETA physical pilot, capacity and Owner acceptance | 3% |
| 12 | STABLE production release, deployment and handover | 2% |
|  | **TOTAL** | **100%** |

The detailed substeps and current evidence for these phases live in `docs/DELIVERY_PLAN_V5.md` or its later superseding version.

## 4. Current baseline — 2026-09-14

This baseline reconciles repository source through the LAN Slice-1/business/materialization/media work and the Owner's V7 UI direction.

| Phase | Weight | Current completion | Weighted contribution |
|---|---:|---:|---:|
| 1. Scope / rules / architecture | 8% | 95% | 7.60% |
| 2. Repo / environments / providers / CI | 8% | 85% | 6.80% |
| 3. Cloud data / auth / Service foundation | 12% | 80% | 9.60% |
| 4. Core business Service/API | 14% | 65% | 9.10% |
| 5. Gateway / adapters / integrations | 10% | 55% | 5.50% |
| 6. LAN continuity / offline / reconcile | 16% | 50% | 8.00% |
| 7. Online Web + LAN Web UI | 10% | 20% | 2.00% |
| 8. Android/PDA App | 10% | 15% | 1.50% |
| 9. Account/admin/reporting/support | 4% | 35% | 1.40% |
| 10. Security / observability / recovery | 3% | 50% | 1.50% |
| 11. BETA physical/capacity/UAT | 3% | 10% | 0.30% |
| 12. STABLE production/handover | 2% | 0% | 0.00% |
|  |  | **TOTAL_PRODUCT_COMPLETION = 53.3%** | **53.30%** |

Human-facing status rounds this baseline to **53% complete**.

## 5. Why the project is not scored higher

The repository has substantial architecture, business/data contracts, Cloud/Service foundation and a materially implemented LAN Slice-1 with automated evidence. Recent current-source work includes LAN operational state materialization, idempotent business replay, actor evidence, employee/MNV/attendance business vectors, atomic employee-code uniqueness, durable staged media and a successful staged-media CI workflow.

However, the final product still lacks enough evidence to credit the remaining half as complete: V7 Web visual implementation is not finished; the current Android/PDA product is not yet a fully accepted final App; current-LAN physical regression on the target company network/PDA is pending; the 60-minute internet-cut acceptance is pending; broader business coverage/reconciliation/provider paths remain; capacity/soak/UAT remain; and STABLE production promotion/handover has not been completed.

## 6. Update rules

1. Recalculate the percentage after any material scope change or after evidence closes/opens a delivery gate.
2. A chat statement, plan, mockup or decision may advance a design substep, but cannot by itself mark runtime behavior complete.
3. Automated tests can support implementation completion, but physical/provider-specific gates stay incomplete until the corresponding real evidence exists.
4. Legacy Pick Pack 1291 and old LAN repositories are reference/evidence only unless a current Owner decision explicitly authorizes a particular reference use; they do not automatically add current-product completion credit.
5. If scope expands, the displayed percentage may decrease. That is expected and must not be hidden by changing weights opportunistically.
6. Never round a phase to 100% while an acceptance item required by that phase is still open.
7. `CURRENT_STATE.md` must publish the current rounded percentage, exact computed value, active phase(s), evidence-through commit and date.
8. `CHECKPOINT.md` must identify the latest progress source/version whenever project state is reconciled.

## 7. User-facing display convention

When the Owner asks where the project is or asks to continue work, report at minimum:

`Overall: NN% | Current: Phase X — <name> | Next gate: <gate>`

If parallel work is active, identify the primary phase and the parallel lane rather than pretending the project is strictly sequential.