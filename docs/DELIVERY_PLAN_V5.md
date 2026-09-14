# VHDCHY DELIVERY PLAN V5 — START TO FINAL PRODUCT

Status: ACTIVE DELIVERY PLAN
Effective date: 2026-09-14
Progress model: `docs/PROGRESS_TRACKING_V1.md`
UI direction: `DECISIONS_V7.md`
Supersedes: `docs/DELIVERY_PLAN_V4.md` for overall delivery sequencing/progress. Older plans remain historical evidence.

## Executive position

The project is **55.4% complete (display: 55%)** against the complete target product.

Primary active phase: **Phase 6 — LAN continuity, local state, offline operation and reconciliation**.

Parallel incomplete lanes: **Phase 4 — broader core business Service/API**, **Phase 5 — Gateway/integrations**, and **Phase 7/8 UI/App implementation**.

The project is past initial scope/architecture/foundation. It is in the middle of implementation: a current LAN/business Slice-1, durable staged-media/reconciliation queue and secure LAN HTTP foundation exist with automated evidence, but the final Web/App surfaces, publicly trusted LAN certificate/target-host acceptance, 60-minute internet-cut acceptance, full business/provider coverage, BETA UAT/capacity, and STABLE production promotion remain incomplete.

---

## Phase 1 — Product scope, Owner rules and target architecture

**Weight:** 8%  
**Current completion:** 95%  
**Status:** ALMOST COMPLETE / ongoing decision maintenance

### 1.1 Scope and product surfaces — DONE

- Define one VHDCHY product consisting of Service/Cloud runtime, Online Web, LAN Web, Android/PDA App and LAN host/edge continuity.
- Define BETA vs STABLE separation.
- Define canonical business-data authority and projection/output boundaries.
- Define reference-only treatment of legacy projects/resources.

### 1.2 Business rules and data semantics — DONE / maintained

- Employee identity and employee-code/MNV lifecycle.
- Attendance/presence and immutable event history.
- Session/task/resource/labor/dropped-goods/document/media semantics.
- Idempotency, device sequence, conflict/correction and audit expectations.
- Permission/business-rule authority captured in current decision/rule documents.

### 1.3 Online/LAN/offline architecture — DONE as architecture

- One product with cloud and LAN execution modes rather than separate business products.
- Local continuity with local state/queue/reconciliation under internet loss.
- LAN host constrained to ordinary-user/no-admin corporate Windows operation.
- Current canonical offline acceptance target: minute-level continuity with **Window 2 >= 60 minutes** after warmup/cut; restoration sync occurs outside that timed window.

### 1.4 Account/language/UI direction — DONE as decision

- Current Web/App acceptance UI is **Vietnamese only**; Vietnamese/English/Chinese support is deferred to a later feature phase.
- Account context/identity rules established, including V6 ROOT email-OTP and normal-account recovery semantics.
- Android/PDA UI direction = Pick Pack 1291 reference adapted to VHDCHY.
- Online Web + LAN Web UI direction = DNSHE-style visual language from Owner screenshots, without copying DNSHE branding/assets.

### 1.5 Architecture closure — REMAINING

- Continue eliminating stale older wording where it could misroute future implementation.
- Incorporate later Owner decisions without allowing older documents to override them.

**Next gate:** all currently active top-level architecture/authority files independently point to the same V3–V7 model.

---

## Phase 2 — Repository, environments, providers/runtime and CI foundation

**Weight:** 8%  
**Current completion:** 85%  
**Status:** MATERIAL IMPLEMENTATION COMPLETE / final production separation pending

### 2.1 Canonical repository and AI operating model — DONE

- Canonical repo established on `tamnv2/vanhanhsupradchungyen` / `main`.
- AI entrypoint, operating contract, context routing and checkpoint model established.
- Changelog/release-history retention principles established.

### 2.2 BETA environment/provider baseline — SUBSTANTIALLY DONE

