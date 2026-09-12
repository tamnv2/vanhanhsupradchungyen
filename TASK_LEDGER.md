# TASK LEDGER

Baseline: `SETUP-RESET-20260912-01`

| ID | Hạng mục | Trạng thái | Ghi chú |
|---|---|---|---|
| RESET-001 | Snapshot pre-reset | DONE | `archive/pre-setup-reset-20260912` |
| RESET-002 | Reset authority/current provider state | IN_PROGRESS | Code/logic/history preserved |
| SETUP-001 | GitHub account/repo rights | VERIFIED_CURRENT | `tamnv2`, admin/push PASS |
| SETUP-002 | Drive project root | VERIFIED_CURRENT | owner `tam95.supra@gmail.com` |
| SETUP-003 | BETA Drive skeleton | VERIFIED_CURRENT | reuse `10_RUNTIME_BETA`; do not recreate |
| SETUP-004 | STABLE Drive skeleton | VERIFIED_EXISTING_NOT_LIVE | defer activation |
| SETUP-005 | BETA projection Sheet | NOT_PROVISIONED | old Sheet IDs invalid as current authority |
| SETUP-006 | Google Cloud/OAuth BETA | SETUP_REQUIRED | `tam95.supra@gmail.com` |
| SETUP-007 | GAS BETA | SETUP_REQUIRED | depends on GCP + current Sheet |
| SETUP-008 | Cloudflare retained account/resources | VERIFY_REQUIRED | `nguyenvantam050595@gmail.com` |
| SETUP-009 | VHDCHY BETA Android signing | VERIFY_REQUIRED | local owner verification |
| SETUP-010 | GitHub `beta` Environment | SETUP_REQUIRED | only after provider outputs verified |
| SETUP-011 | BETA integration/deploy gate | BLOCKED | depends SETUP-005..010 |
| SETUP-012 | STABLE setup | BLOCKED_BY_GATE | BETA PASS + Owner approval |
| GOV-001 | Project scope authority | DONE | updated baseline 2026-09-12 |
| GOV-002 | Indexed AI continuity | DONE | 5-file bootstrap + task-specific reads |
| GOV-003 | Parallel dependency execution | DONE | independent lanes run in parallel |
| GOV-004 | Reference boundary | DONE | `BACKUP PICK PACK 1291` reference-only |
| CODE-001 | Worker/D1 source | PRESERVED_NOT_LIVE | rebuild gate before current deploy claim |
| CODE-002 | Google gateway source | PRESERVED_NOT_LIVE | least-privilege foundation preserved |
| LAN-PILOT-001 | LAN feasibility | PRESERVED_PHYSICAL_VERIFY_REQUIRED | prior evidence retained; final V4 regression pending |
| ANDROID-LAN-TEST-001 | Android LAN candidate | PRESERVED_PHYSICAL_VERIFY_REQUIRED | source/build evidence retained |
| LAN-AGENT-001 | Windows LAN Agent | PRESERVED_PHYSICAL_VERIFY_REQUIRED | source/build evidence retained |
| LAN-WEB-001 | LAN Web | PRESERVED_PHYSICAL_VERIFY_REQUIRED | source retained |
| LAN-LOAD-001 | Load/soak | PHYSICAL_VERIFY_REQUIRED | exactly 2 MT90 real + synthetic headroom |
