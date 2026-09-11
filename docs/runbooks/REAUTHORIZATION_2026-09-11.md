# VHDCHY — PROVIDER-FIRST REAUTHORIZATION / RECOVERY RUNBOOK V3

Date: 2026-09-11
Status: OWNER-ACTION + AI-VERIFY
Authority: `PROJECT_SCOPE.md` + `SERVICE_AUTHORITY.md` + `CURRENT_STATE.md`
Permission audit: `docs/security/PERMISSION_AUDIT_2026-09-11.md`

## 0. Dependency order

Do not begin with GitHub values. Provider outputs come first.

```text
Google BETA ───────────┐
Cloudflare token ──────┼─> verified IDs/secrets -> GitHub beta -> AI/CI verify -> BETA PASS
VHDCHY BETA signer ────┘                                           |
                                                                    v
                                                        Owner approves STABLE
                                                                    |
                                                                    v
                                                     build STABLE providers/GitHub
```

Cloudflare/domain runtime is retained. Google Cloud/OAuth/GAS is rebuilt. The retired Pick Pack 1291 archive is reference only.

## 1. Direct setup links

### Google

- Create Cloud project: https://console.cloud.google.com/projectcreate
- Google Auth Platform: https://console.cloud.google.com/auth/overview
- Apps Script API library: https://console.cloud.google.com/apis/library/script.googleapis.com
- Apps Script account API-access setting: https://script.google.com/home/usersettings
- Apps Script home: https://script.google.com/home
- OAuth Playground: https://developers.google.com/oauthplayground/
- Search Console, only if Google requires domain verification: https://search.google.com/search-console

### Cloudflare

- Dashboard: https://dash.cloudflare.com/
- API tokens: https://dash.cloudflare.com/profile/api-tokens
- Permission reference: https://developers.cloudflare.com/fundamentals/api/reference/permissions/

### GitHub — use after provider values exist

- Environments: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments
- Actions settings: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions

## 2. Phase A — secure current Google identity

Current runtime owner: `automation@supra.cc.cd`.

Before automation credentials:

- independent recovery email;
- recovery phone where appropriate;
- 2-Step Verification/passkey;
- backup codes stored outside the automated Drive account;
- do not add Gmail solely for VHDCHY;
- do not use this identity for bulk email/mass unrelated registrations.

Never use `vanhanhdchungyen@gmail.com` again.

## 3. Phase B — create Google BETA Cloud/OAuth

### B1. Create standard Cloud project

Open https://console.cloud.google.com/projectcreate while logged in as `automation@supra.cc.cd`.

Create:

```text
VHDCHY-BETA
```

Record Project ID and Project number. Project number is used when linking the Apps Script project.

### B2. Enable only the API current CI needs

Enable **Google Apps Script API**:

https://console.cloud.google.com/apis/library/script.googleapis.com

Do **not** enable Drive API, Sheets API, Gmail, Calendar, Contacts, People or Firebase merely for this recovery. Current CI does not call those REST APIs. Apps Script built-in `SpreadsheetApp` does not require enabling the Sheets REST API in this Cloud project.

### B3. Enable Apps Script API access at account level

Open:

https://script.google.com/home/usersettings

Enable the setting allowing **Google Apps Script API** access to script projects. Google keeps this access off by default as a security measure.

### B4. Configure Google Auth Platform

Open:

https://console.cloud.google.com/auth/overview

Use:

```text
App name: VHDCHY BETA Automation
User support email: automation@supra.cc.cd
Audience: External
Developer/contact email: automation@supra.cc.cd
```

`automation@supra.cc.cd` is a normal Google Account, not an Internal Google Workspace organization identity, therefore do not select Internal.

### B5. CI OAuth scopes — exact minimum

Current CI deployment calls Apps Script project/version/deployment endpoints only.

Authorize exactly:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

Do not add Drive, Sheets, Gmail, Calendar, Contacts, `script.send_mail`, `script.scriptapp` or `script.external_request` to the CI OAuth client.

### B6. Publishing state and the 7-day Testing issue

During initial setup, Testing is acceptable. Before producing the **final** CI refresh token, move the External OAuth app to its intended `In production` publishing state.

