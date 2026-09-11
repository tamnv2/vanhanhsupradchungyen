# VHDCHY — REAUTHORIZATION / PROVIDER RECOVERY RUNBOOK

Date: 2026-09-11
Status: OWNER-ACTION + AI-VERIFY
Authority: `PROJECT_SCOPE.md` + `SERVICE_AUTHORITY.md` + `CURRENT_STATE.md`

This runbook rebuilds only resources that were lost with the decommissioned Google account. Provider setups that still exist after an email/login transfer are retained and re-verified rather than recreated.

## 1. Service decision matrix

| Service | Decision | Current rule |
|---|---|---|
| Domain `supra.cc.cd` | RETAIN | Do not recreate |
| Cloudflare zone/DNS/Worker/D1 | RETAIN | Re-enter/recreate deploy token only if needed; verify existing resources |
| Google Account | NEW | `automation@supra.cc.cd` only |
| Google Drive runtime | REBUILT | Use current IDs in `SERVICE_AUTHORITY.md` |
| Google Cloud/OAuth | REBUILD | Separate BETA/STABLE projects/clients |
| GAS | REBUILD | Separate BETA/STABLE scripts/deployments |
| GitHub account/repo | NEW AUTHORITY | `tamnv2/vanhanhsupradchungyen` |
| GitHub Environments/secrets | REBUILD | `beta`, `stable`; secrets cannot be read back from old repo |
| Android signing keys | RETAIN | Restore same keystore values from Owner backup; no new signer |
| Old Pick Pack backup | REFERENCE ONLY | `BACKUP DỰ ÁN CŨ PICK PACK 1291`; never runtime/authority |

## 2. GitHub — Owner UI steps

Repository: `tamnv2/vanhanhsupradchungyen`.

### 2.1 Actions

Go to `Settings -> Actions -> General`.

- Ensure GitHub Actions are enabled for the repository.
- Under Workflow permissions choose `Read and write permissions` so build/release jobs can publish prerelease assets.
- Do not create broad PATs merely to let AI read secrets. Secrets remain write-only provider inputs.

### 2.2 Environments

Go to `Settings -> Environments` and create:

- `beta`
- `stable`

For `stable`, configure Owner approval/required reviewer when available. Do not deploy STABLE until BETA recovery passes.

### 2.3 Environment variables

Use `docs/GITHUB_ENV_SETUP.md` as the exact current value authority. Important changes:

- `OWNER_EMAIL=automation@supra.cc.cd`.
- BETA Drive root = `1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog`.
- STABLE Drive root = `1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE`.
- Old Google Drive/GAS IDs must not be copied.
- GAS Script/Deployment/Exec values remain blank/placeholders until each new GAS environment is created.

### 2.4 Environment secrets

Each environment requires:

- `CLOUDFLARE_API_TOKEN`
- `GOOGLE_OAUTH_CLIENT_ID`
- `GOOGLE_OAUTH_CLIENT_SECRET`
- `GOOGLE_OAUTH_REFRESH_TOKEN`
- `ANDROID_SIGNING_KEY_B64`
- `ANDROID_SIGNING_STORE_PASSWORD`
- `ANDROID_SIGNING_KEY_PASSWORD`

Enter these directly in GitHub UI. Never paste them into chat, issue, commit, Drive doc or repository variable.

## 3. Cloudflare — retain, do not rebuild

Owner confirmed the Cloudflare/domain setup survived the email/account transfer.

- Keep existing zone `supra.cc.cd`.
- Keep existing BETA/STABLE Worker/D1/DNS resources.
- If the old API token plaintext is unavailable, create a new least-privilege deploy token and save it in the corresponding GitHub Environment secret.
- Verification should first be read/health-oriented; do not create replacement D1 databases just because the GitHub repo changed.

## 4. Google account security

Runtime owner: `automation@supra.cc.cd`.

Before authorizing automation:

- configure independent recovery email;
- recovery phone where appropriate;
- enable 2-Step Verification/passkey;
- save backup codes outside the automated Drive account;
- avoid using this account for unrelated bulk mail/service registrations;
- do not add Gmail to the account solely for the project.

The account currently has no Gmail mailbox; that is acceptable for Drive/Sheets/Apps Script/OAuth. Incoming mail to the domain address is handled by the domain routing/forwarding setup.

## 5. Google Cloud/OAuth architecture

Keep BETA and STABLE isolated.

Recommended projects:

- `VHDCHY-BETA`
- `VHDCHY-STABLE`

Both owned by `automation@supra.cc.cd`.

