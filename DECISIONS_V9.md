# DECISIONS V9 — EXECUTION OPTIMIZATION AUTHORITY

Status: ACTIVE / OWNER LOCKED 2026-09-15
Scope: execution, progress measurement, CI/governance cadence and delivery sequencing only.

This layer does **not** replace or weaken the current V3–V7 product/business/security architecture. Where V9 does not explicitly change execution mechanics, all older applicable decisions remain active.

## V9-001 — Vertical slice is the primary delivery unit

The default unit of implementation is an end-to-end acceptance vertical slice, not an isolated helper/mechanic.

A slice should close the relevant path across current contract/domain logic, Cloud and/or LAN runtime, client surface, failure/retry behavior, automated evidence and governance reconciliation as applicable.

A technical task that does not directly close or unblock an acceptance case may proceed only when it is a mandatory architecture, security, migration, provider-safety or release-safety prerequisite. Speculative/general future abstraction is deferred.

## V9-002 — WIP limit and real parallelism

At any time the normal execution queue is limited to:

1. **one integrating vertical slice**; and
2. at most **two independent preparation/client lanes** that do not destabilize the integrating slice.

Blocked provider/physical gates remain visible but do not consume an active WIP slot and do not stop independent READY work.

The normal three-lane shape is:

- **Lane A — current integrating business slice**;
- **Lane B — Web/client work using already stable contracts**;
- **Lane C — next business-domain contract/core preparation**.

Opening more work requires closing, blocking with evidence, or deliberately replacing an existing WIP item.

## V9-003 — Current vertical-slice order

Until later Owner authority changes priority, delivery order is:

1. **Attendance golden path** — scan MNV -> resolve ACTIVE employee + portrait/current presence -> ATTENDANCE_IN/OUT -> Cloud or LAN -> immutable event/current-state update -> visible commit/sync status -> downstream projection where applicable -> retry/restart without duplicate.
2. **Work session + PICK/PACK + resources** — session lifecycle, PICK/PACK, PDA/User Pick/Pack Table/User Pack assignment, release/reissue and current conflict/idempotency rules.
3. **Labor + dropped goods**.
4. **Documents/media** subject to existing portrait-replacement Owner decision gate.
5. Remaining admin/reporting/conflict UI, polish, physical/capacity/soak/UAT and STABLE acceptance.

UI may be developed in parallel against locked contracts/fixtures. Client work must not invent missing business authority or inaccessible final visual details.

## V9-004 — Governance is batched at meaningful boundaries

The preferred delivery shape is one focused PR per meaningful vertical slice or coherent cross-cutting migration, with multiple internal commits when useful.

A separate governance-only PR after every helper/mechanic is no longer the default.

Checkpoint/governance reconciliation is required at meaningful boundaries including:

- risky provider mutation or migration;
- security-sensitive change;
- Owner decision/permission gate;
- vertical-slice PASS/FAIL acceptance boundary;
- release/promotion boundary;
- interruption where unreconciled state could cause unsafe resume.

A helper/class/test addition alone is not a mandatory checkpoint boundary.

## V9-005 — Single-source ownership of volatile truth

Repository information ownership is:

- Owner/product/business decisions -> `DECISIONS*.md`;
- stable product scope -> `PROJECT_SCOPE.md`;
- provider/resource identities and authority boundaries -> `SERVICE_AUTHORITY.md`;
- volatile operational/runtime/product evidence -> `CURRENT_STATE.md`;
- current READY/BLOCKED queue and WIP -> `NEXT_ACTIONS.md`;
- project percentage and evidence-credit ledger -> current `docs/PROGRESS_TRACKING_*.md` authority;
- resume reconciliation point -> `CHECKPOINT.md`;
- routing to the above -> `CONTEXT_INDEX.md`.

Files that do not own a volatile value should reference its owner instead of copying the current percentage/live status. If an old duplicated value conflicts with its owning file, the owner named above wins and the duplicate must be removed at the next reconciliation.

## V9-006 — Evidence-credit progress model

The 12 existing product phases and their top-level weights remain fixed unless Owner authority explicitly changes scope weighting.

Each phase now has fixed subweights expressed as 20 evidence credits. One phase credit equals 5% completion of that phase. Credits are earned only by evidence-backed accepted checkpoints and are never awarded for plans, chat discussion, mockups without acceptance, governance edits, tool-call volume or legacy evidence alone.

The current progress authority is `docs/PROGRESS_TRACKING_V2.md`, which supersedes V1 for current percentage reporting.

## V9-007 — Layered CI

CI validation is evidence-risk based:

- **Tier 1 — fast branch/commit:** syntax, contract checks and focused unit tests for affected code;
- **Tier 2 — vertical-slice PR:** affected Cloud/LAN/Web/Android/harness/parity/security tests;
- **Tier 3 — pre-merge:** clean-baseline authority/schema validation plus affected product foundations;
- **Tier 4 — post-merge main:** affected integration/product foundation confirmation.

Existing focused workflows/harnesses should be reused. Do not create or run redundant full-product validation merely to satisfy process when the affected-scope evidence is already covered. High-risk migration/provider/security paths keep their dedicated guarded workflows.

## V9-008 — Definition of done for a slice

A vertical slice is PASS only when all applicable items are evidenced:

1. locked contract/authority mapping;
2. Cloud/LAN semantic parity where both runtimes apply;
3. user-facing client path or explicitly documented client-independent acceptance;
4. idempotency/retry/failure behavior;
5. focused automated tests/harnesses;
6. required CI tier PASS;
7. provider/physical evidence kept separate from source/CI evidence;
8. current state, progress credits and next queue reconciled once at the meaningful boundary.

Partial mechanics may be SOURCE/CI PASS but must not be represented as full user-flow acceptance.

## V9-009 — Optimization must not trade away correctness

Speed optimization may remove duplicate reads, duplicate status copies, redundant PR/governance roundtrips and unnecessary full-CI work. It must not remove fail-closed behavior, authority checks, security boundaries, idempotency, auditability, BETA/STABLE isolation, physical gates or evidence requirements.
