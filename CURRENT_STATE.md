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

## LAN physical evidence already established

Corporate environment tested with restricted ordinary-user laptop + exactly two real Newland NLS-MT90 Android 11 devices.

From prior 0.1.12 evidence:

- Agent ran without Administrator/network-policy changes;
- both physical PDA reached `LAN_ACTIVE` against `http://192.168.8.173:17891`;
- both diagnostics had blank manual endpoint and used discovered/cached LAN endpoint;
- PDA health success streaks reached 73 and 67 with failure streak 0;
- Agent observed two active clients;
- observed PDA echo samples were approximately 10–57 ms;
- Agent request distribution was approximately p50 47 ms / p95 67 ms / p99 90 ms;
- durable events reached Agent SQLite and pending queues returned to zero;
- duplicate test event was rejected deterministically;
- no observed transport/API error in that session except browser favicon 404 noise.

LAN feasibility remains `FEASIBLE / FINAL V4 PHYSICAL REGRESSION REQUIRED`.

Physical device availability is exactly **2 MT90**. Synthetic clients supplement Agent capacity evidence but never prove RF/Wi-Fi behavior above two physical devices.

## LAN Pilot V4 automated candidate

Final automated candidate release: `lan-pilot-beta-v0.3.36`.

Source commit: `7b4488a89f585812c1bccba5d07d86049482bf4c`.
Build run: `34562489063` — SUCCESS.
Repository validation run: `34562489066` — SUCCESS.

Build/release gates verified:

- Windows updater source invariants PASS;
- Windows Agent self-contained build/package PASS;
- Agent product-version gate `0.3.36` PASS;
- Android background lifecycle source invariants PASS;
- Android release build PASS;
- APK signature verification PASS (v1 + v2);
- package `vn.vhdchy.lanpilot.beta`, versionCode `36`, versionName `0.3.36` PASS;
- Agent ZIP + SHA256 and APK + SHA256 published PASS;
- prerelease publish PASS.

V4 corrects/implements:

- Agent/APK/release use one injected semantic version;
- Agent staged no-admin updater with SHA256 verification, health check and rollback path;
- fixed updater batch health-success logic so a healthy staged update can actually commit rather than false-rollback;
- APK update verifies SHA256, package and target version before invoking Android installer; manual fallback remains;
- realtime uses `streamEpoch + sequence` and explicit resync after Agent restart/buffer gap;
- corrected cross-device latency measurement using Agent clock calibration; ACK latency uses same-device monotonic clock;
- physical and synthetic clients are measured separately;
- long-poll client cancellation is separated from true Agent errors;
- foreground app uses realtime; background realtime stops;
- unfinished task/durable queue may use bounded `START_NOT_STICKY` foreground finish service, bounded wake lock/retry, then stops;
- final destroyed-Activity tracked job explicitly releases executor/database after completion;
- richer Agent/PDA lifecycle/network/update/queue/realtime/transfer/resource diagnostics;
- Agent realtime waiting is signal-driven rather than active 50 ms polling.

Release artifact SHA256 reported by GitHub:

- Agent ZIP: `0fbfcb846c1b81441f65ae484dc701c35381705a001fffca553d8dcdf687a5e4`;
- APK: `02f9f3832f70da808c94890f87bad9fd68a04f4a2f124df74f4d9f5f2d8e465a`.

## What remains before final LAN-PILOT PASS

Automated source/build/sign/version/package/release gates are complete. Remaining gates require the real corporate laptop + two MT90:

- actual no-admin Agent staged replacement and health/rollback behavior;
- update notification/manual fallback and Android installer policy on real devices;
- automatic LAN reacquisition + epoch resync after Agent restart;
- realtime normal/heavy physical propagation and gap/latency evidence;
- transfer throughput/stability;
- Wi-Fi-off durable queue batch recovery with zero unexplained event loss;
- foreground/background finish-service behavior and battery/resource footprint;
- synthetic 10/25/50/100 Agent capacity and soak;
- exported Agent ZIP + FULL TXT analysis from both PDA.

## Branch/live refs

- `main`: source/integration/authority; V4 LAN pilot source and checkpoint live here.
- cloud `beta`: `947a4feb48bc5c99867f1975b56edbb9a7309925` known-good Worker BETA pointer; LAN packaging work does not move it.
- `stable`: `5b7132071f032ab46f133d4416f80791505f080d`; not promoted.

## Next

Install matching `0.3.36` Agent/APK on the restricted laptop and both MT90, run the V4 physical regression matrix, then export one Agent diagnostics ZIP plus FULL diagnostics TXT from each PDA for evidence-based analysis. Do not change corporate firewall/router/DNS or use Administrator. No STABLE promotion.
