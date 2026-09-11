# SESSION CHECKPOINT

Checkpoint ID: `LAN-PILOT-20260911-03`
Timestamp: `2026-09-11 Asia/Ho_Chi_Minh`

## Authority/source basis

- MAIN contains reconciled DC core/Sheet baseline plus LAN pilot 0.2 comprehensive measurement source.
- Cloud BETA live/source ref remains `947a4feb48bc5c99867f1975b56edbb9a7309925`.
- STABLE ref remains `5b7132071f032ab46f133d4416f80791505f080d`.
- No Stable promotion.

## Physical evidence already verified from 0.1.12

- restricted corporate laptop ordinary-user Agent: PASS;
- two real Newland NLS-MT90 Android 11 devices: both reached `LAN_ACTIVE`;
- manual endpoint blank in both uploaded diagnostics;
- health streaks 73/67 with failure 0;
- Agent saw two active clients;
- captured echo samples roughly 10–57 ms; Agent p50/p95/p99 roughly 47/67/90 ms;
- durable event flow to local SQLite: PASS;
- pending queues ended at zero;
- duplicate rejection: PASS;
- no observed transport/API error in that session.

Current feasibility classification: `FEASIBLE / MORE FAILURE+LOAD EVIDENCE REQUIRED`.

## Owner decision

Current physical inventory is exactly **2 MT90**. D-029 allows current feasibility decision using both real devices + synthetic software-capacity headroom, while explicitly forbidding claims about RF/Wi-Fi behavior above two physical PDA.

## New 0.2 BETA artifacts

Release: `lan-pilot-beta-v0.2.20`
Build run: `34548902991`

Verified:

- Windows Agent build/package SUCCESS;
- Android signed APK SUCCESS;
- prerelease publish SUCCESS.

Capabilities added:

- application-level LAN hard-priority/reacquisition testing;
- discovery-source + Agent-start-to-LAN_ACTIVE timing evidence;
- transfer test 1/10/25 MB;
- realtime PDA ↔ laptop ↔ PDA with receiver display latency/p95/gap counts;
- heavy realtime 200 x 2 KB;
- FULL PDA suite and FULL diagnostics;
- Test Center synthetic LoadGen 10/25/50/100 logical clients;
- richer Agent realtime/transfer metrics.

## Exact next physical actions

Use `docs/lan/LAN_PILOT_002_COMPREHENSIVE_TEST.md`:

1. update laptop Agent and both MT90 to 0.2.20;
2. verify both auto LAN_ACTIVE without manual endpoint;
3. restart Agent three times while both PDA are untouched and measure automatic reacquisition;
4. Wi-Fi off/on + durable queue recovery where policy permits;
5. realtime normal each direction;
6. heavy realtime each direction and near-simultaneous;
7. transfer 1 MB and 25 MB, then FULL suites;
8. run both FULL suites near-simultaneously;
9. Test Center LoadGen 10 -> 25 -> 50 -> 100;
10. screen-off/background + Agent restart during screen-off;
11. soak 30 min -> 2 h if feasible;
12. export one Agent ZIP + FULL TXT from each PDA and analyze.

## Known limitation intentionally under test

LAN priority is application-level transport selection, not an Android OS route override. If Android suspends/kills the APK process in background/screen-off and automatic LAN reacquisition stops, record the result and add a foreground LAN monitor in the next build if required.

## Owner action required

YES: physical testing now requires the real corporate laptop/Wi-Fi and both MT90. No Administrator, router, DNS or firewall changes are required or requested.
