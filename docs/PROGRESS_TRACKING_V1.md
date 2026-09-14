# VHDCHY PROJECT PROGRESS TRACKING V1

Status: ACTIVE
Effective date: 2026-09-14
Purpose: provide one evidence-based completion percentage from project start to final production acceptance.

## 1. Primary progress metric

`TOTAL_PRODUCT_COMPLETION` measures completion of the entire target product: Android/PDA App + Online Web + LAN Web + Service/Gateway/LAN continuity + operational/release readiness.

It is not a measure of elapsed time, number of commits, number of documents, number of tool calls, or developer effort. A design decision alone cannot be counted as if the corresponding product behavior were already implemented.

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

This baseline reconciles repository source through the secure LAN HTTPS login/session + reviewed public Slice-1 business HTTP route E2E, durable staged-media/reconciliation queue work, the machine-authenticated Cloud operational snapshot/coverage route, LAN signed refresh/rebase/readiness recovery automation, the Vietnamese-only V7 Web shell, and current product-foundation CI evidence.

| Phase | Weight | Current completion | Weighted contribution |
|---|---:|---:|---:|
| 1. Scope / rules / architecture | 8% | 95% | 7.60% |
| 2. Repo / environments / providers / CI | 8% | 85% | 6.80% |
| 3. Cloud data / auth / Service foundation | 12% | 80% | 9.60% |
| 4. Core business Service/API | 14% | 65% | 9.10% |
| 5. Gateway / adapters / integrations | 10% | 55% | 5.50% |
| 6. LAN continuity / offline / reconcile | 16% | **65%** | **10.40%** |
| 7. Online Web + LAN Web UI | 10% | **25%** | **2.50%** |
| 8. Android/PDA App | 10% | 15% | 1.50% |
| 9. Account/admin/reporting/support | 4% | 35% | 1.40% |
| 10. Security / observability / recovery | 3% | 50% | 1.50% |
| 11. BETA physical/capacity/UAT | 3% | 10% | 0.30% |
| 12. STABLE production/handover | 2% | 0% | 0.00% |
|  |  | **TOTAL_PRODUCT_COMPLETION = 56.2%** | **56.20%** |

Human-facing status rounds this baseline to **56% complete**.

### 2026-09-15 evidence closure with no percentage change

The previously source-advanced Google/Drive integration receipt conflict/recovery layer now has direct hosted-CI proof without changing the weighted baseline. Dedicated workflow `34879543693` at commit `da216d54119280aeed57f19d79ccdde6091e6036` completed `SUCCESS`; its production-runtime regression step proves integration interrupted-claim startup recovery, `/health` recovery visibility, `/api/v1/sync/status` receipt/review visibility, aggregate conflict status including integration `REVIEW_REQUIRED`, and preservation of the existing reconciliation/rebase vectors. Same-commit clean baseline `34879543810` also completed `SUCCESS`.

Historical runs `34853854932` at conflict commit `53ae98a530228badaeaeb9dc2cb03a905ec8df82` and `34853938581` at recovery commit `3d0a732e33a8d4ce4ac97c3e4efaf9f58838381c` remain supporting evidence for receipt-conflict/cloud-conflict/restart mechanics; recovery clean baseline `34853938669` was also successful.

This closes an automated-evidence gap inside the Phase-6 reconciliation sub-slice already scored at 65%. It does **not** close live BETA machine credentials/provider linkage, real Google Sheets/Drive provider I/O, company-network/browser/PDA trust, >=60-minute outage acceptance, capacity/soak/UAT or STABLE promotion. Therefore Phase 6 remains 65% and `TOTAL_PRODUCT_COMPLETION` remains **56.2%**.

### Evidence supporting the increase from 55.4% to 56.2%

**Phase 6: 60% -> 65%.**

The post-reconciliation path is now materially implemented and automated rather than stopping at queue/tracker primitives. Cloud exposes a machine-authenticated Slice-1 operational snapshot route with real D1 state and explicit canonical-event coverage. LAN has the matching signed HTTP client, authoritative refresh coordinator/pump, atomic operational snapshot import, post-reconciliation rebase confirmation, and fail-closed readiness while canonical events remain unre-based. Historical canonical coverage is restart-safe by persistent `edgeInstanceId` while the request still carries the current `edgeEpoch` for machine identity.

Dedicated integration workflow `34851773729` at source commit `42590dcf73ecbe8d1d8ee8dfbd6275aabf9d64fd` completed `SUCCESS`. It proves signed HMAC request construction, exact requested canonical coverage, epoch rotation after restart, rejection of incomplete coverage without changing the active snapshot, atomic import of the complete authoritative snapshot, rebase cursor clearing, and readiness recovery. Clean-baseline workflow `34851772963` on the same source commit also completed `SUCCESS`, including authority checks, Worker unit tests, clean D1 schema and schema/runtime contract validation.

This increase credits an `IMPLEMENTED_AUTOMATED` reconciliation sub-slice only. It does **not** credit live BETA machine credential provisioning/provider transport acceptance, exact live provider linkage, public-trust/canonical-DNS/real PDA acceptance, the >=60-minute Internet-cut physical window, capacity/soak/UAT, or STABLE activation.

### Earlier evidence supporting the increase from 54.6% to 55.4%

**Phase 6: 55% -> 60%.**

