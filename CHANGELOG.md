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

## 2026-09-10 — governance-0.4.0

- Owner chốt VHDCHY là dự án chính bao quát DC; Pick Pack 1291 chỉ là cluster/reference có chọn lọc.
- Thêm authority hierarchy chống reference cũ ghi đè VHDCHY.
- Thiết lập minimal bootstrap 4-file để tối ưu token/time qua nhiều phiên chat.
- Thiết lập dependency graph + parallel execution là mặc định.
- Thiết lập execution tranche khoảng 20 phút, soft stop và mandatory checkpoint trước khi tool/session hard-stop.
- Thiết lập `NEXT_ACTIONS.md` + `SESSION_CHECKPOINT.md` để continuation deterministic.
- Thiết lập detailed append-only changelog record trong `docs/changelog/`.
- Dừng mở rộng business schema cho tới khi `RECONCILE-001` phân loại core chung DC và cluster-specific.
- Chi tiết: `docs/changelog/2026-09-10-governance-0.4.0.md`.

## 2026-09-10 — governance-0.4.1

- Thêm `scripts/validate-governance.sh` để CI kiểm tra bắt buộc các authority/checkpoint/index file và marker cốt lõi.
- Gắn governance continuity validation vào workflow `Validate public repo`.
- Cơ chế continuity từ nay không chỉ là tài liệu hướng dẫn mà có CI guard chống vô tình xóa/hỏng bootstrap/checkpoint.
- Chi tiết: `docs/changelog/2026-09-10-governance-0.4.1.md`.

## 2026-09-10 — reconcile-sheets-0.5.0