Reason: refresh tokens for non-basic scopes issued while an External app remains in Testing have a limited lifetime. `In production` removes that Testing-specific seven-day behavior, but refresh tokens remain revocable and are not immortal.

Domain ownership does not itself remove the Testing lifetime rule.

Official references:

- https://developers.google.com/identity/protocols/oauth2
- https://support.google.com/cloud/answer/15544987
- https://support.google.com/cloud/answer/13464323

### B7. Create BETA CI OAuth client

Google Auth Platform -> Clients -> Create OAuth client.

Use:

```text
Application type: Web application
Name: VHDCHY BETA GitHub CI
Authorized redirect URI: https://developers.google.com/oauthplayground
```

Keep Client ID and Client secret private. Do not enter GitHub yet.

### B8. Generate one durable BETA refresh token

Open https://developers.google.com/oauthplayground/.

1. Gear/settings -> `Use your own OAuth credentials`.
2. Enter BETA Client ID and Client secret locally in Playground.
3. Authorize exactly the two scopes in B5.
4. Authorize as `automation@supra.cc.cd`.
5. Exchange code for tokens.
6. Keep the refresh token private.

Do not mint a new refresh token every CI run.

## 4. Phase C — create GAS BETA with reduced runtime scopes

### C1. Create standalone project

Open https://script.google.com/home and create:

```text
VHDCHY BETA Google Gateway
```

### C2. Link the standard Cloud project

Apps Script -> Project Settings -> Google Cloud Project -> Change project.

Use the Project number from B1.

### C3. Use current authority source only

Use:

- `gateway/Code.gs`
- `gateway/appsscript.json`

Do not copy historical GAS IDs, deployment IDs or manifests from old accounts/projects.

### C4. Current GAS runtime scopes — exact minimum

The deep audit removed artificial bootstrap-only privileges. Current manifest is:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

Why:

- `spreadsheets`: open/update the explicitly designated projection workbook;
- `userinfo.email`: reject accidental authorization from the wrong Google account.

Not authorized now:

```text
Drive
script.external_request
script.scriptapp
script.send_mail
Gmail
Calendar
Contacts
```

The retired Pick Pack project used several of these because it had MailApp, Drive artifacts, triggers and external bridges. Current VHDCHY does not.

### C5. BETA projection workbook

Current BETA projection workbook:

```text
VHDCHY BETA - PICK PACK 1291 - 2026 Q3
ID: 17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ
State: PROVISIONED_NOT_LIVE
```

The corrected bootstrap opens this workbook instead of requesting full Drive permission or creating/trashing temporary spreadsheets.

### C6. Manual authorization

Run `bootstrapAuthorize()` once as `automation@supra.cc.cd`.

Consent must correspond to Sheets access + email identity. If Drive, Gmail/mail, external-request, trigger-management, Calendar or Contacts access appears, stop and re-audit instead of approving.

### C7. Web App deployment

Apps Script -> Deploy -> New deployment -> Web app.

Use current foundation design:

- Execute as: user deploying (`automation@supra.cc.cd`).
- Keep the current foundation Web App access model; `doPost()` is still fail-closed with `FOUNDATION_ONLY` 503.

Record:

- Script ID;
- Deployment ID;
- `/exec` URL.

Before actual business projection writes are enabled later, Worker -> GAS privileged calls need an application-level authenticated boundary. Anonymous Web App reachability must not become authorization for writes.

## 5. Phase D — Cloudflare retained runtime

Do not rebuild Worker/D1/DNS/domain.

### D1. Verify current account

Open https://dash.cloudflare.com/ and read the account ID that owns the retained VHDCHY resources.

### D2. Create/rotate a least-privilege token if necessary

Open https://dash.cloudflare.com/profile/api-tokens.

Create a **custom token**, scoped to the current account, with only:

```text
Account -> Workers Scripts -> Write
Account -> D1 -> Write
```

Do not use the stock `Edit Cloudflare Workers` template unchanged because it grants additional permissions such as routes/KV/R2/Tail/account reads.

Current Worker config uses Custom Domain (`custom_domain: true`). Cloudflare handles its DNS/certificate; current deploy does not need DNS Write or Workers Routes Write.

