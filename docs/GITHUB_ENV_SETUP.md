# GITHUB ENVIRONMENT SETUP — LEAST-PRIVILEGE INPUT AFTER PROVIDERS ARE READY

Repo authority: `tamnv2/vanhanhsupradchungyen`.

## STOP CONDITION

Do **not** start by filling GitHub variables/secrets. GitHub is the final registry for provider values that have already been created and verified.

Correct dependency order:

`Google/Cloudflare/signing setup -> collect/verify IDs + credentials -> GitHub beta Environment -> AI/CI BETA verification -> BETA PASS -> Owner approval -> STABLE provider setup -> GitHub stable Environment -> STABLE verification/deploy`.

Detailed audit/runbook: `docs/security/PERMISSION_AUDIT_2026-09-11.md` and `docs/runbooks/REAUTHORIZATION_2026-09-11.md`.

## 1. Direct links

- Repository Environments: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments
- Repository Actions settings: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions
- Google Cloud project create: https://console.cloud.google.com/projectcreate
- Google Auth Platform: https://console.cloud.google.com/auth/overview
- Google Apps Script API library: https://console.cloud.google.com/apis/library/script.googleapis.com
- Apps Script account API-access setting: https://script.google.com/home/usersettings
- Apps Script home: https://script.google.com/home
- OAuth 2.0 Playground: https://developers.google.com/oauthplayground/
- Cloudflare API tokens: https://dash.cloudflare.com/profile/api-tokens
- Cloudflare dashboard: https://dash.cloudflare.com/

## 2. What must exist BEFORE GitHub BETA entry