Enable only APIs actually required by the current automation path:

- Google Apps Script API
- Google Drive API

Do not enable Gmail/Calendar/Contacts APIs for this recovery.

## 6. Google Auth Platform — Testing vs In production

Google Auth Platform is managed through Overview / Branding / Audience / Clients / Data Access / Verification Center.

For this project, the Google Account is not a Workspace organization account, so use an External audience.

### 6.1 Why Testing is not suitable for the final CI token

An External OAuth project with Publishing status `Testing` issues test-user authorizations that expire after seven days for non-basic scope use. Therefore a GitHub CI refresh token generated while the project remains in Testing is not a durable setup.

### 6.2 Required durable configuration

For each environment OAuth project:

1. Configure Branding with a truthful project/app name and support/developer contact owned by the project owner.
2. Audience: External.
3. Data Access: add only the exact CI scopes in section 7.
4. During setup, `automation@supra.cc.cd` may be added as test user if required.
5. When configuration is ready, use Audience -> `Publish app` / switch Publishing status to `In production`.
6. Generate the final refresh token only after the project is in the intended production publishing state.

`In production` removes the Testing seven-day authorization lifetime, but refresh tokens are still revocable and can become invalid after policy/security events or long inactivity. Treat them as long-lived credentials, not permanent credentials.

### 6.3 Verification nuance

This integration is for the Owner's own account / limited personal-use automation, not a public product. Google documents a personal-use exception where verification is not mandatory below 100 personally known users, although an unverified warning/user cap can apply. If Google Console/Verification Center specifically requires verification for the chosen scope/configuration, follow the current provider requirement rather than bypassing it.

Owning `supra.cc.cd` does not itself remove the seven-day Testing lifetime. Domain ownership is relevant when the domain is used in OAuth branding/authorized domains or verification.

If domain verification is needed, verify the root domain in Google Search Console using a Google account that is a Project Owner; use a Domain property/DNS TXT method.

## 7. CI OAuth client — minimum approved scopes

The current `scripts/deploy-gas.sh` updates Apps Script source/versions/deployments. The environment verification also needs only Drive metadata for the root folder.

Use exactly:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
https://www.googleapis.com/auth/drive.metadata.readonly
```

Do not add:

- Gmail scopes
- `mail.google.com`
- Calendar
- Contacts
- `script.send_mail`
- full Drive
- Sheets

unless current source is changed and an Owner-approved use case proves the additional scope is required.

## 8. Create the OAuth client and one durable refresh token

A practical manual bootstrap is a Web application OAuth client plus Google OAuth 2.0 Playground.

### 8.1 Create client

In Google Auth Platform -> Clients:

- Create OAuth Client ID.
- Application type: Web application.
- Use a clear environment-specific name, e.g. `VHDCHY BETA CI`.
- Add redirect URI: `https://developers.google.com/oauthplayground` only for this manual bootstrap path.

Store Client ID/Client Secret directly into the matching GitHub Environment secret fields.

### 8.2 Generate token

Open OAuth 2.0 Playground.

- Open settings/gear.
- Enable `Use your own OAuth credentials`.
- Enter the environment Client ID/Client Secret locally in the Playground form.
- Authorize only the three CI scopes listed above.
- Complete consent as `automation@supra.cc.cd`.
- Exchange the authorization code for tokens.
- Copy the refresh token directly into `GOOGLE_OAUTH_REFRESH_TOKEN` in the matching GitHub Environment.

Do not keep minting refresh tokens on each run. CI should reuse the same refresh token and exchange it for short-lived access tokens as needed.

## 9. Apps Script runtime scopes

CI OAuth scopes above are NOT the same as Apps Script runtime scopes.

Current gateway source uses DriveApp, SpreadsheetApp, UrlFetchApp, ScriptApp and Session. The audited manifest is:

```text
https://www.googleapis.com/auth/drive
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/script.external_request
https://www.googleapis.com/auth/script.scriptapp
https://www.googleapis.com/auth/userinfo.email
```

`https://www.googleapis.com/auth/script.send_mail` was removed because the current source does not send mail.

Do not add Gmail/MailApp scopes unless a later Owner-approved feature needs them.

## 10. GAS BETA creation

Do BETA first.