The LAN runtime has an implemented secure public HTTP slice rather than only an internal coordinator: direct Kestrel HTTPS using a user-space PFX, a fail-closed HTTP read-only fallback when TLS is absent, paired-device signed normal-user login, durable session issuance, and the reviewed signed-session `POST /api/v1/data/commands` path into `LanBusinessRouteCoordinator`. Dedicated workflow `34811861697`, job/check `103874646267`, at commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1` completed `SUCCESS`. It proves TLS-only credential handling at runtime, wrong-password and signed-body tamper rejection, login/business replay rejection, authorized `EMPLOYEE_CREATE`, restart session continuity, no captured password leakage, and SQLite integrity. Baseline workflow `34811861613` on the same HEAD also completed `SUCCESS`.

This is credited as `IMPLEMENTED_AUTOMATED` progress for a material LAN sub-slice, not physical acceptance. It does not credit publicly trusted BETA certificate issuance/renewal, canonical DNS and real company-network/browser/PDA trust, ROOT email-OTP HTTP flow, public password-change/recovery, live Cloud reconciliation machine credential/provider acceptance, portrait semantic resolution, >=60-minute Internet-cut physical continuity, capacity/UAT, or STABLE activation. See `docs/LAN_SECURE_HTTP_V1.md`.

### Earlier evidence retained in the baseline

The historical measured baseline before the durable reconciliation + Web-shell increase was **53.3%**; subsequent accepted evidence moved it to 54.6%, then secure LAN HTTP moved it to 55.4%, and the automated operational refresh/rebase slice moves the current exact baseline to 56.2%. These historical markers are retained for audit/validator continuity and are not the current percentage.

**Phase 6 durable reconciliation foundation.**

Current LAN source has a durable Cloud reconciliation queue state machine with claim/retry/reconcile/conflict transitions, restart recovery for interrupted claims, immutable edge-event envelope construction, completed Google/Drive receipt attachment and race-safe single-claim behavior. Dedicated workflow `34801533266` at commit `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66` completed `SUCCESS`; earlier vector run `34801198095` also completed `SUCCESS`.

**Phase 7: 20% -> 25% earlier increase.**

The shared Online/LAN Web source contains a V7 Vietnamese-only shell with the approved DNSHE-inspired VHDCHY visual direction, responsive navigation/dashboard/status surfaces, explicit Cloud/LAN/sync/Google/conflict presentation, local/offline-safe critical UI assets and no DNSHE branding/assets. Product-foundation workflow `34801611019` at commit `41565f3b2ffdca473f756e33bd71769e16d8af13` completed `SUCCESS`, including the `web-contract`, `cloud-service`, `android-apk` and `lan-service` jobs. This still does not credit missing authenticated Web login, completed business screens, conflict-resolution UI or full Web E2E acceptance.

The Android phase remains at 15%: visible shell text is Vietnamese and the current APK builds, but this language cleanup alone is not sufficient to advance the product phase. The actual Pick Pack 1291 visual source/artifacts still need to be surfaced before claiming faithful UI reuse.

## 5. Why the project is not scored higher

The repository has substantial architecture, business/data contracts, Cloud/Service foundation and a materially implemented LAN Slice-1 with automated evidence. LAN now also includes durable staged media, durable Cloud-sync queue mechanics, an automated secure HTTPS login/session -> public Slice-1 business route, an automated signed operational refresh/rebase/readiness-recovery path, and hosted-CI-proven integration receipt conflict/recovery status behavior.

However, the final product still lacks enough evidence to credit the remaining work as complete: the production-trusted LAN certificate/DNS/real-device path is unproven; current Android/PDA product UI is not yet faithfully reconstructed from the authorized Pick Pack 1291 visual source; Web authentication and business modules are incomplete; live BETA machine credential/provider LAN->Cloud transport and exact provider linkage are unaccepted; real Google Sheets/Drive provider I/O remains unaccepted; target-company-network/NLS-MT90 physical regression is pending; the 60-minute Internet-cut acceptance is pending; broader business/provider paths remain; capacity/soak/UAT remain; and STABLE production promotion/handover has not been completed.

## 6. Update rules

1. Recalculate the percentage after any material scope change or after evidence closes/opens a delivery gate.
2. A chat statement, plan, mockup or decision may advance a design substep, but cannot by itself mark runtime behavior complete.
3. Automated tests can support implementation completion, but physical/provider-specific gates stay incomplete until the corresponding real evidence exists.
4. Legacy Pick Pack 1291 and old LAN repositories are reference/evidence only unless a current Owner decision explicitly authorizes a particular reference use; they do not automatically add current-product completion credit.
5. If scope expands, the displayed percentage may decrease. That is expected and must not be hidden by changing weights opportunistically.
6. Never round a phase to 100% while an acceptance item required by that phase is still open.
7. `CURRENT_STATE.md` must publish the current rounded percentage, exact computed value, active phase(s), evidence-through commit and date.
8. `CHECKPOINT.md` must identify the latest progress source/version whenever project state is reconciled.
9. Source edits or tool activity without successful validation do not increase progress. If a long execution block produces no new accepted evidence, the percentage remains unchanged.

## 7. User-facing display convention

When the Owner asks where the project is or asks to continue work, report at minimum:

`Overall: NN% | Current: Phase X — <name> | Next gate: <gate>`

If parallel work is active, identify the primary phase and the parallel lane rather than pretending the project is strictly sequential.

Before ending a long tool/session block, report factual PASS/FAIL/IN_PROGRESS/BLOCKED state, direct evidence IDs where available, current exact/rounded progress, and any justified delta from the start of that block.
