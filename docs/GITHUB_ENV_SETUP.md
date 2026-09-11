# GITHUB ENVIRONMENT SETUP — INPUT ONLY AFTER PROVIDERS ARE READY

Repo authority: `tamnv2/vanhanhsupradchungyen`.

## STOP CONDITION

Do **not** start by filling GitHub variables/secrets. GitHub is the final registry for values created or verified at Google/Cloudflare/signing providers.

Correct dependency order:

`Google/Cloudflare/signing setup -> collect/verify IDs + credentials -> GitHub beta Environment -> AI/CI BETA verification -> BETA PASS -> STABLE provider setup -> GitHub stable Environment -> Owner approval -> STABLE verification/deploy`.

Detailed provider-first runbook: `docs/runbooks/REAUTHORIZATION_2026-09-11.md`.

## 1. Links

- Repository Environments: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments
- Repository Actions settings: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions
- Google Cloud project create: https://console.cloud.google.com/projectcreate
- Google Auth Platform: https://console.cloud.google.com/auth/overview
- Enable Apps Script API: https://console.cloud.google.com/apis/library/script.googleapis.com
- Enable Drive API: https://console.cloud.google.com/apis/library/drive.googleapis.com
- Apps Script user settings/API access: https://script.google.com/home/usersettings
- Apps Script home: https://script.google.com/home
- OAuth 2.0 Playground: https://developers.google.com/oauthplayground/
- Cloudflare dashboard: https://dash.cloudflare.com/

## 2. What must exist BEFORE GitHub BETA entry

Complete/verify these first:

1. Current Cloudflare account ID and a new/known-good scoped deploy API token for the retained resources.
2. Google Cloud project `VHDCHY-BETA` owned by `automation@supra.cc.cd`.
3. Apps Script API + Drive API enabled in that project.
4. Apps Script dashboard setting `Google Apps Script API` enabled for `automation@supra.cc.cd` so authorized applications may manage script content/deployments.
5. Google Auth Platform configured as External and moved to `In production` before generating the final CI refresh token.
6. OAuth client for BETA CI and a final refresh token using only the current CI scopes.
7. New BETA Apps Script project linked to the BETA standard Cloud project, authorized, and deployed as a Web app.
8. New BETA `GAS_SCRIPT_ID`, `GAS_DEPLOYMENT_ID`, and `GAS_EXEC_URL` recorded.
9. Existing Android BETA keystore located and its alias/password/base64 available from Owner-controlled backup.
10. Current BETA Drive root is already known from `SERVICE_AUTHORITY.md`.

Only after all ten items are ready should the Owner fill the GitHub `beta` Environment.

## 3. GitHub Actions repository permission

Open: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions

Under `Workflow permissions`, choose `Read and write permissions` because the LAN release workflow publishes prerelease assets using the repository `GITHUB_TOKEN`.

Do not create a broad PAT for this purpose.

## 4. Create Environments

Open: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments

Create exactly:

- `beta`
- `stable`

Configure `stable` approval/reviewer protection when supported by the current plan/UI. Do not populate/deploy STABLE until BETA recovery passes and Owner approves STABLE.

## 5. BETA — exact values currently consumed by workflows

### Environment variables

| Name | Value/source |
|---|---|
| `APP_ENV` | fixed: `beta` |
| `OWNER_EMAIL` | fixed: `automation@supra.cc.cd` |
| `CF_ACCOUNT_ID` | read from the **current retained Cloudflare account**; do not copy an old ID unless it matches current dashboard |
| `PUBLIC_HOST` | current approved BETA hostname: `beta.supra.cc.cd` |
| `GAS_SCRIPT_ID` | new BETA Apps Script `Project Settings -> Script ID` |
| `GAS_DEPLOYMENT_ID` | new BETA Apps Script `Deploy -> Manage deployments` deployment ID |
| `GAS_EXEC_URL` | new BETA Web app URL ending in `/exec` |
| `GOOGLE_DRIVE_ENV_ROOT_ID` | current BETA root: `1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog` |
| `ANDROID_SIGNING_ALIAS` | existing BETA keystore alias; recorded value is `vhdchy-beta`, verify against retained keystore before entry |

`CF_ZONE_ID` is referenced by the current workflow environment block but is **not consumed by the current deploy scripts**. If you populate it, read it from the current Cloudflare zone; do not rely on an unverified historical value.

`CF_ZONE_NAME`, `LAN_HOST`, and `ANDROID_SIGNING_SHA256` are not currently consumed by the restored BETA deploy/build workflow and are not required just to make current CI run.

### Environment secrets

| Name | Source |
|---|---|
| `CLOUDFLARE_API_TOKEN` | newly created or known-good current Cloudflare deploy token |
| `GOOGLE_OAUTH_CLIENT_ID` | BETA Google Auth Platform OAuth client |
| `GOOGLE_OAUTH_CLIENT_SECRET` | same BETA OAuth client |
| `GOOGLE_OAUTH_REFRESH_TOKEN` | generated **after** BETA OAuth project is `In production` |
| `ANDROID_SIGNING_KEY_B64` | retained BETA keystore encoded to base64 |
| `ANDROID_SIGNING_STORE_PASSWORD` | retained BETA keystore password |
| `ANDROID_SIGNING_KEY_PASSWORD` | retained BETA key password |

Never paste secret values into chat, repository files, issues, Drive docs, workflow inputs, or variables.

## 6. CI OAuth scope set — corrected/minimized

The current `scripts/deploy-gas.sh` only modifies Apps Script project content, versions, and deployments. It does not call the Drive REST API.

The final GitHub CI refresh token therefore needs only:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

Do **not** add `drive.metadata.readonly` to the CI token unless a future verified workflow actually calls the Drive REST API. This avoids an unnecessary restricted Drive OAuth scope.

This CI OAuth set is separate from Apps Script runtime scopes in `gateway/appsscript.json`.

## 7. STABLE

Do not configure STABLE just because the fields exist.

After BETA provider setup + GitHub environment + CI/health all PASS and Owner explicitly approves STABLE recovery, repeat the same provider-first process with separate STABLE GAS/OAuth credentials and use:

- `APP_ENV=stable`
- `OWNER_EMAIL=automation@supra.cc.cd`
- `PUBLIC_HOST=supra.cc.cd`
- `GOOGLE_DRIVE_ENV_ROOT_ID=1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE`
- separate STABLE `GAS_SCRIPT_ID`, `GAS_DEPLOYMENT_ID`, `GAS_EXEC_URL`
- retained STABLE signing identity/secrets
- current retained Cloudflare account/token values as verified

Never copy BETA GAS IDs or BETA Google refresh token into STABLE.

## 8. Gate

GitHub BETA is considered complete only when all values above have a verified source and no placeholder remains. Then AI may rebuild the current-ID verification workflow and run BETA verification/deploy. `beta`/`stable` live refs are not moved merely because variables have been entered.
