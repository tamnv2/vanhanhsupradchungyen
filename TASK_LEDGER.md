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
| LAN-PILOT-001 | Early LAN feasibility gate | V4_PHYSICAL_VERIFY_REQUIRED | Basic two-MT90 feasibility proven; final candidate `0.3.36` CI/release green, physical V4 regression remains |
| LAN-CONTRACT-001 | Auto-LAN/discovery/fallback contract | DONE | cached + UDP discovery + manual diagnostic; V4 adds epoch/sequence resync; no DNS/router dependency |
| LAN-AGENT-001 | Portable Windows LAN Agent + tray/settings | V4_CANDIDATE_READY__OWNER_TEST_REQUIRED | `0.3.36`: no-admin portable Agent, tray/dashboard, metrics/log export, realtime V4, staged updater implementation; build PASS |
| ANDROID-LAN-TEST-001 | Signed BETA LAN test APK | V4_CANDIDATE_READY__OWNER_TEST_REQUIRED | signed `0.3.36`, foreground realtime + bounded finish service + update verification; CI PASS |
| LAN-WEB-001 | Internal BETA diagnostics Web | V4_CANDIDATE_READY__OWNER_TEST_REQUIRED | Agent-served laptop Test Center/dashboard; no internal DNS required |
| LAN-LOAD-001 | Physical + synthetic load/soak tests | OWNER_TEST_REQUIRED | exactly 2 MT90 real + 10/25/50/100 synthetic; RF >2 remains unproven |
| LAN-UPDATE-001 | No-admin Agent staged self-update + rollback | IMPLEMENTED__PHYSICAL_VERIFY_REQUIRED | SHA256/staging/restart/health/rollback source + CI gates PASS; actual corporate-laptop replacement/rollback still physical |
| LAN-METRICS-001 | Target-laptop/PDA footprint evidence | IMPLEMENTED__PHYSICAL_VERIFY_REQUIRED | V4 records Agent/laptop/PDA resource, queue, network, realtime and lifecycle metrics; final measured footprint needs physical/soak evidence |
| LAN-REALTIME-001 | Restart-safe realtime transport | IMPLEMENTED__PHYSICAL_VERIFY_REQUIRED | V4 `streamEpoch + sequence`, bounded-buffer resync, signal-driven long-poll, corrected clock calibration; physical heavy test remains |
| LAN-BACKGROUND-001 | PDA bounded background completion | IMPLEMENTED__PHYSICAL_VERIFY_REQUIRED | foreground realtime only; `START_NOT_STICKY` finish service for in-flight/queue, bounded wake lock/retry, cleanup after final job |
| LAN-DIAGNOSTICS-001 | Rich Agent/PDA diagnostics/export | IMPLEMENTED__PHYSICAL_VERIFY_REQUIRED | detailed lifecycle/update/queue/realtime/transfer/resource evidence; next physical run must export Agent ZIP + 2 FULL TXT |
| CORE-REFINE-001 | Generic resource/module catalog refinement | CHECKPOINTED | additive migration source may continue only if independent of LAN gate |
| SHEETS-001 | Projection catalog + transport contract | CHECKPOINTED | workbook exists; deep activation waits LAN gate unless independently useful |
| AUTH-001 | Generic auth/session/permission foundation | CHECKPOINTED | privileged ROOT auth adapter pending exact current spec |
| BUILD-001 | Business runtime Worker/Gateway | IN_PROGRESS | deep business expansion waits LAN-PILOT gate |
| BUILD-002 | Android app | IN_PROGRESS | V4 LAN pilot becomes production-lineage Android transport/lifecycle baseline after physical gate |
| BUILD-003 | LAN Agent | IN_PROGRESS | V4 automated candidate ready; final physical acceptance pending |
| BUILD-004 | Web app | IN_PROGRESS | LAN Web pilot becomes reusable Web foundation |
| LAN-HA-001 | LAN HA/Master-Backup/fencing | DEFERRED | only after single-node pilot and measurement prove need |
