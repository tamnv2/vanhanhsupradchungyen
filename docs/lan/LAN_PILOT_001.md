# LAN-PILOT-001 — BETA FEASIBILITY GATE

Status: `OWNER-APPROVED / PRIORITY GATE`
Approved: 2026-09-11 Asia/Ho_Chi_Minh

## Mục tiêu

Kiểm chứng sớm khả năng vận hành LAN thực tế trước khi đầu tư sâu vào business build. Pilot phải tạo ra ba artifact có thể tiếp tục nâng cấp, không phải prototype bỏ đi:

1. Android/PDA BETA test app.
2. Windows LAN Agent BETA + tray/settings console.
3. Website nội bộ BETA tại `beta-lan.supra.cc.cd` khi internal DNS/routing được cấu hình.

Nếu pilot PASS, chính các artifact này được mở rộng dần thành Android app, LAN Agent và LAN Web chính thức.

## Phạm vi test bắt buộc

### Kết nối và chuyển mode tự động

PDA phải tự nhận biết LAN BETA khả dụng và tự chuyển sang LAN mode mà không cần thao tác người dùng thường xuyên.

State tối thiểu:

- `CLOUD_ONLY`
- `LAN_AVAILABLE`
- `LAN_ACTIVE`
- `LAN_LOST`
- `RECONNECTING`
- `LOCAL_QUEUE_ONLY`

LAN discovery/health phải kiểm chứng đúng service identity + environment BETA, không chỉ kiểm tra host có trả HTTP hay không. Có hysteresis/backoff để tránh flapping khi Wi-Fi chập chờn. UI phải hiển thị rõ transport hiện tại và lý do fallback.

Pilot phải test:

- app start trong LAN;
- app start ngoài LAN;
- vào LAN sau khi app đã chạy;
- rời LAN khi app đang chạy;
- Internet mất nhưng LAN còn;
- LAN mất nhưng Internet còn;
- cả LAN và Internet mất;
- LAN trở lại;
- Wi-Fi reconnect/chuyển access point nếu điều kiện thực tế cho phép.

### Điều kiện thiết bị vật lý

Owner có tối đa khoảng 3 PDA vật lý cùng lúc + 1 laptop.

Do đó test được chia thành hai lớp:

- **Physical LAN test:** 1, 2 và 3 PDA đồng thời để đo hành vi Wi-Fi/LAN thực tế, reconnect, auto-LAN, ổn định phiên và event loss.
- **Synthetic service load:** laptop tạo logical clients/request streams để test capacity của LAN Agent ở mức 10/25/50/100+ clients. Synthetic load không được dùng để tuyên bố đã kiểm chứng RF/Wi-Fi với số PDA tương ứng.

## Metrics bắt buộc

Ghi và hiển thị tối thiểu:

- connect success rate;
- request success/error rate;
- p50/p95/p99 latency;
- requests/sec;
- active connections;
- reconnect time;
- queue pending/age;
- queue recovery time;
- event loss;
- duplicate/canonical conflict count khi có event contract;
- LAN Agent CPU/RAM;
- local DB size/growth;
- service uptime/restart count;
- app/service/web versions.

Không được PASS nếu có mất event không giải thích được hoặc retry/reconnect không deterministic.

## Android update contract

Ngay từ pilot phải có cả hai đường cập nhật:

1. **Automatic discovery/notification:** app tự kiểm tra metadata phiên bản và hiển thị có bản mới.
2. **Manual update:** trong Settings có `Kiểm tra cập nhật`, xem current/latest version, tải/cài bản được chọn khi policy cho phép.

Manual update phải tồn tại độc lập với automatic flow để vẫn cập nhật được nếu automatic notification/check bị lỗi. Mọi package phải verify signer/hash trước khi coi là hợp lệ. Android thường có thể cần người dùng xác nhận bước cài đặt nếu thiết bị không được quản trị Device Owner/MDM.

## Windows LAN Agent UX/runtime contract

LAN Agent không được chỉ là một EXE ẩn không có giao diện quản trị.

Thiết kế ưu tiên tách trách nhiệm:

