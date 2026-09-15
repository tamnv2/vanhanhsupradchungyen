# VHDCHY DELIVERY PLAN V6 — ACCEPTANCE-FIRST DELIVERY

Status: ACTIVE DELIVERY PLAN
Effective: 2026-09-15
Authority: `DECISIONS_V9.md`
Execution procedure: `docs/EXECUTION_MODEL_V1.md`
Current progress: `docs/PROGRESS_TRACKING_V2.md`
Acceptance: `docs/BETA_ACCEPTANCE_MATRIX.md`
Supersedes: `docs/DELIVERY_PLAN_V5.md` for current sequencing and delivery governance. V5 remains historical/detail reference where not conflicting.

## Principle

The target architecture and full product scope are unchanged. Delivery is reorganized around end-to-end acceptance vertical slices instead of spending the primary queue on one infrastructure phase at a time.

This file intentionally contains **no current completion percentage and no volatile provider/runtime status**. Read those from their owning files.

## Fixed top-level phase weights

The product remains measured over the same 12 phases:

| Phase | Weight |
|---|---:|
| 1. Product scope / Owner rules / target architecture | 8% |
| 2. Repository / environments / providers / CI foundation | 8% |
| 3. Cloud data / identity / auth / Service foundation | 12% |
| 4. Core business Service/API/workflows | 14% |
| 5. Gateway / adapters / realtime / external integrations | 10% |
| 6. LAN continuity / local state / offline / reconciliation | 16% |
| 7. Online Web + LAN Web product UI | 10% |
| 8. Android/PDA App | 10% |
| 9. Admin / reporting / operational tooling | 4% |
| 10. Security / observability / backup / recovery | 3% |
| 11. BETA physical / capacity / UAT acceptance | 3% |
| 12. STABLE promotion / production closure | 2% |

Current earned evidence credits per phase are owned only by `docs/PROGRESS_TRACKING_V2.md`.

## Delivery stream A — Attendance golden path

Close one usable warehouse path across client and runtime:

`scan MNV -> authenticated employee/presence lookup -> IN/OUT planning -> Cloud or LAN execution -> immutable event/current presence -> visible commit/sync status -> downstream projection where applicable -> retry/restart without duplicate`

Acceptance includes:

- MNV is never treated as technical `employeeId`;
- Cloud/LAN scan-context response semantics match;
- authenticated permissions and password-change restrictions are enforced;
- Android scanner command uses resolved identity/current presence version;
- actor authority stays server/session derived;
- no client direct database/Sheets/Drive writes;
- idempotent identity survives route/retry/restart;
- local acceptance versus Cloud-synchronized/projection state is visible and distinct;
- source/CI/provider/physical evidence levels remain explicit.

## Delivery stream B — Work session + PICK/PACK + resources

After/while independent preparation does not interfere with Stream A, implement the next complete business slice:

- MAIN/additional session rules;
- PICK/PACK workflow;
- PDA, User Pick, Pack Table and User Pack assignment;
- atomic compatible selection where required;
- release/reuse/reissue rules;
- cross-cluster borrowing;
- immutable history/idempotency/permissions/conflict behavior;
- Cloud/LAN/client parity.

## Delivery stream C — Labor + dropped goods

Close the current approved labor types and dropped-goods workflow end-to-end with current business-date, DO/package count, actor/audit evidence and manual/QR input as applicable.

## Delivery stream D — Documents/media

Close document lifecycle, media metadata/upload/staging/retry/reconciliation and user-visible status. Portrait replacement remains fail-closed until the existing Owner semantic conflict is explicitly resolved.

## Delivery stream E — Product closure

After business slices are materially complete:

- admin/reporting/operational tooling;
- conflict-resolution UI;
- Web/Android product polish;
- target host/PDA/network/public-trust acceptance;
- >=60-minute Internet-cut test;
- reconnect/restart/network-change regression;
- capacity/soak;
- backup/restore/update/rollback;
- Owner UAT;
- exact BETA release acceptance;
- STABLE promotion only after explicit Owner approval.

## Parallel delivery rule

Normal active WIP is limited to:

1. one integrating slice;
2. one independent Web/client lane;
3. one next-domain preparation lane.

Provider/physical gates that cannot currently execute are tracked separately and do not block independent source work.

## Evidence and governance rule

A mechanic may be SOURCE/CI PASS without being a complete product flow. Progress is credited only through `docs/PROGRESS_TRACKING_V2.md` when a fixed evidence credit is justified.

Governance/state/progress reconciliation occurs once at meaningful slice/security/provider/release boundaries rather than after every helper. Existing focused CI/harnesses are reused according to `docs/EXECUTION_MODEL_V1.md`.

## Release rule

BETA acceptance remains governed by `docs/BETA_ACCEPTANCE_MATRIX.md`. STABLE remains isolated/fail-closed until mandatory BETA acceptance and explicit Owner promotion approval. The exact accepted BETA release/artifacts are promoted; BETA runtime/business data is not copied to STABLE by default.
