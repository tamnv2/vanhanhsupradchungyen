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

---

### Decision-ID continuity note

Một nhánh BETA lịch sử đã dùng tạm các ID D-010..D-017 cho business-runtime trước khi governance D-010..D-014 được Owner chốt trên `main`. Không merge các số ID lịch sử đó vào authority. Nội dung còn hợp lệ được re-issued dưới các ID D-015..D-022 bên dưới để giữ decision namespace duy nhất.

## D-015 — DC Core + cluster rollout

VHDCHY phục vụ toàn DC; `cluster_id` là operational boundary. Không hard-code Pick Pack 1291 thành toàn bộ nghiệp vụ. Pick Pack 1291 là cluster đầu tiên để build/test/stress/soak trước khi mở rộng và trước Stable promotion.

## D-016 — Legacy Pick Pack reuse có chọn lọc

Được đọc/reuse có chọn lọc logic/schema/UI/test từ backup Pick Pack 1291. Không write/deploy/runtime fallback sang tài nguyên cũ và không migrate employee/resource/history cũ.

## D-017 — Immutable event + correction

Canonical mutation dùng immutable event, idempotency, entity version và device sequence khi áp dụng. Raw event không UPDATE/DELETE; correction/tombstone/reversal là event mới và conflict phải giữ evidence + resolver decision.

## D-018 — Google Sheets là projection, không phải canonical authority

D1 là authority. Sheets là human-readable projection/đối soát/DR surface theo module. Projection dùng D1 outbox + controlled batch/one-writer, idempotency, retry, ACK/checkpoint; app/web/PDA không trực tiếp multi-write các tab nghiệp vụ.

## D-019 — Free-first / capacity by measurement

Tối ưu request/CPU/D1/Drive/Sheets trước khi Paid; tránh polling liên tục, ưu tiên indexed query/delta/batch/archive. Quyết định nâng quota/paid dựa trên stress/soak và measurement thực tế.

## D-020 — BETA/STABLE runtime isolation

Mở rộng D-002: BETA/STABLE tách D1/Worker/GAS/OAuth/Drive/signing/runtime state. Promotion là source/schema/config đã duyệt, không copy wholesale runtime data.

## D-021 — Không public business data trước auth

Repo và Worker public không đồng nghĩa dữ liệu nghiệp vụ public. Trước khi session/permission layer hoàn tất, chỉ health/meta/capabilities được anonymous; data/admin routes đóng và anonymous mutation bị cấm.

## D-022 — Projection integration không chặn canonical core

D1 mutation phải commit độc lập với availability tức thời của Google. Google integration lỗi được báo degraded và xử lý bằng outbox/retry/ACK/checkpoint. Direct Worker→GAS không phải dependency critical của canonical transaction/health.

## D-023 — Pick Pack 1291 Sheet baseline được ADAPTED từ backup

Owner approved 2026-09-10.

- Dùng schema/tab/header/danh mục cũ làm baseline mạnh cho cluster `PICK_PACK_1291`.
- Không migrate dữ liệu cũ.
- Workbook mới theo `environment + cluster + quarter`.
- Tab `Danh sách Admin` cũ được adapt thành `DANH SÁCH TÀI KHOẢN`; không lưu password verifier/secret trong Sheet.
- Old LAN/emergency/fallback tabs không tạo lại mặc định.
- Các tab mới cho tài liệu, conflict/correction và import audit được bổ sung theo VHDCHY.

## D-024 — Generic resource taxonomy + module-owned exception domains

Kết quả RECONCILE-001:

- Core toàn DC giữ khái niệm generic `resources`, nhưng resource type phải cấu hình/đăng ký theo module; không hard-code toàn hệ thống chỉ có PDA/USER_PICK/BAN_PACK/USER_PACK.
- `dropped_goods`/Nhận hàng rớt là domain của cluster/module Pick Pack 1291, không được suy diễn thành domain universal của DC.
- Applied migration `0001_business_core.sql` không sửa; correction đi qua additive migration mới.
