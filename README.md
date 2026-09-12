# VẬN HÀNH DC HƯNG YÊN

Current repository authority: `tamnv2/vanhanhsupradchungyen`.

## Setup baseline

Dự án được reset **trạng thái setup/provider** ngày 2026-09-12 để cấp quyền và dựng lại tích hợp theo đúng account hiện hành. Reset này **không xóa** code, kiến trúc, decision, LAN evidence hoặc lịch sử changelog đã được Owner chốt.

Current identities:

- Google Drive / Google Sheets / GAS: `tam95.supra@gmail.com`.
- Cloudflare / GitHub account: `nguyenvantam050595@gmail.com`.
- GitHub user/repo: `tamnv2` / `tamnv2/vanhanhsupradchungyen`.
- `automation@supra.cc.cd`: `SUSPENDED_RECOVERY_CANDIDATE`; không dùng cho runtime/setup hiện tại.

## Bootstrap cho AI

Mỗi phiên mới đọc đúng 5 file theo thứ tự:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Sau đó chỉ mở tài liệu liên quan task. Không đọc toàn bộ repo/changelog/reference theo mặc định.

Quy tắc thực thi dài hạn: `AI_OPERATING_CONTRACT.md`.
Hướng dẫn Owner: `docs/AI_USAGE_GUIDE.md`.
Setup hiện hành: `docs/runbooks/SETUP_FROM_ZERO_2026-09-12.md`.

## Authority

- Owner decision mới nhất là cao nhất.
- GitHub là authority cho source/config/trạng thái kỹ thuật.
- `SERVICE_AUTHORITY.md` là authority duy nhất cho account/provider/resource identity hiện hành.
- Provider/resource cũ trước baseline 2026-09-12 không được coi là current nếu chưa được re-verify và ghi lại.

## Reference Pick Pack 1291

Google Drive folder `BACKUP PICK PACK 1291` là **REFERENCE ONLY**. Được đọc để tham khảo nhưng không phải runtime, fallback, source-of-truth hay implicit requirement. Pattern chỉ trở thành thiết kế hiện hành khi được Owner chấp nhận theo `REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED`.

## Release model

- `main`: source authority/integration/checkpoint.
- `beta`: chỉ trở thành live BETA pointer sau BETA gate PASS.
- `stable`: chỉ restore/promote sau BETA PASS + Owner approval.
- BETA/STABLE tách Google Cloud/OAuth/GAS/Sheets, Cloudflare runtime state, secrets và signing theo thiết kế hiện hành.
- Repo PUBLIC có chủ đích; tuyệt đối không commit secret/private key/token/keystore.
