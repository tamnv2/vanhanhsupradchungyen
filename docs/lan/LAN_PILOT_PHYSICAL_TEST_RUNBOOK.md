# VHDCHY LAN PILOT — PHYSICAL TEST RUNBOOK

Status: `READY FOR FIRST PHYSICAL FEASIBILITY TEST`
Scope: `BETA / TEST PAYLOAD ONLY`
Target: restricted corporate Windows laptop + 1→2→3 Newland MT90

## 0. Nguyên tắc an toàn

- Không dùng `Run as administrator`.
- Không sửa firewall, route, Wi-Fi, AP/router, DNS, certificate hoặc policy công ty.
- Không tắt antivirus/EDR/AppLocker.
- Không dùng dữ liệu nhân sự, mật khẩu, token hoặc dữ liệu nghiệp vụ thật trong pilot.
- Nếu Windows/PDA/policy chặn một bước, ghi lại đúng lỗi và dừng bước đó. Không bypass policy.
- Pilot dùng LAN IP; không cần `beta-lan.supra.cc.cd`.

## 1. Artifact chuẩn

Dùng cùng một GitHub prerelease LAN Pilot BETA. Mỗi release cần tối thiểu:

- `VHDCHY-LAN-Agent-BETA-win-x64.zip`
- `VHDCHY-LAN-Agent-BETA-win-x64.sha256`
- `VHDCHY-LAN-Pilot-BETA.apk`
- `VHDCHY-LAN-Pilot-BETA.apk.sha256`

Ghi tag release vào kết quả test.

## 2. Laptop — khởi động Agent bằng user thường

1. Tải/copy ZIP vào thư mục user được phép ghi, ví dụ `Downloads` hoặc thư mục làm việc cá nhân được công ty cho phép.
2. Giải nén ZIP.
3. Chạy `VHDCHY.LanAgent.exe` bằng double-click bình thường.
4. Không chọn `Run as administrator`.
5. Nếu Windows/EDR/AppLocker chặn executable: chụp/ghi chính xác thông báo, đánh dấu `EXECUTION_POLICY_BLOCKED`, không bypass.
6. Nếu chạy được, kiểm tra icon `VHDCHY LAN Agent BETA` trong system tray.
7. Double-click tray icon để mở local dashboard.
8. Trên chính laptop, `http://127.0.0.1:17891/` phải mở được.
9. Dashboard sẽ liệt kê một hoặc nhiều URL dạng `http://<LAN-IP>:17891/`. Ghi lại IP tương ứng Wi-Fi đang dùng.

### Kiểm tra quyền dữ liệu

- Mặc định Agent dùng `%LOCALAPPDATA%\VHDCHY\LanAgentBeta`.
- Thử `Chọn thư mục dữ liệu...` chỉ với một thư mục user được phép ghi.
- Nếu policy không cho ghi: giữ default; không nâng quyền.

### Auto-start

- Chỉ thử `Bật/tắt tự khởi động theo user` nếu cần.
- Nếu HKCU startup bị policy chặn, ghi `USER_AUTOSTART_BLOCKED`; đây không phải lý do nâng quyền.

## 3. PDA #1 — xác minh auto-LAN

1. Cài `VHDCHY-LAN-Pilot-BETA.apk` theo cơ chế cài app mà MT90 hiện cho phép.
2. Nếu managed-device policy chặn APK sideload/update, ghi chính xác lỗi và đánh dấu `APK_INSTALL_POLICY_BLOCKED`.
3. Kết nối PDA vào cùng Wi-Fi mà laptop đang sử dụng; không thay cấu hình AP/router.
4. Mở app.
5. Ghi thông tin app hiển thị: hãng/model/Android version/device ID.
6. Không nhập IP trước. Chờ ít nhất 15 giây để kiểm tra auto-discovery.
7. Kết quả mong đợi nếu UDP discovery hoạt động:
   - `LAN_AVAILABLE`
   - sau hysteresis chuyển `LAN_ACTIVE`.
8. Bấm `Test Echo / Latency` 10 lần; ghi mức latency điển hình và lỗi nếu có.
9. Bấm `Gửi test event durable` 10 lần.
10. Dashboard laptop phải thấy PDA active và event count tăng; duplicate không tăng ngoài retry có chủ đích.

## 4. Nếu auto-discovery không hoạt động — phân loại, không sửa mạng

Không kết luận LAN fail ngay.

1. Lấy LAN IP hiển thị trên dashboard laptop, ví dụ `192.168.x.x`.
2. Trong app MT90, nhập manual endpoint:
   `http://<LAN-IP>:17891`
3. Bấm `Lưu endpoint thủ công`.
4. Chờ health verification.

Diễn giải:

- Manual IP PASS nhưng UDP discovery FAIL → `DISCOVERY_BLOCKED_OR_FILTERED`; TCP LAN vẫn khả thi.
- Manual IP FAIL và laptop local dashboard PASS → nghi `INBOUND_TCP_BLOCKED` hoặc `PDA_AP_ISOLATION`.
- Cả laptop local dashboard FAIL → Agent/port local issue.
- Không yêu cầu Owner sửa firewall/router để biến FAIL thành PASS.

## 5. Test 1 PDA — network transitions

