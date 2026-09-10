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
| LAN-PILOT-001 | Early LAN feasibility gate | OWNER_TEST_REQUIRED | v0.1.3 artifacts ready; physical restricted-laptop + 1/2/3 MT90 evidence required |
| LAN-CONTRACT-001 | Auto-LAN/discovery/fallback contract | DONE | `VHDCHY_LAN_PILOT_V1`; cached + UDP discovery + manual-IP diagnostic; no DNS/router dependency |
| LAN-AGENT-001 | Portable Windows LAN Agent + tray/settings | FIRST_ARTIFACT_READY | user-mode/asInvoker, API/Web/UDP discovery/SQLite/metrics/update check; actual self-replace updater + CPU metric still pending final gate |
| ANDROID-LAN-TEST-001 | Signed BETA LAN test APK | FIRST_ARTIFACT_READY | APK v0.1.3 signed/verified; auto-LAN + fallback + local queue + manual/auto update discovery |
| LAN-WEB-001 | Internal BETA diagnostics Web | BASELINE_READY | served directly by Agent at laptop LAN IP:17891; no internal DNS required |
| LAN-LOAD-001 | Physical + synthetic load/soak tests | OWNER_TEST_REQUIRED | runbook ready; 1/2/3 MT90 real + 10/25/50/100 synthetic clients |
| LAN-UPDATE-001 | No-admin Agent staged self-update + rollback | NEXT_AFTER_FIRST_CONNECTIVITY | current v0.1.3 has automatic release notification + manual release path; self-replace/rollback not yet implemented |
| LAN-METRICS-001 | Target-laptop footprint evidence | OWNER_TEST_REQUIRED | dashboard reports RAM/latency/etc.; CPU currently measured via Task Manager until in-app sampler is added |
| CORE-REFINE-001 | Generic resource/module catalog refinement | CHECKPOINTED | additive migration source may continue only if independent of LAN gate |
| SHEETS-001 | Projection catalog + transport contract | CHECKPOINTED | workbook exists; deep activation waits LAN gate unless independently useful |
| AUTH-001 | Generic auth/session/permission foundation | CHECKPOINTED | privileged ROOT auth adapter pending exact current spec |
| BUILD-001 | Business runtime Worker/Gateway | IN_PROGRESS | deep business expansion waits LAN-PILOT gate |
| BUILD-002 | Android app | IN_PROGRESS | LAN pilot APK becomes production-lineage Android app |
| BUILD-003 | LAN Agent | IN_PROGRESS | mandatory early BETA feasibility stream |
| BUILD-004 | Web app | IN_PROGRESS | LAN Web pilot becomes reusable Web foundation |
| LAN-HA-001 | LAN HA/Master-Backup/fencing | DEFERRED | only after single-node pilot and measurement prove need |