1. Current retained Cloudflare account ID verified from the current account.
2. A Cloudflare deploy API token scoped only to current account with `Workers Scripts Write` + `D1 Write`.
3. Google Cloud project `VHDCHY-BETA`, owned/managed by `automation@supra.cc.cd`.
4. **Google Apps Script API only** enabled in that Cloud project for the current CI deployment path. Do not enable Drive/Sheets/Gmail/Calendar/Contacts APIs merely because Apps Script built-in services use those products.
5. Apps Script dashboard setting `Google Apps Script API` enabled for `automation@supra.cc.cd` so authorized applications can modify script projects/deployments.
6. Google Auth Platform configured as External, with the final CI app published/In production before the durable refresh token is created.
7. BETA OAuth client + refresh token with only `script.projects` and `script.deployments`.
8. New BETA Apps Script project linked to the BETA standard Cloud project, using the audited manifest, manually authorized, and deployed as Web app.
9. New BETA `GAS_SCRIPT_ID`, `GAS_DEPLOYMENT_ID`, and `GAS_EXEC_URL` recorded.
10. Current BETA projection workbook verified: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`.
11. Existing **VHDCHY BETA** Android keystore located and alias/password/base64 verified. Do not use the retired Pick Pack 1291 keystore from the legacy reference archive.

Only then fill the GitHub `beta` Environment.

## 3. Repository Actions permission — keep the default restricted

Open: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions

Under `Workflow permissions` keep **Read repository contents and packages permissions** as the repository default.

Reason: current workflows already declare their own minimum `permissions`:

- BETA/STABLE deploy workflows: `contents: read`.
- LAN Pilot release workflow: `contents: write` because `gh release create` publishes release assets.

Do **not** globally switch the whole repository to `Read and write permissions`, and do **not** enable `Allow GitHub Actions to create and approve pull requests`; current workflows do not need it.

## 4. Environments

Open: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments

Create exactly:

- `beta`
- `stable`

Protection:

- `beta`: restrict deployment branches to branch `beta` where the UI supports selected deployment branches.
- `stable`: restrict to branch `stable`; enable Owner required review/approval where supported.
- Environment secrets are only exposed to jobs referencing that environment.

Do not populate/deploy STABLE until BETA recovery passes and Owner explicitly approves STABLE.

## 5. BETA — exact current variables consumed by workflows

### Environment variables

| Name | Exact source/value |
|---|---|
| `APP_ENV` | `beta` |
| `OWNER_EMAIL` | `automation@supra.cc.cd` |
| `CF_ACCOUNT_ID` | current retained Cloudflare account ID, read from current dashboard |
| `PUBLIC_HOST` | `beta.supra.cc.cd` |
| `GAS_SCRIPT_ID` | new BETA Apps Script -> Project Settings -> Script ID |
| `GAS_DEPLOYMENT_ID` | new BETA Apps Script -> Deploy -> Manage deployments |
| `GAS_EXEC_URL` | new BETA Web app `/exec` URL |
| `GOOGLE_SHEETS_PROJECTION_ID` | `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ` |
| `ANDROID_SIGNING_ALIAS` | retained VHDCHY BETA keystore alias; recorded value `vhdchy-beta`, verify before entry |

Not required by current workflows/scripts and therefore **do not create just for completeness**:

- `CF_ZONE_ID`
- `CF_ZONE_NAME`
- `LAN_HOST`
- `GOOGLE_DRIVE_ENV_ROOT_ID`
- `ANDROID_SIGNING_SHA256` (until a workflow actually consumes it; fingerprint remains a verification reference)

### Environment secrets

Current BETA deploy/GAS:

| Name | Source |
|---|---|
| `CLOUDFLARE_API_TOKEN` | current scoped Cloudflare token: Workers Scripts Write + D1 Write |
| `GOOGLE_OAUTH_CLIENT_ID` | BETA CI OAuth client |
| `GOOGLE_OAUTH_CLIENT_SECRET` | same BETA client |
| `GOOGLE_OAUTH_REFRESH_TOKEN` | final BETA refresh token generated after intended production publishing state |

Current BETA Android build/release:

| Name | Source |
|---|---|
| `ANDROID_SIGNING_KEY_B64` | retained **VHDCHY BETA** keystore encoded base64 |
| `ANDROID_SIGNING_STORE_PASSWORD` | retained VHDCHY BETA keystore password |
| `ANDROID_SIGNING_KEY_PASSWORD` | retained VHDCHY BETA key password |

Never paste secret values into chat, repository files, issues, Drive docs, workflow inputs, or ordinary variables.

## 6. CI Google OAuth — exact scope set

Current `scripts/deploy-gas.sh` uses only Apps Script API project/version/deployment endpoints. It does not call Drive or Sheets REST APIs.

Use exactly:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

Do not add Drive, Sheets, Gmail, Calendar, Contacts, `mail.google.com`, `script.send_mail`, or `script.scriptapp` to the **CI OAuth client** unless current source changes and the requirement is re-audited.

## 7. GAS runtime OAuth — separate from CI OAuth

Current audited manifest requests only:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

Why:

- `spreadsheets`: standalone gateway must be able to open/update the designated projection workbook; `spreadsheets.currentonly` is not suitable for a standalone gateway targeting a specified workbook.
- `userinfo.email`: manual bootstrap verifies authorization is being performed as `automation@supra.cc.cd`.

Explicitly excluded at current checkpoint:

- full Drive / Drive read-only/file scopes
- `script.external_request`
- `script.scriptapp`
- `script.send_mail`
- Gmail / Calendar / Contacts

If a later approved feature actually adds Drive file management, outbound GAS HTTP calls, installable triggers, or email, re-audit and add only the specific scope at that time.

## 8. STABLE

Do not configure STABLE credentials now.

After BETA provider setup + GitHub Environment + CI/health all PASS and Owner explicitly approves STABLE recovery, repeat the provider-first process with separate STABLE Google Cloud/OAuth/GAS credentials and a separately provisioned STABLE projection workbook.

At that time the required STABLE variables will mirror the BETA set with `APP_ENV=stable`, `PUBLIC_HOST=supra.cc.cd`, and STABLE-specific GAS/projection IDs.

Do not enter STABLE Android signing secrets until a current STABLE Android build workflow actually consumes them.

## 9. Gate

GitHub BETA is complete only when every input above has a verified provider source and no placeholder remains. Then AI may run current-ID verification and BETA deploy. Entering variables alone never moves `beta` or `stable` live refs.
