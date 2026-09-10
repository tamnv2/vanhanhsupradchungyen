# SECURITY

Repository này là PUBLIC.

## Không bao giờ commit

- Cloudflare API token.
- Google OAuth client secret hoặc refresh token.
- Android `.jks`, `.keystore`, file Base64 của keystore, signing passwords.
- HMAC/JWT/application secret.
- `.env` có credential.

## GitHub Actions

- Secret chỉ dùng trong job gắn Environment `beta` hoặc `stable`.
- Workflow từ pull request không được truy cập deployment secrets.
- Không dùng `pull_request_target` cho pipeline có secret.
- STABLE phải có required reviewer là Owner.
- Log phải tránh `set -x` khi xử lý credential.

## Signing

Stable keystore là critical asset. Backup phải mã hóa và Owner kiểm soát. Không upload raw keystore vào repo/chat/email.

## Báo cáo

Nếu phát hiện credential bị commit, coi credential đó đã lộ: revoke/rotate ngay, sau đó mới xử lý Git history.
