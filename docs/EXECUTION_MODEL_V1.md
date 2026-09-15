# VHDCHY EXECUTION MODEL V1

Status: ACTIVE
Effective: 2026-09-15
Authority: `DECISIONS_V9.md`

## Objective

Deliver the approved VHDCHY target faster without reducing authority, security, correctness, evidence quality or BETA/STABLE isolation. Execution is optimized around acceptance throughput rather than the number of individual mechanics completed.

## Operating loop

Use this loop for normal product work:

`authority -> acceptance slice -> dependency graph -> WIP selection -> parallel implementation -> affected CI -> E2E evidence -> one reconciliation -> progress credits -> next slice`

Do not default to:

`mechanic -> governance -> mechanic -> governance -> mechanic -> governance`

unless each boundary is independently high-risk and requires its own checkpoint.

## WIP board

Normal maximum active WIP is three lanes.

| Lane | Purpose | Current assignment |
|---|---|---|
| A | Integrating vertical business slice | Attendance golden path |
| B | Independent client/Web surface on stable contracts | Web business UI/parity work that does not block Lane A |
| C | Next-domain preparation | Work session + PICK/PACK + resources contract/core preparation |

Provider/physical tasks that cannot run now are recorded as GATED/BLOCKED and do not occupy WIP.

### WIP admission rule

A new item enters active WIP only if at least one is true:

- it directly closes the current slice;
- it directly unblocks the current slice;
- it is independent client/domain preparation with a stable contract and will create accepted product evidence;
- it is a mandatory security/migration/provider/release-safety action.

Otherwise it stays in backlog.

## Slice 1 — Attendance golden path

Acceptance path:

`PDA scan MNV -> authenticated scan-context resolution -> ACTIVE employee/name/portrait/current presence -> ATTENDANCE_IN or ATTENDANCE_OUT -> Cloud or LAN -> immutable event + current presence -> commit/sync status -> Google projection when applicable -> retry/restart no duplicate`

Immediate dependency graph:

1. Cloud + LAN authenticated scan-context parity.
2. Android attendance command planning from returned technical `employeeId` and `presence.entityVersion`.
3. Minimal Vietnamese scanner/result/state UI using current VHDCHY authority and accessible reference evidence only.
4. Cloud/LAN dispatch with stable request/idempotency identity.
5. Commit/pending-sync/conflict/error status surfaced to client.
6. Retry/restart/idempotency vectors.
7. Applicable projection confirmation kept distinct from command acceptance.
8. Slice reconciliation/progress update.

The current MNV lookup route is read-only; it must not create events/outboxes or write Google.

## Slice 2 — Work session + PICK/PACK + resources

Prepare in Lane C while Slice 1 integrates where dependencies permit. Required business coverage includes:

- MAIN session lifecycle and additional-session reason/approval rules;
- PICK requiring PDA and optional/multi User Pick history;
- Pack Table -> User Pack mapping and atomic compatible assignment;
- PDA/User Pick/User Pack/Pack Table assignment;
- release/reuse/reissue rules;
- cross-cluster borrowing where authorized;
- stable command identity, idempotency, audit evidence and Online/LAN parity.

Do not invent command contracts. New commands must be derived from active Owner rules and added to current contracts deliberately.

## Subsequent slices

After Slice 2:

- Slice 3: labor + dropped goods;
- Slice 4: documents/media, while portrait replacement remains fail-closed until the existing Owner semantic conflict is resolved;
- then admin/reporting/conflict UI, polish, physical/capacity/soak/UAT and STABLE promotion.

## Governance batching

One meaningful vertical slice or cross-cutting operating-model migration should normally be one PR. Multiple logical commits are allowed inside the PR.

Create an additional governance-only PR only when the previous merge creates a required reconciliation boundary that cannot safely be included in the same tested lineage. Do not create one merely because a helper/mechanic was merged.

Checkpoint at:

- provider mutation/migration boundaries;
- security-sensitive boundaries;
- Owner decision/permission boundaries;
- slice acceptance boundaries;
- release/promotion boundaries;
- interruption where stale resume state would be unsafe.

## Information ownership

| Information | Owner |
|---|---|
| Product/business decisions | `DECISIONS*.md` |
| Stable product scope | `PROJECT_SCOPE.md` |
| Provider identities/boundaries | `SERVICE_AUTHORITY.md` |
| Volatile operational evidence | `CURRENT_STATE.md` |
| READY/BLOCKED/WIP queue | `NEXT_ACTIONS.md` |
| Percentage/evidence credits | `docs/PROGRESS_TRACKING_V2.md` |
| Resume reconciliation | `CHECKPOINT.md` |
| Authority routing | `CONTEXT_INDEX.md` |

Do not duplicate volatile percentages or live provider status into non-owning files.

## CI tiers

### Tier 1 — fast affected checks

Use syntax/compile, current contracts, focused unit tests and pure harnesses for changed code. This is the fastest feedback loop and may run repeatedly on the branch.

### Tier 2 — slice PR evidence

Run only affected component/slice suites, such as Worker tests, LAN focused harnesses, Web tests, Android transport/foundation or parity/security tests. Include all affected runtimes, not unrelated product areas.

### Tier 3 — pre-merge baseline

Require `Validate clean baseline` and the affected product foundation/build workflow(s). High-risk provider/migration/security changes additionally require their dedicated guarded workflow/evidence.

### Tier 4 — post-merge main

Confirm main remains clean and the affected integration/product foundations succeed. A docs-only governance change does not justify redundant provider deployment.

## Evidence semantics

Keep these states distinct:

- `DESIGNED`: authority/contract exists;
- `SOURCE_PASS`: source + focused automated tests pass;
- `CI_PASS`: repository CI/harness passes on exact lineage;
- `LIVE_PROVIDER_PASS`: exact provider path verified;
- `PHYSICAL_PASS`: target device/network/host verified;
- `ACCEPTED`: required evidence for the acceptance item is complete.

A lower evidence level never silently implies a higher one.

## Anti-overengineering test

Before starting a technical task, identify the acceptance item it closes or unblocks. If none exists, the task is deferred unless it is a mandatory architecture/security/migration/provider/release prerequisite.

Generic future abstractions, speculative provider layers and extra infrastructure are not prioritized merely because they could be useful later.

## Parallel UI rule

Web/Android UI may progress before all backend work is complete when the UI is based on locked contracts/fixtures. The UI must not invent business semantics, permissions or unavailable Pick Pack visual details. Integration replaces fixtures once the runtime path is ready.

## Slice completion gate

A slice is complete only when applicable authority, runtime parity, client path, retry/idempotency, focused tests, CI, provider/physical separation and final governance/progress reconciliation are all evidenced. Until then, individual mechanics remain partial evidence rather than a completed product workflow.