Recovery hardening: if expected D1 (`vhdchy-data-beta` or later STABLE equivalent) is missing, deployment now fails rather than silently creating a replacement database.

## 6. Phase E — recover VHDCHY BETA Android signer

Use the retained **VHDCHY** BETA keystore, not the retired Pick Pack 1291 keystore from the reference archive.

Current BETA workflow consumes:

```text
ANDROID_SIGNING_ALIAS
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Recorded VHDCHY BETA alias is `vhdchy-beta`; verify against the retained keystore before entry.

`ANDROID_SIGNING_SHA256` is not consumed by the current workflow and therefore does not need to be created as a GitHub variable merely for completeness.

Do not put keystore/password material in chat or repo files.

Cloudflare token work and signing recovery can be done in parallel with Google setup.

## 7. Phase F — populate GitHub BETA only now

### F1. Repository Actions permission

Open https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions.

Keep repository default `GITHUB_TOKEN` permissions at **Read repository contents and packages permissions**.

Do not globally switch to repository-wide Read and write. Current workflows declare minimum permissions themselves:

- deploy BETA/STABLE: `contents: read`;
- LAN Pilot release: `contents: write` to publish release assets.

Do not enable `Allow GitHub Actions to create and approve pull requests`.

### F2. Create environment `beta`

Open https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments.

Create `beta`. If available, restrict deployment branches/tags so only branch `beta` can deploy to this environment.

### F3. BETA variables

Enter only current consumed variables:

```text
APP_ENV=beta
OWNER_EMAIL=automation@supra.cc.cd
CF_ACCOUNT_ID=<verified current account ID>
PUBLIC_HOST=beta.supra.cc.cd
GAS_SCRIPT_ID=<new BETA GAS Script ID>
GAS_DEPLOYMENT_ID=<new BETA deployment ID>
GAS_EXEC_URL=<new BETA /exec URL>
GOOGLE_SHEETS_PROJECTION_ID=17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ
ANDROID_SIGNING_ALIAS=<verified VHDCHY BETA alias>
```

Do not create unconsumed variables just because old runbooks had them:

```text
CF_ZONE_ID
CF_ZONE_NAME
LAN_HOST
GOOGLE_DRIVE_ENV_ROOT_ID
ANDROID_SIGNING_SHA256
```

### F4. BETA secrets

Deploy/GAS:

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
```

Android build/release:

```text
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Secrets go only into GitHub Environment secrets/provider secret stores.

## 8. Phase G — AI/CI BETA verification

After Owner finishes F:

1. verify OAuth refresh with the two approved scopes;
2. verify Apps Script content/deployment and `/exec` identity;
3. run `bootstrapAuthorize()` result check against the new projection workbook;
4. verify Cloudflare token sees the retained D1 and Worker deployment succeeds without recreation;
5. verify VHDCHY BETA signing material/build;
6. run BETA deploy and health;
7. only after all PASS may the BETA restoration gate be considered complete.

`config/projections.beta.json` must use the new workbook ID and remain marked `PROVISIONED_NOT_LIVE` until this gate passes.

## 9. STABLE

Do not pre-fill STABLE just because GitHub exposes the fields.

After BETA PASS and explicit Owner approval:

- create separate `VHDCHY-STABLE` Google Cloud/OAuth/GAS resources;
- provision a separate STABLE projection workbook;
- create separate STABLE OAuth client/refresh token with the same two CI scopes;
- create environment `stable`, restrict it to branch `stable`, and enable Owner required review where supported;
- add only STABLE variables/secrets consumed by current STABLE workflows;
- do not add STABLE Android signing secrets until a current STABLE Android build workflow actually needs them.

Never copy BETA Google credentials/IDs into STABLE.

## 10. Legacy Pick Pack 1291 rule

The full retired project used broader permissions for real historical features: MailApp password/OTP mail, Drive file/OTA/log storage, triggers, UrlFetch bridges, direct Worker Google Sheets OAuth, Drive REST, FCM service account and DR providers.

Those capabilities are **not** current VHDCHY requirements. Do not carry their permissions/credentials forward unless a current Owner-approved VHDCHY feature is implemented and re-audited.

Detailed evidence: `docs/security/PERMISSION_AUDIT_2026-09-11.md`.
