# VẬN HÀNH DC HƯNG YÊN

Current repository authority: `tamnv2/vanhanhsupradchungyen`.

## Môi trường

- `BETA` -> `beta.supra.cc.cd`
- `STABLE` -> `supra.cc.cd`
- LAN hostname chỉ dùng trong mạng nội bộ, không tạo public DNS.

Trong account/provider recovery, `beta` và `stable` không được move cho tới khi provider gate tương ứng PASS.

## Bootstrap cho AI

Mỗi phiên mới bắt đầu tại `AI_BOOTSTRAP.md` và đọc tối thiểu:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Chi tiết cách AI làm việc dài hạn: `AI_OPERATING_CONTRACT.md`.
Hướng dẫn Owner: `docs/AI_USAGE_GUIDE.md`.
Provider recovery manual: `docs/runbooks/REAUTHORIZATION_2026-09-11.md`.

## Authority

GitHub là authority cho code/config/trạng thái kỹ thuật; `SERVICE_AUTHORITY.md` là authority cho current account/provider/resource identity.

`vanhanhdchungyen@gmail.com` là DECOMMISSIONED và không được dùng lại.

Secret chỉ được đặt trong GitHub Environments hoặc provider secret store, không commit vào repository public hoặc chat.

## Reference

`BACKUP DỰ ÁN CŨ PICK PACK 1291` chỉ là read-only evidence/reference cho cluster/module tương ứng và các pattern cần thiết. Không clone 100% và không dùng dữ liệu/ID của dự án cũ làm runtime/fallback/authority của VHDCHY.

## Release model

- `main`: source authority/integration/checkpoint.
- `beta`: known-good live BETA ref sau BETA gate.
- `stable`: known-good STABLE ref; chỉ restore/promote sau BETA PASS và Owner duyệt.
- BETA/STABLE tách Drive/GAS/OAuth/secrets/signing/release gate.
- Changelog append-only; thay đổi đáng kể có detailed record trong `docs/changelog/`.