- Current provider/runtime identities and deployment boundaries established.
- Cloud/Google/GitHub/runtime setup has working BETA paths and project authority records.
- Google operational account scope has been changed to the currently available authorized account while the separate domain account issue remains non-blocking for core implementation.

### 2.3 CI/build guardrails — SUBSTANTIALLY DONE

- Automated source validation exists for multiple current LAN/service slices.
- Secure LAN HTTP, reconciliation queue, staged-media and product-foundation paths have accepted automated evidence on reviewed source lineage.
- Android signing/release groundwork exists, but current final App build path still needs to converge with the current-product implementation.

### 2.4 STABLE provider/runtime separation — REMAINING

- Materialize STABLE provider configuration and deployment automation without reusing BETA business data.
- Verify fail-closed behavior before production activation.
- Ensure promotion uses the exact accepted BETA release/artifacts.

**Next gate:** BETA and STABLE source/runtime/release automation are fully explicit and production promotion can occur without ad-hoc setup.

---

## Phase 3 — Cloud data, identity/auth and Service foundation

**Weight:** 12%  
**Current completion:** 80%  
**Status:** IMPLEMENTED FOUNDATION / coverage hardening remaining

### 3.1 Canonical data model — SUBSTANTIALLY DONE

- Canonical business schema covers configuration/platform data, identity linkage, attendance/presence, sessions/tasks, resources, labor, dropped goods, documents/media, immutable domain events, projection/outbox, conflicts, snapshots/archive and telemetry.
- Employee-code lifecycle is separated from employee identity.
- Idempotent event/device-sequence semantics are represented.
- Reviewed source migrations now include additive edge reconciliation, V6 email-OTP and dynamic permission-catalog foundations; live provider application must still be verified per environment.

### 3.2 Authentication/account context — SUBSTANTIALLY DONE as contracts and current service paths

- V6 ROOT and normal-account recovery semantics are locked.
- Password/session/email-OTP primitives have current source implementation evidence.
- Public Cloud password-login/change-password routing is under current implementation/validation; ROOT email-OTP delivery/verification provider integration remains incomplete until verified end-to-end.
- Provider-evidence-before-routing hardening exists in recent source lineage.

### 3.3 Service runtime/API foundation — SUBSTANTIALLY DONE

- Current Service API contracts and permission/error conventions exist.
- Status/health/operator surfaces and environment awareness have material implementation evidence.
- Current user-facing Web/App acceptance language is Vietnamese only; multilingual product UI is deferred by V7.

### 3.4 Remaining foundation work

- Verify/apply intended additive migrations to exact provider environments using controlled workflows.
- Ensure current auth, provider linkage and permission semantics are identical across Online/LAN entry paths where applicable.
- Complete ROOT email-OTP request/delivery/verify E2E without exposing credentials.

**Next gate:** all current business adapters and LAN reconciliation use one canonical current schema/auth contract with no legacy contract ambiguity.

---

## Phase 4 — Core business Service/API/workflows

**Weight:** 14%  
**Current completion:** 65%  
**Status:** IN PROGRESS

### 4.1 Employee + MNV/employee-code workflows — CURRENT WORKING SLICE

- Current LAN/business adapter includes employee/MNV vectors.
- Atomic employee-code uniqueness was implemented and validated by harness/CI-oriented work.
- Actor evidence and idempotent replay preflight exist in current source lineage.

### 4.2 Attendance/presence — CURRENT WORKING SLICE

- Attendance business vectors exist.
- Repeated-IN semantics were aligned in current LAN Slice-1 implementation.
- Operational state materialization/snapshot behavior has automated evidence.

### 4.3 Sessions, Pick/Pack tasks and resource assignment — PARTIAL / REMAINING

- Domain decisions/data model exist.
- Complete current Service + LAN + Web/App runtime coverage still needs material implementation/acceptance across normal and conflict cases.

### 4.4 Labor, dropped goods, borrowing/reuse/resource lifecycle — PARTIAL / REMAINING

- Rules/schema coverage exists.
- Full current API/business adapter/user-flow test matrix remains to be closed.

