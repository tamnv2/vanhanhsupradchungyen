# CURRENT STATE

Cập nhật: 2026-09-10

## Hoàn tất

- Account Google/Gmail/Drive mới đã kết nối.
- GitHub connector đã kết nối account mới.
- Drive runtime BETA/STABLE đã được tạo.
- Hai Google Cloud project BETA/STABLE đã được tạo và liên kết đúng Apps Script bằng Project Number.
- OAuth cho Google CI đã được Owner tạo.
- Apps Script BETA/STABLE đã authorize PASS và có deployment cố định.
- Cloudflare zone `supra.cc.cd` đã Active.
- Cloudflare CI token BETA/STABLE đã được Owner tạo.
- Android signing key BETA/STABLE đã được tạo; fingerprint đã được ghi vào Resource Registry.
- Repo `tamnv2/vanhanhdchungyen` đã được khởi tạo và giữ PUBLIC có chủ đích.

## Đang setup

- GitHub Environments `beta` và `stable`.
- Nhập toàn bộ Variables/Secrets một lần.
- Kiểm tra workflow deploy Google Gateway + Cloudflare Worker/D1.

## Chưa triển khai business runtime

- Worker hiện chỉ có health skeleton.
- D1 sẽ được tự tạo ở lần deploy đầu tiên.
- Durable Objects chỉ tạo khi business runtime bắt đầu dùng lock/rate-limit/concurrency.
- R2 giữ OFF cho tới khi có nhu cầu rõ ràng.
- Android app business code chưa được bootstrap trong repo.

## Next checkpoint

Owner tạo GitHub Environments và nhập Variables/Secrets theo `docs/GITHUB_ENV_SETUP.md`, sau đó AI chạy/kiểm tra deploy BETA trước.
