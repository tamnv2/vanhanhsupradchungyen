# CURRENT STATE

Cập nhật: 2026-09-11

## Trạng thái tổng quát

- Dự án chính: `VẬN HÀNH DC HƯNG YÊN` — nền tảng bao quát DC Hưng Yên.
- Cluster/module đầu tiên: `PICK_PACK_1291`.
- Pick Pack 1291 cũ: read-only reference/evidence; không phải authority/runtime dependency.
- Governance/continuity contract đã được Owner duyệt, có CI guard.
- `RECONCILE-001`: DONE.
- `LAN-PILOT` là priority feasibility gate trước business build sâu.

## BETA cloud runtime — verified known-good

- BETA Worker/source pointer: `947a4feb48bc5c99867f1975b56edbb9a7309925`.
- Deploy run `34495312746`: SUCCESS.
- Worker `vhdchy-beta` live tại `beta.supra.cc.cd`.
- Worker Version ID: `f8ddf638-0829-4249-9ea0-8f2d38b03f05`.
- D1 `vhdchy-data-beta`: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, APAC.
- D1 schema `business_core_v1`: PASS.
- Google Gateway advisory probe: PASS.
- Protected business routes remain closed with `AUTH_REQUIRED`; anonymous mutation disabled.

## BETA Pick Pack 1291 Sheet

- Workbook: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`.
- Spreadsheet ID: `14gHSgWXP2QvtPmBD3CzFt3vQ6AED2hPSm_LxMGpXAKg`.
- Sheet schema: `PP1291_SHEETS_BETA_V1`.
- D1 remains authority; Sheet is projection/human-readable surface.
- Old Pick Pack rows migrated: NONE.

## LAN physical evidence from 0.1.12

Corporate environment tested with restricted ordinary-user laptop + two real Newland NLS-MT90 Android 11 devices.

Observed from uploaded Agent/PDA diagnostics:

- Agent ran successfully without Administrator/network-policy changes.
- both physical PDA reached `LAN_ACTIVE` against `http://192.168.8.173:17891`;
- both exported diagnostics had blank `SavedManualEndpoint` and cached the automatically discovered/verified LAN endpoint;
- PDA health success streaks reached 73 and 67 with failure streak 0;
- Agent observed two active clients;
- observed PDA echo samples were approximately 10–57 ms in the captured logs;
- Agent request distribution in the captured session was approximately p50 47 ms / p95 67 ms / p99 90 ms;
- durable events reached Agent SQLite and pending queues returned to zero;
- duplicate test event was rejected deterministically;
- no observed transport/API error in that session; one HTTP 404 was browser favicon noise, not LAN transport failure.

Therefore LAN feasibility moved from `UNKNOWN` to `FEASIBLE / MORE FAILURE+LOAD EVIDENCE REQUIRED`.

Physical device availability is now exactly **2 MT90**, not 3. Synthetic clients supplement Agent capacity evidence but do not prove RF/Wi-Fi behavior above two physical devices.

## LAN Pilot comprehensive 0.2 build

Release: `lan-pilot-beta-v0.2.20`.
Build run: `34548902991`.

Verified CI jobs:

- Windows Agent build/package: SUCCESS;
- Android signed APK build: SUCCESS;
- prerelease publish: SUCCESS;
- repository validation around the build: SUCCESS.

0.2 adds:

- application-level LAN hard-priority/reacquisition testing;
- Agent-start timestamp and discovery-source evidence;
- PDA upload/download throughput tests (1/10/25 MB suite; 25 MB heavy button);
- realtime PDA ↔ Agent ↔ laptop/PDA long-poll stream;
- heavy realtime `200 x 2 KB`;
- receiver display-latency/p95 and sequence-gap counters;
- laptop Test Center realtime view and transfer/realtime metrics;
- local-only dashboard LoadGen triggers for 10/25/50/100 logical clients;
- FULL PDA diagnostics export;
- comprehensive two-PDA runbook in `docs/lan/LAN_PILOT_002_COMPREHENSIVE_TEST.md`.

Important boundary: hard LAN priority is application-level transport selection. The 0.2 test intentionally measures screen-off/background behavior; if Android suspends/kills the process and reacquisition stops, that is evidence to add a foreground LAN monitor rather than hide the limitation.

## Branch/live refs

- `main`: source/integration/authority; 0.2 LAN pilot source and plan live here.
- cloud `beta`: `947a4feb48bc5c99867f1975b56edbb9a7309925` known-good Worker BETA pointer; LAN packaging work does not move it.
- `stable`: `5b7132071f032ab46f133d4416f80791505f080d`; not promoted.

## Next

Use release `lan-pilot-beta-v0.2.20` on the laptop and both MT90. Execute `LAN-PILOT-002`: baseline, repeated Agent restart/reacquisition, Wi-Fi queue recovery, realtime normal/heavy, transfer normal/heavy, concurrent FULL suites, synthetic 10/25/50/100 load, background/screen-off and soak. Export one Agent ZIP + FULL TXT from each PDA for evidence-based analysis. No STABLE promotion.