- **Background service:** chạy khi Windows khởi động, xử lý local API/cache/queue/sync/health; tối ưu idle CPU/RAM.
- **Tray/settings console:** icon taskbar/system tray cho người vận hành xem trạng thái và chỉnh cấu hình cơ bản. Đóng cửa sổ console không được dừng background service.

Tray/settings tối thiểu có:

- trạng thái Service/LAN/Internet/Cloud;
- hostname/IP/port đang phục vụ;
- số PDA/client đang kết nối;
- latency/error cơ bản;
- queue pending;
- CPU/RAM cơ bản;
- current service version + latest available version;
- `Kiểm tra cập nhật` thủ công;
- automatic update notification;
- install/update action có verify + rollback nếu health fail;
- chọn/thay đổi thư mục local database/data directory bằng flow an toàn;
- mở thư mục log;
- start/stop/restart service có kiểm soát;
- diagnostics/export log tối thiểu.

Không hiển thị hoặc lưu plaintext secret trong UI/config/log.

## Resource budget mục tiêu

Pilot phải đo trên laptop thật, không chỉ ước lượng. Mục tiêu là footprint nhỏ nhất hợp lý; mọi regression CPU/RAM phải được ghi lại trong test report.

Ưu tiên:

- idle CPU gần 0, tránh polling dày;
- event/timer driven khi có thể;
- bounded logs/cache;
- SQLite/local DB hoặc lựa chọn tương đương nhẹ;
- tray refresh ở nhịp thấp và chỉ tăng khi cửa sổ diagnostics đang mở;
- background service phải tiếp tục hoạt động khi tray UI không mở.

Không khóa trước một framework chỉ vì tiện phát triển; framework/runtime phải được chọn sau khi so sánh footprint, maintainability và khả năng service/tray/update trên Windows.

## LAN Web BETA

`beta-lan.supra.cc.cd` chỉ resolve/route trong LAN khi internal DNS được setup. Website pilot phục vụ từ LAN Agent/local web host và tối thiểu hiển thị:

- service health;
- connected PDA;
- request/latency/error metrics;
- queue/backlog;
- CPU/RAM/local DB;
- transport/Internet status;
- version/build information;
- test controls phù hợp cho BETA.

Web pilot phải được thiết kế để sau này cập nhật thành Web nghiệp vụ mà không thay transport contract.

## Update contract cho LAN Agent

Ngay từ pilot có automatic update discovery/notification và manual update. Update flow mục tiêu:

`manifest -> download -> verify -> stage -> stop service -> replace -> start -> health check -> commit | rollback`

Updater không được phá local DB/config/log hoặc để service ở trạng thái half-updated. BETA và STABLE dùng channel/artifact riêng.

## Gate PASS/FAIL

Pilot chỉ PASS khi tối thiểu:

- 3 PDA vật lý có thể đồng thời kết nối và auto-enter LAN mode đúng policy;
- chuyển LAN/cloud/local queue không làm mất event test;
- reconnect ổn định qua các kịch bản mất/khôi phục mạng;
- synthetic load cho thấy LAN Agent còn headroom rõ ràng so với 3 PDA thật;
- service restart/laptop restart có recovery xác định;
- updater automatic notification + manual update path đều hoạt động;
- tray/settings đáp ứng vận hành nhưng background footprint vẫn thấp;
- LAN Web hiển thị metrics/status nhất quán;
- toàn bộ test result được lưu làm evidence trước khi mở business build sâu.

Nếu FAIL, sửa architecture/transport/LAN runtime trước. Không che FAIL bằng cách chuyển toàn bộ nghiệp vụ về Cloud rồi coi LAN đã đạt.

## Dependency sau PASS

Sau LAN-PILOT PASS, tiếp tục song song:

- CORE/AUTH/API;
- Sheets/projection;
- nâng Android pilot thành business APK;
- nâng LAN Agent pilot thành business LAN service;
- nâng LAN Web pilot thành operational/admin Web;
- test E2E/soak/stress.

STABLE vẫn cần BETA gate + Owner approval.