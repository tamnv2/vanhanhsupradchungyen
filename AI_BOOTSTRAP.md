# AI BOOTSTRAP

Mục tiêu: tiếp tục VHDCHY qua nhiều phiên mà không phụ thuộc trí nhớ hội thoại và không dùng nhầm tài khoản/ID dịch vụ cũ.

## Mỗi phiên mới — đọc đúng 5 file trước

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Sau đó chỉ mở tài liệu chi tiết liên quan task. Nếu cần quy tắc thực thi, đọc `AI_OPERATING_CONTRACT.md`.

## Quy tắc cứng

- GitHub `tamnv2/vanhanhsupradchungyen` là authority cho source/config/technical state sau khi migration gate của repo mới hoàn tất.
- `SERVICE_AUTHORITY.md` là authority cho account/provider/resource identity. Trước mọi provider write/deploy phải đối chiếu file này.
- `vanhanhdchungyen@gmail.com` là DECOMMISSIONED; tuyệt đối không dùng lại trong login/OAuth/GAS/Drive/GitHub/CI/recovery.
- Không lấy ID/token/account từ trí nhớ AI, chat cũ, backup, screenshot hoặc repo cũ nếu chúng mâu thuẫn hoặc chưa được xác nhận trong authority hiện hành.
- Owner decision mới nhất > `PROJECT_SCOPE.md` > `SERVICE_AUTHORITY.md` > decisions > current specs > references > historical backup.
- `BACKUP DỰ ÁN CŨ PICK PACK 1291` chỉ là read-only evidence/reference; không dùng làm runtime/fallback/authority của VHDCHY.
- Trước thực thi: phân rã dependency; chạy song song mọi việc độc lập; chỉ tuần tự hóa gate thật sự phụ thuộc.
- Với tranche dài/CI/provider job dài: mục tiêu soft-stop khoảng 20 phút; trước khi dừng phải checkpoint `đã làm / đang làm / sẽ làm / run ID / commit / blockers / exact next action`.
- `CHANGELOG.md` + `docs/changelog/` phải append-only; không xóa lịch sử để làm sạch.
- BETA/STABLE tách tài nguyên/secrets/release; STABLE cần Owner approval.
- Không move `beta`/`stable` khi provider/resource của environment chưa VERIFIED_CURRENT.
- Không public DNS LAN.
- Không log/commit token, client secret, refresh token, private key, keystore base64 hoặc signing password.
- OAuth/GAS chỉ xin scope được current source chứng minh là cần; không xin quyền cho tính năng tương lai.
- Repo PUBLIC có chủ đích; security không được dựa vào giấu source/ID.
