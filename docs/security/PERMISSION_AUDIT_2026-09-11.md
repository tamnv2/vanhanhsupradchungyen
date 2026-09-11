# VHDCHY DEEP PERMISSION AUDIT — 2026-09-11

Status: REVIEWED AGAINST CURRENT VHDCHY + FULL LEGACY PICK PACK 1291 SOURCE BACKUP

Purpose: define the smallest permission/credential surface that is sufficient for the **current approved VHDCHY checkpoint**, while preventing historical Pick Pack 1291 permissions from being copied forward merely because they existed.

## 1. Evidence actually reviewed

### Current VHDCHY

Permission-relevant source/config reviewed directly:

- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `docs/BUSINESS_CORE_V1.md`
- `docs/reconciliation/RECONCILE_001.md`
- `docs/reference/PICK_PACK_1291_REFERENCE.md`
- `worker/src/index.js`
- `gateway/Code.gs`
- `gateway/appsscript.json`
- `scripts/deploy-gas.sh`
- `scripts/deploy-cloudflare.sh`
- `scripts/deploy-environment.sh`
- `.github/workflows/deploy-beta.yml`
- `.github/workflows/deploy-stable.yml`
- `.github/workflows/build-lan-pilot.yml`
- `android-pilot/app/build.gradle`
- `config/projections.beta.json`
- current Google Drive resource registry and current BETA projection workbook identity.

### Retired Pick Pack 1291

The legacy reference archive was not evaluated from one README alone. The full source recovery backup was expanded and the main + `beta/current` trees were scanned for permission-bearing services/credentials/APIs, then the matching manifests and implementation paths were inspected.

Legacy permission-bearing features found include:

- GAS `MailApp.sendEmail()` for password reset/SUPERADMIN OTP;
- GAS `DriveApp` for OTA/log/file/fallback artifacts;
- installable-trigger management through `ScriptApp` in historical bridges;
- GAS outbound HTTP through `UrlFetchApp`;
- Worker/service direct Google Sheets REST calls using OAuth refresh tokens;
- historical Google Drive REST document management;
- Firebase Cloud Messaging service-account/JWT path;
- historical DR/provider experiments including Turso/Render/Deno/R2 and other credentials.

These findings explain why the retired project needed a wider permission surface. They **do not** justify granting those permissions to current VHDCHY.

## 2. Architecture boundary that drives permissions

Current VHDCHY is Service-first:

`PDA / Web -> Cloudflare Worker -> D1 canonical authority`

with Google Sheets as asynchronous projection/human-readable/DR surface.

Current Worker:

- binds D1;
- calls GAS `/exec` only for integration health;
- does not directly call Google Sheets/Drive APIs;
- keeps business data/mutation routes closed until auth is implemented.

Current GAS:

- is still a foundation gateway;
- `doGet()` exposes health/identity;
- `doPost()` returns `FOUNDATION_ONLY` 503;
- must not receive legacy fallback/mail/Drive capabilities just because Pick Pack 1291 used them.

## 3. Final minimum permission matrix — CURRENT checkpoint

| Component | Required now | Explicitly NOT required now |
|---|---|---|
| Google Cloud APIs | Google Apps Script API | Drive API, Sheets API, Gmail, Calendar, Contacts, Firebase APIs |
| Google CI OAuth | `script.projects`, `script.deployments` | Drive, Sheets, Gmail, Calendar, Contacts, `script.send_mail`, `script.scriptapp` |
| GAS runtime OAuth | `spreadsheets`, `userinfo.email` | Drive, `script.external_request`, `script.scriptapp`, `script.send_mail`, Gmail/Calendar/Contacts |
| Cloudflare API token | Account `Workers Scripts Write`, Account `D1 Write` | DNS Write, Workers Routes Write, KV, R2, Tail, Account Settings, User Memberships |
| GitHub default GITHUB_TOKEN | repository default read | repository-wide default read/write, PR approval capability |
| GitHub LAN release job | workflow-level `contents: write` | PAT / repo-wide admin token |
| GitHub deploy jobs | workflow-level `contents: read` | write permissions |
| Android BETA | alias + existing VHDCHY BETA keystore + store/key passwords | legacy Pick Pack signer; STABLE signer during BETA recovery |
| Gmail / Calendar / Contacts | none | all scopes/APIs |
| Firebase/FCM | none | service account / `firebase.messaging` |
| R2 / DO / Turso / Render / Deno | none at current checkpoint | all provider credentials unless later adopted |

## 4. Google CI OAuth — exact result

Current `scripts/deploy-gas.sh` performs:

- OAuth refresh-token exchange;
- Apps Script `projects.updateContent`;
- Apps Script `versions.create`;
- Apps Script `deployments.update`;
- ordinary unauthenticated HTTP GET of the Web App health endpoint.

Therefore the CI refresh token needs exactly:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

It does not need Drive metadata/read/full scopes. The previous recovery verifier used Drive REST only as an environment proof; that test was a convenience, not a runtime requirement, and must not dictate the production OAuth scope.

Account-level prerequisite: in Apps Script dashboard settings, enable `Google Apps Script API` for `automation@supra.cc.cd`. This is separate from enabling the Apps Script API in the Cloud project.

## 5. GAS runtime OAuth — corrected result

The earlier bootstrap code itself created artificial permission needs by:

- calling `DriveApp.getFolderById()` and trashing a temporary Sheet -> broad Drive scope;
- calling `UrlFetchApp.fetch(generate_204)` -> `script.external_request`;
- calling `ScriptApp.getProjectTriggers()` -> `script.scriptapp`.

