# HƯỚNG DẪN OWNER SỬ DỤNG AI — VHDCHY

## Chat mới

Chỉ cần yêu cầu: `Tiếp tục VHDCHY từ checkpoint hiện tại.`

AI phải tự đọc đúng bootstrap authority:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Sau đó chỉ đọc tài liệu chi tiết liên quan task và tiếp tục đúng dependency. Owner không cần kể lại lịch sử.

Model memory/chat history không được ghi đè các file authority trên. Nếu provider/account/resource ID không có trong authority hiện hành, AI phải kiểm chứng hoặc đánh dấu UNKNOWN chứ không tự suy diễn.

## Khi giao yêu cầu mới

Nói trực tiếp yêu cầu/nghiệp vụ. AI phải đối chiếu scope/authority/decisions hiện hành. Nếu yêu cầu mới thay đổi authority/architecture/business rule lớn, AI phải ghi decision hoặc authority update mới thay vì âm thầm ghi đè lịch sử.

## Khi muốn dừng/chuyển chat

Có thể nói: `Checkpoint và dừng.`

AI phải hoàn tất atomic work an toàn, cập nhật `SESSION_CHECKPOINT.md` + `NEXT_ACTIONS.md`, ghi changelog nếu có thay đổi đáng kể, rồi dừng.

Không bắt buộc phải nói câu này nếu AI đã gần soft-stop: AI phải tự checkpoint.

## Khi hỏi trạng thái

Nói: `Trạng thái dự án hiện tại?`

AI ưu tiên đọc authority/current state/checkpoint, không suy đoán từ memory chat.

## Khi muốn tham khảo Pick Pack 1291

Nói rõ mục tiêu. Folder hiện hành là `BACKUP DỰ ÁN CŨ PICK PACK 1291` và chỉ là reference. AI phải nói phần nào ADOPT/ADAPT/REJECT, không bê nguyên dữ liệu/ID/schema cũ vào VHDCHY.

## Khi promote STABLE

Owner phải dùng ý rõ ràng như: `Duyệt promote Stable bản BETA đã đạt gate.`

Không có câu/ý duyệt này thì AI không được suy diễn quyền promote Stable.

## Khi tool/CI đang chạy lâu

Owner không cần canh thời gian. AI contract yêu cầu soft-stop khoảng 20 phút: không khởi động job dài mới, ghi commit/run ID/status/provider state và exact next action nếu job chưa kết thúc, rồi checkpoint để phiên sau tiếp tục.

Trong thời gian một job đang chạy, AI phải tiếp tục các work item độc lập có thể làm song song thay vì chờ thụ động.
