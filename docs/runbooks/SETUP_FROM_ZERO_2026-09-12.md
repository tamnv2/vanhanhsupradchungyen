# SETUP FROM ZERO — VHDCHY — 2026-09-12

Status: ACTIVE RUNBOOK
Baseline: `SETUP-RESET-20260912-01`

This runbook rebuilds the authorization/provider chain while reusing already verified current-account resources. It is mirrored in the Owner Word guide generated for this baseline.

## 0. Fixed identity map

- Drive/Sheets/GAS: `tam95.supra@gmail.com`.
- Cloudflare/GitHub account: `nguyenvantam050595@gmail.com`.
- GitHub: `tamnv2/vanhanhsupradchungyen`.
- Google suspended account: `automation@supra.cc.cd` — do not use.
- Drive root: `VẬN HÀNH DC HƯNG YÊN` / `19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`.
- BETA root: `10_RUNTIME_BETA` / `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`.
- Reference: `BACKUP PICK PACK 1291` / `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h` / reference-only.

## 1. GitHub baseline — AI/tool

PASS if:

- connected user is `tamnv2`;
- repo is current authority and admin/push is available;
- snapshot branch exists;
- authority files contain current identity map;
- `validate.yml` passes after reset commit.

Do not move `beta`/`stable` during this phase.

Links:

- repo: `https://github.com/tamnv2/vanhanhsupradchungyen`
- Actions: `https://github.com/tamnv2/vanhanhsupradchungyen/actions`
- Actions settings: `https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions`
- Environments: `https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments`

## 2. Drive + BETA Sheet — AI/tool where possible

Already PASS:

- project root owner;
- BETA runtime root owner;
- existing BETA folder skeleton.

Next:

1. verify/create `PICK_PACK_1291` folder under BETA `01_CLUSTERS`;
2. create fresh BETA workbook for current quarter/current adopted naming convention;
3. build tabs according to `PP1291_SHEETS_BETA_V1`/latest adopted schema;
4. no old business data migration and no password verifier;
5. verify owner, folder location, tab schema;
6. record spreadsheet ID in `SERVICE_AUTHORITY.md` + `config/projections.beta.json`.

Links:

- root: `https://drive.google.com/drive/folders/19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`
- BETA: `https://drive.google.com/drive/folders/1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`
- reference: `https://drive.google.com/drive/folders/1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`

## 3A. Google Cloud/OAuth BETA — Owner UI

Can run in parallel with Cloudflare and signing.

Account: `tam95.supra@gmail.com`.

1. Create/select dedicated standard project for BETA.
   Link: `https://console.cloud.google.com/projectcreate`
2. Enable **Google Apps Script API**.
   Link: `https://console.cloud.google.com/apis/library/script.googleapis.com`
3. Enable Apps Script account API access.
   Link: `https://script.google.com/home/usersettings`
4. Configure Google Auth Platform.
   Link: `https://console.cloud.google.com/auth/overview`
5. Create BETA OAuth client for CI.
6. Authorize only:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

7. For durable CI use, set intended production publishing state before generating the final refresh token. Testing is only temporary for these scopes.
8. Store client ID/secret/refresh token privately; never paste them into chat.

Optional token tool:
`https://developers.google.com/oauthplayground/`

Record only non-secret identifiers/status in checkpoint.

## 3B. Cloudflare — Owner UI, parallel

Account: `nguyenvantam050595@gmail.com`.

1. Login and verify account ID.
2. Verify zone `supra.cc.cd` and existing intended Worker/D1 resources.
3. Do not recreate/delete resources based solely on old Git history.
4. Create/verify custom token with:
   - Account / Workers Scripts / Write
   - Account / D1 / Write
5. Save token only in secret store.

Links:

- dashboard: `https://dash.cloudflare.com/`
- tokens: `https://dash.cloudflare.com/profile/api-tokens`

PASS = current account/resource identity + token behavior verified.

## 3C. Android BETA signing — Owner local, parallel

1. Locate current VHDCHY BETA keystore backup.
2. Verify alias and SHA256 fingerprint locally.
3. Verify store/key passwords locally.
4. Prepare Base64 only for direct GitHub secret input; do not put it in chat/Drive/repo.
5. Never substitute retired Pick Pack signer.

PASS = signed build can be reproduced with expected current signer.

## 4. GAS BETA — Owner UI + AI source

Depends on current BETA Sheet + BETA Google Cloud project.

1. Open Apps Script: `https://script.google.com/home`.
2. Create standalone `VHDCHY BETA Google Gateway` owned by `tam95.supra@gmail.com`.
3. Link it to the standard BETA Cloud project.
4. Use current `gateway/Code.gs` and `gateway/appsscript.json`.
5. Manifest scopes must be exactly:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

6. Replace bootstrap placeholders through the controlled deploy path/current BETA values.
7. Run `bootstrapAuthorize()` once.
8. Consent must not request Drive/Gmail/Calendar/Contacts/mail/external-request/trigger-management. If it does, STOP and audit source.
9. Deploy Web App and record non-secret:
   - Script ID
   - Deployment ID
   - `/exec` URL

PASS = `bootstrapAuthorize()` points to current workbook/account and `/exec` health identifies correct environment/script.

## 5. GitHub `beta` Environment — Owner UI after provider outputs

Variables:

```text
APP_ENV=beta
OWNER_EMAIL=tam95.supra@gmail.com
CF_ACCOUNT_ID=<verified>
PUBLIC_HOST=beta.supra.cc.cd
GAS_SCRIPT_ID=<verified>
GAS_DEPLOYMENT_ID=<verified>
GAS_EXEC_URL=<verified>
GOOGLE_SHEETS_PROJECTION_ID=<verified>
ANDROID_SIGNING_ALIAS=<verified>
```

Secrets:

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Do not enter secret values in ordinary variables.

## 6. BETA verification — AI/CI

Run verification without reading secret values:

- repo/governance validation;
- Google token refresh and Apps Script content/deployment access;
- GAS `/exec` identity;
- current Sheet open/bootstrap;
- Cloudflare account/Worker/D1/token behavior;
- Android signer/build;
- BETA deploy/health.

Record commit, workflow run IDs and provider resource IDs. Only then mark BETA PASS/move live pointer.

## 7. LAN V4 physical regression

After foundation BETA PASS, resume corporate-laptop + exactly two MT90 tests. No admin/router/DNS/firewall bypass. Synthetic load is software capacity evidence only.

## 8. STABLE

Requires explicit Owner approval after BETA PASS. Repeat the provider-first chain with isolated STABLE Google/OAuth/GAS/Sheet/secret state; never copy BETA credentials merely to save time.
