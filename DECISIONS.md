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

## D-009 — Changelog append-only

Mỗi thay đổi version/release phải thêm entry mới; không sửa/xóa lịch sử để làm sạch bề ngoài.
