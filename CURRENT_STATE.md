# CURRENT STATE

Updated: 2026-09-12
Baseline: `SETUP-RESET-20260912-01`

## Overall

Project logic/architecture/code is preserved. Provider/setup state is being rebuilt from the current authority baseline.

## Verified now

- GitHub `tamnv2/vanhanhsupradchungyen`: current admin/push connection.
- Google Drive root `VẬN HÀNH DC HƯNG YÊN`: owner `tam95.supra@gmail.com`.
- Project root now contains only two active environment roots:
  - `01_BETA` — ID `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`.
  - `02_STABLE` — ID `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI`.
- Each environment keeps the same level-1 contract: `00_SHARED`, `01_CLUSTERS`, `02_MEDIA`, `03_ARCHIVE`, `04_BACKUP`, `05_LOG`, `06_EXPORT`, `07_SYSTEM`.
- Previous setup-era folders `00_DỮ_LIỆU_DÙNG_CHUNG` through `06_HỆ_THỐNG` were moved out of the project root into sibling folder `VHDCHY_LEGACY_SETUP_20260908`, ID `1NysNtmsMAxFA5JgsYwEJNgMVvbogvf9R`; legacy only, not runtime/authority.
- BETA cluster folder `PICK_PACK_1291`: `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`.
- Current BETA projection Sheet: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`, status `PROVISIONED_NOT_LIVE`.
- Reference folder `BACKUP PICK PACK 1291`: reference-only.
- Pre-reset snapshot branch: `archive/pre-setup-reset-20260912`.

## Current provider matrix

| Area | Status | Current owner/account | Next gate |
|---|---|---|---|
| GitHub repo access/governance | VERIFIED_CURRENT | `tamnv2` / `nguyenvantam050595@gmail.com` | keep CI green |
| Drive root / environment structure | VERIFIED_CURRENT | `tam95.supra@gmail.com` | reuse |
| BETA cluster folder | VERIFIED_CURRENT | `tam95.supra@gmail.com` | reuse |
| BETA Google Sheet | VERIFIED_EXISTING_NOT_LIVE | `tam95.supra@gmail.com` | GAS bootstrap/integration |
| BETA GCP/OAuth | SETUP_REQUIRED | `tam95.supra@gmail.com` | Owner UI setup |
| BETA GAS | SETUP_REQUIRED | `tam95.supra@gmail.com` | depends on GCP/OAuth + current Sheet |
| Cloudflare | OWNER_CONFIRMED_NOT_TOOL_VERIFIED | `nguyenvantam050595@gmail.com` | verify retained account/resources/token |
| Android BETA signer | VERIFY_REQUIRED | Owner-controlled | local fingerprint/alias verify |
| GitHub beta Environment | SETUP_REQUIRED | `tamnv2` repo | after provider outputs exist |
| BETA integration/live | BLOCKED | — | provider inputs verified |
| LAN V4 physical regression | DEFERRED_UNTIL_AT_COMPANY | corporate laptop + 2 MT90 | resume on-site |
| STABLE | BLOCKED | — | BETA PASS + Owner approval |

## Preserved technical state

The following remains valid as source/evidence but is not proof that the rebuilt environment is live:

- service-first Worker/D1 architecture and migrations;
- Google projection model;
- gateway foundation source and least-privilege manifest;
- Android LAN pilot source;
- Windows LAN Agent/LAN Web source;
- LAN V4 automated candidate/build evidence;
- prior physical LAN evidence with exactly 2 MT90;
- decisions D-001..D-031 unless superseded.

## Current execution order

1. Authority reset — DONE.
2. Drive root cleanup/rename to `01_BETA` + `02_STABLE` — DONE.
3. BETA projection Sheet baseline — DONE / NOT LIVE.
4. While Owner is away from corporate site: continue Service setup only.
5. Next independent Service lanes: Google Cloud/OAuth BETA, Cloudflare retained-resource verification, Android BETA signer verification.
6. GAS BETA after Google Cloud/OAuth is ready.
7. Populate GitHub `beta` Environment from verified provider outputs.
8. CI/provider verification + BETA deploy/health.
9. When Owner is back at company: resume LAN V4 physical regression in parallel with remaining Service work.
10. STABLE only after BETA PASS + explicit Owner approval.
