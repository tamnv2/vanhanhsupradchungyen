# VHDCHY — PROVIDER-FIRST REAUTHORIZATION / RECOVERY RUNBOOK

Date: 2026-09-11
Status: OWNER-ACTION + AI-VERIFY
Authority: `PROJECT_SCOPE.md` + `SERVICE_AUTHORITY.md` + `CURRENT_STATE.md`

## 0. Critical correction — GitHub is NOT step 1

Do not start by filling GitHub variables/secrets. Most of those values are outputs of Google/Cloudflare/signing setup.

Correct dependency order:

```text
Retained provider state / backups
        │
        ├── Google BETA setup ─────────────┐
        ├── Cloudflare token verification ├── can run in parallel
        └── Android signing recovery ──────┘
                         │
                         v
             Collect verified IDs/secrets
                         │
                         v
               GitHub beta Environment
                         │
                         v
                  AI/CI BETA verify
                         │
                  BETA PASS required
                         │
                         v
             STABLE provider setup + Owner approval
                         │
                         v
              GitHub stable Environment
                         │
                         v
                 AI/CI STABLE verify
```

## 1. Current service decision matrix

| Service | Decision | Current rule |
|---|---|---|
| Domain `supra.cc.cd` | RETAIN | Do not recreate |
| Cloudflare zone/DNS/Worker/D1 | RETAIN | Only re-verify and create/rotate deploy token if needed |
| Google Account | CURRENT NEW OWNER | `automation@supra.cc.cd` only |
| Google Drive runtime | REBUILT | Use current IDs in `SERVICE_AUTHORITY.md` |
| Google Cloud/OAuth | REBUILD | BETA first; STABLE after BETA PASS |
| GAS | REBUILD | BETA first; STABLE after BETA PASS |
| GitHub repo | CURRENT AUTHORITY | `tamnv2/vanhanhsupradchungyen` |
| GitHub Environments/secrets | REBUILD LAST | Fill only after provider values exist |
| Android signing | RETAIN | Reuse original keystore identity; no key rotation unless explicitly approved |
| Old Pick Pack backup | REFERENCE ONLY | `BACKUP DỰ ÁN CŨ PICK PACK 1291`; never authority/runtime |

## 2. Official setup links

### Google

- Google Cloud create project: https://console.cloud.google.com/projectcreate
- Google Cloud console: https://console.cloud.google.com/
- Google Auth Platform overview: https://console.cloud.google.com/auth/overview
- Enable Google Apps Script API: https://console.cloud.google.com/apis/library/script.googleapis.com
- Enable Google Drive API: https://console.cloud.google.com/apis/library/drive.googleapis.com
- Apps Script dashboard user settings: https://script.google.com/home/usersettings
- Apps Script home: https://script.google.com/home
- OAuth 2.0 Playground: https://developers.google.com/oauthplayground/
- Search Console/domain verification: https://search.google.com/search-console

### Cloudflare

- Dashboard: https://dash.cloudflare.com/
- API token documentation: https://developers.cloudflare.com/fundamentals/api/get-started/create-token/
- GitHub Actions/Workers CI documentation: https://developers.cloudflare.com/workers/ci-cd/external-cicd/github-actions/

### GitHub — use only after provider setup

- Environments: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments
- Actions settings: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions

## 3. Phase A — Google account safety prerequisite

Current Google runtime owner: `automation@supra.cc.cd`.

Before creating automation credentials:

- confirm independent recovery email;
- configure recovery phone where appropriate;
- enable 2-Step Verification/passkey;
- store backup codes outside this Google Drive account;
- do not add Gmail solely for the project;
- do not use this identity for unrelated bulk-email or mass account-registration activity.

The account has no Gmail mailbox; that does not block Drive/Sheets/Apps Script/OAuth.

## 4. Phase B — Google BETA foundation

Do BETA completely before STABLE.

### B1. Create the standard Cloud project

Open: https://console.cloud.google.com/projectcreate

