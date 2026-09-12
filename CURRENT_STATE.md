# CURRENT STATE

Updated: 2026-09-12
Baseline: `SETUP-RESET-20260912-01`

## Overall

Project logic/architecture/code is preserved. Provider/setup state has been reset and is being rebuilt in strict dependency order.

### Verified now

- GitHub `tamnv2/vanhanhsupradchungyen`: current admin/push connection.
- Reset governance CI run `34666033568`: SUCCESS.
- Google Drive root `VẬN HÀNH DC HƯNG YÊN`: owner `tam95.supra@gmail.com`.
- BETA Drive root `10_RUNTIME_BETA`: owner `tam95.supra@gmail.com`; existing skeleton reused.
- BETA cluster folder `PICK_PACK_1291`: `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`.
- Current BETA projection Sheet: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`, owner `tam95.supra@gmail.com`, correct parent/schema, `PROVISIONED_NOT_LIVE`.
- Reference folder `BACKUP PICK PACK 1291`: owner `tam95.supra@gmail.com`; reference-only.
- Pre-reset snapshot branch: `archive/pre-setup-reset-20260912`.

### Reset / not current

- Any old Google Cloud project/OAuth client/refresh token/GAS Script ID/deployment/exec URL from earlier accounts.
- Old projection spreadsheet IDs from prior accounts/baselines.
- Old GitHub Environment variable/secret contents until verified through rebuilt provider chain.
- Old claims that provider setup is DONE/LIVE.

## Current provider matrix

| Area | Status | Current owner/account | Next gate |
|---|---|---|---|
| GitHub repo access/governance | VERIFIED_CURRENT | `tamnv2` / `nguyenvantam050595@gmail.com` | keep CI green |
| Drive root | VERIFIED_CURRENT | `tam95.supra@gmail.com` | reuse |
| BETA Drive structure | VERIFIED_CURRENT | `tam95.supra@gmail.com` | reuse |
| BETA cluster folder | VERIFIED_CURRENT | `tam95.supra@gmail.com` | reuse |
| BETA Google Sheet | VERIFIED_EXISTING_NOT_LIVE | `tam95.supra@gmail.com` | GAS bootstrap/integration |
| BETA GCP/OAuth | SETUP_REQUIRED | `tam95.supra@gmail.com` | Owner UI setup |
| BETA GAS | SETUP_REQUIRED | `tam95.supra@gmail.com` | depends on GCP/OAuth + current Sheet |
| Cloudflare | OWNER_CONFIRMED_NOT_TOOL_VERIFIED | `nguyenvantam050595@gmail.com` | verify retained account/resources/token |
| Android BETA signer | VERIFY_REQUIRED | Owner-controlled | local fingerprint/alias verify |
| GitHub beta Environment | SETUP_REQUIRED | `tamnv2` repo | after provider outputs exist |
| BETA integration/live | BLOCKED | — | provider inputs verified |
| STABLE | BLOCKED | — | BETA PASS + Owner approval |

## Preserved technical state

The following remains valid as source/evidence but is **not proof that the rebuilt environment is live**:

- service-first Worker/D1 architecture and migrations;
- Google projection model;
- gateway foundation source and least-privilege manifest;
- Android LAN pilot source;
- Windows LAN Agent/LAN Web source;
- LAN V4 automated candidate/build evidence;
- LAN physical evidence with exactly 2 MT90 before provider reset;
- decisions D-001..D-031 unless superseded.

Final LAN V4 physical regression remains pending and resumes after BETA setup is operational.

## Current execution order

1. Freeze/snapshot old authority state — DONE.
2. Reset GitHub authority/current-state docs + validate — DONE.
3. Verify/reuse current Drive structure — DONE for BETA.
4. Provision/verify BETA Pick Pack 1291 projection Sheet — DONE as `PROVISIONED_NOT_LIVE`.
5. Run independent setup lanes in parallel: Google Cloud/OAuth BETA, Cloudflare verification, Android signer verification — NEXT.
6. GAS BETA after Google Cloud/OAuth is ready; Sheet dependency is already satisfied.
7. Populate GitHub `beta` Environment only from verified provider outputs.
8. CI/provider verification + BETA deploy/health.
9. Physical LAN V4 regression.
10. STABLE only after BETA PASS + Owner approval.