Those operations were only authorization probes, not current business requirements. Authorizing them would violate least privilege.

The corrected bootstrap opens the designated projection workbook and checks owner identity. The current manifest is therefore reduced to:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

`spreadsheets.currentonly` is not suitable because this is a standalone script that must target an explicitly designated workbook rather than only a bound/current spreadsheet.

Future scope additions are feature-gated:

- Drive scope: only after current VHDCHY implements an approved Drive file-management/archive/media flow.
- `script.external_request`: only after GAS itself must call an external endpoint.
- `script.scriptapp`: only after installable trigger create/list/delete is implemented.
- `script.send_mail`/Gmail: only after an approved mail feature exists. Current gateway has none.

## 6. Google Auth Platform / refresh-token lifetime

For the CI client, `automation@supra.cc.cd` is a normal Google Account, not an Internal Workspace-org user. Use External audience.

Testing is suitable only during setup. A non-basic authorization created while an External app remains in Testing has a limited refresh-token lifetime. Before generating the durable CI refresh token, move the intended app to its production publishing state (`In production`).

`In production` removes the Testing-specific seven-day lifetime; it does **not** make a token immortal. Refresh tokens can still be revoked or invalidated by security/policy/usage conditions.

Owning `supra.cc.cd` does not itself remove the seven-day Testing rule. Domain ownership is relevant for authorized-domain/branding/verification requirements.

## 7. Cloudflare — exact current token result

Current deploy script:

- lists the existing D1 database;
- applies remote D1 migrations;
- deploys a Worker;
- attaches the Worker as a Custom Domain through Wrangler.

Minimum token:

```text
Account -> Workers Scripts -> Write
Account -> D1 -> Write
```

No DNS Write is required for the current `custom_domain: true` Worker model; Cloudflare manages the corresponding DNS record/certificate for a Custom Domain. No Workers Routes permission is needed because the config is not using a zone route.

The standard Cloudflare `Edit Cloudflare Workers` token template grants additional KV/R2/Tail/route/account permissions. Do not use that template unchanged for this project; create a custom least-privilege token.

Recovery hardening: a missing expected D1 now fails closed. The deployment script must not silently create a replacement database during provider-account recovery.

## 8. GitHub — exact current permission result

Keep repository default Workflow permissions restricted/read-only. The current workflows already elevate only where required:

- deploy BETA/STABLE: `contents: read`;
- LAN Pilot build/release: `contents: write` to create GitHub Release assets.

Do not enable repository-wide default read/write solely for the release workflow. Do not enable `Allow GitHub Actions to create and approve pull requests`; no current workflow requires it.

Use environments:

- `beta`: branch restriction `beta` where supported;
- `stable`: branch restriction `stable` + Owner required review when supported.

## 9. GitHub values after audit

### BETA variables currently consumed

```text
APP_ENV=beta
OWNER_EMAIL=automation@supra.cc.cd
CF_ACCOUNT_ID=<verify current Cloudflare dashboard>
PUBLIC_HOST=beta.supra.cc.cd
GAS_SCRIPT_ID=<new BETA GAS>
GAS_DEPLOYMENT_ID=<new BETA deployment>
GAS_EXEC_URL=<new BETA /exec>
GOOGLE_SHEETS_PROJECTION_ID=17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ
ANDROID_SIGNING_ALIAS=vhdchy-beta   # verify against VHDCHY keystore
```

Not required merely for current CI:

```text
CF_ZONE_ID
CF_ZONE_NAME
LAN_HOST
GOOGLE_DRIVE_ENV_ROOT_ID
ANDROID_SIGNING_SHA256
```

### BETA secrets currently consumed

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Do not enter STABLE signing secrets during BETA recovery because no current STABLE Android build workflow consumes them.

## 10. Legacy Pick Pack permissions explicitly rejected for carry-forward

Do not carry the following forward from the retired project unless a new Owner-approved VHDCHY feature is implemented and re-audited:

- `script.send_mail` / MailApp password-reset or OTP mail;
- GAS full Drive access for OTA/log/fallback files;
- installable trigger management;
- GAS external fetch;
- Worker direct Google Sheets/Drive OAuth credentials;
- Google service account / Firebase Messaging;
- R2/KV/Turso/Render/Deno/provider DR credentials;
- old project spreadsheet IDs, GAS IDs, endpoints, client IDs, refresh tokens or legacy signing key.

## 11. Additional findings that must not be missed

1. `config/projections.beta.json` still referenced the decommissioned-account workbook. It is corrected on this audit branch to the new workbook ID and marked `PROVISIONED_NOT_LIVE`.
2. Current Cloudflare deployment code could silently create D1 if lookup returned no database. That is unsafe during account recovery and is corrected to fail closed.
3. Before business `doPost()` is activated, Worker -> GAS privileged projection calls need an application-level authenticated boundary (for example a purpose-built shared-secret/HMAC scheme stored as provider secrets). Public Web App access must not become the authorization control for writes. This future application secret does not require broader Google OAuth scopes.
4. The retired Pick Pack signing keystore exists in the reference archive. It is **not** the VHDCHY signer and must not be copied into the new VHDCHY GitHub environments.

## 12. Approval rule for future permission changes

Any new permission must satisfy all three:

1. a current Owner-approved feature exists;
2. current source actually invokes the capability requiring that permission;
3. the narrowest provider scope/resource boundary has been identified and documented.

If any condition is false, the permission is not granted.
