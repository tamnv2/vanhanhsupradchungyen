# RECONCILE-001 — BUSINESS CORE VS VHDCHY SCOPE

Status: `DONE`
Date: 2026-09-10

## Purpose

Reconcile the already-deployed BETA `business_core_v1` with the Owner-approved VHDCHY scope. Pick Pack 1291 is the first cluster/module and its retired project is reference only.

## Evidence reviewed

- current BETA migration `worker/migrations/0001_business_core.sql`;
- current BETA Worker contract `worker/src/index.js`;
- `docs/BUSINESS_CORE_V1.md`;
- Owner-approved `PROJECT_SCOPE.md` and `AI_OPERATING_CONTRACT.md`;
- full read-only `BACKUP PICK PACK 1291` reference and the extracted Sheets schema/reference workbook;
- latest verified BETA deploy run `34495312746`.

## A — Core audit classification

| Current area | Classification | Decision |
|---|---|---|
| `vhdchy_meta` | GENERIC_DC | KEEP |
| `clusters` | GENERIC_DC | KEEP |
| `shift_definitions` | GENERIC_DC | KEEP; cluster-scoped configuration |
| `employees` | GENERIC_DC | KEEP |
| `employee_cluster_memberships` | GENERIC_DC | KEEP |
| `resources` concept | GENERIC_DC | ADAPT |
| `resources.resource_type CHECK(PDA,USER_PICK,BAN_PACK,USER_PACK)` | CLUSTER_1291 leakage | REWORK in a new migration; do not edit applied migration 0001 |
| `work_sessions` | GENERIC_DC | KEEP; positions representation may be normalized later if query needs justify it |
| `session_resource_bindings` | GENERIC_DC | KEEP |
| `labor_records` | GENERIC_DC concept | ADAPT so labor types come from module/cluster catalog, not implicit Pick Pack assumptions |
| `dropped_goods` | CLUSTER_1291 | KEEP physically for compatibility in v1, but treat as a Pick Pack 1291 module domain rather than universal DC core |
| `document_metadata` | GENERIC_DC | KEEP |
| `domain_events` | GENERIC_DC | KEEP; immutable/idempotent canonical event pattern |
| `conflict_corrections` | GENERIC_DC | KEEP |
| `projection_outbox` | GENERIC_DC | KEEP |
| `projection_catalog` | GENERIC_DC | KEEP |
| `import_audit` | GENERIC_DC | KEEP |

## B — Pick Pack 1291 reference evaluation

### ADOPT / ADAPT

- Human-facing Sheet groups for PDA, User Pick, Bàn Pack, User Pack, nhân sự, lịch sử nghiệp vụ, RA/VÀO trong ca, thông tin user NLĐ, công nhật, vị trí and nhận hàng rớt are useful for the new cluster 1291 projection.
- Immutable events, idempotency, durable client/outbox concepts, transactional projection outbox and conflict/correction evidence are proven patterns worth retaining.
- Sheet categories/headers are reused as a baseline where they still match current cluster semantics.

### REJECT as default

- No old employee/resource/history rows are migrated.
- Old `Danh sách Admin` password verifier is not reproduced in the new Sheet.
- Old LAN authority fence/emergency/fallback tabs are not recreated merely because they existed in the retired project; they require a current VHDCHY gate/use case.
- No old Pick Pack resource type is allowed to define the global DC resource type universe.

## C — VHDCHY module boundary

The platform core owns identity/access, people, cluster membership, shifts, generic resources, sessions, immutable events, audit/conflict/correction, documents/media metadata, projection infrastructure and import/export audit.

Cluster `PICK_PACK_1291` owns its operational resource types and workflows, including PDA/User Pick/Bàn Pack/User Pack semantics, RA/VÀO projection, công nhật details and nhận hàng rớt.

See `docs/architecture/MODULE_MAP.md`.

## D — Reconciliation result

`business_core_v1` is not rolled back. It is a valid deployed BETA baseline, but future work must correct the two scope leaks through additive migrations/contracts:

1. genericize resource typing/catalogs instead of hard-coding Pick Pack resource types in DC core;
2. explicitly mark/module-scope dropped-goods and other Pick Pack-only behavior.

Applied migration `0001_business_core.sql` remains immutable historical deployment evidence. Corrections must be additive (`0002+`).

## E — Parallel build plan

After this reconciliation, the following tracks may proceed independently until their integration gates:

- `CORE-REFINE-001`: additive schema refinement for generic resource/module catalogs;
- `SHEETS-001`: register and integrate the new Pick Pack 1291 BETA quarterly projection workbook;
- `AUTH-001`: generic users/roles/permissions/sessions foundation, while privileged ROOT/SUPERADMIN credential acceptance remains blocked until the current VHDCHY Master Spec semantics are confirmed;
- `ANDROID-FOUNDATION-001`: environment/API/local durable-outbox foundation without inventing privileged-login rules.

STABLE remains untouched.