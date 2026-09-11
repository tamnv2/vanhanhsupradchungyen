# GITHUB ENVIRONMENT SETUP — REBUILD 2026-09-11

Repo authority: `tamnv2/vanhanhsupradchungyen`.

Owner thao tác UI một lần cho các mục GitHub không thể/không nên tự động đọc secret. Không copy secret/ID Google cũ từ `tamnv2supra/vanhanhdchungyen`.

## 1. Repository Actions permission

Vào `Settings → Actions → General`:

- Actions: cho phép workflow cần thiết của repo.
- `Workflow permissions`: chọn `Read and write permissions` để release workflow có thể tạo prerelease/assets.
- Không bật quyền rộng ngoài nhu cầu chỉ để giải quyết lỗi mirror workflow cũ.

## 2. Environments

Tạo đúng hai Environments:

- `beta`
- `stable`

`stable`: bật Required reviewers/Owner approval nếu gói/UI hiện tại hỗ trợ. BETA không cần approval.

## 3. Variables — BETA

```text
APP_ENV=beta
OWNER_EMAIL=automation@supra.cc.cd
CF_ACCOUNT_ID=1b1695e4f2a3abfe08dc475b352c7f42
CF_ZONE_ID=880b887071f771f5be3caf5726d5df11
CF_ZONE_NAME=supra.cc.cd
PUBLIC_HOST=beta.supra.cc.cd
LAN_HOST=beta-lan.supra.cc.cd
GOOGLE_DRIVE_ENV_ROOT_ID=1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog
GAS_SCRIPT_ID=<NEW_BETA_GAS_SCRIPT_ID>
GAS_DEPLOYMENT_ID=<NEW_BETA_DEPLOYMENT_ID>
GAS_EXEC_URL=<NEW_BETA_EXEC_URL>
ANDROID_SIGNING_ALIAS=vhdchy-beta
ANDROID_SIGNING_SHA256=77:F1:80:45:03:DA:22:22:CE:92:58:58:95:1F:90:B6:12:AA:05:89:6B:53:DE:A0:CB:8F:FB:F0:7B:14:2A:16
```

Cloudflare IDs/hostnames above are retained from the existing provider setup per Owner confirmation; Google Drive/GAS values are new-account resources.

## 4. Variables — STABLE

```text
APP_ENV=stable
OWNER_EMAIL=automation@supra.cc.cd
CF_ACCOUNT_ID=1b1695e4f2a3abfe08dc475b352c7f42
CF_ZONE_ID=880b887071f771f5be3caf5726d5df11
CF_ZONE_NAME=supra.cc.cd
PUBLIC_HOST=supra.cc.cd
LAN_HOST=lan.supra.cc.cd
GOOGLE_DRIVE_ENV_ROOT_ID=1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE
GAS_SCRIPT_ID=<NEW_STABLE_GAS_SCRIPT_ID>
GAS_DEPLOYMENT_ID=<NEW_STABLE_DEPLOYMENT_ID>
GAS_EXEC_URL=<NEW_STABLE_EXEC_URL>
ANDROID_SIGNING_ALIAS=vhdchy-stable
ANDROID_SIGNING_SHA256=88:ED:44:66:EB:0E:4F:10:57:26:75:C4:8B:B9:86:30:F7:36:06:4C:5F:1A:95:04:5C:FB:C6:A4:AA:8B:42:C3
```

Không copy GAS/Drive BETA sang STABLE.

## 5. Secrets — mỗi Environment

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

### Source/rotation rule

- `CLOUDFLARE_API_TOKEN`: dùng token hợp lệ cho setup Cloudflare hiện hữu; nếu không còn plaintext/không chắc quyền thì tạo token mới với quyền tối thiểu cần deploy Worker + D1 + custom domain hiện hành. Không rebuild Cloudflare resources.
- `GOOGLE_OAUTH_*`: **tạo mới** dưới Google Cloud/OAuth mới của `automation@supra.cc.cd`; không dùng token/client cũ.
- Android signing secrets: nhập lại từ Owner-controlled keystore backup hiện có. Không generate signer mới nếu keystore cũ vẫn còn.
- Secret chỉ nhập trực tiếp vào GitHub Environment secret UI; không paste vào chat/commit/issue/document.

## 6. Google CI OAuth scope set

Refresh token dùng bởi `scripts/deploy-gas.sh` chỉ cần quyền cho Apps Script API và kiểm tra metadata Drive root:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
https://www.googleapis.com/auth/drive.metadata.readonly
```

Không thêm Gmail, Calendar, Contacts, `mail.google.com`, `script.send_mail`, full Drive hoặc Sheets vào **CI OAuth client** nếu script CI hiện hành không dùng chúng.

Lưu ý: đây là scope của OAuth client mà GitHub Actions dùng; khác với scope runtime trong `gateway/appsscript.json`.

## 7. Gate trước khi deploy

Không restore/chạy `verify-environments.yml` với hard-coded ID cũ. Sau khi new GAS IDs/secrets đã điền đầy đủ, cập nhật workflow verify bằng current IDs rồi mới chạy.

Thứ tự gate:

1. BETA Environment vars/secrets complete.
2. Google OAuth refresh test PASS.
3. Apps Script BETA project/deployment + Drive root verify PASS.
4. Cloudflare retained resources/token verify PASS.
5. Android signer verify PASS.
6. Chạy BETA CI/deploy và health PASS.
7. Chỉ sau đó cấu hình/verify STABLE và xin Owner approval trước deploy.
