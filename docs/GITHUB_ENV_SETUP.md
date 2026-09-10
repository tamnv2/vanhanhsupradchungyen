# GITHUB ENVIRONMENT SETUP

Owner chỉ cần cấu hình một lần.

Tạo hai Environments trong `Settings → Environments`:

- `beta`
- `stable`

Với `stable`, bật Required reviewers và chọn Owner. BETA không cần approval.

## Variables — mỗi Environment

Tạo các biến sau, giá trị BETA/STABLE khác nhau khi có ghi chú:

```text
APP_ENV
OWNER_EMAIL
CF_ACCOUNT_ID
CF_ZONE_ID
CF_ZONE_NAME
PUBLIC_HOST
LAN_HOST
GAS_SCRIPT_ID
GAS_DEPLOYMENT_ID
GAS_EXEC_URL
GOOGLE_DRIVE_ENV_ROOT_ID
ANDROID_SIGNING_ALIAS
ANDROID_SIGNING_SHA256
```

`CF_ACCOUNT_ID`, `CF_ZONE_ID`, `CF_ZONE_NAME`, `OWNER_EMAIL` có thể giống nhau giữa hai môi trường. `PUBLIC_HOST`, `LAN_HOST`, GAS/Drive/signing values phải đúng environment.

## Secrets — mỗi Environment

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Không nhập secret vào repository variable, issue, commit, Actions input hoặc chat.

## Sau khi nhập xong

1. Chạy workflow `Deploy BETA foundation`.
2. AI kiểm tra health/API/deployment.
3. Chỉ khi BETA pass mới chạy `Deploy STABLE foundation`; GitHub sẽ yêu cầu Owner approve environment `stable`.
