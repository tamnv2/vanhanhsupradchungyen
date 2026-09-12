# CURRENT STATE

Updated: 2026-09-12
Baseline: `SETUP-RESET-20260912-01`

## Overall

Project logic/architecture/code is preserved. Provider/setup state has been reset and is being rebuilt in strict dependency order.

### Verified now

- GitHub `tamnv2/vanhanhsupradchungyen`: connection is current and has admin/push rights.
- Google Drive root `VẬN HÀNH DC HƯNG YÊN`: owner `tam95.supra@gmail.com`.
- BETA Drive root `10_RUNTIME_BETA`: owner `tam95.supra@gmail.com`; existing skeleton can be reused.
- Reference folder `BACKUP PICK PACK 1291`: owner `tam95.supra@gmail.com`; reference-only.
- Pre-reset GitHub snapshot branch exists: `archive/pre-setup-reset-20260912`.

### Reset / not current

- Any old Google Cloud project/OAuth client/refresh token/GAS Script ID/deployment/exec URL from earlier accounts.
- Old BETA projection spreadsheet ID `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ` and other old-account workbook IDs.
- Old GitHub Environment variable/secret contents until verified through the rebuilt provider chain.
- Old claims that provider setup is DONE/LIVE.

## Current provider matrix

| Area | Status | Current owner/account | Next gate |
|---|---|---|---|
| GitHub repo access | VERIFIED_CURRENT | `tamnv2` / `nguyenvantam050595@gmail.com` | governance reset commit + CI |
| Drive root | VERIFIED_CURRENT | `tam95.supra@gmail.com` | reuse structure |
| BETA Drive structure | VERIFIED_CURRENT | `tam95.supra@gmail.com` | provision BETA Sheet |
| STABLE Drive structure | VERIFIED_EXISTING_NOT_LIVE | `tam95.supra@gmail.com` | defer until BETA PASS |
| BETA Google Sheet | NOT_PROVISIONED | `tam95.supra@gmail.com` | create + schema verify |
| BETA GCP/OAuth | SETUP_REQUIRED | `tam95.supra@gmail.com` | owner UI setup |
| BETA GAS | SETUP_REQUIRED | `tam95.supra@gmail.com` | depends on BETA GCP + Sheet |
| Cloudflare | OWNER_CONFIRMED_NOT_TOOL_VERIFIED | `nguyenvantam050595@gmail.com` | verify retained account/resources/token |
| Android BETA signer | VERIFY_REQUIRED | Owner-controlled | local fingerprint/alias verify |
| GitHub beta Environment | SETUP_REQUIRED | `tamnv2` repo | after provider outputs exist |
| BETA integration/live | BLOCKED | — | all provider inputs verified |
| STABLE | BLOCKED | — | BETA PASS + Owner approval |

## Preserved technical state

The following remains valid as source/evidence, but is **not proof that the rebuilt environment is live**:

- service-first Worker/D1 architecture and migrations;
- Google projection model;
- gateway foundation source and least-privilege manifest;
- Android LAN pilot source;
- Windows LAN Agent/LAN Web source;
- LAN V4 automated candidate/build evidence;
- LAN physical evidence with exactly 2 MT90 before provider reset;
- decisions D-001..D-029 unless superseded by a newer decision.

Final LAN V4 physical regression remains pending and resumes after BETA setup is operational.

## Current execution order

1. Freeze/snapshot old authority state — DONE.
2. Reset GitHub authority/current-state docs and stale projection registry — IN PROGRESS.
3. Reuse/verify current Drive structure — BETA root PASS; Sheet pending.
4. Run independent setup lanes in parallel: Google BETA, Cloudflare verification, Android signer verification.
5. GAS BETA after Google Cloud + current projection Sheet exist.
6. Populate GitHub `beta` Environment only from verified provider outputs.
7. CI/provider verification + BETA deploy/health.
8. Physical LAN V4 regression.
9. STABLE only after BETA PASS + Owner approval.
