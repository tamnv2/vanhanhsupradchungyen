# NEXT ACTIONS

Checkpoint: `RECONCILE-20260910-02`

## Mục tiêu kế tiếp

`PARALLEL-BUILD-001 — chuyển từ reconciled baseline sang core/auth/projection/Android foundation có thể tích hợp.`

## Work streams độc lập có thể khởi động song song

### A — CORE-REFINE-001 [independent]

Thiết kế additive migration `0002+` để:

- đưa resource taxonomy về generic/configurable DC model;
- đăng ký resource types của `PICK_PACK_1291` theo module thay vì global hard-code;
- bổ sung/chuẩn hóa catalog cần cho labor/position/module semantics;
- giữ migration 0001 bất biến và bảo toàn BETA data/schema compatibility.

Gate: local migration validation + invariants + no destructive rewrite.

### B — SHEETS-001 [independent until projection integration]

Workbook BETA đã tạo. Tiếp theo:

- chuẩn hóa tab map contract cho schema `PP1291_SHEETS_BETA_V1`;
- đăng ký `PICK_PACK_1291 + 2026_Q3` vào projection catalog bằng additive/config-safe path;
- thiết kế batch projection payload/idempotency/ACK/checkpoint;
- chọn và test transport machine-to-machine an toàn trước khi bật dữ liệu thật;
- không để Google availability chặn canonical D1 mutation.

### C — AUTH-001 [independent]

Xây generic schema/contracts cho:

- users;
- roles;
- permissions;
- grants/effective permissions;
- sessions/tokens;
- device registry;
- auth/audit events.

Không mở privileged ROOT/SUPERADMIN credential acceptance cho tới khi current VHDCHY Master Spec xác nhận chính xác semantics. Không copy thuật toán auth cũ từ backup làm mặc định.

### D — ANDROID-FOUNDATION-001 [independent]

Có thể bắt đầu:

- BETA/STABLE environment contract;
- API client foundation;
- device identity;
- local durable state/outbox;
- replay/idempotency envelope;
- update/release skeleton.

Privileged login UI/acceptance logic để adapter pending, không invent.

## Integration gates

1. `CORE-REFINE-001` + `AUTH-001` schema/contracts PASS trước khi mở protected mutation.
2. `SHEETS-001` transport/projection PASS với synthetic data trước dữ liệu thật.
3. Android chỉ bind vào API contract đã versioned/verified.
4. Durable Objects, LAN Agent, emergency fallback và R2 vẫn ngoài tranche cho tới khi có use case/measurement gate.

## Owner action

`NONE` ở đầu tranche. Chỉ hỏi Owner nếu current Master Spec không thể được truy hồi và exact privileged-login semantics trở thành blocker thực tế.

## Release safety

- Không move `stable`.
- Không move `beta` chỉ vì source trên `main` tiến lên.
- Chỉ move `beta` tới candidate mới sau validation/migration/integration gate phù hợp.
