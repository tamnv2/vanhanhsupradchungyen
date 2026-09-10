# DECISIONS

## D-001 — Repository PUBLIC

Repo giữ PUBLIC để dùng GitHub-hosted Actions không phụ thuộc quota private repo hoặc self-hosted runner. Hệ quả: không được commit bất kỳ secret/private key nào.

## D-002 — Tách môi trường

BETA và STABLE có token, Apps Script, Drive runtime, D1, Worker và signing key riêng.

## D-003 — Public hostnames

- BETA: `beta.supra.cc.cd`
- STABLE: `supra.cc.cd`

## D-004 — LAN isolation

LAN hostname không có public DNS record. LAN routing/resolution sẽ được cấu hình riêng trong mạng nội bộ.

## D-005 — Cloudflare runtime

Worker là public service layer; D1 là structured store chính. Durable Objects dành cho lock/rate-limit/concurrency. R2 mặc định OFF.

## D-006 — Google runtime

Drive/Sheets được truy cập qua Google Gateway/Apps Script. Hai Apps Script project giữ deployment ID cố định và CI cập nhật source/version vào deployment đó.

## D-007 — Stable promotion

STABLE chỉ deploy khi Owner chốt và GitHub Environment `stable` yêu cầu approval.

## D-008 — Authority và continuity

GitHub lưu bootstrap/current-state/decisions/task-ledger/changelog để AI đọc lại ở mọi phiên.

Superseded/expanded by D-010 đến D-014 về scope hierarchy, indexed inheritance, timebox/checkpoint và parallel execution.

## D-009 — Changelog append-only

Mỗi thay đổi version/release phải thêm entry mới; không sửa/xóa lịch sử để làm sạch bề ngoài.

## D-010 — VHDCHY scope và Pick Pack 1291 reference boundary

Owner approved 2026-09-10.

- Dự án chính là `VẬN HÀNH DC HƯNG YÊN`, bao quát rộng hơn Pick Pack 1291.
- Pick Pack 1291 là cluster/module đầu tiên và dự án cũ chỉ là nguồn tham khảo.
- Không clone 100% hoặc mặc định schema/architecture/behavior cũ là requirement.
- Pattern cũ chỉ trở thành VHDCHY design khi được `ADOPTED` hoặc `ADAPTED`.
- Backup cũ là read-only evidence/reference và không được trở thành runtime/fallback/authority.

## D-011 — Indexed inheritance / minimal bootstrap

Owner approved 2026-09-10.

Mỗi phiên AI đọc tối thiểu `PROJECT_SCOPE.md`, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, `DECISIONS_INDEX.md`; sau đó chỉ mở tài liệu chi tiết liên quan task. Mục tiêu là giữ continuity mà không bắt AI nạp toàn bộ history/reference vào context.

## D-012 — 20-minute execution tranche + checkpoint

Owner approved 2026-09-10.

Không cố chạy đến hard limit khoảng 25 phút. Tranche dài dùng soft stop khoảng 18–20 phút; khoảng 15–17 phút không mở tác vụ dài mới; trước dừng phải verify và cập nhật checkpoint/next actions. CI/provider job chưa xong phải ghi run ID + trạng thái thay vì chờ tới bị cắt.

## D-013 — Parallel execution by dependency graph

Owner approved 2026-09-10.

Trước work tranche phải phân tích dependencies. Work items độc lập phải được xử lý song song khi tool cho phép; chỉ tuần tự hóa dependency/migration/release gate thật sự.

## D-014 — Detailed immutable change history

Owner approved 2026-09-10.

`CHANGELOG.md` là append-only index. Mỗi thay đổi đáng kể có detailed record trong `docs/changelog/` chứa change ID/time/commit/module/environment/reason/changes/resources/migrations/tests/result/rollback/decisions/next impact. Sai lịch sử dùng correction/supersede entry mới, không xóa lịch sử cũ.
