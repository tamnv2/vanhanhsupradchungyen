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
