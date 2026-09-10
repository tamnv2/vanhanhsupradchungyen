# CHANGE RECORD — reconcile-sheets-0.5.0

- Change ID: `CHG-20260910-050`
- Time: 2026-09-10, Asia/Ho_Chi_Minh
- Main implementation range: `314006fc3ee02817b621c231d235369c9162e7d2` .. `7994c5b783d8b3bd94bbba97a1b05e32978f05c0`
- BETA runtime commit verified: `947a4feb48bc5c99867f1975b56edbb9a7309925`
- Environment: BETA + main authority documentation; STABLE untouched
- Modules: governance, DC core, Pick Pack 1291, Google Sheets projection, CI

## Reason

Owner approved using the retired Pick Pack 1291 Google Sheet model as a selective baseline for the new `PICK_PACK_1291` cluster while preserving VHDCHY as the broader DC platform. Existing BETA business core also required scope reconciliation before further expansion.

## Changes

1. Completed `RECONCILE-001` and classified deployed schema areas as GENERIC_DC / CLUSTER_1291 / ADAPT/REWORK.
2. Added `docs/architecture/MODULE_MAP.md`.
3. Synchronized the already-deployed BETA Business Core V1 migration/Worker/deploy contract into `main` authority without moving the BETA live pointer.
4. Merged governance continuity validation and local D1 migration validation in main CI.
5. Created Drive BETA cluster folder `PICK_PACK_1291`.
6. Created native Google Sheet `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`, schema `PP1291_SHEETS_BETA_V1`.
7. Adapted useful old Pick Pack headers/catalogs and added VHDCHY control/document/conflict/import tabs.
8. Replaced old human-readable Admin projection with `DANH SÁCH TÀI KHOẢN`; password verifier/secrets are excluded.
9. Did not migrate old data and did not recreate old LAN/emergency/fallback tabs.
10. Updated the Drive Resource Registry with latest BETA Worker/schema and workbook identifiers.
11. Re-issued valid historical BETA business decisions under unique main authority IDs D-015..D-024 to avoid collision with governance IDs.

## Resources changed

### Google Drive BETA

- Cluster folder ID: `1toB5gBne5-v15clBkdbL6O5CrxzPBmEs`
- Spreadsheet ID: `14gHSgWXP2QvtPmBD3CzFt3vQ6AED2hPSm_LxMGpXAKg`
- Resource Registry document ID: `1lMrW6btaun1kWNsi0_jf7UH1lOhXonBwJ_xp08ahPYw`

### Cloudflare BETA — verification only, no new deployment in this tranche

- Worker version remains `f8ddf638-0829-4249-9ea0-8f2d38b03f05`
- D1 schema remains `business_core_v1`

## Migrations

No new remote migration was applied in this tranche. `0001_business_core.sql` was copied into main authority as historical/deployed source. It must not be edited; scope corrections will use additive migration `0002+`.

## Tests / verification

- Existing BETA deploy run `34495312746`: SUCCESS; Worker health, D1 schema, meta and Google Gateway advisory probe PASS.
- New Google Sheet read-back verified: control metadata, categories, headers, formatting and data-validation rules present.
- Main CI run `34506547071` at implementation commit `7994c5b...`: SUCCESS after source sync; governance continuity + D1 migration validation therefore PASS together.

## Result

PASS for reconciliation, authority sync and BETA Sheet creation. Projection transport is not yet active for business data and protected mutation remains auth-closed.

## Rollback

- Do not delete or rewrite migration 0001.
- Sheet is currently a BETA projection surface with no migrated production data; if schema must be revised, create a new schema/version or make additive controlled changes and record them.
- BETA live runtime remains pinned to the previously verified commit; this tranche did not move BETA or STABLE refs.

## Decisions

D-015 through D-024. See `DECISIONS.md` and `DECISIONS_INDEX.md`.

## Next impact

Start `PARALLEL-BUILD-001`: CORE-REFINE-001, SHEETS-001, AUTH-001 and ANDROID-FOUNDATION-001. Integrate only after each track passes its gate.