Sign in as `automation@supra.cc.cd`.

Create:

```text
Project name: VHDCHY-BETA
```

If the account is not part of a Google Workspace/Cloud Identity organization, leave organization/location at the available personal/default choice; do not invent an organization.

After creation, record locally:

- Project name;
- Project ID;
- Project number.

The **Project number** will later be used to associate the Apps Script project with this standard Cloud project.

### B2. Enable required APIs in VHDCHY-BETA

Select project `VHDCHY-BETA`, then open:

- Apps Script API: https://console.cloud.google.com/apis/library/script.googleapis.com
- Drive API: https://console.cloud.google.com/apis/library/drive.googleapis.com

Enable both.

Reason:

- CI uses the Apps Script API to update project content/versions/deployments.
- The GAS runtime uses the built-in Drive service and a standard Cloud project; Google documents enabling Drive API when using built-in Drive service after switching to a standard project.

Do **not** enable Gmail, Calendar, Contacts, People, or unrelated APIs for this recovery.

### B3. Enable Apps Script API access at the Google-account level

This is separate from enabling the API in Cloud Console and was missing from the earlier guide.

Open: https://script.google.com/home/usersettings

Under Apps Script user settings, enable the setting that allows the **Google Apps Script API** to access/manage your script projects.

Why this matters: Google disables Apps Script API management of script content/deployments by default. Even with a valid OAuth token, CI can fail unless this account-level access is explicitly enabled.

Official explanation: https://developers.google.com/apps-script/api/how-tos/enable

### B4. Configure Google Auth Platform

Open while `VHDCHY-BETA` is selected:

https://console.cloud.google.com/auth/overview

Because `automation@supra.cc.cd` is a normal Google Account using an external domain email, not an account inside a Google Workspace organization owned by this project, use **External** audience.

Configure the pages Google currently exposes as Branding / Audience / Data Access / Clients.

Suggested truthful values:

```text
App name: VHDCHY BETA Automation
User support email: automation@supra.cc.cd
Developer/contact email: automation@supra.cc.cd
Audience: External
```

Do not add logos/homepage/privacy-policy URLs just to make the page look complete unless Google requires them for the action you are taking and the URLs actually exist.

### B5. Configure only the CI OAuth scopes needed by current code

The current `scripts/deploy-gas.sh` calls only the Apps Script API for:

- project content update;
- version creation;
- deployment update.

The final CI refresh token needs only:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

Do **not** add `drive.metadata.readonly` to the CI token now. The restored deploy script does not call the Drive REST API, and `drive.metadata.readonly` is a restricted Drive scope. Removing it reduces unnecessary OAuth exposure.

Also do not add Gmail/Calendar/Contacts/`script.send_mail`/full Drive/Sheets scopes to the **CI OAuth client**.

Runtime Apps Script scopes are separate and are covered later.

### B6. Move OAuth publishing status to In production BEFORE generating the final refresh token

This step is specifically for the GitHub CI refresh token.

In Google Auth Platform -> Audience, use `Publish app` / set publishing status to **In production**.

Why: Google states that for an External OAuth project in `Testing`, refresh tokens expire after 7 days unless the request is limited to basic identity scopes. Our CI requests Apps Script scopes, so a Testing token is not suitable for durable CI.

Official token rule: https://developers.google.com/identity/protocols/oauth2

Important nuance:

- `In production` removes the special 7-day Testing lifetime.
- It does **not** mean a refresh token is mathematically permanent. Tokens can still be revoked, invalidated by security/policy changes, or expire after extended non-use.
- Domain ownership itself does not remove the 7-day Testing rule.

For this owner-only / very small personal-use automation, Google documents verification exceptions for personal use. An unverified warning/user cap can still appear if sensitive/restricted scopes are involved. Do not attempt to bypass provider warnings; follow the current Google UI if it requires an additional verification step.

References:

- Audience/publishing status: https://support.google.com/cloud/answer/15549945
- Personal-use verification exception: https://support.google.com/cloud/answer/13464323

