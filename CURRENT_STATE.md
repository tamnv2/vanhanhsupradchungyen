# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Protocol: `AI_AUTHORITY_RESUME_V2`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% displayed | Exact weighted baseline: 55.4% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

No progress increase is recorded from the latest block. Runtime post-reconciliation rebase enforcement and signed live LAN->Cloud->refresh/rebase acceptance are still incomplete.

## Reconciliation / rebase

- Existing durable LAN reconciliation queue, retry/restart/conflict mechanics and signed Cloud ingest foundations remain source/CI PASS.
- Post-reconciliation rebase tracker is SOURCE/HARNESS PASS.
- Dedicated run `34844270179`: SUCCESS.
- Proven vectors: stale snapshot rejection, incomplete canonical coverage rejection, complete coverage clearing the covered backlog, and correct cursor behavior when a later canonical event appears.
- Runtime readiness enforcement is not yet accepted. The attempted direct security-gate integration was blocked by platform safety guard; no bypass was attempted.

## Cloud schema parity — BETA live PASS

A real parity gap was closed: Cloud `employee_codes` now has guarded `entity_version` semantics required by the current command contract.

Evidence:

- clean-baseline run `34844597357`: SUCCESS;
- read-only BETA preflight run `34844821406`: SUCCESS, prestate ABSENT, zero employee-code rows;
- guarded live migration run `34844932823`: SUCCESS;
- poststate FINAL, row-count invariance PASS, metadata PASS, foreign-key check PASS and quick-check PASS.

Migration dispatch is back to disabled state. Do not replay this migration.

## Immediate source dependency

Cloud still lacks an accepted operational snapshot/delta route that supplies authoritative Slice-1 current state plus explicit canonical reconciliation coverage to LAN.

Next implementation node:

1. add/test the machine-authenticated Cloud operational snapshot/delta route using the existing request-auth model and exact edge identity;
2. return real canonical Slice-1 state and explicit reconciliation coverage from D1;
3. connect LAN refresh/rebase consumption and fail-closed readiness enforcement;
4. prove canonical ACK -> stale local state -> authoritative refresh -> covered rebase -> readiness recovery end to end.

No fabricated/default coverage may be used.

## Blocked and parallel lanes

- Worker reconciliation credential provisioning: `OWNER_PERMISSION_REQUIRED`; this is lane-local and does not block independent source work.
- Physical company-network/PDA acceptance and >=60-minute Internet-cut acceptance remain pending.
- ROOT OTP real delivery/recovery live E2E remains incomplete.
- Portrait replacement behavior remains an Owner decision gate.
- STABLE activation/promotion still requires mandatory BETA acceptance plus explicit Owner approval.
