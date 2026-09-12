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
- BETA cluster: `PICK_PACK_1291` / `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`.
- BETA projection Sheet: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3` / `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk` / `PROVISIONED_NOT_LIVE`.
- Reference: `BACKUP PICK PACK 1291` / `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h` / reference-only.

## 1. GitHub baseline — PASS

Verified:

- connected user `tamnv2`;
- repo current authority and admin/push available;
- snapshot branch exists;
- authority files contain current identity map;
- `Validate public repo` run `34666033568` SUCCESS.

Do not move `beta`/`stable` during setup.

Links:

- repo: `https://github.com/tamnv2/vanhanhsupradchungyen`
- Actions: `https://github.com/tamnv2/vanhanhsupradchungyen/actions`
- Actions settings: `https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions`
- Environments: `https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments`

## 2. Drive + BETA Sheet — PASS / NOT LIVE

Verified/reused:

- project root;
- BETA runtime root + skeleton;
- BETA cluster folder `PICK_PACK_1291`;
- native current BETA workbook with 17 current tabs.

Current links:

- root: `https://drive.google.com/drive/folders/19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`
- BETA: `https://drive.google.com/drive/folders/1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`
- cluster: `https://drive.google.com/drive/folders/1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`
- BETA Sheet: `https://docs.google.com/spreadsheets/d/1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk/edit`
- reference: `https://drive.google.com/drive/folders/1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`

Rules:

- no historical business rows migrated;
- no password verifier/secret;
- reference-only LAN/emergency/fallback tabs are excluded;
- Sheet remains `PROVISIONED_NOT_LIVE` until GAS + BETA integration gate.

## 3A. Google Cloud/OAuth BETA — NEXT / Owner UI

Run in parallel with 3B/3C.

Account: `tam95.supra@gmail.com`.

1. Create/select dedicated standard BETA project.
   `https://console.cloud.google.com/projectcreate`
2. Enable **Google Apps Script API**.
   `https://console.cloud.google.com/apis/library/script.googleapis.com`
3. Enable Apps Script account API access.
   `https://script.google.com/home/usersettings`
4. Configure Google Auth Platform.
   `https://console.cloud.google.com/auth/overview`
5. Create BETA OAuth client for CI.
6. Authorize only:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

7. For durable CI use, set intended production publishing state before generating the final refresh token; Testing tokens for these non-basic scopes are temporary.
8. Store client ID/secret/refresh token privately; never paste them into chat.

Optional token tool:
`https://developers.google.com/oauthplayground/`

## 3B. Cloudflare — NEXT / Owner UI / parallel

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

## 3C. Android BETA signing — NEXT / Owner local / parallel

1. Locate current VHDCHY BETA keystore backup.
2. Verify alias and SHA256 fingerprint locally.
3. Verify store/key passwords locally.
4. Prepare Base64 only for direct GitHub secret input; do not put it in chat/Drive/repo.
5. Never substitute retired Pick Pack signer.

PASS = signed build can be reproduced with expected current signer.

## 4. GAS BETA — after 3A

Sheet dependency is already satisfied.

1. Open Apps Script: `https://script.google.com/home`.
2. Create standalone `VHDCHY BETA Google Gateway` owned by `tam95.supra@gmail.com`.
3. Link it to the standard BETA Cloud project.
4. Use current `gateway/Code.gs` and `gateway/appsscript.json`.
5. Manifest scopes exactly:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

6. Target current workbook ID `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`.
7. Run `bootstrapAuthorize()` once.
8. Consent must not request Drive/Gmail/Calendar/Contacts/mail/external-request/trigger-management. If it does, STOP and audit source.
9. Deploy Web App and record non-secret Script ID, Deployment ID and `/exec` URL.

## 5. GitHub `beta` Environment — after verified provider outputs

Variables:

```text
APP_ENV=beta
OWNER_EMAIL=tam95.supra@gmail.com
CF_ACCOUNT_ID=<verified>
PUBLIC_HOST=beta.supra.cc.cd
GAS_SCRIPT_ID=<verified>
GAS_DEPLOYMENT_ID=<verified>
GAS_EXEC_URL=<verified>
GOOGLE_SHEETS_PROJECTION_ID=1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk
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

Only then mark BETA PASS/move live pointer.

## 7. LAN V4 physical regression

After BETA foundation PASS, resume corporate-laptop + exactly two MT90 tests. No admin/router/DNS/firewall bypass. Synthetic load is software-capacity evidence only.

## 8. STABLE

Requires explicit Owner approval after BETA PASS. Repeat provider-first chain with isolated STABLE Google/OAuth/GAS/Sheet/secret state; never copy BETA credentials merely to save time.
