# VHDCHY LAN PILOT PROTOCOL V1

Status: `IMPLEMENTATION BASELINE / BETA ONLY`
Environment: `BETA`
Protocol ID: `VHDCHY_LAN_PILOT_V1`

## 1. Mục tiêu

Protocol này chỉ dùng để kiểm chứng feasibility LAN trước khi đưa dữ liệu nghiệp vụ thật vào LAN. Pilot không được chứa dữ liệu nhạy cảm hoặc credential nghiệp vụ.

## 2. Ràng buộc laptop công ty

Thiết kế mặc định giả định trường hợp hạn chế nhất hợp lý:

- Windows corporate laptop;
- user thường, không có Administrator;
- không được giả định có quyền cài Windows Service, driver, firewall rule, certificate, route, DNS hoặc thay đổi cấu hình Wi-Fi/router/AP;
- không được giả định biết topology/subnet/router/AP;
- CPU mục tiêu cũ, tiết kiệm điện, khoảng 2 core / 4 thread;
- RAM 8 GB;
- app phải portable/user-mode và dữ liệu nằm ở thư mục user có quyền ghi;
- nếu corporate firewall/AppLocker/endpoint policy chặn inbound socket hoặc executable thì ghi nhận đó là evidence của pilot, không yêu cầu Owner phá policy để làm test PASS.

Không scan subnet và không sửa route/DNS để tìm LAN Agent.

## 3. Ràng buộc PDA

Thiết bị vật lý mục tiêu đầu tiên: Newland MT90. Vì chưa xác minh exact OS/hardware revision của từng máy, Android pilot dùng API nền tảng phổ thông, không phụ thuộc scanner SDK và ưu tiên compatibility rộng.

## 4. Cổng pilot

- TCP HTTP local API/Web: `17891`
- UDP discovery: `17892`

Cổng là cấu hình user-space và có thể đổi nếu bị policy/port conflict; không yêu cầu port reservation hệ thống.

## 5. Discovery không phụ thuộc DNS/router

Discovery ưu tiên theo thứ tự:

1. endpoint LAN đã cache và còn health PASS;
2. UDP broadcast discovery request;
3. endpoint/IP nhập thủ công làm recovery path;
4. `beta-lan.supra.cc.cd` chỉ dùng khi sau này internal DNS thực sự có sẵn.

PDA gửi UDP broadcast payload UTF-8:

`VHDCHY_DISCOVER_BETA_V1`

LAN Agent hợp lệ trả unicast JSON về source endpoint:

```json
{
  "ok": true,
  "service": "VHDCHY_LAN_AGENT",
  "environment": "BETA",
  "protocol": "VHDCHY_LAN_PILOT_V1",
  "instanceId": "...",
  "httpPort": 17891,
  "version": "..."
}
```

PDA chỉ đưa candidate vào health verification; discovery response một mình không đủ để bật LAN mode.

Nếu broadcast bị corporate Wi-Fi/AP isolation chặn, pilot phải thể hiện FAIL/DEGRADED đúng sự thật. Manual endpoint vẫn tồn tại để phân biệt lỗi discovery với lỗi TCP/inbound LAN.

## 6. Health identity

`GET /health`

Response tối thiểu:

```json
{
  "ok": true,
  "service": "VHDCHY_LAN_AGENT",
  "environment": "BETA",
  "protocol": "VHDCHY_LAN_PILOT_V1",
  "instanceId": "...",
  "version": "...",
  "httpPort": 17891
}
```

PDA chỉ coi endpoint là LAN BETA hợp lệ khi toàn bộ `ok/service/environment/protocol` khớp.

## 7. Auto-LAN state machine

Các state:

- `CLOUD_ONLY`
- `LAN_AVAILABLE`
- `LAN_ACTIVE`
- `LAN_LOST`
- `RECONNECTING`
- `LOCAL_QUEUE_ONLY`

Policy pilot:

- không chuyển `LAN_ACTIVE` sau một success đơn lẻ;
- cần health success liên tiếp theo hysteresis config;
- không rời LAN sau một timeout đơn lẻ;
- retry dùng bounded backoff;
- UI luôn hiển thị transport thực tế và endpoint đang dùng;
- cached endpoint được thử lại trước khi broadcast;
- manual endpoint không vô hiệu hóa health identity validation.

## 8. Test endpoints

### Echo

`POST /api/pilot/echo`

Dùng đo latency/request path. Payload không chứa business data.

### Durable test event

`POST /api/pilot/event`

Request:

```json
{
  "eventId": "uuid",
  "deviceId": "pilot-device-id",
  "deviceSeq": 1,
  "createdAt": "ISO-8601",
  "payload": "test-only"
}
```

Uniqueness pilot: `(deviceId, deviceSeq)` và `eventId`. Retry cùng event phải trả trạng thái duplicate/idempotent thay vì tạo bản ghi thứ hai.

### Metrics

`GET /api/pilot/metrics`

Tối thiểu cung cấp request/error count, latency sample percentiles, active device count, stored event count, duplicate count, uptime, process RAM và local DB size.

## 9. LAN Web

Pilot Web được phục vụ trực tiếp từ LAN Agent tại:

`http://<laptop-lan-ip>:17891/`

Không yêu cầu `beta-lan.supra.cc.cd` trong giai đoạn feasibility. Hostname chỉ được bật khi internal DNS có sẵn mà không phá policy mạng công ty.

## 10. User-mode Windows Agent

Pilot không cài SCM Windows Service. Runtime là per-user background/tray application:

- không elevation;
- manifest `asInvoker`;
- không ghi HKLM;
- không ghi Program Files;
- không tự tạo firewall rule;
- optional auto-start chỉ dùng HKCU nếu policy cho phép;
- default data nằm dưới `%LOCALAPPDATA%`;
- Owner có thể chọn data directory khác nếu user có write permission;
- đóng dashboard/settings không đồng nghĩa dừng Agent; Exit Agent phải là thao tác riêng.

Nếu inbound TCP/UDP bị Windows/corporate policy chặn và user không có quyền mở, đó là kết quả feasibility quan trọng.

## 11. Update

Android và LAN Agent đều phải có:

- automatic update discovery/notification;
- manual `Kiểm tra cập nhật` độc lập;
- version/channel rõ ràng;
- hash/signature verification trước acceptance.

Self-update LAN Agent không được phụ thuộc quyền Administrator. Nếu current execution directory không writable/policy chặn replace, updater phải fail an toàn và vẫn cung cấp manual package/recovery path.

## 12. Security boundary pilot

- HTTP cleartext local chỉ được dùng cho pilot test payload không nhạy cảm.
- Không truyền credential/business PII trong pilot endpoints.
- Identity string không phải cryptographic authentication.
- Trước business LAN, protocol phải có authenticated/pairing transport phù hợp và threat review.

## 13. Gate interpretation

Pilot tách nguyên nhân lỗi thành ít nhất:

- `DISCOVERY_BLOCKED`
- `INBOUND_TCP_BLOCKED`
- `PDA_AP_ISOLATION`
- `AGENT_UNREACHABLE`
- `AGENT_IDENTITY_INVALID`
- `UNSTABLE_WIFI`
- `CLIENT_FALLBACK_ERROR`
- `RESOURCE_LIMIT`
- `UPDATE_POLICY_BLOCKED`

Không yêu cầu Owner có quyền router/admin để sửa các lỗi này trong pilot. Mục đích là đo xem mô hình có khả thi trong môi trường quyền thật hiện có hay không.