### B7. Create the BETA OAuth client

In Google Auth Platform -> Clients:

1. Create OAuth client.
2. Application type: `Web application`.
3. Name: `VHDCHY BETA GitHub CI`.
4. Authorized redirect URI: `https://developers.google.com/oauthplayground`.
5. Create.

Record privately:

- Client ID;
- Client secret.

Do not put them in GitHub yet. Keep them ready until all BETA provider outputs have been collected.

### B8. Generate the final BETA refresh token

Only do this after B6 shows the intended `In production` publishing state.

Open: https://developers.google.com/oauthplayground/

1. Open the gear/settings panel.
2. Enable `Use your own OAuth credentials`.
3. Enter the BETA Client ID and Client Secret in the Playground UI.
4. In the scope field, authorize exactly:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

5. Authorize using `automation@supra.cc.cd`.
6. Review the consent screen; do not approve Gmail/Calendar/Contacts/Drive scopes in this CI authorization.
7. Exchange authorization code for tokens.
8. Record the returned refresh token privately.

If a refresh token was previously generated while the project was still in Testing, do not treat that old token as the final CI token; generate the final token after the intended production publishing state is set.

Google documents a limit of 100 refresh tokens per Google Account per OAuth client; repeatedly minting new ones can invalidate older tokens. Therefore create/reuse one intended BETA CI refresh token rather than generating one on every deployment.

## 5. Phase C — Create and authorize GAS BETA

### C1. Create the new Apps Script project

Open: https://script.google.com/home

Create a standalone script project named:

```text
VHDCHY BETA Google Gateway
```

### C2. Associate GAS with the standard VHDCHY-BETA Cloud project

In the Apps Script editor:

`Project Settings -> Google Cloud Project -> Change project`

Enter the **Project number** recorded in B1 and set the project.

Google recommends choosing the standard Cloud project early because switching later can force re-authorization.

Official reference: https://developers.google.com/apps-script/guides/cloud-platform-projects

### C3. Put the current authority source into GAS

Use current repo source only:

- `gateway/Code.gs`
- `gateway/appsscript.json`

Do not copy old Script ID/deployment ID/OAuth credentials from the decommissioned Google account.

Current audited runtime manifest scopes are:

```text
https://www.googleapis.com/auth/drive
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/script.external_request
https://www.googleapis.com/auth/script.scriptapp
https://www.googleapis.com/auth/userinfo.email
```

`script.send_mail` is intentionally absent because current gateway source does not send mail.

These are **GAS runtime scopes**, not the two-scope GitHub CI OAuth token.

### C4. Run bootstrapAuthorize() once manually

Run `bootstrapAuthorize()` as `automation@supra.cc.cd`.

Expected behavior:

- account authorization prompt appears;
- owner identity check passes;
- BETA Drive root can be resolved;
- temporary test Spreadsheet is created and trashed;
- URL fetch probe runs;
- script properties are written.

Review the consent screen carefully. If Gmail/send-mail/Calendar/Contacts permissions appear, stop and audit the manifest/source instead of approving them.

### C5. Create the BETA Web App deployment

In Apps Script:

`Deploy -> New deployment -> Web app`

Use the current approved architecture:

- Execute as: `User deploying` / `automation@supra.cc.cd`.
- Access setting must match the current approved web-app design. Do not change the security model merely to make deployment easier.

After deployment, record privately/non-secret locally:

- **Script ID**: `Project Settings -> Script ID`.
- **Deployment ID**: `Deploy -> Manage deployments`.
- **Web app /exec URL**.

These three outputs are what GitHub needs later.

## 6. Phase D — Cloudflare retained setup: verify and create token only

Cloudflare zone/DNS/Worker/D1 are retained; do not recreate them because Google/GitHub identities changed.

### D1. Read current IDs from the current dashboard

Open: https://dash.cloudflare.com/

