# NEXT ACTIONS

Checkpoint: `SETUP-RESET-20260912-01`

## Objective

Rebuild setup from a clean authority baseline without discarding valid code/logic or recreating already verified resources.

## Dependency graph

```text
[GitHub authority reset + validation]
               |
               v
[Drive structure verification] ----> [Create/verify BETA projection Sheet]
               |                                  |
               |                                  v
               |                        [Google Cloud/OAuth BETA]
               |                                  |
               |                                  v
               |                              [GAS BETA]
               |
               +---- parallel ---- [Cloudflare account/resources/token verify]
               |
               +---- parallel ---- [VHDCHY BETA signer verify]

[Sheet + GCP/OAuth + GAS + Cloudflare + signer verified]
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

## Phase 0 — authority reset

- [x] Verify current GitHub account/repo rights.
- [x] Create pre-reset snapshot branch `archive/pre-setup-reset-20260912`.
- [x] Verify Drive root/reference ownership.
- [x] Verify current BETA Drive root ownership and existing skeleton.
- [ ] Commit baseline authority reset to `main` and run repo validation.

## Phase 1 — BETA data surface

- Reuse `10_RUNTIME_BETA`; do not recreate it.
- Under `01_CLUSTERS`, create/verify `PICK_PACK_1291` structure if missing.
- Create a fresh current BETA quarterly projection workbook under `tam95.supra@gmail.com` using schema `PP1291_SHEETS_BETA_V1`/latest adopted schema.
- Do not import historical business data or password verifier.
- Record new spreadsheet ID only after schema/owner/location verification.

## Phase 2A — Google Cloud/OAuth BETA

Owner UI; can run in parallel with Phase 2B/2C after baseline docs are committed.

- Current account: `tam95.supra@gmail.com`.
- Create/select dedicated BETA standard Google Cloud project.
- Enable Google Apps Script API only for current CI path.
- Enable account-level Apps Script API access.
- Configure Google Auth Platform / OAuth client.
- CI scopes exactly:
  - `https://www.googleapis.com/auth/script.projects`
  - `https://www.googleapis.com/auth/script.deployments`
- Generate durable refresh token only after intended production publishing state is set; never paste credentials into chat.

## Phase 2B — Cloudflare retained resources

Parallel lane.

- Login/account: `nguyenvantam050595@gmail.com`.
- Verify account ID and zone `supra.cc.cd`.
- Verify expected BETA Worker/D1 rather than blindly recreating.
- Create/verify custom token with only current required permissions: `Workers Scripts Write` + `D1 Write`.
- Missing expected resource = STOP/record mismatch; do not silently create replacement.

## Phase 2C — Android BETA signer

Parallel lane.

- Verify retained VHDCHY BETA keystore locally.
- Confirm alias + SHA256 + store/key passwords.
- Do not use Pick Pack 1291 legacy signer.
- Do not paste key/base64/passwords into chat/repo.

## Phase 3 — GAS BETA

Depends on current BETA projection Sheet + selected BETA Google Cloud project.

- Create standalone BETA Apps Script owned by `tam95.supra@gmail.com`.
- Link to the BETA standard Cloud project.
- Use `gateway/Code.gs` + `gateway/appsscript.json` from current `main`.
- Runtime scopes exactly `spreadsheets` + `userinfo.email`.
- Run `bootstrapAuthorize()` once and verify account/workbook.
- Deploy Web App; record Script ID, Deployment ID, `/exec` URL.

## Phase 4 — GitHub beta Environment

Only after provider outputs are verified. Populate variables/secrets from `docs/GITHUB_ENV_SETUP.md`. Do not enter placeholders as if they were current values.

## Phase 5 — BETA gate

- validate repository/governance;
- verify OAuth refresh behavior without exposing token;
- verify Apps Script content/deployment and `/exec` identity;
- verify projection workbook through GAS;
- verify Cloudflare token/resource identity and fail-closed behavior;
- verify signed Android build;
- deploy/health test BETA;
- record exact commit/run/resource IDs;
- only then mark BETA PASS/move live pointer if release gate allows.

## Phase 6 — LAN physical regression

Resume exact 2-MT90 corporate-laptop V4 regression after BETA foundation is working. No admin/router/firewall/DNS bypass.

## Phase 7 — STABLE

Blocked until BETA PASS + explicit Owner approval. Repeat provider-first setup with isolated STABLE resources; do not copy BETA credentials/IDs.
