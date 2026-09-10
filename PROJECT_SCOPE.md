# PROJECT SCOPE — VẬN HÀNH DC HƯNG YÊN

Status: ACTIVE / OWNER-APPROVED 2026-09-10

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
- LAN Agent/Windows service chỉ triển khai khi module hoặc measurement gate yêu cầu.
- LAN hostname không public DNS.
- Durable Objects/R2/dịch vụ bổ sung chỉ bật khi có use case rõ ràng.

## 6. Môi trường

### BETA

AI được phép phát triển, test, migrate, deploy và sửa tự động tối đa trong phạm vi Owner đã cấp. BETA phải luôn trỏ tới known-good live commit gần nhất.

### STABLE

Chỉ promote khi BETA gate đạt và Owner duyệt theo release gate hiện hành. Không tự suy diễn quyền promote.

## 7. Secrets

Không ghi secret/private key/token/password/keystore base64 vào repo public, Drive docs, changelog hoặc chat. Secrets chỉ nằm trong GitHub Environment/provider secret store hoặc Owner-controlled encrypted backup.