Read the **current account ID** from the account that now owns the retained VHDCHY resources. Do not blindly reuse a historical ID if the dashboard does not match it.

The restored workflow currently needs `CF_ACCOUNT_ID` for deployment.

`CF_ZONE_ID` is present in the workflow environment block but is not consumed by the current deploy scripts. If you decide to populate it, read it from the current `supra.cc.cd` zone rather than using an unverified old value.

### D2. Create/rotate a scoped CI token if needed

If the previous token value is unavailable or its scope is uncertain, create a new token.

Cloudflare docs: https://developers.cloudflare.com/fundamentals/api/get-started/create-token/

For the current deployment path, start with only permissions needed for retained Worker + D1 deployment. Current API calls require at least:

- Account: Workers Scripts — Edit/Write;
- Account: D1 — Edit/Write.

Wrangler/custom-domain operations may also require route/account lookup permissions depending on the current provider behavior. If needed, add only the specific documented permission required by the observed error rather than granting Global API Key/full account access.

Cloudflare's official Workers CI guidance starts from the `Edit Cloudflare Workers` token template; if using the template, restrict resources to the intended account/zone and remove unrelated permissions where the dashboard allows it without breaking the current Wrangler path.

Record the token privately. Do not put it in GitHub until Phase F.

## 7. Phase E — Android BETA signing recovery

Keep the existing BETA signing identity. Do not generate a new keystore.

Locate the Owner-controlled BETA keystore backup.

Verify alias/fingerprint locally before GitHub entry. Recorded alias is:

```text
vhdchy-beta
```

If using Java `keytool`, inspect the keystore rather than trusting filename alone.

Prepare privately:

- `ANDROID_SIGNING_ALIAS`;
- keystore base64 value for `ANDROID_SIGNING_KEY_B64`;
- store password;
- key password.

Do not put keystore bytes/passwords into chat or repository files.

The Cloudflare token work and Android signing recovery can run in parallel with Google BETA setup because they do not depend on each other.

## 8. Phase F — NOW fill GitHub BETA

Only after Phases B/C/D/E have produced verified values should you open GitHub.

### F1. Repository Actions permission

Open: https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions

Under `Workflow permissions`, select `Read and write permissions` because the LAN build workflow publishes prerelease assets using `GITHUB_TOKEN`.

### F2. Create Environment `beta`

Open: https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments

Create/configure `beta`.

### F3. BETA variables required by current workflows

| GitHub variable | Enter this value/source |
|---|---|
| `APP_ENV` | `beta` |
| `OWNER_EMAIL` | `automation@supra.cc.cd` |
| `CF_ACCOUNT_ID` | verified current Cloudflare account ID from D1 |
| `PUBLIC_HOST` | `beta.supra.cc.cd` |
| `GAS_SCRIPT_ID` | new BETA value from C5 |
| `GAS_DEPLOYMENT_ID` | new BETA value from C5 |
| `GAS_EXEC_URL` | new BETA `/exec` URL from C5 |
| `GOOGLE_DRIVE_ENV_ROOT_ID` | `1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog` |
| `ANDROID_SIGNING_ALIAS` | verified retained BETA alias (`vhdchy-beta` if keystore check matches) |

`CF_ZONE_ID` may be left absent unless a current workflow change starts consuming it; if populated, use the verified current zone ID.

Do not add obsolete/unconsumed variables merely because an older runbook listed them.

### F4. BETA secrets required by current workflows

Enter directly in GitHub Environment secrets:

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Source mapping:

- `CLOUDFLARE_API_TOKEN` -> D2.
- `GOOGLE_OAUTH_CLIENT_ID` -> B7.
- `GOOGLE_OAUTH_CLIENT_SECRET` -> B7.
- `GOOGLE_OAUTH_REFRESH_TOKEN` -> B8.
- Android secrets -> E.

Do not send these values to AI. After entry, AI only needs to test whether workflows can use them.

## 9. Phase G — AI/CI BETA verification gate

