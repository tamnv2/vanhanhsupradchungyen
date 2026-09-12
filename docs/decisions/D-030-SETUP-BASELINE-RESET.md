# D-030 — SETUP BASELINE RESET

Status: ACTIVE / OWNER-APPROVED 2026-09-12

## Decision

Reset toàn bộ **current provider/setup state** của VHDCHY về baseline mới, sau đó setup lại theo dependency thực tế.

## Preserve

Không xóa hoặc phủ nhận:

- approved architecture/business logic;
- source code/migrations;
- governance model;
- historical changelog/decisions;
- LAN pilot source/build/physical evidence.

## Reset

Không còn coi các giá trị sau là current chỉ vì đã từng PASS:

- old Google account/project/client/token;
- GAS Script ID/deployment/exec URL;
- old projection Sheet IDs;
- old GitHub Environment values/secrets;
- old provider LIVE/DONE claims.

## Reuse rule

Nếu resource hiện hữu dưới current account được tool/provider xác minh đúng owner, đúng purpose và đủ quyền thì re-use thay vì xóa/tạo lại. “Setup từ đầu” là setup lại **authority và verification chain**, không phải phá resource hợp lệ.

## Safety

Pre-reset snapshot được giữ ở branch `archive/pre-setup-reset-20260912`.
