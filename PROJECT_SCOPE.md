# PROJECT SCOPE — VẬN HÀNH DC HƯNG YÊN

Status: ACTIVE / OWNER-APPROVED 2026-09-11

## 1. Dự án chính

`VẬN HÀNH DC HƯNG YÊN` (VHDCHY) là nền tảng bao quát vận hành DC Hưng Yên. Dự án không đồng nhất với Pick Pack 1291 và không bị giới hạn bởi phạm vi của dự án Pick Pack 1291 cũ.

## 2. Cụm/module đầu tiên

`Pick Pack 1291` là một cluster/module đầu tiên trong VHDCHY. Dự án Pick Pack 1291 cũ chỉ là nguồn tham khảo để tái sử dụng có chọn lọc những phần đã chứng minh hữu ích.

## 3. Pick Pack 1291 reference policy

Folder Drive `BACKUP PICK PACK 1291` bên trong `VẬN HÀNH DC HƯNG YÊN` là reference archive read-only.

Nguyên tắc cứng:

- Pick Pack 1291 là evidence/reference, không phải authority của VHDCHY.
- Không clone 100%.
- Không mặc định hành vi/schema/kiến trúc cũ là yêu cầu VHDCHY.
- Pattern tham khảo phải được đánh giá theo `REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED`.
- Chỉ `ADOPTED` hoặc `ADAPTED` mới trở thành thiết kế VHDCHY.
- Dự án cũ không được trở thành runtime dependency, fallback target, authority data source hoặc implicit scope definition của VHDCHY.

## 4. Authority hierarchy

Khi có mâu thuẫn, áp dụng thứ tự:

1. Quyết định mới nhất của Owner.
2. `PROJECT_SCOPE.md`.
3. `DECISIONS.md` và decision record hiện hành.
4. `CURRENT_STATE.md`.
5. Spec/architecture hiện hành của VHDCHY.
6. Spec/module hiện hành.
7. Pick Pack 1291 reference digest/index.
8. File lịch sử/backup cũ.

Không dùng nguồn cấp thấp để ghi đè nguồn cấp cao hơn.

## 5. Phạm vi hệ thống và dịch vụ

- Source authority: GitHub `tamnv2/vanhanhdchungyen`.
- Google owner/runtime account: `vanhanhdchungyen@gmail.com`.
- Cloudflare zone: `supra.cc.cd`.
- BETA/STABLE dùng Worker, D1, GAS, Drive runtime và secrets độc lập.
- Google Cloud/OAuth/GAS tách theo BETA/STABLE.
- Google Drive chứa runtime data, archive/media/export/backup theo thiết kế hiện hành.
- Google Sheets là thành phần dữ liệu/projection/human-readable/DR theo module; không mặc định là authority nếu spec không nói vậy.
- Android/PDA là client native chính cho nghiệp vụ phù hợp.
- Windows LAN Agent + LAN Web là deliverable chính thức của VHDCHY. Trước business build sâu, BETA phải chạy `LAN-PILOT-001` để kiểm chứng auto-LAN trên PDA, kết nối/ổn định/độ trễ/chịu tải/reconnect, update flow và footprint Windows thực tế.
- Điều kiện test vật lý hiện có: tối đa khoảng 3 PDA Newland MT90 đồng thời + 1 laptop công ty khoảng 2 core/4 thread, 8 GB RAM.
- Laptop được coi là môi trường minimum-information/no-admin: không giả định quyền Administrator, Windows Service, firewall rule, route, Wi-Fi/AP/router hay internal DNS. Pilot không được yêu cầu Owner bypass policy bảo mật công ty.
- LAN Agent pilot là portable per-user background process có tray/settings, manifest `asInvoker`; không phải SCM Windows Service. Đóng UI không dừng Agent; dữ liệu mặc định nằm trong vùng user có quyền ghi và có thể đổi sang thư mục writable khác với verify/rollback.
- Android và LAN Agent đều phải có automatic update discovery/notification và manual update fallback độc lập; LAN Agent self-update về sau không được phụ thuộc Administrator.
- Synthetic load trên laptop được dùng để đo service capacity nhưng không thay thế bằng chứng Wi-Fi/RF của PDA thật.
- LAN hostname không public DNS. Trong feasibility pilot, `beta-lan.supra.cc.cd` không phải dependency: PDA dùng cached endpoint + discovery và Web dùng LAN IP/port. Hostname LAN chỉ được gắn nếu internal DNS thực sự khả dụng mà không cần phá policy mạng công ty.
- Nếu inbound/discovery bị corporate firewall/AppLocker/AP isolation chặn và không thể xử lý trong quyền hợp lệ của user thường, đó là kết quả feasibility của pilot, không phải lý do để yêu cầu nâng quyền.
- LAN HA/Master-Backup/fencing nâng cao chỉ áp dụng sau single-node LAN pilot nếu measurement/use case chứng minh cần.
- Durable Objects/R2/dịch vụ bổ sung chỉ bật khi có use case rõ ràng.

## 6. Môi trường

### BETA

AI được phép phát triển, test, migrate, deploy và sửa tự động tối đa trong phạm vi Owner đã cấp. BETA phải luôn trỏ tới known-good live commit gần nhất. `LAN-PILOT-001` là priority feasibility gate trước khi đầu tư sâu vào business implementation.

### STABLE

Chỉ promote khi BETA gate đạt và Owner duyệt theo release gate hiện hành. Không tự suy diễn quyền promote.

## 7. Secrets

Không ghi secret/private key/token/password/keystore base64 vào repo public, Drive docs, changelog hoặc chat. Secrets chỉ nằm trong GitHub Environment/provider secret store hoặc Owner-controlled encrypted backup.
