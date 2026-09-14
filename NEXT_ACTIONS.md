# NEXT ACTIONS — VHDCHY

Updated: 2026-09-14
Delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress: `docs/PROGRESS_TRACKING_V1.md`
Current overall: **55% displayed / 54.6% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with a mandatory ready-queue scheduler.

At every step:

1. identify all safe nodes whose prerequisites are already satisfied;
2. execute independent ready nodes in parallel where available tools permit;
3. serialize only dependency-bound work or writes to the same file/ref/database/provider resource;
4. when one dependency chain advances, immediately start its next ready node while unrelated lanes continue;
5. isolate a blocked/failed node and continue all independent ready work;
6. refill the ready queue rather than waiting for an unrelated lane to finish.

Public business mutation paths remain fail-closed until the corresponding current readiness/security/domain acceptance gates are proven.

Before a long tool/session block ends, report evidence-backed PASS/FAIL/IN_PROGRESS/BLOCKED state, direct evidence IDs, current exact/rounded percentage and any justified delta. Tool activity without proof is not progress.

## Ready queue NOW

### A — Cloud/LAN reconciliation chain — PRIMARY

Already PASS at the local queue layer:

- durable claim/retry/reconcile/conflict state machine;
- restart recovery of interrupted claims;
- immutable edge-event reconciliation envelope;
- completed Google/Drive receipt attachment;
- race-safe single claimant.

Evidence: dedicated run `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66` — `SUCCESS`.

Next dependency chain:

1. lock the current Cloud reconciliation transport/API path against `docs/SERVICE_API_CONTRACT_V3.md`;
2. implement Cloud ingestion over the existing edge-reconciliation D1 schema without opening unrelated public mutations;
3. validate event/idempotency/device/source identity collision behavior;
4. accept completed LAN integration receipts without duplicate Google output;
5. return stable reconcile/conflict/retry semantics to LAN;
6. add source-level/CI acceptance vectors;
7. only then connect the LAN network sender and prove end-to-end retry/restart/reconciliation.

### B — LAN security/readiness — PARALLEL

1. Preserve proven employee/MNV/attendance Slice-1 behavior, atomic uniqueness, staged media and Cloud-sync queue mechanics.
2. Extend current LAN auth/pairing/security-epoch and permission enforcement; pilot identity hints are not sufficient security.
3. Extend current business adapters and acceptance vectors beyond the existing Slice-1 subset according to `docs/DELIVERY_PLAN_V5.md`.
4. Add sync cursor/delta refresh/rebase behavior and dependency-safe conflict handling.
5. Keep current public LAN mutations fail-closed until readiness explicitly links the complete reviewed business/auth path.
6. Do not claim physical PASS from CI/source evidence.

### C — Online Web + LAN Web — PARALLEL

Current shared V7 shell and Web contract are PASS inside product-foundation run `34801611019`.

Continue with:

1. authenticated login/recovery flow only after current Service auth routes exist; do not fake login success;
2. migrate employee/attendance Slice-1 screens onto the shared V7 shell;
3. add ADMIN+ conflict-resolution surface when the reconciliation contract is stable;
4. preserve Online/LAN parity and local/offline-safe critical assets;
5. keep all current user-facing Web UI **Vietnamese only**;
6. defer language selectors/catalogs/locale persistence until a later Owner decision.

### D — Android/PDA App — PARALLEL WHERE NOT BLOCKED

Current foundation APK builds and visible text is Vietnamese-only.

1. Continue locating the actual final Pick Pack 1291 App UI source/artifacts from the authorized backup/reference.
2. Do not invent exact visual details while that artifact remains unavailable.
3. Independently continue non-visual current-contract work that does not depend on the missing reference: Service/LAN endpoint abstraction, scanner-to-domain-command boundary, durable retry mechanics and current network-state model.
4. Once actual UI evidence is surfaced, build the VHDCHY PDA shell using that recognizable direction, adapted to current workflows/permissions.
5. Keep all current user-facing App UI **Vietnamese only**; multilingual work is deferred.
6. Verify final signed APK/update/background/battery behavior on real target devices later.

### E — Cloud/Gateway/provider — PARALLEL

1. Continue current Service/API business coverage and provider-neutral adapters.
2. Complete controlled Google Drive/Sheets projection/upload receipt, retry and readback behavior.
3. Keep Sheets/Drive downstream only; never use them as canonical business authority.
4. Complete current Gateway/provider failure/recovery and operator-visible diagnostics.
5. Keep sensitive provider operations within allowed high-level action safety; never bypass provider/platform guards.

## Owner decision gate — portrait only

The existing portrait conflict remains genuinely unresolved:

- current business rule requires previous portrait deletion immediately;
- offline/LAN media rules allow staging when Drive is unavailable.

Continue decision-independent media infrastructure, but do not silently choose offline portrait replacement semantics. When implementation reaches that exact mutation behavior, obtain explicit Owner authority, then implement and test the chosen lifecycle.

## Physical BETA acceptance — when source is ready

Run against the intended company ordinary-user Windows laptop/network and real NLS-MT90 devices:

1. LAN reachability/discovery/reacquisition under company network policy.
2. Real PDA workflow latency/Wi-Fi behavior and queue recovery.
3. >=60-minute Internet-cut continuity Window 2.
4. Reconnect/reconciliation after restoration as a separate post-window test.
5. Host restart/network-change and no-admin update/rollback.
6. Background/battery behavior.
7. Synthetic 10/25/50/100 load plus soak.
8. Full Owner UAT across Online Web, LAN Web, App and Service/Gateway.

## STABLE after BETA acceptance only

- Prepare isolated STABLE infrastructure safely during development.
- Do not activate production business traffic or promote until mandatory BETA gates pass and the Owner explicitly approves promotion.
- Promote the exact accepted BETA release/artifacts.
- Do not copy BETA runtime/business data into STABLE unless a separately approved migration requires it.

## Current execution line

`Overall: 55% displayed / 54.6% exact | Current: Phase 6 — LAN continuity/offline/reconcile | Parallel: Phase 4/5 + V7 Web/App | Next gate: Cloud reconciliation ingestion/transport + LAN security/readiness -> physical regression later`
