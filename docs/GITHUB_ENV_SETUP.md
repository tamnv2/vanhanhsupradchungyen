# GITHUB ENVIRONMENT SETUP — BASELINE 2026-09-12

Repo: `tamnv2/vanhanhsupradchungyen`.

## Rule

GitHub Environment là **điểm nhập cuối** cho provider outputs đã được tạo và verify. Không điền placeholder/old value trước để “chuẩn bị”.

Order:

`Provider setup/verification -> collect current IDs/credentials -> GitHub beta Environment -> CI verification -> BETA PASS -> Owner approval -> STABLE`.

## Direct links

- Environments: `https://github.com/tamnv2/vanhanhsupradchungyen/settings/environments`
- Actions settings: `https://github.com/tamnv2/vanhanhsupradchungyen/settings/actions`
- Google Cloud project create: `https://console.cloud.google.com/projectcreate`
- Google Auth Platform: `https://console.cloud.google.com/auth/overview`
- Apps Script API: `https://console.cloud.google.com/apis/library/script.googleapis.com`
- Apps Script API account access: `https://script.google.com/home/usersettings`
- Apps Script home: `https://script.google.com/home`
- OAuth Playground: `https://developers.google.com/oauthplayground/`
- Cloudflare tokens: `https://dash.cloudflare.com/profile/api-tokens`
- Cloudflare dashboard: `https://dash.cloudflare.com/`

## GitHub repo permissions

Keep repository default workflow permissions restricted/read unless a verified current workflow requires more. Current deploy workflows request `contents: read`; LAN release workflow may request workflow-level `contents: write` for release assets.

Connector verifies repo admin/push access but cannot read Environment secret values. Treat pre-reset secret contents as stale/unknown.

## BETA prerequisites

Before filling `beta`, all of these must have current verified source:

- Cloudflare account ID + least-privilege API token;
- current BETA projection Sheet ID under `tam95.supra@gmail.com`;
- BETA Google Cloud/OAuth client + refresh token;
- BETA GAS Script ID / Deployment ID / `/exec` URL;
- verified VHDCHY BETA Android signer.

## BETA variables

```text
APP_ENV=beta
OWNER_EMAIL=tam95.supra@gmail.com
CF_ACCOUNT_ID=<verified current Cloudflare account ID>
PUBLIC_HOST=beta.supra.cc.cd
GAS_SCRIPT_ID=<verified BETA GAS script ID>
GAS_DEPLOYMENT_ID=<verified BETA deployment ID>
GAS_EXEC_URL=<verified BETA /exec URL>
GOOGLE_SHEETS_PROJECTION_ID=<verified current BETA workbook ID>
ANDROID_SIGNING_ALIAS=<verified VHDCHY BETA alias>
```

Do not create historical/unused variables solely for parity.

## BETA secrets

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Never paste actual values into repo/chat/docs.

## Google CI OAuth scopes

Exactly for current deploy source:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

## GAS runtime scopes

Exactly for current gateway foundation:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

## STABLE

Do not populate STABLE merely because the environment already exists. After BETA PASS + Owner approval, provision/verify isolated STABLE provider values and then populate `stable`.
