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
- GitHub Environments `beta` và `stable` đã được Owner cấu hình đủ Variables/Secrets.
- Full credential/environment verification PASS cho cả BETA và STABLE.
- BETA D1 `vhdchy-data-beta` tồn tại tại APAC; ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- BETA Worker `vhdchy-beta` LIVE tại `beta.supra.cc.cd`.
- BETA Google Gateway CI deployment hiện version 4 và endpoint PASS.
- BETA `/health` PASS với D1.
- BETA `/health/deep` PASS end-to-end: Worker + D1 + Google Gateway.
- Repository validation PASS sau runtime-foundation update.
- CI đã có selective component deploy để tránh cập nhật GAS/Cloudflare không cần thiết ở các commit sau.

## Trạng thái môi trường

- BETA: LIVE FOUNDATION / DEEP HEALTHY.
- STABLE: credentials VERIFIED nhưng runtime chưa promote/deploy; vẫn yêu cầu Owner approval.
- LAN hostnames vẫn private/reserved, không public DNS.
- R2 vẫn OFF.
- Durable Objects chưa tạo cho tới khi business runtime cần lock/rate-limit/concurrency.

## Đang tiếp tục

- BUILD-001 business-runtime foundation đang IN_PROGRESS.
- Endpoint `/api/v1/meta` đã có để xác nhận contract/version/build mà chưa mở mutation nghiệp vụ công khai.
- Schema và các entity nghiệp vụ chỉ triển khai theo authority đã chốt; không tự phát minh trường dữ liệu.
- Mọi thay đổi runtime mới vào BETA trước; STABLE chỉ promote sau gate và Owner acceptance.

## Next checkpoint

Mở rộng business-runtime contract/schema và client foundation theo Master Spec; giữ STABLE nguyên trạng cho tới khi có quyết định promote.