### 4.5 Documents/media — PARTIAL, recent progress

- Durable staged-media local store/schema/lifecycle and automated harness now exist.
- End-to-end canonical metadata, provider upload/projection, retry, reconciliation and user-facing state still require full-path verification.

### 4.6 Permissions/idempotency/audit/conflict correction — PARTIAL

- Strong contract/data-model groundwork and several implemented protections exist.
- Need complete coverage for every mutation family and every Online/LAN/App entry path.

**Next gate:** every locked Owner business rule maps to a current API/adapter implementation plus automated scenario coverage, not only schema/design evidence.

---

## Phase 5 — Gateway, adapters, realtime and external integrations

**Weight:** 10%  
**Current completion:** 55%  
**Status:** IN PROGRESS

### 5.1 Gateway command/event path — MATERIAL PARTIAL

- Gateway/operator action workflow and signed-in staged action flow have implementation evidence.
- Provider evidence is fetched before main routing in recent source lineage.
- Message-only Zalo manual-send recovery rule remains current for the approved v1 scope.

### 5.2 Google Drive/Sheets outputs/projections — PARTIAL

- Google output is a projection/integration path, not canonical business authority.
- Current account/provider scope has been re-established after the locked Google account issue.
- Need complete receipt/deduplication/retry/reconciliation behavior for current LAN+Cloud flows.

### 5.3 Realtime/status path — PARTIAL

- Current architecture and legacy-adapted design cover stream epoch/sequence/resync concepts.
- Current-product implementation must close full reconnect/resync/status coverage and UI state propagation.

### 5.4 Camera/media/provider adapters — PARTIAL

- FCVT must use official FisherCamera release, never an emulator, for acceptance.
- Durable local staged-media work exists.
- Full provider upload/receipt/retry and current physical capture path remain.

### 5.5 Notifications/support adapters — REMAINING/PARTIAL

- Complete bounded failure/recovery behavior and operator-visible status.

**Next gate:** all external/provider actions are idempotent, diagnosable, recoverable and have evidence for both success and failure paths.

---

## Phase 6 — LAN continuity, local state, offline operation and reconciliation

**Weight:** 16%  
**Current completion:** 60%  
**Status:** PRIMARY ACTIVE PHASE — MATERIAL AUTOMATED FOUNDATION, PHYSICAL/PUBLIC-TRUST ACCEPTANCE PENDING

### 6.1 No-admin Windows LAN host — MATERIAL SOURCE/CI PASS, target host pending

- Portable ordinary-user Windows operation is a hard requirement.
- Current Windows no-admin host packaging/certificate-manager lineage has automated evidence; the current package line includes the reviewed `0.2.2` host bundle.
- Real company Windows host/public-trust/browser/PDA acceptance is still required and cannot be replaced by CI evidence.

### 6.2 LAN discovery/endpoint/reconnect — DESIGN + reusable source evidence

- Cached healthy endpoint -> LAN discovery -> manual recovery ordering is approved as a reusable pattern.
- Anti-flapping/reconnect/resync concepts are approved.
- Current company-network physical proof remains required.

### 6.3 Local operational state and snapshots — CURRENT WORKING SLICE

- Current source includes LAN operational state materialization and an automated harness proving Slice-1 snapshot semantics.

### 6.4 Durable events, idempotent replay and actor evidence — CURRENT WORKING SLICE

- Idempotent replay preflight exists.
- Atomic actor evidence capture exists.
- Employee/MNV/attendance business vectors and uniqueness handling exist.

### 6.5 Durable staged media — CURRENT WORKING SLICE WITH CI PASS

- Durable media store/schema, serialized identity claims and lifecycle harness exist.
- Dedicated staged-media automated evidence is retained in project history.

### 6.6 LAN auth/pairing/security epoch — MATERIAL PARTIAL

