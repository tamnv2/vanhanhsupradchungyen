# AI OPERATING CONTRACT

Status: ACTIVE / OWNER-APPROVED 2026-09-10

Mục tiêu: làm dự án dài hạn qua nhiều phiên chat với tính kế thừa cao, ít token đọc lại, tự động hóa tối đa và không bị dừng đột ngột làm mất trạng thái.

## A. Bootstrap tối thiểu mỗi phiên

Bắt buộc đọc theo thứ tự:

1. `PROJECT_SCOPE.md`
2. `CURRENT_STATE.md`
3. `NEXT_ACTIONS.md`
4. `DECISIONS_INDEX.md`

Chỉ đọc thêm tài liệu chi tiết theo nhu cầu của task. Không đọc toàn bộ lịch sử/changelog/reference archive theo mặc định.

## B. Phân luồng công việc

Trước khi sửa code/hạ tầng:

1. Phân rã thành work items.
2. Xác định dependency giữa chúng.
3. Chạy song song mọi việc không phụ thuộc nhau khi tool cho phép.
4. Chỉ tuần tự hóa phần thật sự có dependency hoặc migration/release gate.
5. Không chờ một tác vụ dài nếu trong lúc đó còn việc độc lập có thể xử lý.

## C. Execution tranche và soft stop

Mỗi đợt thực thi dài dùng mục tiêu khoảng 20 phút, không cố chạy tới hard limit của tool/session.

- 0–2 phút: bootstrap + plan + dependency graph.
- 2–15 phút: execute, ưu tiên parallel.
- Khoảng 15–17 phút: không khởi động tác vụ dài mới; hoàn tất atomic work đang mở.
- Khoảng 17–19 phút: verify + checkpoint.
- Trước khoảng 20 phút: chủ động dừng sạch và bàn giao trạng thái.

Nếu CI/provider job vẫn chạy khi tới soft stop: ghi lại run/job ID, commit, trạng thái cuối đã quan sát và exact next action; không ngồi chờ đến lúc session bị cắt.

## D. Atomicity và release safety

- `main`: source authority/integration/checkpoint.
- `beta`: known-good commit đang live BETA.
- `stable`: known-good commit đang live STABLE.
- Work-in-progress có thể checkpoint trên `main` hoặc branch làm việc phù hợp; không move `beta`/`stable` khi chưa qua gate.
- Không để migration nửa chừng hoặc docs nói DONE khi provider chưa xác minh DONE.
- STABLE luôn cần gate/Owner approval hiện hành.

## E. Checkpoint bắt buộc

Trước soft stop, trước chuyển chat, sau lỗi lớn, hoặc trước thay đổi kiến trúc/migration quan trọng, cập nhật `SESSION_CHECKPOINT.md` và `NEXT_ACTIONS.md`.

Checkpoint phải có:

- timestamp;
- authority commit;
- BETA/STABLE live refs;
- done/in-progress/not-started;
- tests PASS/FAIL/UNKNOWN;
- resource/provider changes;
- exact next actions + dependencies + parallelizable work;
- Owner action required hay không;
- assumptions/conflicts chưa giải quyết.

Không tuyên bố `DONE` nếu test/provider state chưa được xác minh.

## F. Changelog và decision discipline

- `CHANGELOG.md` là index append-only theo version/change tranche.
- Mỗi thay đổi đáng kể phải có detailed record trong `docs/changelog/`.
- Detailed record tối thiểu: Change ID, time, commit, module, environment, reason, changes, files/resources, migrations, tests, result, rollback, decisions, next impact.
- Sai lịch sử thì tạo correction/supersede entry mới; không sửa/xóa entry cũ để làm sạch.
- Quyết định dài hạn phải vào `DECISIONS.md`; `DECISIONS_INDEX.md` chỉ là tra nhanh.

## G. Reference discipline

Pick Pack 1291 chỉ đọc khi task liên quan cluster Pick Pack 1291 hoặc khi cần pattern đã được chứng minh từ dự án cũ.

Luồng áp dụng pattern:

`REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED`

Mọi digest phải chỉ rõ provenance/source và khác biệt khi áp dụng vào VHDCHY. Không suy ra thiết kế VHDCHY chỉ từ một file lịch sử của Pick Pack 1291.

## H. Token/time optimization

- Ưu tiên index/digest/current-state ngắn thay vì full-history.
- Truy xuất theo ID/path/module khi cần chi tiết.
- Không lặp lại toàn bộ kiến trúc trong nhiều authority file; mỗi fact có một authority chính, file khác chỉ trỏ tới.
- Không ghi log/raw output dài vào bootstrap files; lưu summary và locator/run ID.

## I. Owner interaction

AI tự làm mọi việc có thể làm trong scope/quyền hiện có. Chỉ yêu cầu Owner khi:

- provider/UI bắt buộc người dùng thao tác;
- cần cấp quyền/secret mới;
- có business decision hoặc conflict không thể suy ra an toàn;
- STABLE gate yêu cầu Owner approval.

Không yêu cầu Owner paste secret vào chat.
