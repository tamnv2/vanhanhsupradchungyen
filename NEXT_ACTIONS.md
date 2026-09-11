# NEXT ACTIONS

Checkpoint: `LAN-PILOT-20260911-04`

## Priority objective

`LAN-PILOT-002-V4 — final physical regression of the CI-verified LAN V4 candidate on restricted corporate laptop + exactly 2 Newland MT90, supplemented by synthetic Agent load.`

Owner approved the LAN-first feasibility gate. Deep business feature work remains behind this gate unless independent/supportive.

## Candidate to test

Release: `lan-pilot-beta-v0.3.36`
Source commit: `7b4488a89f585812c1bccba5d07d86049482bf4c`
Build run: `34562489063` — SUCCESS
Repository validation: `34562489066` — SUCCESS

Automated gates already PASS:

- one semantic version across Agent/APK/Release;
- Windows Agent source updater invariants;
- Windows self-contained build/package;
- Agent version `0.3.36`;
- Android background lifecycle source invariants;
- Android signed release APK;
- APK package/version verification;
- SHA256 files for Agent/APK;
- prerelease publication with all four expected assets.

## V4 behavior under physical verification

- PDA auto-discovers/reacquires valid LAN Agent without manual endpoint.
- Realtime uses `streamEpoch + sequence`; Agent restart or buffer gap forces deterministic resync.
- Receiver latency uses Agent clock calibration instead of raw PDA-vs-laptop wall-clock subtraction.
- Foreground app: realtime LAN ON.
- App leaves foreground: realtime OFF.
- If unfinished work or durable pending queue exists: bounded foreground finish service may continue only that work/recovery, then stops.
- If network remains unavailable: durable queue remains local and resumes on next app open; app must not stay awake indefinitely.
- Agent/APK update notification and manual update path both remain available.
- Agent staged updater verifies SHA256, stages without Administrator, restarts, health-checks and has rollback path.
- Agent/PDA diagnostics include richer lifecycle, queue, update, realtime, transfer and resource evidence.

## Physical evidence ceiling

Owner currently has exactly **2 physical MT90**. Therefore:

- both PDA must pass connection/reacquisition/realtime/transfer/queue/background/update/soak tests;
- synthetic load must show clear software headroom above two clients;
- final report must state RF/Wi-Fi behavior above two physical PDA remains unproven;
- synthetic clients must never be presented as equivalent physical-PDA RF evidence.

## Final physical regression sequence

1. Replace laptop Agent with `0.3.36`; install matching `0.3.36` APK on both MT90.
2. Verify Agent and both APK screens report `0.3.36`; both PDA auto-enter `LAN_ACTIVE` with no manual endpoint.
3. Restart Agent at least three times while both PDA remain untouched; verify automatic reacquisition, epoch change and clean resync without stale-cursor lock.
4. Realtime normal: PDA A → laptop + PDA B, then B → laptop + A; record corrected latency/gaps.
5. Realtime heavy: `200 × 2 KB` each direction and near-simultaneously; verify no unexplained gap/loss.
6. Transfer: 1/10/25 MB both directions, then run FULL suites on both PDA near-simultaneously.
7. Offline durable queue: turn Wi-Fi off where policy permits; create a 5-event batch; verify pending reaches 5; restore Wi-Fi; verify LAN reacquires, all events are accepted/idempotent and pending returns to 0.
8. Background policy: with no job pending, Home/background and confirm realtime stops; while an active transfer/FULL task runs, leave the UI and confirm finish-service notification/work completion then service stops; repeat with pending queue and unavailable network to confirm bounded retry then sleep.
9. Update flow: verify automatic notification detects a truly newer semantic version only; verify manual fallback is always reachable. For Agent, physically verify staged no-admin replacement and healthy commit; rollback behavior should be tested only with a controlled safe failure case if practical. For APK, verify SHA/package/version validation reaches Android installer under MT90/company policy.
10. Laptop LoadGen: 10 → 25 → 50 → 100 logical clients; record physical-vs-synthetic counts separately.
11. Soak: 30 min → 2 h → longer work window if feasible, watching Agent CPU/RAM, laptop CPU/RAM, DB/log growth, errors and reconnects.
12. Export one Agent ZIP + FULL diagnostics TXT from each PDA and submit for final analysis.

## Gate metrics

- automatic LAN reacquisition success/delay and discovery source;
- epoch-reset/resync correctness and sequence gaps;
- corrected realtime display latency/p95 and publish ACK latency;
- request success/error/client-cancel counts and p50/p95/p99;
- upload/download Mbps and completion errors;
- durable queue peak/recovery/event loss/duplicate handling;
- foreground/background service duration, wake-lock boundedness and battery/power state;
- Agent/laptop CPU/RAM/threads/handles/GC and DB/log growth;
- synthetic req/s at 10/25/50/100;
- update notification/manual path/install/staged update/health/rollback evidence;
- soak stability.

## Completion rule

Do not mark `LAN-PILOT PASS` solely because CI is green. Final PASS requires physical V4 evidence from the restricted laptop + both MT90 with no unexplained event loss, predictable restart/reconnect, acceptable footprint, and update/background behavior consistent with the design above.

## Owner action

Use only release `lan-pilot-beta-v0.3.36` for the next regression. Do not change corporate firewall/router/DNS or use Administrator. No STABLE promotion.