- Secure public LAN HTTP slice exists with Kestrel HTTPS/user-space PFX, fail-closed HTTP behavior, paired-device signed normal-user login, durable session issuance and signed-session Slice-1 business routing.
- Complete ROOT email-OTP HTTP flow, broader permission/business route coverage and final provider/security E2E remain.

### 6.7 LAN Web local shell/assets/domain — PARTIAL / REMAINING

- Canonical LAN domain direction exists.
- V7 requires LAN-critical UI assets to work without internet dependencies; current shared Web shell provides material local/offline-safe foundation.
- Publicly trusted certificate issuance/renewal on the target host, canonical DNS resolution and real browser/PDA trust remain unaccepted.

### 6.8 Reconciliation and conflict handling — MATERIAL PARTIAL

- Durable Cloud-reconciliation queue mechanics, claim/retry/restart/conflict state and immutable edge envelope foundations have automated evidence.
- Complete machine/service-authenticated Cloud network ingestion, exact provider linkage, Google receipts E2E and conflict-resolution workflow remain.

### 6.9 Physical internet-cut test — NOT YET ACCEPTED

- Warm up connected state.
- Cut internet while retaining valid LAN connectivity.
- Prove App + LAN Web + local workflow remain usable for **>= 60 minutes** in Window 2.
- External internet embeds may fail; this does not fail local continuity.
- Restore internet after timed window and separately verify reconciliation/sync.

### 6.10 Physical device/network/capacity test — NOT YET ACCEPTED

- Current corporate laptop/network reachability/discovery.
- Real NLS-MT90 behavior, reconnect, Wi-Fi loss, queue recovery.
- Background lifecycle/battery footprint.
- Host restart/network-change reacquisition.
- No-admin update/rollback on target laptop.
- Synthetic 10/25/50/100 Agent/client capacity plus soak.

**Next gate:** target company Windows host -> ACME staging -> production public CA -> Windows/browser trust -> real NLS-MT90 -> >=60-minute Internet-cut acceptance, while independent source lanes continue in parallel.

---

## Phase 7 — Online Web + LAN Web product UI

**Weight:** 10%  
**Current completion:** 25%  
**Status:** V7 SHARED SHELL MATERIALIZED / business UI not final

### 7.1 Shared design system — MATERIAL PARTIAL

- One Web design system serves Online and LAN modes.
- Visual direction: dark navy navigation; light blue/white page field; white rounded cards; restrained borders/shadows; royal-blue primary actions; compact status/icon tiles.
- VHDCHY identity/assets are used; DNSHE branding/assets are not copied.

### 7.2 Login/authentication shell — REMAINING

- Central focused sign-in card.
- Context/security/support areas where useful.
- Account behavior compliant with V6.

### 7.3 Authenticated dashboard shell — MATERIAL PARTIAL

- Current shared Vietnamese shell has responsive navigation/dashboard/status surfaces and explicit Cloud/LAN/sync/Google/conflict presentation.
- It is not considered complete until authenticated data/business flows are wired and accepted.

### 7.4 Business modules — REMAINING

- Employee/personnel, attendance/presence, Pick/Pack/session/task/resource, labor, dropped goods, media/documents, diagnostics/other approved modules.
- Permission-aware actions and clear loading/error/conflict states.

### 7.5 Online/LAN parity and network-state UX — PARTIAL / REMAINING

- Same navigation language/hierarchy in Online and LAN modes.
- Clear state where behavior differs: Online / LAN active / degraded-reconnecting / queued-local-only as applicable.
- No confusing split into two independent products.

### 7.6 Offline-safe LAN assets — MATERIAL FOUNDATION

- Core LAN-critical assets are required to be local/offline-capable and current source has an offline-safe shell foundation.
- Full business-screen acceptance under outage remains open.

### 7.7 Responsive + language acceptance — CURRENT V7 RULE

- Current Web acceptance UI is **Vietnamese only**.
- Vietnamese/English/Chinese implementation is deferred to a later feature phase and is not a current acceptance requirement.
- Desktop-first Web console remains responsive to supported smaller screens.

