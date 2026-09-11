# AI OPERATING CONTRACT

Status: ACTIVE / OWNER-APPROVED 2026-09-11

Mục tiêu: vận hành dự án dài hạn qua nhiều phiên chat với tính kế thừa cao, ít token đọc lại, tự động hóa tối đa, không suy diễn trạng thái provider và không mất trạng thái khi CI/provider job kéo dài.

## A. Bootstrap tối thiểu mỗi phiên

Bắt buộc đọc theo thứ tự:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Chỉ đọc thêm tài liệu chi tiết theo nhu cầu của task. Không đọc toàn bộ lịch sử/changelog/reference archive theo mặc định.

Nếu task chạm provider/account/secret/resource ID, bắt buộc đọc `SERVICE_AUTHORITY.md` trước khi gọi tool hoặc sửa config.

## B. Authority và chống nhầm trạng thái

- Owner decision mới nhất là nguồn cao nhất.
- GitHub current authority files là nguồn điều phối kỹ thuật; không dùng trí nhớ hội thoại để ghi đè chúng.
- `SERVICE_AUTHORITY.md` là nguồn duy nhất cho account/provider/resource identity hiện hành.
- `vanhanhdchungyen@gmail.com` là DECOMMISSIONED; mọi reference còn sót phải được coi là stale cho tới khi được sửa/supersede.
- Không lấy account ID, folder ID, spreadsheet ID, Script ID, deployment ID, token/client info từ backup, screenshot, old repo, old run log hoặc model memory nếu authority hiện hành không xác nhận.
- Mỗi provider fact phải được phân loại tối thiểu một trong: `VERIFIED_CURRENT`, `OWNER_CONFIRMED_NOT_TOOL_VERIFIED`, `PROVISIONED_NOT_LIVE`, `REBUILD_REQUIRED`, `LEGACY_REFERENCE`, `DECOMMISSIONED`, `UNKNOWN`.
- Không tuyên bố DONE/LIVE/PASS cho state `PROVISIONED_NOT_LIVE`, `OWNER_CONFIRMED_NOT_TOOL_VERIFIED`, `REBUILD_REQUIRED` hoặc `UNKNOWN`.

## C. Phân luồng công việc

Trước khi sửa code/hạ tầng:

1. Phân rã thành work items.
2. Xác định dependency/gate giữa chúng.
3. Chạy song song mọi việc không phụ thuộc nhau khi tool cho phép.
4. Chỉ tuần tự hóa phần thật sự có dependency, provider mutation, migration hoặc release gate.
5. Không chờ một CI/provider job dài nếu trong lúc đó còn việc độc lập có thể xử lý.
6. Với các thao tác write cùng một file/ref/resource, tuần tự hóa để tránh race.

## D. Execution tranche và soft stop

Mỗi đợt thực thi dài dùng mục tiêu khoảng 20 phút, không cố chạy tới hard limit của tool/session.

- 0–2 phút: bootstrap + provider preflight + dependency graph.
- 2–15 phút: execute, ưu tiên parallel.
- Khoảng 15–17 phút: không khởi động tác vụ dài mới; hoàn tất atomic work đang mở.
- Khoảng 17–19 phút: verify + backup/checkpoint.
- Trước khoảng 20 phút: chủ động dừng sạch và bàn giao trạng thái.

Nếu CI/provider job vẫn chạy khi tới soft stop: ghi lại run/job ID, commit/ref, trạng thái cuối đã quan sát, provider/resource đang chờ và exact next action. Không ngồi chờ tới lúc session bị cắt.

## E. Atomicity và BETA/STABLE safety

- `main`: source authority/integration/checkpoint.
- `beta`: live BETA pointer chỉ sau khi BETA resource/secret/provider gate VERIFIED_CURRENT.
- `stable`: live STABLE pointer chỉ sau khi STABLE gate VERIFIED_CURRENT và Owner approve.
- BETA/STABLE tách Drive root, GAS project/deployment, OAuth client/token, GitHub Environment vars/secrets, signing identity và release gate.
- Work-in-progress dùng branch làm việc hoặc `main`; không move `beta`/`stable` để “thử”.
- Không để migration nửa chừng hoặc docs nói DONE khi provider chưa xác minh DONE.
- Không copy BETA IDs/secrets sang STABLE chỉ để vượt gate.

## F. Provider write preflight