Khi PDA đang `LAN_ACTIVE`, lần lượt kiểm thử các tình huống có thể thực hiện mà không sửa policy:

### A. Restart app

- force close/đóng app rồi mở lại;
- cached endpoint phải được thử trước;
- LAN phải recover deterministic.

### B. Restart Agent

- từ tray chọn restart Agent;
- PDA phải chuyển khỏi `LAN_ACTIVE`, sau đó reconnect khi Agent trở lại;
- không mất test event pending.

### C. Wi-Fi PDA off/on

- tắt Wi-Fi trên PDA trong khoảng 15–30 giây nếu policy cho phép;
- tạo một số test event local khi mất kết nối;
- bật lại Wi-Fi;
- queue phải flush khi LAN_ACTIVE trở lại.

### D. Internet availability

Nếu có cách hợp lệ để Internet trên PDA mất trong khi LAN vẫn còn mà không sửa router/policy, kiểm tra LAN tiếp tục hoạt động. Nếu không thể tạo điều kiện này bằng quyền hiện có, đánh dấu `NOT_TESTABLE_IN_CURRENT_ENV`, không tự suy đoán.

## 6. PDA #2 và #3

Lặp lại bước 3–5 với PDA #2, sau đó PDA #3.

Với 3 PDA đồng thời:

- cả ba phải có device ID riêng;
- cả ba ở `LAN_ACTIVE` trong cùng khoảng test;
- dashboard laptop `activeDevices60s` phải phản ánh ba thiết bị khi chúng gửi request;
- mỗi PDA gửi ít nhất 50 echo + 50 durable test event trong phiên đồng thời;
- quan sát p50/p95/p99, errors, duplicates, RAM và DB growth.

Không tuyên bố `3 PDA PASS` nếu chỉ có 1–2 thiết bị được test thật.

## 7. Synthetic service load trên laptop

Trong ZIP Agent có LoadGen ở thư mục `tools`.

Chạy trên chính laptop, target loopback để đo software/service capacity không phụ thuộc Wi-Fi:

```text
VHDCHY.LanLoadGen.exe http://127.0.0.1:17891 10 100
VHDCHY.LanLoadGen.exe http://127.0.0.1:17891 25 100
VHDCHY.LanLoadGen.exe http://127.0.0.1:17891 50 100
VHDCHY.LanLoadGen.exe http://127.0.0.1:17891 100 100
```

Nếu một mức bắt đầu error/resource saturation thì dừng tăng tải, lưu kết quả. Không cần cố 500 client.

Synthetic result chỉ trả lời `LAN Agent software capacity`, không trả lời Wi-Fi có chịu cùng số PDA hay không.

## 8. Footprint laptop

Trong khi Agent idle ít nhất 5 phút:

- ghi RAM của `VHDCHY.LanAgent.exe` từ Task Manager hoặc dashboard;
- ghi CPU idle từ Task Manager nếu Agent version chưa có CPU metric nội bộ;
- sau đó ghi CPU/RAM khi 3 PDA active;
- ghi CPU/RAM trong từng load test 10/25/50/100.

Không kết luận footprint từ GitHub runner.

## 9. Update tests

### Android

- bấm `Kiểm tra cập nhật` thủ công;
- xác minh app vẫn có manual release path ngay cả khi automatic check không chạy;
- khi có version mới, test update/install thật trên MT90.

### LAN Agent

- tray → `Kiểm tra cập nhật`;
- automatic release notification phải được kiểm tra khi có release mới;
- current first connectivity build có manual package recovery;
- actual staged self-replace + health rollback phải được kiểm chứng ở một build sau trước khi final LAN-PILOT PASS.

## 10. Laptop restart/login

Chỉ kiểm thử theo quyền user hiện có:

- nếu HKCU auto-start được phép, reboot/login và xác minh Agent tự chạy;
- nếu không được phép, mở Agent thủ công và ghi `USER_AUTOSTART_BLOCKED`;
- không cài Windows Service hoặc Scheduled Task cần elevation để né hạn chế.

## 11. Kết luận feasibility

### Có thể tiếp tục LAN architecture

Khi:

- Agent chạy user-mode ổn;
- PDA có đường LAN TCP hợp lệ bằng auto-discovery hoặc ít nhất manual-IP diagnostic chứng minh transport khả thi;
- 3 PDA thật cùng hoạt động nếu đủ 3 máy;
- reconnect/queue không mất event;
- footprint laptop chấp nhận được;
- synthetic load có headroom rõ trên tải thật.

### Chưa PASS nhưng có thể sửa phần mềm

Ví dụ:

- timeout/hysteresis chưa tốt;
- app/Agent crash;
- queue/duplicate bug;
- package/update bug;
- performance phần mềm chưa đạt.

### NOT-FEASIBLE dưới policy hiện tại

Ví dụ:

- corporate policy chặn chạy Agent hoàn toàn;
- inbound PDA→laptop bị chặn và user không có quyền hợp lệ để mở;
- AP client isolation khiến PDA không thể tới laptop;
- managed PDA policy không cho cài/chạy app test.

Trong các trường hợp này không yêu cầu Owner bypass bảo mật; kiến trúc phải đổi.