**Next gate:** wire authenticated login/session and current business modules into the shared V7 shell with Online/LAN parity tests.

---

## Phase 8 — Android/PDA App

**Weight:** 10%  
**Current completion:** 15%  
**Status:** REFERENCE DIRECTION LOCKED / current final app not accepted

### 8.1 Inspect Pick Pack 1291 UI reference — PARTIAL

- Owner authorized `BACKUP PICK PACK 1291` for UI/UX reference only.
- The Drive backup exists and is accessible as a reference container.
- Detailed App UI source/evidence has not yet been surfaced sufficiently to justify inventing exact visual details; inspect available source/artifacts before visual finalization.

### 8.2 Current App design system — REMAINING

- Reuse recognizable Pick Pack 1291 interaction/layout direction where appropriate.
- Adapt terminology, workflows, permissions, network states and information hierarchy to VHDCHY.

### 8.3 Auth/account/device state — REMAINING/PARTIAL architecture

- Account context, device identity, transport state and current permissions.
- Clear Online/LAN/queue/reconnect status without obstructing warehouse workflow.

### 8.4 Warehouse business workflows — REMAINING

- Implement current approved scan/session/Pick/Pack/resource/attendance and related PDA flows according to current business rules.
- Fast scanner-first interaction and robust error/retry handling.

### 8.5 Durable local queue/offline recovery — PARTIAL reusable design

- Persistent device sequence and durable pending queue.
- ACK-driven deletion, automatic recovery after LAN reacquisition, epoch reset/resync.
- Adapt legacy-proven patterns to current contracts rather than copy old pilot assumptions.

### 8.6 Camera/media — REMAINING

- Current official camera/provider behavior and durable media staging/upload/retry state.

### 8.7 Background/battery/update/package/sign — PARTIAL foundation / remaining current acceptance

- Bounded background behavior.
- Current APK signing/release pipeline.
- Physical battery/background lifecycle and update acceptance.

### 8.8 Language/device acceptance — CURRENT V7 RULE

- Current Android/PDA acceptance UI is **Vietnamese only**; multilingual implementation is deferred.
- Real NLS-MT90 layout/scanner/performance validation remains required.

**Next gate:** surface the Pick Pack 1291 UI reference, create the current App shell/design tokens, then implement the highest-frequency PDA business flow end-to-end against current Service/LAN contracts.

---

## Phase 9 — Account/admin/reporting/support operational surfaces

**Weight:** 4%  
**Current completion:** 35%  
**Status:** PARTIAL

### 9.1 Account/tenant context — PARTIAL

- Display current account/environment/site/cluster context where operationally relevant.
- Account operations remain distinct from warehouse business identity where current rules require it.

### 9.2 Enterprise administration — PARTIAL/REMAINING

- Current approved account/tenant/service operations menu and permission surfaces.
- Configuration/status surfaces without exposing secrets.

### 9.3 Reporting/search/export — REMAINING/PARTIAL

- Operational summaries, event/history search, export/report outputs and Google projections as approved.
- Preserve canonical authority and auditability.

### 9.4 Support/diagnostics — PARTIAL

- Health/status/diagnostics export patterns exist.
- Final Web/App presentation and safe redaction rules need acceptance.

**Next gate:** required supervisor/admin/support workflows are fully usable without manual database/provider intervention for normal operations.

---

## Phase 10 — Security, observability, backup/recovery and update safety

**Weight:** 3%  
**Current completion:** 50%  
**Status:** PARTIAL

### 10.1 Security — PARTIAL

- Fail-closed direction, permissions, idempotency and evidence capture have current implementation.
- Finish LAN pairing/auth/security epoch, ROOT OTP provider path, secret handling, provider scopes and end-to-end threat review.

### 10.2 Observability — PARTIAL

- Health/status/metrics/diagnostic patterns exist.
- Finish useful Service/Gateway/LAN/App metrics, failure classification and operator visibility.

### 10.3 Backup/recovery — PARTIAL

