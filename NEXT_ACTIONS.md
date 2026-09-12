# NEXT ACTIONS

Checkpoint: `SETUP-RESET-20260912-02`

## Objective

Rebuild setup from a clean authority baseline without discarding valid code/logic or recreating already verified resources.

## Dependency graph

```text
[GitHub authority reset + validation] PASS
               |
               v
[Drive + BETA Sheet baseline] PASS / PROVISIONED_NOT_LIVE
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
[CI verification -> BETA deploy/health -> BETA PASS]
               |
               v
[LAN V4 physical regression]
               |
               v
[Owner approval -> isolated STABLE setup]
```

Independent lanes must run in parallel when tool/UI permits. Never serialize unrelated work merely because it appears later in a numbered list.

## Phase 0 — authority reset — PASS

- [x] Verify current GitHub account/repo rights.
- [x] Create pre-reset snapshot branch `archive/pre-setup-reset-20260912`.
- [x] Reset current provider authority while preserving code/history.
- [x] Validation run `34666033568` SUCCESS.

## Phase 1 — BETA data surface — PASS / NOT LIVE

- [x] Reuse `10_RUNTIME_BETA`.
- [x] Create current `PICK_PACK_1291` folder: `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`.
- [x] Create current BETA workbook: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`.
- [x] Verify owner/location/17 tabs/schema.
- [x] No old business rows or password verifier migrated.
- [x] Legacy LAN/emergency/fallback tabs not recreated.

Status remains `PROVISIONED_NOT_LIVE` until GAS + BETA integration gate.

## Phase 2A — Google Cloud/OAuth BETA — NEXT / Owner UI

Can run in parallel with Phase 2B/2C.

- Account: `tam95.supra@gmail.com`.
- Create/select dedicated BETA standard Google Cloud project.
- Enable Google Apps Script API only for current CI path.
- Enable account-level Apps Script API access.
- Configure Google Auth Platform / OAuth client.
- CI scopes exactly:
  - `https://www.googleapis.com/auth/script.projects`
  - `https://www.googleapis.com/auth/script.deployments`
- Generate durable refresh token only after intended production publishing state is set; never paste credentials into chat.

## Phase 2B — Cloudflare retained resources — NEXT / Owner UI

Parallel lane.

- Login/account: `nguyenvantam050595@gmail.com`.
- Verify account ID and zone `supra.cc.cd`.
- Verify expected BETA Worker/D1 rather than blindly recreating.
- Create/verify custom token with current minimum: `Workers Scripts Write` + `D1 Write`.
- Missing expected resource = STOP/record mismatch; do not silently create replacement.

## Phase 2C — Android BETA signer — NEXT / Owner local

Parallel lane.

- Verify retained VHDCHY BETA keystore locally.
- Confirm alias + SHA256 + store/key passwords.
- Do not use Pick Pack 1291 legacy signer.
- Do not paste key/base64/passwords into chat/repo.

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

Only after provider outputs are verified. Populate variables/secrets from `docs/GITHUB_ENV_SETUP.md`. Do not enter placeholders as current values.

## Phase 5 — BETA gate

- repo/governance validation;
- OAuth refresh without exposing token;
- Apps Script content/deployment and `/exec` identity;
- projection workbook through GAS;
- Cloudflare account/Worker/D1/token behavior;
- Android signer/build;
- BETA deploy/health;
- record exact commit/run/resource IDs;
- only then mark BETA PASS/move live pointer if release gate allows.

## Phase 6 — LAN physical regression

Resume exact 2-MT90 corporate-laptop V4 regression after BETA foundation is working. No admin/router/firewall/DNS bypass.

## Phase 7 — STABLE

Blocked until BETA PASS + explicit Owner approval. Repeat provider-first setup with isolated STABLE resources; do not copy BETA credentials/IDs.