- Hoàn tất `RECONCILE-001`: phân tách generic DC core và cluster-specific Pick Pack 1291.
- Đồng bộ source của Business Core V1 đang live BETA vào `main` authority mà không move BETA/STABLE.
- Xác minh lại BETA deploy `947a4...`: Worker version `f8ddf638-0829-4249-9ea0-8f2d38b03f05`, D1 `business_core_v1`, Worker meta và Google Gateway advisory đều PASS.
- Tạo BETA cluster folder `PICK_PACK_1291` và workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3` schema `PP1291_SHEETS_BETA_V1`.
- Adapt tab/header/danh mục hữu ích từ backup Pick Pack 1291; không migrate dữ liệu cũ, không copy password verifier, không tự tạo lại LAN/emergency fallback tabs.
- Chốt generic resource taxonomy và module ownership cho `dropped_goods` qua decision D-024; sửa schema tương lai bằng migration additive.
- Main CI giờ kiểm tra đồng thời governance continuity và D1 migrations; run `34506547071` PASS.
- Chi tiết: `docs/changelog/2026-09-10-reconcile-sheets-0.5.0.md`.

## 2026-09-11 — lan-pilot-0.6.0

- Owner chuyển LAN thành priority feasibility gate trước business build sâu.
- Khóa `LAN-PILOT-001`: build reusable Android/PDA BETA test app + Windows LAN Agent BETA + internal LAN Web BETA.
- PDA phải tự phát hiện LAN BETA hợp lệ và tự chuyển LAN mode; fallback/reconnect phải deterministic, có trạng thái rõ và không flapping.
- Điều kiện test vật lý được chốt: tối đa khoảng 3 PDA + 1 laptop; test capacity lớn hơn dùng synthetic logical clients và không được đánh đồng với Wi-Fi/RF evidence.
- Android và LAN Agent đều phải có automatic update discovery/notification cùng manual update fallback.
- LAN Agent phải tối ưu nhẹ nhưng có tray/settings console: trạng thái, metrics, update, local data directory, logs/diagnostics, start/stop/restart.
- `beta-lan.supra.cc.cd`/`lan.supra.cc.cd` tiếp tục internal-only, không public DNS.
- `PROJECT_SCOPE.md`, `DECISIONS.md`, `DECISIONS_INDEX.md`, `NEXT_ACTIONS.md`, `TASK_LEDGER.md`, `CURRENT_STATE.md`, `SESSION_CHECKPOINT.md` đã được cập nhật để LAN gate không bị mất qua phiên chat.
- Chi tiết: `docs/changelog/2026-09-11-lan-pilot-0.6.0.md` và `docs/lan/LAN_PILOT_001.md`.

## 2026-09-11 — lan-pilot-0.6.1

- Phát hành LAN Pilot BETA `v0.1.12` sau khi cả Windows Agent, Android APK và prerelease job PASS trong run `34516199267`.
- Agent tray hiển thị nhanh CPU/RAM/Disk/Network; dashboard bổ sung CPU/RAM riêng của Agent và chú thích tác dụng của từng thông số.
- Agent có rotating diagnostic log và một-click export ZIP từ tray hoặc local dashboard; export không chứa `pilot.db`.
- Android giữ nguyên transport implementation hiện có, thêm diagnostic snapshot history và nút `Xuất log chẩn đoán` tạo TXT bằng Android document picker.
- Android UI thêm diễn giải Transport state, endpoint, hysteresis, latency, durable event, manual endpoint, queue và update flow.
- Không thêm quyền Admin, không yêu cầu router/DNS/firewall, không thêm public diagnostic upload endpoint.
- Ghi nhận build trung gian `34516009029`: Android PASS, Windows compile FAIL do overload `StopAsync`; đã sửa ở commit `bc3736237c70de4a0f0b236b0f12a607bad00676` và final build PASS.
- Chi tiết: `docs/changelog/2026-09-11-lan-pilot-0.6.1.md`.

## 2026-09-11 — lan-pilot-0.7.0

- Phân tích evidence thật từ Agent + 2 Newland MT90 trên mạng công ty: hai PDA đạt `LAN_ACTIVE`, manual endpoint trống, health streak 73/67 với failure 0; latency và durable-event path đủ tốt để chuyển trạng thái feasibility từ UNKNOWN sang FEASIBLE/NEEDS-STRESS-EVIDENCE.
- Owner xác nhận hiện chỉ có đúng 2 MT90; thêm D-029: dùng 2 PDA thật + synthetic capacity để quyết định feasibility hiện tại, nhưng không suy diễn RF/Wi-Fi cho >2 PDA.
- Phát hành comprehensive measurement build `lan-pilot-beta-v0.2.20`.
- Android 0.2 thêm application-level LAN priority/reacquisition, discovery-source evidence, upload/download 1/10/25 MB, realtime PDA↔laptop↔PDA, heavy realtime 200×2KB, FULL suite và FULL diagnostics.
- Agent 0.2 thêm realtime bounded buffer/long-poll, transfer endpoints, richer metrics, laptop Test Center và local-only dashboard LoadGen 10/25/50/100 logical clients.
- Build run `34548902991`: Windows Agent SUCCESS, Android signed APK SUCCESS, prerelease SUCCESS.
- Không thay Worker/D1/Stable; LAN build vẫn portable no-admin và không yêu cầu router/DNS/firewall change.
- Chi tiết: `docs/changelog/2026-09-11-lan-pilot-0.7.0.md` và `docs/lan/LAN_PILOT_002_COMPREHENSIVE_TEST.md`.

## 2026-09-11 — lan-pilot-0.8.0

- Hoàn tất LAN Pilot V4 automated regression candidate `lan-pilot-beta-v0.3.36` từ source `7b4488a89f585812c1bccba5d07d86049482bf4c`.
- Sửa cơ chế version/update: Agent, APK và Release dùng cùng semantic version; Agent/APK đều có SHA256 verification và manual fallback; Agent có staged no-admin update + restart health check + rollback path.
- Sửa staged Agent updater health loop để healthy update có thể commit thay vì false-rollback; CI có source invariant chống tái phát.
- Sửa realtime restart/cursor bằng `streamEpoch + sequence`, explicit resync khi epoch/buffer gap thay đổi và chuyển Agent long-poll sang signal-driven wait.
- Sửa đo realtime cross-device bằng Agent clock calibration; ACK latency dùng monotonic clock trên cùng PDA.
- Tách physical PDA/synthetic metrics, client-cancel khỏi service error; mở rộng diagnostics Agent/PDA.
- Android V4 thực thi foreground realtime; khi app rời foreground chỉ unfinished work/durable recovery mới dùng bounded `START_NOT_STICKY` finish service; service/wake lock/retry tự dừng, và Activity resources được giải phóng sau final tracked job.
- Build run `34562489063`: Windows Agent SUCCESS, Android signed APK SUCCESS, prerelease SUCCESS; repo validation `34562489066` SUCCESS.
- APK verified package `vn.vhdchy.lanpilot.beta`, versionCode `36`, versionName `0.3.36`, signature v1/v2 PASS.
- Automated source/build/sign/version/package/release gates đã PASS; final LAN-PILOT PASS vẫn cần physical regression trên laptop công ty + đúng 2 MT90, gồm update thực tế, restart/resync, realtime/transfer, Wi-Fi-off queue recovery, background/battery/resource, load/soak và log analysis.
- Không move cloud BETA/STABLE; không thay Worker/D1.
- Chi tiết: `docs/changelog/2026-09-11-lan-pilot-0.8.0.md`.
