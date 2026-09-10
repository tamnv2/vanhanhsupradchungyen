# VẬN HÀNH DC HƯNG YÊN

Repository authority cho phát triển và vận hành nền tảng DC Hưng Yên.

## Môi trường

- `BETA` → `beta.supra.cc.cd`
- `STABLE` → `supra.cc.cd`
- LAN hostname chỉ dùng trong mạng nội bộ, **không tạo public DNS**.

## Bootstrap cho AI

Mỗi phiên mới bắt đầu tại `AI_BOOTSTRAP.md`.

Bootstrap tối thiểu được thiết kế để đọc nhanh và vẫn giữ continuity qua nhiều phiên:

1. `PROJECT_SCOPE.md`
2. `CURRENT_STATE.md`
3. `NEXT_ACTIONS.md`
4. `DECISIONS_INDEX.md`

Chi tiết cách AI làm việc dài hạn: `AI_OPERATING_CONTRACT.md`.
Hướng dẫn Owner: `docs/AI_USAGE_GUIDE.md`.

## Authority

GitHub là authority cho code/config/trạng thái kỹ thuật. Secret chỉ được đặt trong GitHub Environments hoặc provider secret store, không commit vào repository public.

## Reference

Pick Pack 1291 cũ chỉ là read-only evidence/reference cho cluster/module tương ứng và các pattern cần thiết. Không clone 100% và không dùng dự án cũ làm runtime/fallback/authority của VHDCHY.

## Release

- `main`: source authority/integration/checkpoint.
- `beta`: known-good live BETA ref.
- `stable`: known-good STABLE ref; chỉ promote khi Owner duyệt.
- Changelog append-only; thay đổi đáng kể có detailed record trong `docs/changelog/`.
