# TASK LEDGER

Baseline: `SETUP-RESET-20260912-01`

| ID | Hạng mục | Trạng thái | Ghi chú |
|---|---|---|---|
| RESET-001 | Snapshot pre-reset | DONE | `archive/pre-setup-reset-20260912` |
| RESET-002 | Reset authority/current provider state | DONE | Code/logic/history preserved; CI PASS |
| SETUP-001 | GitHub account/repo rights | VERIFIED_CURRENT | `tamnv2`, admin/push PASS |
| SETUP-002 | Drive project root | VERIFIED_CURRENT | owner `tam95.supra@gmail.com` |
| SETUP-003 | BETA Drive skeleton | VERIFIED_CURRENT | reuse `10_RUNTIME_BETA` |
| SETUP-004 | STABLE Drive skeleton | VERIFIED_EXISTING_NOT_LIVE | defer activation |
| SETUP-005 | BETA projection Sheet | VERIFIED_EXISTING_NOT_LIVE | ID `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`; PROVISIONED_NOT_LIVE |
| SETUP-006 | Google Cloud/OAuth BETA | SETUP_REQUIRED | `tam95.supra@gmail.com`; next parallel lane |
| SETUP-007 | GAS BETA | SETUP_REQUIRED | Sheet ready; waits for GCP/OAuth |
| SETUP-008 | Cloudflare retained account/resources | VERIFY_REQUIRED | `nguyenvantam050595@gmail.com`; next parallel lane |
| SETUP-009 | VHDCHY BETA Android signing | VERIFY_REQUIRED | next parallel lane; local Owner verification |
| SETUP-010 | GitHub `beta` Environment | SETUP_REQUIRED | only after provider outputs verified |
| SETUP-011 | BETA integration/deploy gate | BLOCKED | depends SETUP-006..010 |
| SETUP-012 | STABLE setup | BLOCKED_BY_GATE | BETA PASS + Owner approval |
| GOV-001 | Project scope authority | DONE | baseline 2026-09-12 |
| GOV-002 | Indexed AI continuity | DONE | 5-file bootstrap + task-specific reads |
| GOV-003 | Parallel dependency execution | DONE | independent lanes run in parallel |
| GOV-004 | Reference boundary | DONE | `BACKUP PICK PACK 1291` reference-only |
| CODE-001 | Worker/D1 source | PRESERVED_NOT_LIVE | rebuild gate before current deploy claim |
| CODE-002 | Google gateway source | PRESERVED_NOT_LIVE | least-privilege foundation preserved |
| LAN-PILOT-001 | LAN feasibility | PRESERVED_PHYSICAL_VERIFY_REQUIRED | final V4 regression pending |
| ANDROID-LAN-TEST-001 | Android LAN candidate | PRESERVED_PHYSICAL_VERIFY_REQUIRED | source/build evidence retained |
| LAN-AGENT-001 | Windows LAN Agent | PRESERVED_PHYSICAL_VERIFY_REQUIRED | source/build evidence retained |
| LAN-WEB-001 | LAN Web | PRESERVED_PHYSICAL_VERIFY_REQUIRED | source retained |
| LAN-LOAD-001 | Load/soak | PHYSICAL_VERIFY_REQUIRED | exactly 2 MT90 real + synthetic headroom |
