# SESSION CHECKPOINT

Checkpoint ID: `LAN-PILOT-20260911-01`
Timestamp: `2026-09-11 Asia/Ho_Chi_Minh`

## Authority/source basis

- MAIN contains reconciled DC core/Sheet baseline plus new Owner-approved LAN pilot priority decisions/spec.
- BETA live/source ref: `947a4feb48bc5c99867f1975b56edbb9a7309925`.
- STABLE ref: `5b7132071f032ab46f133d4416f80791505f080d`.

## Verified BETA runtime

- Validation run `34495312672`: SUCCESS.
- Deploy run `34495312746`: SUCCESS.
- Worker version: `f8ddf638-0829-4249-9ea0-8f2d38b03f05`.
- D1 schema: `business_core_v1` PASS.
- Worker meta: `BUSINESS_CORE_V1` PASS.
- Google Gateway advisory probe on latest deploy: PASS.
- STABLE promotion: NOT DONE / gated.

## Done before this checkpoint

- `RECONCILE-001` completed and BETA Pick Pack 1291 quarterly workbook created.
- Generic DC vs cluster-specific boundaries recorded.
- Current Business Core V1 BETA known-good state recorded.

## Owner decisions added 2026-09-11

- `LAN-PILOT-001` is the priority feasibility gate before deep business build.
- PDA must automatically detect valid BETA LAN and enter LAN mode; fallback/reconnect must be deterministic.
- Available physical test capacity: about 3 PDA simultaneously + 1 laptop.
- Larger load testing uses synthetic logical clients on laptop; this is service-capacity evidence, not equivalent RF/Wi-Fi evidence.
- Android and LAN Agent require both automatic update discovery/notification and a manual update fallback path.
- LAN Agent must be lightweight but operationally visible: background Windows service + tray/settings console with status, update, local data-directory selection, logs/diagnostics and basic metrics.
- `beta-lan.supra.cc.cd` remains internal-only; no public DNS.

Detailed contract: `docs/lan/LAN_PILOT_001.md`.

## Exact next action

Execute `LAN-PILOT-001` from `NEXT_ACTIONS.md` in parallel streams:

1. LAN discovery/auto-LAN/fallback contract.
2. Windows LAN Agent BETA skeleton + tray/settings/updater.
3. Android signed BETA LAN test app skeleton + auto-LAN/update/manual-update controls.
4. LAN Web diagnostics foundation.
5. Physical 1/2/3-PDA + synthetic load test harness and runbook.

Do not spend a tranche extending deep Pick Pack business features before this gate passes. Existing core/auth/sheets source remains checkpointed and may proceed only if independent/supportive.

## Owner action required

NO at tranche start. Once installable artifacts are ready, Owner will need to run them on the test laptop/PDA and apply local DNS/hosts/router configuration from the test runbook where needed.
