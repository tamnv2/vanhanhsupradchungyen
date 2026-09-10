# CHANGELOG

Lịch sử này là append-only.

## 2026-09-10 — foundation-0.1.0

- Chuyển authority sang account Google/GitHub mới.
- Chốt domain mới `supra.cc.cd`.
- Hoàn tất Google Drive/GCP/OAuth/Apps Script foundation.
- Hoàn tất Cloudflare zone và CI token foundation.
- Hoàn tất Android BETA/STABLE signing foundation.
- Khởi tạo public repository với security model phù hợp public source.
- Thêm workflow skeleton để validate và deploy BETA/STABLE sau khi Owner nhập GitHub Environment secrets.

## 2026-09-10 — foundation-0.2.0

- Thêm workflow read-only kiểm tra toàn bộ GitHub Environment configuration và provider credentials.
- Xác minh BETA và STABLE PASS: Cloudflare token, Google OAuth, Apps Script, Drive, GAS endpoint và Android signing material.
- Sửa Cloudflare deploy config từ TOML động sang JSON để tránh sai scope của `routes` sau `[[d1_databases]]`.
- D1 mới được tạo với location hint `apac` phù hợp tải chính tại Việt Nam.
- Thêm post-deploy validation cho cả Cloudflare Worker/D1 và Google Apps Script deployment.
- Chốt branch-driven deployment: cập nhật `beta` tự deploy BETA; promote `stable` kích hoạt STABLE nhưng vẫn yêu cầu Owner approval.

## 2026-09-10 — foundation-0.2.1

- Lần BETA deploy đầu đã tạo thành công D1 `vhdchy-data-beta` tại APAC nhưng phát hiện Wrangler không tìm thấy entrypoint vì config sinh trong runner temp.
- Chuyển generated Wrangler config vào repository working directory để relative entrypoint resolve đúng.
- Thêm explicit Apps Script web-app manifest `ANYONE_ANONYMOUS` + `USER_DEPLOYING` cho API-managed deployment.
- Thêm GAS known-good recovery/rollback guard về version 1 nếu endpoint bị lỗi trước hoặc sau CI update.
- Giới hạn auto-deploy theo paths để thay đổi tài liệu/trạng thái không làm tốn Actions/deploy provider không cần thiết.

## 2026-09-10 — foundation-0.3.0

- Automatic recovery đã restore Google Gateway BETA về version 1 trước khi retry, sau đó CI deploy version 3 thành công.
- BETA Google Gateway endpoint PASS.
- BETA Worker `vhdchy-beta` deploy thành công lên custom domain `beta.supra.cc.cd`.
- BETA D1 binding PASS; D1 ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- BETA `/health` PASS và toàn bộ environment deploy kết thúc SUCCESS.
- SETUP-009 được đánh dấu DONE; chuyển sang business-runtime foundation.

## 2026-09-10 — foundation-0.3.1

- Thêm `/health/deep` để kiểm tra end-to-end Worker + D1 + Google Gateway.
- Thêm `/api/v1/meta` làm endpoint contract/version/build an toàn trước khi mở API nghiệp vụ.
- BETA deep health PASS sau deploy; Worker version `1e479ad6-528b-4375-83c0-65f6c3d362fc`.
- Google Gateway BETA version 4 PASS.
- CI chọn component thay đổi để những commit sau không tạo GAS version hoặc deploy Worker không cần thiết.
- Repository validation tiếp tục PASS.
