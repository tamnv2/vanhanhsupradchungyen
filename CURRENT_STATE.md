# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence through source commit: `42590dcf73ecbe8d1d8ee8dfbd6275aabf9d64fd`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

Phase 6 advances from 60% to 65% because the signed operational refresh/rebase/readiness path is now materially implemented with automated integration evidence. This does not credit live provider credentials, target-network/PDA evidence or physical outage acceptance.

## Reconciliation / operational refresh — SOURCE/CI PASS

- Durable LAN reconciliation queue, retry/restart/conflict mechanics and signed Cloud ingest foundations remain PASS.
- Post-reconciliation rebase tracker remains PASS, including stale snapshot rejection, incomplete canonical coverage rejection, full-coverage cursor advance and later-event reopening behavior.
- Cloud operational snapshot route now returns real Slice-1 D1 state for employees, employee codes and presence plus explicit canonical reconciliation coverage under machine authentication.
- Canonical coverage across a LAN restart is restricted by persistent `edgeInstanceId` while the current `edgeEpoch` remains part of machine request identity; no fabricated/default coverage is accepted.
- LAN now has the signed Cloud snapshot client, authoritative refresh coordinator/pump, atomic snapshot import and post-reconciliation rebase confirmation.
- Business readiness fails closed while reconciled canonical events remain unre-based and recovers only after verified authoritative coverage.
- Dedicated integration run `34851773729` on source commit `42590dcf73ecbe8d1d8ee8dfbd6275aabf9d64fd`: SUCCESS.
- Same-source clean-baseline run `34851772963`: SUCCESS, including authority invariants, Worker unit tests, clean D1 schema and schema/runtime contract validation.

## Cloud schema parity — BETA live PASS

A prior parity gap remains closed: Cloud `employee_codes` has guarded `entity_version` semantics required by the current command contract.

Evidence:

- clean-baseline run `34844597357`: SUCCESS;
- read-only BETA preflight run `34844821406`: SUCCESS, prestate ABSENT, zero employee-code rows;
- guarded live migration run `34844932823`: SUCCESS;
- poststate FINAL, row-count invariance PASS, metadata PASS, foreign-key check PASS and quick-check PASS.

Migration dispatch is disabled. Do not replay migration `0014_employee_code_entity_version.sql`.

## Immediate provider gate

The operational refresh/rebase implementation is accepted at source/CI level. The remaining direct provider gate is live BETA machine credential provisioning and exact LAN -> Cloud network proof against the real endpoint.

This lane is `OWNER_PERMISSION_REQUIRED`; do not request or infer raw credentials in chat and do not treat source/harness PASS as live provider PASS.

After Owner-controlled setup exists, require guarded provider preflight/postflight and prove the real chain:

`LAN signed request -> Cloud operational snapshot/coverage -> LAN atomic import -> covered rebase -> readiness recovery`.

## Blocked and parallel lanes

- Worker/LAN reconciliation credential provisioning: `OWNER_PERMISSION_REQUIRED`; lane-local and non-blocking for independent source work.
- Physical company-network/PDA acceptance and >=60-minute Internet-cut acceptance remain pending.
- ROOT OTP real delivery/recovery live E2E remains incomplete.
- Web authenticated business surfaces remain incomplete.
- Android/PDA final current-product UI/workflow implementation and real-device acceptance remain incomplete.
- Gateway/Google projection/upload receipt and readback coverage remains incomplete.
- Portrait replacement behavior remains an Owner decision gate.
- STABLE activation/promotion still requires mandatory BETA acceptance plus explicit Owner approval.
