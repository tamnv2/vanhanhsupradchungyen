# HƯỚNG DẪN OWNER SỬ DỤNG AI — VHDCHY

## Chat mới

Chỉ cần nói:

`Tiếp tục VHDCHY từ checkpoint hiện tại.`

AI phải tự đọc theo thứ tự:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Sau đó chỉ đọc tài liệu/code liên quan task. Owner không phải kể lại lịch sử và AI không được mặc định đọc toàn bộ repo/reference mỗi lần.

## Current setup baseline

Baseline hiện hành: `SETUP-RESET-20260912-01`.

Các provider ID/credential/live claim cũ trước baseline chỉ là lịch sử nếu `SERVICE_AUTHORITY.md` không xác nhận lại.

## Khi giao việc mới

AI phải:

- đối chiếu authority + current state;
- phân rã dependency;
- chạy song song công việc độc lập;
- không bắt Owner làm lại bước đã được kiểm chứng đủ;
- không dùng memory/chat cũ để ghi đè GitHub authority.

## Khi chuyển chat/dừng

Có thể nói `Checkpoint và dừng.` AI phải cập nhật `SESSION_CHECKPOINT.md` + `NEXT_ACTIONS.md` và ghi changelog/decision khi cần.

## Khi hỏi trạng thái

Nói `Trạng thái dự án hiện tại?` AI ưu tiên authority/current checkpoint, không suy đoán từ memory.

## Khi dùng Pick Pack 1291

Reference folder là `BACKUP PICK PACK 1291`. AI chỉ đọc khi task cần; phải phân loại pattern `ADOPTED`, `ADAPTED` hoặc `REJECTED` trước khi biến thành current design.

## Khi promote STABLE

Owner phải duyệt rõ. Không có approval thì AI không được suy diễn quyền promote.

## Secrets

Không gửi token/password/private key/keystore/base64 vào chat. Khi cần, AI chỉ hướng dẫn nhập trực tiếp vào provider/GitHub secret UI.
