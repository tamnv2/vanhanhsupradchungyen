# 2026-09-12 — beta-sheet-reprovision-1.0.1

Change ID: `SETUP-RESET-20260912-02`
Environment: BETA only

## Reason

Phase 1 of the post-reset setup required a fresh current-account projection workbook without reviving old Google account IDs or legacy runtime dependencies.

## Changes

- Reused verified `10_RUNTIME_BETA` and existing folder skeleton.
- Created BETA cluster folder `PICK_PACK_1291`: `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`.
- Created native Google Sheet `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`.
- Owner verified by Drive API as `tam95.supra@gmail.com`; parent verified as current BETA cluster folder.
- Provisioned 17 current tabs from `PP1291_SHEETS_BETA_V1` / current adopted tab map.
- Added `00_CONTROL` baseline/authority markers.
- Adapted useful legacy business headers only; no historical rows imported.
- Replaced legacy Admin/password-verifier shape with non-secret `DANH SÁCH TÀI KHOẢN`.
- Consolidated duplicate legacy USER PACK header.
- Did not recreate LAN authority/emergency/fallback legacy tabs.

## Result

`VERIFIED_EXISTING_NOT_LIVE` / `PROVISIONED_NOT_LIVE`.

The workbook is not runtime authority and is not marked LIVE until current GAS/OAuth/CI integration passes.

## Next

Run Google Cloud/OAuth BETA, Cloudflare verification and Android BETA signer verification in parallel. GAS follows Google Cloud/OAuth; GitHub beta Environment follows all provider outputs.
