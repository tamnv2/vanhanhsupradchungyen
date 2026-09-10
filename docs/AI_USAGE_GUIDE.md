# HƯỚNG DẪN OWNER SỬ DỤNG AI — VHDCHY

## Chat mới

Chỉ cần yêu cầu: `Tiếp tục VHDCHY từ checkpoint hiện tại.`

AI phải tự đọc `PROJECT_SCOPE.md`, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, `DECISIONS_INDEX.md` rồi tiếp tục đúng dependency; Owner không cần kể lại lịch sử.

## Khi giao yêu cầu mới

Nói trực tiếp yêu cầu/nghiệp vụ. AI phải đối chiếu scope/decisions hiện hành. Nếu yêu cầu mới thay đổi authority/architecture/business rule lớn, AI phải ghi decision mới thay vì âm thầm ghi đè lịch sử.

## Khi muốn dừng/chuyển chat

Có thể nói: `Checkpoint và dừng.`

AI phải hoàn tất atomic work an toàn, cập nhật `SESSION_CHECKPOINT.md` + `NEXT_ACTIONS.md`, ghi changelog nếu có thay đổi đáng kể, rồi dừng.

Không bắt buộc phải nói câu này nếu AI đã gần soft-stop: AI phải tự checkpoint.

## Khi hỏi trạng thái

Nói: `Trạng thái dự án hiện tại?`

AI ưu tiên đọc current state/checkpoint, không suy đoán từ memory chat.

## Khi muốn tham khảo Pick Pack 1291

Nói rõ mục tiêu, ví dụ: `Tham khảo cách 1291 xử lý hàng rớt để đề xuất cho cluster 1291 mới.`

AI phải coi backup là reference; phải nói phần nào ADOPT/ADAPT/REJECT, không bê nguyên mặc định.

## Khi promote STABLE

Owner phải dùng ý rõ ràng như: `Duyệt promote Stable bản BETA đã đạt gate.`

Không có câu/ý duyệt này thì AI không được suy diễn quyền promote Stable.

## Khi tool/CI đang chạy lâu

Owner không cần canh 25 phút. AI contract yêu cầu soft-stop khoảng 18–20 phút, ghi run ID/status và next action nếu job chưa kết thúc. Phiên sau tiếp tục từ checkpoint.
