# PROJECT SCOPE — VẬN HÀNH DC HƯNG YÊN

Status: ACTIVE / OWNER-APPROVED 2026-09-12

## 1. Dự án chính

`VẬN HÀNH DC HƯNG YÊN` (VHDCHY) là nền tảng vận hành DC Hưng Yên. `PICK_PACK_1291` là cluster/module đầu tiên, không phải toàn bộ phạm vi hệ thống.

## 2. Baseline reset 2026-09-12

Owner yêu cầu bắt đầu lại phần setup/quyền/provider từ mốc đã chốt logic/kịch bản.

Reset có nghĩa:

- reset current setup/runtime/provider claims về trạng thái cần verify/provision lại;
- không xóa code/logic/architecture/decision/test evidence đã được chốt;
- không tự tái sử dụng old ID/token/deployment/workbook chỉ vì từng chạy thành công;
- resource hiện hữu dưới account mới được **re-use** nếu kiểm chứng đúng owner/quyền/mục đích;
- không làm lại một bước đã được tool xác minh đủ và vẫn phù hợp baseline hiện hành.

Snapshot trước reset: branch `archive/pre-setup-reset-20260912`.

## 3. Current identity scope

- Google Drive: `tam95.supra@gmail.com`.
- Google Sheets: `tam95.supra@gmail.com`.
- Google Apps Script: `tam95.supra@gmail.com`.
- Cloudflare: account/login do `nguyenvantam050595@gmail.com` quản lý.
- GitHub: user `tamnv2`, account email `nguyenvantam050595@gmail.com`.
- Source authority repo: `tamnv2/vanhanhsupradchungyen`.
- `automation@supra.cc.cd`: bị Google khóa/nghi automation; hiện là `SUSPENDED_RECOVERY_CANDIDATE`, không phải current runtime owner. Nếu lấy lại được thì migration từ `tam95.supra@gmail.com` chỉ thực hiện theo decision mới của Owner.

## 4. Google Drive boundary

Current project root:

- name: `VẬN HÀNH DC HƯNG YÊN`;
- ID: `19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`;
- owner: `tam95.supra@gmail.com`.

Current runtime skeleton được giữ lại nếu ownership/quyền vẫn PASS:

- BETA root: `10_RUNTIME_BETA` — `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`;
- STABLE root: `20_RUNTIME_STABLE` — `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI`.

Reference archive:

- name: `BACKUP PICK PACK 1291`;
- ID: `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`;
- owner: `tam95.supra@gmail.com`;
- hiện là sibling của project root, không phải child của project root;
- `REFERENCE ONLY / NOT AUTHORITY / NOT RUNTIME`;
- không tự di chuyển, không tự sửa nội dung, không tự lấy dữ liệu/ID cũ làm current config.

## 5. Architecture đã chốt và được giữ

- D1 là canonical structured authority cho service-first business core.
- Google Sheets là projection/human-readable/reconciliation/DR surface, không phải canonical authority trừ khi decision tương lai thay đổi.
- Projection dùng controlled writer/outbox/idempotency/retry/ACK/checkpoint; client không multi-write trực tiếp nghiệp vụ vào Sheets.
- Android/PDA, LAN Agent và LAN Web tiếp tục là deliverable chính thức.
- LAN pilot theo mô hình no-admin/minimum-information; không giả định quyền chỉnh router/firewall/DNS/Wi-Fi của công ty.
- LAN hostname không public DNS.
- Physical evidence hiện có đúng 2 Newland MT90; synthetic clients chỉ chứng minh service capacity, không thay thế RF/Wi-Fi evidence >2 PDA.
- Android/LAN Agent phải có automatic update discovery và manual fallback.
- Repo public; secret chỉ ở provider secret store/GitHub Environment.

## 6. Reference Pick Pack 1291 policy

Pick Pack 1291 cũ là evidence/reference. Không clone 100%, không migrate dữ liệu lịch sử mặc định, không dùng legacy account/resource IDs làm current config.

Pattern phải đi qua:

`REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED`.

Chỉ `ADOPTED` hoặc `ADAPTED` mới trở thành VHDCHY design.

## 7. Môi trường

### BETA

Setup lại trước. Không move `beta` live pointer cho tới khi provider/resource/secrets hiện hành được verify và BETA integration gate PASS.

### STABLE

Không setup credential/deploy sâu trước BETA PASS nếu không có lý do độc lập. Promotion yêu cầu Owner approval rõ ràng. Không copy BETA IDs/secrets/token sang STABLE.

## 8. Authority hierarchy

Khi có mâu thuẫn:

1. Owner decision mới nhất.
2. `PROJECT_SCOPE.md`.
3. `SERVICE_AUTHORITY.md`.
4. Active decision record / `DECISIONS.md`.
5. `CURRENT_STATE.md`.
6. Current architecture/spec.
7. Module spec.
8. Reference digest/index.
9. Historical backup/changelog/log.

Nguồn cấp thấp không được ghi đè nguồn cấp cao hơn.

## 9. Secrets

Không commit/paste token, password, OAuth client secret, refresh token, private key, keystore bytes/base64 hoặc signing password vào repo public, Drive docs hay chat. Chỉ ghi **tên secret**, nguồn tạo và trạng thái verification.
