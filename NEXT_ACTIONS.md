# NEXT ACTIONS

Checkpoint: `PROVIDER-RECOVERY-20260911-01`

## Priority objective

Restore the project to the pre-account-loss operating model without rebuilding providers that still retain valid setup:

`main authority -> BETA isolated environment -> verified gate -> STABLE isolated environment + Owner approval`.

LAN Pilot V4 physical regression remains preserved and resumes after BETA infrastructure recovery.

## Dependency graph

### Lane A — GitHub authority (independent now)

- Complete authority/current-state/security documentation on `recovery/identity-authority-20260911`.
- PR to `main`; run validation.
- Do not move `beta` or `stable`.
- Owner configures GitHub `beta` and `stable` Environments, variables and secrets from `docs/GITHUB_ENV_SETUP.md`.
- Recreate release objects/assets only after signing environment is verified; imported tags alone are not equivalent to releases.

### Lane B — Google Cloud/OAuth BETA (Owner UI required, can run in parallel with Lane A)

- Create/select a dedicated BETA Google Cloud project owned by `automation@supra.cc.cd`.
- Enable Apps Script API and Drive API.
- Configure Google Auth Platform Branding/Audience/Data Access.
- Use External audience because this is a consumer Google Account, not Workspace-internal identity.
- Request only CI scopes documented in `docs/GITHUB_ENV_SETUP.md`.
- Move Publishing status from Testing to In production before generating the final durable CI refresh token.
- Generate one offline refresh token and store it only in GitHub `beta` Environment secrets.

### Lane C — Google Drive BETA (automatic work mostly complete)

Current provisioned BETA resources under `automation@supra.cc.cd` are recorded in `SERVICE_AUTHORITY.md` and are `PROVISIONED_NOT_LIVE`.

Next automatic step after GAS identity exists: update the BETA projection registry/config to current workbook ID, verify sheet schema/readback, then mark it live only after integration PASS.

### Lane D — GAS BETA (depends on BETA OAuth/project creation)

- Create a new BETA standalone Apps Script project owned by `automation@supra.cc.cd`.
- Associate with the intended BETA Google Cloud project where required by the current API/deployment flow.
- Upload gateway source + manifest with audited runtime scopes.
- Run `bootstrapAuthorize()` once interactively as `automation@supra.cc.cd`.
- Create BETA Web App deployment using current design.
- Record new `GAS_SCRIPT_ID`, `GAS_DEPLOYMENT_ID`, `GAS_EXEC_URL` in GitHub `beta` Environment variables.
- Test endpoint and Apps Script API mutation/version/deployment from CI credentials.

### Lane E — retained Cloudflare provider (parallel after GitHub token is entered)

- Do not recreate zone/Worker/D1/DNS.
- Validate current scoped Cloudflare token against existing account/zone/D1.
- Verify BETA Worker/D1 health without destructive deploy first.
- Next deploy must reuse existing resources rather than create replacements if names/IDs resolve correctly.

### Lane F — Android signing (parallel after GitHub Environment exists)

- Restore BETA signing secrets from Owner-controlled keystore backup.
- Verify alias and SHA256 against recorded fingerprint.
- Do not generate a new key.

## BETA integration gate

Only after Lanes A–F prerequisites are complete:

1. restore/update `verify-environments.yml` with CURRENT IDs only;
2. verify Google OAuth refresh + Apps Script project + Drive root;
3. verify Cloudflare token/resource access;
4. verify Android signing material;
5. run repository validation and BETA deployment/health;
6. update checkpoint with exact commit/run/resource IDs;
7. move/confirm `beta` pointer only to the verified known-good recovery commit.

## STABLE recovery

Blocked until BETA PASS.

After BETA PASS:

- configure isolated STABLE Google Cloud/OAuth/GAS resources;
- use STABLE Drive root only;
- restore STABLE GitHub Environment vars/secrets;
- verify retained STABLE Cloudflare resources;
- verify STABLE signing material;
- require Owner approval before STABLE deployment/pointer move.

Do not copy BETA GAS IDs, Drive IDs, OAuth tokens or signing secrets into STABLE.

## LAN Pilot continuity after recovery

Pre-recovery checkpoint is preserved at `docs/checkpoints/2026-09-11-LAN-PILOT-20260911-04.md`.

After BETA infra is restored, resume the physical V4 regression on the restricted corporate laptop + exactly two MT90. No Administrator/router/DNS/firewall changes. Synthetic clients are capacity evidence only.

## Soft-stop/checkpoint rule

For any CI/provider tranche approaching ~20 minutes:

- stop starting new long work;
- record commit/ref + run/job ID + provider state;
- update `SESSION_CHECKPOINT.md` with `DONE / IN_PROGRESS / NOT_STARTED`, tests, blockers and exact next actions;
- continue independent lanes in parallel instead of waiting idly for a long job.
