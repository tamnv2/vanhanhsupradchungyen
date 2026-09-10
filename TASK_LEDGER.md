# TASK LEDGER

| ID | Hạng mục | Trạng thái | Ghi chú |
|---|---|---|---|
| SETUP-001 | Google connectors/account mới | DONE | Gmail/Drive đúng account dự án |
| SETUP-002 | Drive runtime BETA/STABLE | DONE | Đã tạo đủ cấu trúc |
| SETUP-003 | GCP + OAuth BETA/STABLE | DONE | Owner đã cấu hình |
| SETUP-004 | GAS authorize + deployment | DONE | BETA/STABLE PASS |
| SETUP-005 | Cloudflare zone + CI tokens | DONE | `supra.cc.cd` Active |
| SETUP-006 | Android signing | DONE | 2 signer độc lập |
| SETUP-007 | Repo authority/bootstrap | DONE | Foundation commit 2026-09-10 |
| SETUP-008 | GitHub Environments + secrets | DONE | Owner nhập đủ BETA/STABLE |
| SETUP-008A | Full environment credential verification | DONE | BETA + STABLE PASS toàn bộ |
| SETUP-009 | BETA infra deploy test | DONE | D1 + Worker + custom domain + GAS + deep health PASS |
| SETUP-010 | STABLE infra deploy test | BLOCKED_BY_GATE | Chỉ promote sau BETA gate và Owner acceptance |
| GOV-001 | Project scope authority | DONE | `PROJECT_SCOPE.md`, Owner approved |
| GOV-002 | AI long-running operating contract | DONE | minimal bootstrap + parallelism + soft stop/checkpoint |
| GOV-003 | Detailed changelog mechanism | DONE | append-only index + `docs/changelog/` records |
| GOV-004 | Pick Pack 1291 reference boundary | DONE | reference-only, adoption state required |
| RECONCILE-001 | Business core vs VHDCHY scope | NEXT | A+B+C parallel, then reconciliation decision/build plan |
| BUILD-001 | Business runtime Worker/Gateway | IN_PROGRESS | Tạm không mở rộng schema/auth trước RECONCILE-001 |
| BUILD-002 | Android app | PENDING | Sau contract/module reconciliation |
| BUILD-003 | LAN Agent | PENDING | Sau API/client contract và LAN measurement gate |
