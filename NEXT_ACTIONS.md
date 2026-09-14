# NEXT ACTIONS — VHDCHY

Updated: 2026-09-14
Delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress: `docs/PROGRESS_TRACKING_V1.md`
Current overall: **53%**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Do not serialize independent Web/App/Cloud/LAN work unnecessarily, but do not allow parallel lanes to invent different business contracts.

Public business mutation paths remain fail-closed until the corresponding current readiness/security/domain acceptance gates are proven.

## Priority A — current LAN/business lane

1. Preserve the proven employee/MNV/attendance Slice-1 behavior, atomic uniqueness and durable staged-media primitives.
2. Extend current LAN auth/pairing/security-epoch and permission enforcement; pilot identity hints are not sufficient security.
3. Extend current business adapters and acceptance vectors beyond the existing Slice-1 subset according to `docs/DELIVERY_PLAN_V5.md`.
4. Complete Cloud/LAN reconciliation identity, sync cursors/checkpoints, Google receipts/deduplication and explicit conflict evidence.
5. Keep current public LAN mutations fail-closed until readiness explicitly links the complete reviewed business/auth path.
6. Do not claim physical PASS from CI/source evidence.

## Priority B — portrait Owner decision gate

The existing portrait conflict remains genuinely unresolved:

- current business rule requires previous portrait deletion immediately;
- offline/LAN media rules allow staging when Drive is unavailable.

Continue decision-independent media infrastructure, but do not silently choose offline portrait replacement semantics. When implementation reaches that exact mutation behavior, obtain explicit Owner authority, then implement and test the chosen lifecycle.

## Priority C — Online Web + LAN Web V7 UI lane

1. Build a shared VHDCHY Web design system from the V7 DNSHE-style direction without copying DNSHE branding/assets.
2. Implement login shell and authenticated dashboard shell first.
3. Keep Online/LAN as one UI product with shared navigation/hierarchy.
4. Add explicit network/sync/queue states only where they affect behavior.
5. Package LAN-critical fonts/icons/scripts/styles/images locally; no Internet-only dependency for continuity UI.
6. Migrate business modules onto the shared shell as their Service/LAN contracts stabilize.
7. Preserve Vietnamese / English / Chinese, default English.

## Priority D — Android/PDA App V7 lane

1. Surface and inspect the actual Pick Pack 1291 App UI source/artifacts from the authorized backup reference before finalizing visuals.
2. Build the current VHDCHY PDA shell using that UI/UX direction, adapted to current workflows and permissions.
3. Reuse only reviewed low-level legacy mechanics: discovery/cache/hysteresis, device sequence, durable queue, ACK deletion, reconnect/resync and bounded background work.
4. Implement scanner-first warehouse flows only through the current domain commands.
5. Integrate current durable media/camera behavior and current network-state UX.
6. Verify final signed APK/update/background/battery behavior on real target devices.

## Priority E — Cloud/Gateway/provider lane

1. Continue current Service/API business coverage and provider-neutral adapters.
2. Complete controlled Google Drive/Sheets projection/upload receipt, retry and readback behavior.
3. Keep Sheets/Drive downstream only; never use them as canonical business authority.
4. Complete current Gateway/provider failure/recovery and operator-visible diagnostics.
5. Keep sensitive provider operations within allowed high-level action safety; never bypass provider/platform guards.

## Priority F — physical BETA acceptance when source is ready

Run against the intended company ordinary-user Windows laptop/network and real NLS-MT90 devices:

1. LAN reachability/discovery/reacquisition under company network policy.
2. Real PDA workflow latency/Wi-Fi behavior and queue recovery.
3. >=60-minute Internet-cut continuity Window 2 under V6.
4. Reconnect/reconciliation after restoration as a separate post-window test.
5. Host restart/network-change and no-admin update/rollback.
6. Background/battery behavior.
7. Synthetic 10/25/50/100 load plus soak.
8. Full Owner UAT across Online Web, LAN Web, App and Service/Gateway.

## Priority G — STABLE after BETA acceptance only

- Prepare isolated STABLE infrastructure safely during development.
- Do not activate production business traffic or promote until mandatory BETA gates pass and the Owner explicitly approves promotion.
- Promote the exact accepted BETA release/artifacts.
- Do not copy BETA runtime/business data into STABLE unless a separately approved migration requires it.

## Current execution line

`Overall: 53% | Current: Phase 6 — LAN continuity/offline/reconcile | Parallel: Phase 4/5 + V7 Web/App UI | Next gate: broader LAN business/auth/reconcile coverage -> current physical regression`
