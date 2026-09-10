# LAN-PILOT-001 — BETA FEASIBILITY GATE

Status: `OWNER-APPROVED / PRIORITY GATE`
Approved: 2026-09-11 Asia/Ho_Chi_Minh
Protocol: `docs/lan/LAN_PROTOCOL_V1.md`

## Mục tiêu

Kiểm chứng LAN trong đúng điều kiện Owner thực sự có trước khi đầu tư sâu vào business build. Ba artifact pilot phải được giữ làm lineage chính thức, không phải prototype bỏ đi:

1. Android/PDA BETA test app — mục tiêu vật lý đầu tiên Newland MT90.
2. Windows LAN Agent BETA — portable user-mode + tray/settings, không yêu cầu Administrator.
3. LAN diagnostics Web — phục vụ trực tiếp từ Agent bằng IP/port trong pilot; `beta-lan.supra.cc.cd` chỉ dùng sau này nếu internal DNS có sẵn.

Nếu pilot PASS, ba artifact này được nâng dần thành Android app, LAN Agent và LAN Web nghiệp vụ.

## Điều kiện thực tế bắt buộc phải giả định

### Laptop công ty

- CPU cũ tiết kiệm điện khoảng 2 core / 4 thread.
- RAM 8 GB.
- User thường, không có quyền Administrator.
- Quyền hệ thống bị hạn chế; Owner không có quyền xem/sửa router, route Wi-Fi, AP hoặc DNS nội bộ.
- Không giả định được phép tạo firewall rule, cài Windows Service, driver, certificate hay thay network policy.
- Không yêu cầu Owner bypass security policy để làm pilot PASS.

Nếu executable, inbound TCP/UDP, broadcast discovery hoặc auto-start bị corporate policy chặn thì phải ghi đúng là evidence feasibility, không che bằng workaround cần quyền admin.

### PDA

- Có tối đa khoảng 3 PDA Newland MT90 dùng đồng thời với laptop.
- Chưa khóa scanner SDK trong pilot; test dùng Android API phổ thông để giảm phụ thuộc exact MT90 hardware/OS revision.
- Physical test 1/2/3 PDA là evidence Wi-Fi/LAN thật.
- Synthetic logical clients trên laptop dùng để test service capacity 10/25/50/100+, không được coi là evidence RF/Wi-Fi cho số PDA tương ứng.

## Kết nối và tự bật LAN mode

PDA phải tự phát hiện LAN BETA hợp lệ và tự chuyển LAN mode mà không cần thao tác thường xuyên.

State tối thiểu:

- `CLOUD_ONLY`
- `LAN_AVAILABLE`
- `LAN_ACTIVE`
- `LAN_LOST`
- `RECONNECTING`
- `LOCAL_QUEUE_ONLY`

Discovery không phụ thuộc quyền router/DNS. Thứ tự pilot:

1. thử cached LAN endpoint;
2. UDP broadcast discovery;
3. manual endpoint/IP làm recovery/diagnostic path;
4. hostname `beta-lan.supra.cc.cd` chỉ khi internal DNS thật sự khả dụng sau này.

PDA bắt buộc gọi `/health` và xác minh `service + environment + protocol`; không tự chuyển LAN chỉ vì có host trả HTTP. Có hysteresis/backoff để tránh flapping.

Pilot phải test:

- app start khi đang cùng LAN với laptop;
- app start ngoài LAN;
- vào/rời LAN khi app đang chạy;
- Internet mất nhưng LAN còn;
- LAN mất nhưng Internet còn;
- cả LAN và Internet mất;
- LAN trở lại;
- Agent restart;
- laptop restart/user login lại;
- app/PDA restart;
- Wi-Fi reconnect/chuyển AP nếu điều kiện thực tế cho phép.

## Windows LAN Agent — low privilege contract

Không cài SCM Windows Service trong pilot vì laptop không có quyền admin.

Runtime mặc định:

- portable per-user EXE, manifest `asInvoker`;
- background/tray application sau khi user đăng nhập;
- không ghi HKLM/Program Files;
- không tạo firewall rule;
- optional auto-start chỉ dùng HKCU nếu corporate policy cho phép;
- default data dưới `%LOCALAPPDATA%`;
- cho phép chọn data directory khác nếu user có write permission;
- local API/Web dùng high user-space port, mặc định TCP `17891`;
- UDP discovery mặc định `17892`;
- đóng dashboard không dừng Agent; `Thoát Agent` là thao tác riêng.

Tray/settings tối thiểu:

