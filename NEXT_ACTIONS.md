# NEXT ACTIONS

Checkpoint: `SETUP-RESET-20260912-03`

## Objective

Rebuild Service setup from the current authority baseline, reuse verified Drive resources, and defer only the physical LAN work that requires the corporate site.

## Dependency graph

```text
[GitHub authority reset + validation] PASS
               |
               v
[Drive root cleaned: 01_BETA / 02_STABLE] PASS
               |
               v
[BETA Sheet baseline] PASS / PROVISIONED_NOT_LIVE
               |
               +---- parallel ----> [Google Cloud/OAuth BETA]
               |
               +---- parallel ----> [Cloudflare account/resources/token verify]
               |
               +---- parallel ----> [VHDCHY BETA signer verify]
               |
               v
[Google Cloud/OAuth + current Sheet]
               |
               v
[GAS BETA]
               |
[all provider outputs verified]
               |
               v
[GitHub beta Environment]
               |
               v
[CI verification -> BETA deploy/health]
               |
               +---- when Owner is back at company ----> [LAN V4 physical regression]
               |
               v
[BETA integration acceptance]
               |
               v
[Owner approval -> isolated STABLE setup]
```

Independent Service lanes must run in parallel when tool/UI permits. LAN physical work is deferred only because Owner is currently away from the corporate site; once on-site it resumes in parallel with any remaining Service work that does not share state.

## Phase 0 — authority reset — PASS

- [x] Verify current GitHub account/repo rights.
- [x] Create pre-reset snapshot branch `archive/pre-setup-reset-20260912`.
- [x] Reset current provider authority while preserving code/history.
- [x] Validation run `34666033568` SUCCESS.

## Phase 1 — Drive + BETA data surface — PASS / NOT LIVE

- [x] Rename BETA root to `01_BETA`, ID unchanged: `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`.
- [x] Rename STABLE root to `02_STABLE`, ID unchanged: `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI`.
- [x] Project root now contains only `01_BETA` and `02_STABLE`.
- [x] Move setup-era folders `00_DỮ_LIỆU_DÙNG_CHUNG`..`06_HỆ_THỐNG` out of project root to sibling `VHDCHY_LEGACY_SETUP_20260908`, ID `1NysNtmsMAxFA5JgsYwEJNgMVvbogvf9R`.
- [x] Reuse BETA level-1 contract: `00_SHARED`, `01_CLUSTERS`, `02_MEDIA`, `03_ARCHIVE`, `04_BACKUP`, `05_LOG`, `06_EXPORT`, `07_SYSTEM`.
- [x] Current BETA cluster `PICK_PACK_1291`: `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`.
- [x] Current BETA workbook: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`.
- [x] Verify owner/location/17 tabs/schema; no old business data or password verifier migrated.

Status remains `PROVISIONED_NOT_LIVE` until GAS + BETA integration gate.

## Phase 2A — Google Cloud/OAuth BETA — NEXT / Owner UI

Run in parallel with Phase 2B/2C.

- Account: `tam95.supra@gmail.com`.
- Create/select dedicated BETA standard Google Cloud project.
- Enable Google Apps Script API for the current CI path.
- Enable account-level Apps Script API access.
- Configure Google Auth Platform / OAuth client.
- CI scopes exactly:
  - `https://www.googleapis.com/auth/script.projects`
  - `https://www.googleapis.com/auth/script.deployments`
- Generate durable refresh token only after intended production publishing state is set.

## Phase 2B — Cloudflare retained resources — NEXT / Owner UI

Parallel lane.

- Login/account: `nguyenvantam050595@gmail.com`.
- Verify account ID and zone `supra.cc.cd`.
- Verify intended BETA Worker/D1 rather than blindly recreating.
- Create/verify custom token with current minimum: `Workers Scripts Write` + `D1 Write`.
- Missing expected resource = STOP/record mismatch; do not silently create replacement.

## Phase 2C — Android BETA signer — NEXT / Owner local

Parallel lane.

- Verify retained VHDCHY BETA keystore locally.
- Confirm alias + SHA256 + store/key passwords.
- Do not use Pick Pack 1291 legacy signer.

## Phase 3 — GAS BETA

Depends on Phase 2A. Sheet dependency is already satisfied.

- Create standalone BETA Apps Script owned by `tam95.supra@gmail.com`.
- Link to BETA standard Cloud project.
- Use `gateway/Code.gs` + `gateway/appsscript.json` from current `main`.
- Runtime scopes exactly `spreadsheets` + `userinfo.email`.
- Target Sheet ID: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`.
- Run `bootstrapAuthorize()` once and verify account/workbook.
- Deploy Web App; record Script ID, Deployment ID, `/exec` URL.

## Phase 4 — GitHub beta Environment

Only after provider outputs are verified. Populate variables/secrets from the current provider outputs; never enter stale values from pre-reset setup.

## Phase 5 — BETA Service gate

- repo/governance validation;
- OAuth refresh;
- Apps Script content/deployment and `/exec` identity;
- projection workbook through GAS;
- Cloudflare account/Worker/D1/token behavior;
- Android signer/build;
- BETA deploy/health;
- record exact commit/run/resource IDs.

## Phase 6 — LAN physical regression — DEFERRED UNTIL OWNER IS AT COMPANY

When Owner is back on the corporate laptop/network, resume the exact 2-MT90 V4 physical matrix. No admin/router/firewall/DNS bypass. This work can run in parallel with remaining Service work that has no shared-state dependency.

## Phase 7 — STABLE

Blocked until BETA PASS + explicit Owner approval. Repeat provider-first setup with isolated STABLE resources; do not copy BETA credentials/IDs.
