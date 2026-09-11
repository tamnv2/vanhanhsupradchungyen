# SESSION CHECKPOINT

Checkpoint ID: `LAN-PILOT-20260911-04`
Timestamp: `2026-09-11 Asia/Ho_Chi_Minh`

## Authority/source basis

- MAIN contains reconciled DC core/Sheet baseline plus LAN Pilot V4 source and CI guards.
- Cloud BETA live/source ref remains `947a4feb48bc5c99867f1975b56edbb9a7309925`.
- STABLE ref remains `5b7132071f032ab46f133d4416f80791505f080d`.
- LAN packaging/build work did not move cloud BETA or STABLE.
- No Stable promotion.

## Prior physical evidence

Restricted corporate ordinary-user laptop + exactly two Newland NLS-MT90 Android 11 devices already established basic LAN feasibility:

- Agent runs without Administrator/network-policy changes;
- both PDA reached `LAN_ACTIVE` with no manual endpoint stored;
- health success streaks 73/67, failures 0;
- Agent saw two active clients;
- captured echo roughly 10–57 ms; Agent p50/p95/p99 roughly 47/67/90 ms;
- durable event flow to local SQLite PASS;
- pending queues ended at zero;
- duplicate rejection PASS;
- no observed transport/API error in that session except favicon noise.

Current feasibility classification: `FEASIBLE / FINAL V4 PHYSICAL REGRESSION REQUIRED`.

## Owner requirements implemented in V4 source

- correct Agent/APK update/version behavior;
- automatic update notification plus independent manual update fallback;
- Agent staged no-admin update path with SHA256, restart health probe and rollback path;
- realtime restart/cursor fix using `streamEpoch + sequence` and explicit resync;
- corrected cross-device realtime latency via Agent clock calibration;
- substantially richer Agent/PDA diagnostics;
- resource/battery policy: foreground realtime, no background realtime, bounded finish-only service for unfinished work/queue, stop when work completes, resume discovery/sync on next app open;
- Agent remains portable user-mode with tray/settings/dashboard and no router/DNS/firewall/Admin assumptions.

## Defects found and corrected during V4 hardening

Automated regression exposed and blocked multiple issues before the final candidate:

- AndroidX not enabled after FileProvider/AndroidX dependency;
- malformed C# updater raw/interpolated string;
- `Debug.getPss()` type mismatch;
- Android BuildConfig generation absent;
- Windows staged updater health-success branch unreachable because stale `%HEALTH_OK%` batch expansion;
- Activity resources not released after a final foreground-started tracked job completed after Activity destruction;
- transient CI source-gate pattern mismatches corrected.

CI now guards the important updater and Android lifecycle source invariants so these regressions cannot silently re-enter the packaged candidate.

## Final automated candidate

Release: `lan-pilot-beta-v0.3.36`
Source commit: `7b4488a89f585812c1bccba5d07d86049482bf4c`
Build run: `34562489063` — SUCCESS
Repository validation run: `34562489066` — SUCCESS

Verified jobs:

- Windows Agent job `103147915091`: SUCCESS;
- Android PDA job `103147914859`: SUCCESS;
- prerelease job `103148233504`: SUCCESS.

Android verification:

- source background-lifecycle invariants PASS;
- release build PASS;
- signature v1 + v2 PASS;
- package `vn.vhdchy.lanpilot.beta`;
- versionCode `36`;
- versionName `0.3.36`.

Windows verification:

- updater source invariants PASS;
- .NET self-contained Agent + LoadGen publish PASS;
- product version gate for `0.3.36` PASS;
- Agent artifact upload PASS.

Release contains four required assets:

- `VHDCHY-LAN-Agent-BETA-win-x64.zip`;
- `VHDCHY-LAN-Agent-BETA-win-x64.sha256`;
- `VHDCHY-LAN-Pilot-BETA.apk`;
- `VHDCHY-LAN-Pilot-BETA.apk.sha256`.

GitHub-reported release digests:

- Agent ZIP SHA256 `0fbfcb846c1b81441f65ae484dc701c35381705a001fffca553d8dcdf687a5e4`;
- APK SHA256 `02f9f3832f70da808c94890f87bad9fd68a04f4a2f124df74f4d9f5f2d8e465a`.

## Exact next action

Owner physical action is now the only blocker to final LAN feasibility PASS:

1. install matching `0.3.36` Agent and APK on both MT90;
2. verify displayed versions and automatic LAN_ACTIVE without manual endpoint;
3. restart Agent repeatedly and verify automatic reacquisition + epoch resync;
4. realtime normal/heavy both directions and near-simultaneous;
5. transfer 1/10/25 MB + concurrent FULL suites;
6. Wi-Fi-off 5-event durable queue test, then recovery to pending 0 with no unexplained loss;
7. foreground/background/no-work/in-flight-work/pending-queue lifecycle tests;
8. automatic/manual update path and actual no-admin Agent staged update validation;
9. synthetic 10/25/50/100 load;
10. soak;
11. export one Agent ZIP + FULL TXT from each PDA for final analysis.

Detailed sequence is in `NEXT_ACTIONS.md`.

## Boundary

Two real MT90 are the current physical ceiling. Synthetic capacity evidence must not be reported as proof of RF/Wi-Fi behavior above two real devices.

## Owner action required

YES: install and physically test release `lan-pilot-beta-v0.3.36` on the real corporate laptop/Wi-Fi + both MT90. No Administrator, router, DNS or firewall changes are requested.
