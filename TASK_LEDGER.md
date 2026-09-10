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
| LAN-PILOT-001 | Early LAN feasibility gate | NEXT_PRIORITY | build reusable APK test + Windows LAN Agent + LAN Web; 3-PDA physical gate |
| LAN-CONTRACT-001 | Auto-LAN/discovery/fallback contract | NEXT | PDA tự bật LAN mode; deterministic fallback/reconnect |
| LAN-AGENT-001 | Windows lightweight service + tray/settings | NEXT | local API/cache/queue/metrics/update + manual fallback |
| ANDROID-LAN-TEST-001 | Signed BETA LAN test APK | NEXT | auto-LAN + queue/reconnect/metrics/update controls |
| LAN-WEB-001 | Internal BETA diagnostics Web | NEXT | `beta-lan.supra.cc.cd` after internal DNS/routing setup |
| LAN-LOAD-001 | Physical + synthetic load/soak tests | BLOCKED_BY_ARTIFACTS | 1/2/3 PDA real + 10/25/50/100+ synthetic clients |
| CORE-REFINE-001 | Generic resource/module catalog refinement | CHECKPOINTED | additive migration source may continue only if independent of LAN gate |
| SHEETS-001 | Projection catalog + transport contract | CHECKPOINTED | workbook exists; deep activation waits LAN gate unless independently useful |
| AUTH-001 | Generic auth/session/permission foundation | CHECKPOINTED | privileged ROOT auth adapter pending exact current spec |
| BUILD-001 | Business runtime Worker/Gateway | IN_PROGRESS | deep business expansion waits LAN-PILOT gate |
| BUILD-002 | Android app | IN_PROGRESS | pilot APK becomes the production-lineage Android app |
| BUILD-003 | LAN Agent | IN_PROGRESS | now mandatory early BETA feasibility stream |
| BUILD-004 | Web app | IN_PROGRESS | LAN Web pilot becomes reusable Web foundation |
| LAN-HA-001 | LAN HA/Master-Backup/fencing | DEFERRED | only after single-node pilot and measurement prove need |
