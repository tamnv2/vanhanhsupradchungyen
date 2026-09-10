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
- Full credential/environment verification đã PASS cho cả BETA và STABLE, gồm Cloudflare, Google OAuth/API, GAS endpoint, Drive và Android signing.

## Đang triển khai

- Chuẩn bị BETA foundation deploy thật: Google Gateway source/version + D1 + Worker + custom domain + health validation.
- BETA deploy tự động khi branch `beta` được cập nhật.
- STABLE deploy tự động khi branch `stable` được promote nhưng vẫn bị chặn bởi Owner approval của Environment `stable`.

## Chưa triển khai business runtime

- Worker hiện chỉ có health skeleton.
- D1 sẽ được tự tạo ở lần deploy đầu tiên với location hint `apac`.
- Durable Objects chỉ tạo khi business runtime bắt đầu dùng lock/rate-limit/concurrency.
- R2 giữ OFF cho tới khi có nhu cầu rõ ràng.
- Android app business code chưa được bootstrap trong repo.

## Next checkpoint

Deploy BETA foundation và xác minh public endpoint `https://beta.supra.cc.cd/health` cùng Google Gateway BETA trước khi phát triển business runtime.