1. Log in as `automation@supra.cc.cd`.
2. Create a standalone Apps Script project named clearly, e.g. `VHDCHY BETA Google Gateway`.
3. Ensure Apps Script API access is enabled for the intended BETA Cloud/OAuth project and link/associate the standard Google Cloud project as required by the current console flow.
4. Put the gateway source/manifest from the authority repo into the project. Do not copy old Script IDs or deployment IDs.
5. Run `bootstrapAuthorize()` once manually.
6. Review the consent screen. It must request only the expected runtime scopes listed above. If mail/Gmail/Calendar/Contacts appear, stop and audit before accepting.
7. Confirm the function completes successfully and the temporary authorization-test Sheet is trashed.
8. Deploy -> New deployment -> Web app.
9. Execute as: the user deploying (`automation@supra.cc.cd`).
10. Access setting must match the approved web-app design. The current manifest is `ANYONE_ANONYMOUS`; do not widen/change the design outside an approved architecture change.
11. Record the new Script ID, Deployment ID and `/exec` URL.
12. Put these three values in the GitHub `beta` Environment variables.

Then AI/CI can verify and take over version updates via Apps Script API.

## 11. GAS STABLE creation

Only after BETA integration PASS and Owner approval.

Repeat section 10 using a separate STABLE Apps Script project and separate STABLE Google Cloud/OAuth project/client/token. Do not copy BETA resource IDs into STABLE.

## 12. Android signing restore

Keep the original signing identities.

BETA recorded fingerprint:

`77:F1:80:45:03:DA:22:22:CE:92:58:58:95:1F:90:B6:12:AA:05:89:6B:53:DE:A0:CB:8F:FB:F0:7B:14:2A:16`

STABLE recorded fingerprint:

`88:ED:44:66:EB:0E:4F:10:57:26:75:C4:8B:B9:86:30:F7:36:06:4C:5F:1A:95:04:5C:FB:C6:A4:AA:8B:42:C3`

Re-enter the existing keystore base64/passwords in the GitHub Environment secrets. Never commit or paste them into chat. CI must verify fingerprint before building releases.

## 13. Anti-abuse / account-lock risk reduction

No configuration can guarantee that Google will never flag an account. Use these controls:

- dedicated automation identity only;
- minimum current scopes;
- no Gmail/Calendar/Contacts scopes without need;
- no `script.send_mail` for the current gateway;
- no bulk email behavior;
- one stable OAuth client/refresh token per environment, not token churn;
- do not repeatedly authorize/revoke/create projects without reason;
- keep 2FA/recovery current;
- stay within API/Apps Script quotas;
- use BETA/STABLE projects separately but do not create unnecessary throwaway Google accounts;
- log only IDs/status/error codes, never access/refresh tokens or client secrets.

Google does not publish an anti-bot 'safe threshold'; being under quota is not a guarantee against security/abuse flags.

## 14. Final verification matrix

### BETA

- [ ] GitHub Environment `beta` configured.
- [ ] Owner email variable is `automation@supra.cc.cd`.
- [ ] New BETA Drive root/current workbook IDs only.
- [ ] BETA OAuth Publishing status intended for durable use (`In production`).
- [ ] One valid BETA CI refresh token using only three approved CI scopes.
- [ ] BETA GAS Script/Deployment/Exec IDs are new and owned by current account.
- [ ] `bootstrapAuthorize()` requested only expected runtime scopes.
- [ ] Cloudflare token resolves retained existing resources.
- [ ] Android signer fingerprint matches recorded BETA fingerprint.
- [ ] Current-ID environment verification workflow PASS.
- [ ] BETA deploy/health PASS before `beta` pointer is moved.

### STABLE

- [ ] BETA recovery PASS.
- [ ] Owner explicitly approves STABLE recovery/deploy.
- [ ] Separate STABLE OAuth/GAS resources and token.
- [ ] STABLE Drive root only.
- [ ] Retained Cloudflare STABLE resources verified.
- [ ] STABLE signer fingerprint verified.
- [ ] STABLE CI/health PASS before `stable` pointer move.

## 15. New-chat continuity

Every new AI session must bootstrap from:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Then read only task-specific detail. Model memory/chat history never overrides these authority files. Around 20 minutes into a long CI/provider tranche, checkpoint before stopping so the next session can continue without reconstructing hidden model memory.

## Official references

- OAuth refresh token expiration: https://developers.google.com/identity/protocols/oauth2
- Google Auth Platform Audience / Publishing status: https://support.google.com/cloud/answer/15549945
- Verification exceptions/personal use: https://support.google.com/cloud/answer/13464323
- Google Auth Platform overview: https://support.google.com/cloud/answer/15544987
- Domain verification: https://support.google.com/cloud/answer/13804266
- OAuth policies: https://developers.google.com/identity/protocols/oauth2/policies