After Owner says GitHub BETA is fully populated, AI should:

1. verify current GitHub workflow/config state;
2. rebuild the environment verification workflow using current IDs only;
3. test OAuth token exchange without printing tokens;
4. verify Apps Script API can read/update the intended BETA script/deployment;
5. verify GAS `/exec` returns the expected BETA identity;
6. verify current BETA Drive root through authorized GAS behavior/current provider state;
7. verify Cloudflare token resolves/deploys the retained BETA resources rather than creating replacements unexpectedly;
8. verify Android signer by CI build/signature checks;
9. run BETA deployment/health;
10. checkpoint run IDs, commits, PASS/FAIL and exact blocker.

Do not move the `beta` live pointer merely because secrets were entered. Move only after the BETA gate passes according to the project release contract.

## 10. STABLE — only after BETA PASS + Owner approval

Do not pre-populate STABLE with BETA credentials.

After BETA PASS and explicit Owner approval, repeat the provider-first process for STABLE:

- separate standard Cloud/GAS project;
- separate OAuth client + refresh token;
- new STABLE GAS Script ID/Deployment ID/Exec URL;
- current STABLE Drive root: `1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE`;
- retained STABLE signing identity;
- verified retained Cloudflare state;
- GitHub `stable` Environment filled only after those values exist.

Then run STABLE verification/deployment behind the Owner gate.

## 11. Account-lock / anti-abuse controls

No configuration guarantees Google will never flag an account. The recovery design minimizes unnecessary risk:

- dedicated `automation@supra.cc.cd` identity;
- no Gmail sending workload;
- current manifest has no `script.send_mail`;
- CI token has only `script.projects` + `script.deployments`;
- one stable refresh token per environment; no token churn;
- no repeated authorize/revoke loops;
- no unnecessary throwaway projects/accounts;
- 2FA/recovery enabled;
- quotas respected;
- secrets never logged/committed;
- BETA used to validate before STABLE.

Google does not publish a magic anti-bot threshold. Staying under quota is necessary but not a guarantee against abuse/security systems.

## 12. Important Google OAuth facts used by this runbook

- External OAuth project in Testing: refresh token expires after 7 days when non-basic scopes are used.
- Publishing status `In production`: removes that special Testing lifetime, but tokens remain revocable.
- Google documents personal-use/few-known-users exceptions where OAuth verification is not mandatory; warning/user cap may remain.
- `drive.metadata.readonly` is a restricted Drive scope, so it is intentionally removed from current CI authorization because the current deploy script does not need it.
- Apps Script API access has two separate gates: API enabled in the Cloud project **and** explicit account-level permission in Apps Script dashboard for applications to manage scripts/deployments.

Official references:

- OAuth refresh tokens: https://developers.google.com/identity/protocols/oauth2
- OAuth publishing status: https://support.google.com/cloud/answer/15549945
- Personal-use verification exception: https://support.google.com/cloud/answer/13464323
- Apps Script API access gate: https://developers.google.com/apps-script/api/how-tos/enable
- Apps Script standard Cloud projects: https://developers.google.com/apps-script/guides/cloud-platform-projects
- Apps Script updateContent scope: https://developers.google.com/apps-script/api/reference/rest/v1/projects/updateContent
- Apps Script versions.create scope: https://developers.google.com/apps-script/api/reference/rest/v1/projects.versions/create
- Apps Script deployments scope: https://developers.google.com/apps-script/api/reference/rest/v1/projects.deployments/create
- Drive scope classification: https://developers.google.com/workspace/drive/api/guides/api-specific-auth
- Cloudflare token creation: https://developers.cloudflare.com/fundamentals/api/get-started/create-token/
- GitHub Environments: https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments

## 13. New-chat continuity

Every new AI session bootstraps from:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Then read only task-specific detail. Chat/model memory never overrides authority files. Around the 20-minute execution soft-stop, checkpoint before stopping so the next session can continue without reconstructing hidden model memory.
