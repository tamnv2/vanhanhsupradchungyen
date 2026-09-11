# PROJECT SCOPE — VẬN HÀNH DC HƯNG YÊN

Status: ACTIVE / OWNER-APPROVED 2026-09-11

## 1. Dự án chính

`VẬN HÀNH DC HƯNG YÊN` (VHDCHY) là nền tảng bao quát vận hành DC Hưng Yên. Dự án không đồng nhất với Pick Pack 1291 và không bị giới hạn bởi phạm vi của dự án Pick Pack 1291 cũ.

## 2. Cụm/module đầu tiên

`Pick Pack 1291` là một cluster/module đầu tiên trong VHDCHY. Dự án Pick Pack 1291 cũ chỉ là nguồn tham khảo để tái sử dụng có chọn lọc những phần đã chứng minh hữu ích.

## 3. Pick Pack 1291 reference policy

Folder Drive `BACKUP DỰ ÁN CŨ PICK PACK 1291` bên trong `VẬN HÀNH DC HƯNG YÊN` là reference archive read-only.

Nguyên tắc cứng:

- Pick Pack 1291 là evidence/reference, không phải authority của VHDCHY.
- Không clone 100%.
- Không mặc định hành vi/schema/kiến trúc cũ là yêu cầu VHDCHY.
- Pattern tham khảo phải được đánh giá theo `REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED`.
- Chỉ `ADOPTED` hoặc `ADAPTED` mới trở thành thiết kế VHDCHY.
- Dự án cũ không được trở thành runtime dependency, fallback target, authority data source hoặc implicit scope definition của VHDCHY.
- Legacy account/resource IDs trong folder này không phải current config.

## 4. Authority hierarchy

Khi có mâu thuẫn, áp dụng thứ tự:

1. Quyết định mới nhất của Owner.
2. `PROJECT_SCOPE.md`.
3. `SERVICE_AUTHORITY.md` cho account/provider/resource identity.
4. `DECISIONS.md` và decision record hiện hành.
5. `CURRENT_STATE.md`.
6. Spec/architecture hiện hành của VHDCHY.
7. Spec/module hiện hành.
8. Pick Pack 1291 reference digest/index.
9. File lịch sử/backup cũ.

Không dùng nguồn cấp thấp để ghi đè nguồn cấp cao hơn.

## 5. Phạm vi hệ thống và dịch vụ

- Source authority repo hiện hành: GitHub `tamnv2/vanhanhsupradchungyen`.
- Old repo `tamnv2supra/vanhanhdchungyen`: migration/reference source trong thời gian đối soát; không phải writable authority sau khi repo mới qua restoration gate.
- Google owner/runtime account hiện hành: `automation@supra.cc.cd`.
- `vanhanhdchungyen@gmail.com`: DECOMMISSIONED / không được sử dụng lại.
- Cloudflare zone: `supra.cc.cd`; setup Cloudflare/domain hiện hữu được giữ nguyên vì Owner chỉ thay login/email, không rebuild provider resources.
- BETA/STABLE dùng Worker, D1, GAS, Drive runtime và secrets độc lập.
- Google Cloud/OAuth/GAS tách theo BETA/STABLE.
- Google Drive chứa runtime data, archive/media/export/backup theo thiết kế hiện hành.
- Google Sheets là thành phần dữ liệu/projection/human-readable/DR theo module; không mặc định là authority nếu spec không nói vậy.
- Android/PDA là client native chính cho nghiệp vụ phù hợp.
- Windows LAN Agent + LAN Web là deliverable chính thức của VHDCHY. Trước business build sâu, BETA phải hoàn tất LAN Pilot feasibility gate.
- Điều kiện test vật lý hiện có: **chính xác 2 PDA Newland MT90** + 1 laptop công ty; synthetic clients chỉ bổ sung capacity evidence, không thay thế RF/Wi-Fi evidence vật lý.
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

AI được phép phát triển, test, migrate, deploy và sửa tự động tối đa trong phạm vi Owner đã cấp. Trong giai đoạn account/provider recovery, không move live BETA pointer cho tới khi GitHub Environment + Google Drive/GAS/OAuth BETA + provider verification đều PASS.

### STABLE

Chỉ restore/promote khi BETA gate đạt và Owner duyệt theo release gate hiện hành. Không tự suy diễn quyền promote. Không copy BETA IDs/secrets sang STABLE.

## 7. Secrets

Không ghi secret/private key/token/password/keystore base64 vào repo public, Drive docs, changelog hoặc chat. Secrets chỉ nằm trong GitHub Environment/provider secret store hoặc Owner-controlled encrypted backup.
