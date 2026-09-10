# NEXT ACTIONS

Checkpoint: GOV-20260910-01

## Mục tiêu kế tiếp

`RECONCILE-001 — Reconcile business core with VHDCHY scope before further expansion.`

## Work items

### A — Core audit [independent]

Rà D1 migrations, Worker contracts, event/idempotency/projection logic hiện có. Phân loại từng phần:

- `GENERIC_DC` — dùng cho nền tảng toàn DC;
- `CLUSTER_1291` — chỉ thuộc cluster Pick Pack 1291;
- `REFERENCE_PATTERN` — mới là tham khảo, chưa adopted;
- `REMOVE_OR_REWORK` — đang lệch scope/spec.

### B — Reference digest [independent]

Dựa trên `BACKUP PICK PACK 1291` read-only, hoàn thiện digest/index theo chủ đề cần thiết cho cluster 1291. Không bê nguyên schema/logic sang VHDCHY.

### C — VHDCHY module map [independent]

Xác định boundary nền tảng chung vs cluster/module; tránh đặt entity đặc thù Pick/Pack vào core toàn DC nếu không cần.

### D — Reconciliation decision [depends on A+B+C]

Ghi rõ phần business core hiện tại: KEEP / ADAPT / REWORK / RETIRE. Chỉ sau đó mới mở rộng auth/business API.

### E — Parallel build plan [depends on D]

Tạo dependency graph để có thể triển khai song song các nhánh thích hợp: D1/core, Google Sheets/projection, API, Android/PDA, docs/tests. LAN/DO/DR chỉ vào plan khi có gate/use case.

## Owner action

`NONE` cho RECONCILE-001 trừ khi phát hiện business conflict thực sự cần Owner chốt.

## Release safety

Không move/promote `stable`. Không move `beta` nếu tranche chỉ là governance/reconciliation và chưa có tested runtime candidate.
