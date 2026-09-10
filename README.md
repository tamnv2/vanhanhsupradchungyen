# VẬN HÀNH DC HƯNG YÊN

Repository vận hành và phát triển hệ thống DC Hưng Yên.

## Môi trường

- `BETA` → `beta.supra.cc.cd`
- `STABLE` → `supra.cc.cd`
- LAN hostname chỉ dùng trong mạng nội bộ, **không tạo public DNS**.

## Authority

Mỗi phiên AI phải đọc theo thứ tự:

1. `AI_BOOTSTRAP.md`
2. `CURRENT_STATE.md`
3. `DECISIONS.md`
4. `TASK_LEDGER.md`
5. `CHANGELOG.md`

GitHub là authority cho code/config/trạng thái kỹ thuật. Secret chỉ được đặt trong GitHub Environments hoặc secret store của provider, không commit vào repository public.

## Nguyên tắc release

- BETA ưu tiên tự động hóa tối đa.
- STABLE chỉ promote khi Owner chốt.
- Changelog là append-only, không xóa lịch sử version.
