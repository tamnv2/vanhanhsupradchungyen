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
| SETUP-009 | BETA infra deploy test | DONE | D1 + Worker + custom domain + GAS + health PASS |
| SETUP-010 | STABLE infra deploy test | BLOCKED_BY_GATE | Chỉ promote sau BETA gate và Owner acceptance |
| GOV-001 | Project scope authority | DONE | `PROJECT_SCOPE.md`, Owner approved |
| GOV-002 | AI long-running operating contract | DONE | minimal bootstrap + parallelism + soft stop/checkpoint |
| GOV-003 | Detailed changelog mechanism | DONE | append-only index + `docs/changelog/` records |
| GOV-004 | Pick Pack 1291 reference boundary | DONE | reference-only, adoption state required |
| RECONCILE-001 | Business core vs VHDCHY scope | DONE | generic core vs cluster-specific classified; module map recorded |
| SHEETS-000 | Create PP1291 BETA quarterly workbook | DONE | `PP1291_SHEETS_BETA_V1`, no old data migration |
| BUILD-001A | BETA Business Core V1 baseline | DONE | D1 schema/meta/health PASS; Worker version `f8ddf638-0829-4249-9ea0-8f2d38b03f05` |
| CORE-REFINE-001 | Generic resource/module catalog refinement | NEXT | additive migration only; keep 0001 immutable |
| SHEETS-001 | Projection catalog + transport contract | NEXT | workbook exists; register/map/test with synthetic data |
| AUTH-001 | Generic auth/session/permission foundation | NEXT | privileged ROOT auth adapter pending exact current spec |
| ANDROID-FOUNDATION-001 | Native Android platform foundation | NEXT | API/env/device/outbox/update skeleton can run independently |
| BUILD-001 | Business runtime Worker/Gateway | IN_PROGRESS | continues through core/auth/projection tracks |
| BUILD-002 | Android app | IN_PROGRESS | foundation track unlocked; business UI later |
| BUILD-003 | LAN Agent | PENDING | after API/client contract and LAN measurement gate |
