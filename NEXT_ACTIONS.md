# NEXT ACTIONS

Checkpoint: `LAN-PILOT-20260911-03`

## Priority objective

`LAN-PILOT-002 — comprehensive LAN validation on restricted corporate laptop + exactly 2 available Newland MT90, supplemented by synthetic Agent load.`

Owner approved 2026-09-11. Deep business feature work remains behind this feasibility gate unless independent/supportive.

## Build target — 0.2 series

New measurement build adds:

- application-level hard LAN priority while APK process is alive;
- Agent restart -> automatic PDA LAN reacquisition measurement;
- discovery source evidence (`UDP`/`CACHE` where observable);
- PDA upload/download test at 1 MB and 25 MB;
- FULL transfer suite at 1/10/25 MB both directions;
- realtime PDA ↔ Agent ↔ laptop/PDA using bounded long-poll event stream;
- realtime heavy burst `200 x 2 KB`;
- receiver display latency, p95 and sequence-gap counters;
- laptop Test Center realtime visibility;
- local-only LoadGen buttons for 10/25/50/100 logical clients;
- FULL PDA diagnostics export combining transport, realtime, transfer and test history.

Detailed procedure: `docs/lan/LAN_PILOT_002_COMPREHENSIVE_TEST.md`.

## Physical evidence available

Owner currently has exactly **2 physical MT90**. Therefore:

- both PDA must pass connection/reacquisition/realtime/transfer/queue/soak tests;
- synthetic load must show clear software headroom above two clients;
- final report must explicitly state that RF/Wi-Fi behavior above two physical PDA is unproven until more devices are available;
- synthetic clients must never be presented as equivalent physical-PDA RF evidence.

## Test sequence after release

1. Install same 0.2 BETA APK on both MT90 and run matching 0.2 Agent.
2. Baseline: both PDA auto-enter `LAN_ACTIVE`, no manual endpoint.
3. Restart Agent three times while both PDA remain untouched; measure automatic reacquisition.
4. Wi-Fi off/on + durable queue recovery on each PDA if policy permits.
5. Realtime normal: A -> laptop + B, then B -> laptop + A.
6. Realtime heavy: 200 x 2 KB each direction, then both near-simultaneously.
7. Transfer: 1 MB x3, 25 MB x3 per PDA; then FULL suite 1/10/25 MB.
8. Run FULL suite on both PDA near-simultaneously.
9. Laptop synthetic LoadGen: 10 -> 25 -> 50 -> 100 clients.
10. Screen-off/background test; Agent restart while screen off.
11. Soak: 30 min -> 2 h -> longer work-window if feasible.
12. Export one Agent ZIP + FULL TXT from both PDA for evidence analysis.

## Gate metrics

- automatic LAN reacquisition success/delay;
- discovery source;
- request success/error and p50/p95/p99;
- realtime display latency/p95 and sequence gaps;
- transfer Mbps and completion errors;
- queue recovery/event loss/duplicate handling;
- Agent/laptop CPU/RAM and DB growth;
- synthetic req/s at 10/25/50/100;
- screen-off/background behavior;
- soak stability.

## Still required before final LAN-PILOT PASS

- physical 0.2 test evidence from both MT90;
- no unexplained event loss;
- predictable restart/reconnect behavior;
- acceptable resource footprint;
- automatic update notification + manual fallback verified;
- actual no-admin Agent staged self-update/health rollback remains a later mandatory gate before final production LAN release.

## Owner action

Wait for a CI-verified 0.2 prerelease. Then update Agent and both APKs and follow the Test Center / FULL suite workflow. Do not change corporate firewall/router/DNS or use Administrator. No STABLE promotion.
