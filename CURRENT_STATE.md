# CURRENT STATE

Cập nhật: 2026-09-11

## Trạng thái tổng quát

- Dự án chính: `VẬN HÀNH DC HƯNG YÊN` — nền tảng bao quát DC Hưng Yên.
- Cluster/module đầu tiên: `PICK_PACK_1291`.
- Pick Pack 1291 cũ: read-only reference/evidence; không phải authority/runtime dependency.
- Governance/continuity contract đã được Owner duyệt, có CI guard.
- `RECONCILE-001`: DONE.
- Owner đã chuyển `LAN-PILOT-001` thành priority feasibility gate trước business build sâu.

## Foundation đã xác minh

- Google/Gmail/Drive account dự án và GitHub connector đã kết nối.
- Drive runtime BETA/STABLE đã tạo.
- GCP/OAuth/GAS BETA/STABLE đã cấu hình; Apps Script authorize/deployment PASS.
- Cloudflare zone `supra.cc.cd` Active; CI token BETA/STABLE đã cấu hình.
- Android signing BETA/STABLE đã tạo và credential verification PASS.
- GitHub Environments `beta`/`stable` đầy đủ Variables/Secrets và verification PASS.
- R2 OFF; Durable Objects chưa bật; LAN hostnames private/reserved.

## BETA runtime — verified known-good

- BETA deployed/source commit: `947a4feb48bc5c99867f1975b56edbb9a7309925`.
- Deploy run `34495312746`: SUCCESS.
- Worker `vhdchy-beta` live tại `beta.supra.cc.cd`.
- Worker Version ID: `f8ddf638-0829-4249-9ea0-8f2d38b03f05`.
- D1 `vhdchy-data-beta`: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, APAC.
- D1 schema `business_core_v1`: PASS.
- Worker meta `BUSINESS_CORE_V1`: PASS.
- Google Gateway advisory probe on latest deploy: PASS.
- Protected business routes remain closed with `AUTH_REQUIRED`; anonymous mutation remains disabled.

## BETA Pick Pack 1291 Sheet

- Cluster folder: `PICK_PACK_1291`, Drive ID `1toB5gBne5-v15clBkdbL6O5CrxzPBmEs`.
- Workbook: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`.
- Spreadsheet ID: `14gHSgWXP2QvtPmBD3CzFt3vQ6AED2hPSm_LxMGpXAKg`.
- Sheet schema: `PP1291_SHEETS_BETA_V1`.
- D1 remains authority; Sheet is projection/human-readable surface.
- Old Pick Pack rows migrated: NONE.
- Old Admin password verifier is not reproduced.
- Old LAN/emergency/fallback tabs are not recreated by default.
- Human-facing headers/catalogs from backup were ADAPTED, with validation/filter/header formatting verified.

## Reconciliation result

KEEP as generic DC concepts: clusters, shifts, employees/membership, work sessions, resource binding, labor concept, documents, immutable events, conflict/correction, projection outbox/catalog and import audit.

REWORK additively:

- resource typing must become generic/configurable; v1 hard-coded Pick Pack resource types must not define whole-DC taxonomy;
- dropped-goods/nhận hàng rớt is module `PICK_PACK_1291`, not universal DC core;
- labor/resource/position catalogs must remain configurable where semantics are module-specific.

Applied migration `0001_business_core.sql` is historical and must not be edited; fixes use `0002+`.

## LAN priority gate — approved

`LAN-PILOT-001` now precedes deep business implementation. Deliverables are reusable, not disposable prototypes:

- signed Android/PDA BETA LAN test app;
- Windows LAN Agent BETA with lightweight background service + tray/settings console;
- internal LAN Web BETA at `beta-lan.supra.cc.cd` after internal DNS/routing setup;
- test harness and evidence report.

Mandatory pilot behavior:

- PDA auto-detects valid BETA LAN service and auto-enters LAN mode;
- deterministic fallback/reconnect without unexplained event loss;
- physical test with 1/2/3 PDA + 1 laptop;
- synthetic service-load test on laptop for larger logical-client counts;
- Android and LAN Agent each have automatic update discovery/notification plus manual update fallback;
- LAN Agent exposes operational settings/metrics including selectable local data directory while keeping low measured CPU/RAM footprint.

Detailed contract: `docs/lan/LAN_PILOT_001.md`.

## Branch/live refs

- `main`: source/integration/authority; LAN pilot decisions/spec now live here.
- `beta`: `947a4feb48bc5c99867f1975b56edbb9a7309925` known-good live BETA pointer; not moved by planning/governance work.
- `stable`: `5b7132071f032ab46f133d4416f80791505f080d`; runtime not promoted.

## Next

Execute `LAN-PILOT-001` from `NEXT_ACTIONS.md`: LAN contract + Windows LAN Agent + Android LAN test app + LAN Web + physical/synthetic test harness. Existing core/auth/sheets source work remains checkpointed and may proceed only where independent/supportive; it no longer outranks the LAN feasibility gate.