Trước write/deploy trên GitHub/Google/Cloudflare:

1. Xác nhận account/profile đang active bằng tool nếu tool hỗ trợ.
2. Đối chiếu account/resource với `SERVICE_AUTHORITY.md`.
3. Kiểm tra environment BETA/STABLE đích.
4. Kiểm tra thao tác có giữ nguyên setup provider đã được Owner xác nhận hay đang rebuild provider mới.
5. Nếu quyền thiếu, dừng tại boundary và tạo Owner action cụ thể; không bù bằng scope/token rộng hơn không cần thiết.

## G. OAuth/GAS security discipline

- Dedicated Google runtime owner hiện hành: `automation@supra.cc.cd`.
- Không xin Gmail/Calendar/Contacts scope nếu current code không dùng.
- Apps Script manifest và CI OAuth client là hai lớp quyền khác nhau; audit riêng từng lớp.
- Chỉ xin narrowest scopes current source chứng minh cần; không xin scope cho future feature.
- Không tạo refresh token trên mỗi run. Một client/environment dùng một refresh token bền vững; chỉ rotate khi có lý do.
- Không log/commit client secret, refresh token, access token, private key hoặc authorization code.
- OAuth Publishing Status `Testing` không phù hợp cho CI lâu dài vì external-user refresh authorization có giới hạn thời gian; production/personal-use configuration phải được hoàn tất trước khi coi token là durable.
- “In production” không có nghĩa token vĩnh viễn: vẫn phải chịu revoke, inactivity, policy và provider security events.
- Không dùng email automation để gửi mail hàng loạt; không thêm `MailApp`/`GmailApp`/`script.send_mail` nếu use case chưa được Owner duyệt.

## H. Checkpoint bắt buộc

Trước soft stop, trước chuyển chat, sau lỗi lớn, sau provider/account migration, hoặc trước thay đổi kiến trúc/migration quan trọng, cập nhật `SESSION_CHECKPOINT.md` và `NEXT_ACTIONS.md`.

Checkpoint phải có:

- timestamp;
- authority commit/branch;
- BETA/STABLE live refs và restoration status;
- done/in-progress/not-started;
- tests PASS/FAIL/UNKNOWN;
- account/provider/resource changes;
- exact next actions + dependencies + parallelizable work;
- Owner action required hay không;
- assumptions/conflicts chưa giải quyết;
- CI/provider run IDs nếu còn đang chạy.

## I. Changelog và decision discipline

- `CHANGELOG.md` là index append-only theo version/change tranche.
- Mỗi thay đổi đáng kể phải có detailed record trong `docs/changelog/`.
- Sai lịch sử thì tạo correction/supersede entry mới; không sửa/xóa entry cũ để làm sạch.
- Quyết định dài hạn phải vào `DECISIONS.md`; `DECISIONS_INDEX.md` chỉ để tra nhanh.

## J. Reference discipline

`BACKUP DỰ ÁN CŨ PICK PACK 1291` chỉ đọc khi task liên quan cluster Pick Pack 1291 hoặc cần pattern đã được chứng minh từ dự án cũ.

Luồng áp dụng pattern:

`REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED`

Không suy ra thiết kế VHDCHY chỉ từ một file lịch sử. Legacy IDs/accounts/config trong reference không được phép trở thành current config nếu chưa được authority hiện hành xác nhận.

## K. Token/time optimization

- Ưu tiên compact authority/index/current-state thay vì full-history.
- Truy xuất theo ID/path/module khi cần chi tiết.
- Mỗi fact có một authority chính; file khác chỉ trỏ tới.
- Không ghi raw logs dài vào bootstrap files; lưu summary + locator/run ID.
- Khi cần kiểm tra kế thừa, so sánh checksum/commit/path cụ thể thay vì đọc lại toàn repo.

## L. Owner interaction

AI tự làm mọi việc có thể làm trong scope/quyền hiện có. Chỉ yêu cầu Owner khi:

- provider/UI bắt buộc người dùng thao tác;
- cần nhập secret/2FA/consent/verification;
- cần cấp quyền provider mà tool không có;
- có business decision/conflict không thể suy ra an toàn;
- STABLE gate yêu cầu Owner approval.

Không yêu cầu Owner paste secret/token/password/private key vào chat. Hướng dẫn Owner nhập trực tiếp vào provider secret store/UI.