- Agent/LAN/Internet/Cloud status;
- IP/port đang phục vụ;
- PDA/client count;
- latency/error/queue;
- process RAM/CPU khi hoàn thiện metrics;
- current/latest version;
- `Kiểm tra cập nhật` thủ công;
- automatic update notification;
- local database/data directory selection có verify/rollback;
- mở data/log/diagnostics;
- restart/exit có kiểm soát.

## Local database / queue

Pilot phải có local durable store nhẹ để kiểm chứng retry/idempotency và event-loss behavior. Baseline dùng SQLite WAL. Chỉ test event không nhạy cảm.

Đổi data directory phải theo flow:

`stop listener -> copy -> integrity_check -> switch pointer -> restart -> rollback on failure`.

## LAN Web BETA

Trong feasibility pilot, Web dùng trực tiếp:

`http://<laptop-lan-ip>:17891/`

Không yêu cầu DNS/hosts/router change.

Dashboard tối thiểu hiển thị:

- service health/identity/version;
- địa chỉ LAN đang có;
- connected PDA/device count;
- requests/errors;
- p50/p95/p99 latency;
- event count/duplicate count;
- RAM/local DB size;
- update check;
- data directory.

Sau khi LAN được chứng minh và nếu internal DNS khả dụng, mới gắn `beta-lan.supra.cc.cd`; STABLE tương ứng mới dùng `lan.supra.cc.cd`.

## Android update contract

Ngay từ pilot phải có hai đường độc lập:

1. automatic release discovery/notification;
2. manual `Kiểm tra cập nhật`/mở package release để recovery.

APK BETA build phải dùng BETA signing key. Trước gate PASS phải kiểm chứng download/install/update thật trên MT90; Android có thể yêu cầu user xác nhận cài package nếu không có Device Owner/MDM.

## LAN Agent update contract

Ngay từ pilot phải có automatic release discovery và manual update fallback. Self-replace/updater hoàn chỉnh phải tuân theo:

`manifest/release -> download -> verify hash/signature -> stage -> stop Agent -> replace -> start -> health -> commit | rollback`.

Không phụ thuộc Administrator. Nếu thư mục app không writable hoặc endpoint security chặn replace, updater phải fail an toàn và manual package vẫn tồn tại. DB/config/log không được ghi đè bởi update.

## Metrics và resource budget

Bắt buộc đo trên laptop thật. Không tuyên bố footprint bằng ước lượng từ máy build.

Theo dõi:

- connect success rate;
- request success/error rate;
- p50/p95/p99;
- requests/sec;
- active devices;
- reconnect time;
- pending queue/queue recovery;
- event loss/duplicate count;
- Agent RAM/CPU;
- DB/log growth;
- uptime/restart count;
- app/Agent/Web versions.

Tối ưu:

- idle CPU gần 0, event/timer-driven;
- tránh polling dày;
- bounded metrics/logs/cache;
- UI refresh chậm khi không mở diagnostics;
- ưu tiên footprint phù hợp laptop 2C/4T + 8 GB RAM.

## Gate PASS/FAIL

Pilot chỉ PASS khi tối thiểu:

- Agent chạy được bằng user thường trong policy hiện có;
- 3 PDA vật lý cùng kết nối và tự vào `LAN_ACTIVE` đúng policy;
- xác định rõ discovery có hoạt động hay không mà không thay router/DNS;
- LAN/cloud/local queue chuyển trạng thái deterministic và không mất test event;
- restart/reconnect có recovery xác định;
- synthetic load cho thấy headroom rõ ràng trên laptop thật;
- automatic + manual update path có thể sử dụng; self-update Agent phải được test trước PASS;
- dashboard/tray vận hành được với footprint chấp nhận được;
- evidence report phân biệt lỗi app, firewall/policy, AP isolation và network instability.

Nếu corporate policy chặn inbound LAN tới mức PDA không thể kết nối laptop mà không cần quyền admin/network change, `LAN-PILOT` phải kết luận FAIL/NOT-FEASIBLE cho mô hình laptop-as-LAN-Agent trong điều kiện hiện tại, rồi mới đề xuất kiến trúc khác cho Owner.

## Sau PASS

Tiếp tục song song:

- CORE/AUTH/API;
- Sheets/projection;
- nâng Android pilot thành business APK;
- nâng user-mode LAN Agent thành business LAN runtime;
- nâng LAN Web thành operational/admin Web;
- E2E/stress/soak;
- chỉ đánh giá Windows Service thật/LAN HA nếu sau này có quyền hoặc use case chứng minh cần.

STABLE vẫn cần BETA gate + Owner approval.