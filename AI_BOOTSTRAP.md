# AI BOOTSTRAP

## Mục tiêu

Đảm bảo AI có thể tiếp tục công việc qua nhiều phiên chat mà không phụ thuộc trí nhớ hội thoại.

## Quy trình bắt buộc khi bắt đầu phiên

Đọc lần lượt `CURRENT_STATE.md`, `DECISIONS.md`, `TASK_LEDGER.md`, `CHANGELOG.md` trước khi sửa code hoặc hạ tầng.

Nếu công việc liên quan kiến trúc, dữ liệu Google Sheets, offline/sync, authority/fallback, LAN hoặc tham chiếu dự án cũ thì bắt buộc đọc thêm `docs/reference/PICK_PACK_1291_REFERENCE.md` trước khi kết luận hoặc thay đổi implementation.

## Quy tắc vận hành

- GitHub là authority cho source, workflow, quyết định kỹ thuật và trạng thái triển khai.
- Drive Resource Registry giữ ID tài nguyên không nhạy cảm; secret không được ghi vào repo hoặc tài liệu public.
- Khi yêu cầu mới mâu thuẫn quyết định cũ, không tự suy đoán: nêu mâu thuẫn và yêu cầu Owner chốt nếu ảnh hưởng kiến trúc/dữ liệu/quyền.
- Luôn phân tích việc nào độc lập để xử lý song song.
- Không xóa changelog/version history.
- BETA và STABLE phải tách tài nguyên, secret và release flow.
- Không tạo DNS public cho hostname LAN.
- Không dùng `pull_request_target` trong workflow có khả năng tiếp cận secret.
- Không log token, private key, refresh token, keystore base64 hoặc signing password.
- Stable deployment phải đi qua GitHub Environment `stable` và Owner approval.
- Pick Pack 1291 chỉ là nguồn tham khảo read-only. Không được suy diễn kiến trúc cuối từ một file lịch sử đơn lẻ và không được dùng dự án cũ làm runtime/fallback của VHDCHY.

## Public repository hardening

Repository này chủ động để PUBLIC nhằm sử dụng GitHub-hosted Actions mà không phụ thuộc runner cố định. Vì vậy mọi thiết kế phải mặc định source có thể bị xem công khai; security không được phụ thuộc vào việc giấu source hoặc ID tài nguyên.