- Recovery/export concepts and project backup artifacts exist.
- Verify current canonical data restore/export runbook and provider-independent owner recovery path.

### 10.4 Safe update/rollback — PARTIAL design/reference

- No-admin staged update/hash/health/rollback pattern is reusable.
- Current host/App release rollback must be verified on target environment.

**Next gate:** documented recovery/update/security procedures are exercised, not only described.

---

## Phase 11 — BETA physical pilot, capacity and Owner acceptance

**Weight:** 3%  
**Current completion:** 10%  
**Status:** EARLY / current physical acceptance pending

### 11.1 Full automated acceptance matrix — PARTIAL

- Existing matrix and multiple current harnesses provide a base.
- Reconcile all stale rows to V6/V7/current contracts and close missing business scenarios.

### 11.2 Target hardware/network pilot — NOT CURRENTLY ACCEPTED

- Company ordinary-user laptop.
- Real NLS-MT90 PDA devices.
- Real company network constraints.

### 11.3 Internet-cut acceptance — NOT CURRENTLY ACCEPTED

- Prove >=60-minute LAN continuity window under the V6 rule.

### 11.4 Capacity/soak — NOT CURRENTLY ACCEPTED

- Synthetic 10/25/50/100 load levels plus soak/resource stability.

### 11.5 Owner UAT — NOT CURRENTLY ACCEPTED

- End-to-end business operation across Online Web, LAN Web, App and Service/Gateway.
- UI/workflow confirmation against V7 and current business rules.

**Next gate:** Owner accepts an exact BETA release with all mandatory physical/business/security gates green.

---

## Phase 12 — STABLE production release, deployment and handover

**Weight:** 2%  
**Current completion:** 0%  
**Status:** NOT STARTED as final acceptance

### 12.1 Exact-release promotion

- Promote the exact Owner-accepted BETA source/artifacts; do not rebuild a different production artifact without justification/re-acceptance.

### 12.2 Clean STABLE environment

- Provision STABLE runtime/configuration separately.
- Do not copy BETA business/runtime data into STABLE unless an explicit approved migration says otherwise.

### 12.3 Production deployment

- Service, Online Web, LAN package/Web shell, Android APK/release channel and required provider configuration.
- Verify health, permissions, domain/routing and rollback.

### 12.4 Final documentation/handover

- Operator/admin runbook.
- Install/update/recovery procedure.
- Backup/export procedure.
- Release notes/changelog preserved.
- Owner acceptance and stable baseline checkpoint.

**Final gate:** STABLE operates the accepted product in production, recovery/rollback is proven, and project authority/checkpoint identifies the exact production baseline.

---

## Current execution order from this point

The plan is not strictly serial: independent work should run in parallel when it does not create contract drift. The recommended current ordering is:

1. **Primary physical gate:** target company Windows host -> ACME staging -> production public CA -> Windows/browser trust -> real NLS-MT90 -> >=60-minute continuity acceptance when target hardware/network and least-privilege DNS credentials are available.
2. **Parallel source lane:** close Phase 3/4/5 auth/business/provider paths and tests, including ROOT email-OTP E2E and Cloud/LAN reconciliation transport.
3. **Parallel UI lane:** wire authenticated/business Phase 7 Web flows; surface Pick Pack 1291 App UI evidence and continue Phase 8 App shell/business flows without inventing missing visuals.
4. Execute Phase 11 physical target-network/PDA, load/soak and UAT gates when prerequisites are ready.
5. Only after exact BETA acceptance, execute Phase 12 STABLE promotion/deployment/handover.

## Progress reporting

Current baseline:

`Overall: 55% (55.4 exact) | Current: Phase 6 — LAN continuity/offline/reconcile | Parallel: Phase 3/4/5 auth/business/provider + Phase 7/8 Web/App | Next physical gate: target host -> public trust -> real PDA -> >=60-minute Internet cut`

Update this line and `docs/PROGRESS_TRACKING_V1.md` whenever material accepted evidence changes.