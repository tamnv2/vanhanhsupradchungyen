# AI BOOTSTRAP

Mục tiêu: tiếp tục VHDCHY qua nhiều phiên mà không phụ thuộc trí nhớ hội thoại.

## Mỗi phiên mới — đọc đúng 4 file trước

1. `PROJECT_SCOPE.md`
2. `CURRENT_STATE.md`
3. `NEXT_ACTIONS.md`
4. `DECISIONS_INDEX.md`

Sau đó chỉ mở tài liệu chi tiết liên quan task. Nếu cần quy tắc thực thi, đọc `AI_OPERATING_CONTRACT.md`.

## Quy tắc cứng

- GitHub là authority cho source/config/technical state.
- Owner decision mới nhất > scope > decisions > current specs > references > historical backup.
- Pick Pack 1291 chỉ là read-only evidence/reference; không clone 100% và không dùng làm runtime/fallback/authority của VHDCHY.
- Phân tích dependency và chạy song song việc độc lập.
- Với tranche dài, soft-stop trước khoảng 20 phút; checkpoint trước khi dừng.
- `CHANGELOG.md` + `docs/changelog/` phải ghi đầy đủ thay đổi; không xóa lịch sử.
- BETA/STABLE tách tài nguyên/secrets/release; STABLE cần Owner approval.
- Không public DNS LAN.
- Không log/commit token, private key, refresh token, keystore base64 hoặc signing password.
- Repo PUBLIC có chủ đích; security không được dựa vào giấu source/ID.
