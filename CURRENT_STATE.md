# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence through source/validation commit: `da216d54119280aeed57f19d79ccdde6091e6036`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

Phase 6 remains at 65%. The newly accepted conflict/recovery evidence closes an automated-evidence gap inside the already credited LAN reconciliation sub-slice, but does not yet justify another weighted progress increase because live provider linkage, target-network/PDA evidence and physical outage acceptance remain open.

## Reconciliation / operational refresh — SOURCE/HOSTED CI PASS

- Durable LAN reconciliation queue, retry/restart/conflict mechanics and signed Cloud ingest foundations remain PASS.
- Post-reconciliation rebase tracker remains PASS, including stale snapshot rejection, incomplete canonical coverage rejection, full-coverage cursor advance and later-event reopening behavior.
- Cloud operational snapshot route returns real Slice-1 D1 state for employees, employee codes and presence plus explicit canonical reconciliation coverage under machine authentication.
- Canonical coverage across a LAN restart is restricted by persistent `edgeInstanceId` while the current `edgeEpoch` remains part of machine request identity; no fabricated/default coverage is accepted.
- LAN has the signed Cloud snapshot client, authoritative refresh coordinator/pump, atomic snapshot import and post-reconciliation rebase confirmation.
- Business readiness fails closed while reconciled canonical events remain unre-based and recovers only after verified authoritative coverage.
- Dedicated integration run `34851773729` on source commit `42590dcf73ecbe8d1d8ee8dfbd6275aabf9d64fd`: SUCCESS.
- Same-source clean-baseline run `34851772963`: SUCCESS, including authority invariants, Worker unit tests, clean D1 schema and schema/runtime contract validation.

## Google/Drive integration receipt conflict + recovery — SOURCE/HOSTED CI PASS

The LAN integration-output/reconciliation layer now has direct hosted evidence for the source behavior that was previously only partially covered.

Accepted behavior:

- Google projection and Drive output work use durable local queue/receipt state rather than becoming business authority;
- provider completion evidence is read back and persisted, with idempotent replay of matching evidence;
- conflicting provider receipt evidence moves the work/receipt to `REVIEW_REQUIRED` instead of silently overwriting the first evidence;
- review-required integration work is excluded from reconciliation receipt attachment;
- Cloud entity-version conflict remains explicit in `edge_conflicts`/reconciliation state;
- interrupted integration claims and Cloud reconciliation claims recover after restart;
- `EdgeStore.ReadStatusAsync().ConflictCount` includes both open/review edge conflicts and integration output rows requiring review;
- production LAN startup invokes integration-output interrupted-claim recovery;
- production `/health` exposes the recovered integration-claim count;
- production `/api/v1/sync/status` exposes integration review work, completed/review receipt counts and aggregate conflict count.

Evidence:

- historical conflict commit `53ae98a530228badaeaeb9dc2cb03a905ec8df82`, dedicated run `34853854932`: SUCCESS for receipt conflict/cloud conflict/restart mechanics;
- historical recovery commit `3d0a732e33a8d4ce4ac97c3e4efaf9f58838381c`, dedicated run `34853938581`: SUCCESS; same-commit clean baseline `34853938669`: SUCCESS;
- focused regression/production-runtime proof commit `da216d54119280aeed57f19d79ccdde6091e6036`;
- dedicated run `34879543693`: SUCCESS, including `Prove production runtime integration recovery and conflict status` SUCCESS and post-reconciliation rebase vectors SUCCESS;
- same-commit clean baseline `34879543810`: SUCCESS.

This is **HOSTED CI**, not live Google provider acceptance and not physical company-network/PDA acceptance.

## Cloud schema parity — BETA live PASS

A prior parity gap remains closed: Cloud `employee_codes` has guarded `entity_version` semantics required by the current command contract.

Evidence:

- clean-baseline run `34844597357`: SUCCESS;
- read-only BETA preflight run `34844821406`: SUCCESS, prestate ABSENT, zero employee-code rows;
- guarded live migration run `34844932823`: SUCCESS;
- poststate FINAL, row-count invariance PASS, metadata PASS, foreign-key check PASS and quick-check PASS.

Migration dispatch is disabled. Do not replay migration `0014_employee_code_entity_version.sql`.

## Immediate provider gate

The operational refresh/rebase and local integration conflict/recovery implementations are accepted at source/hosted-CI level. The remaining direct provider gate is live BETA machine credential provisioning and exact LAN -> Cloud network proof against the real endpoint, plus later real Google provider linkage where applicable.

The machine-credential lane is `OWNER_PERMISSION_REQUIRED`; do not request or infer raw credentials in chat and do not treat source/harness PASS as live provider PASS.

After Owner-controlled setup exists, require guarded provider preflight/postflight and prove the real chain:

`LAN signed request -> Cloud operational snapshot/coverage -> LAN atomic import -> covered rebase -> readiness recovery`.

## Blocked and parallel lanes

- Worker/LAN reconciliation credential provisioning: `OWNER_PERMISSION_REQUIRED`; lane-local and non-blocking for independent source work.
- Physical company-network/PDA acceptance and >=60-minute Internet-cut acceptance remain pending.
- ROOT OTP real delivery/recovery live E2E remains incomplete.
- Web authenticated business surfaces remain incomplete.
- Android/PDA final current-product UI/workflow implementation and real-device acceptance remain incomplete.
- Live Google Sheets/Drive provider projection/upload acceptance remains incomplete even though local receipt/readback/conflict/recovery mechanics are now HOSTED CI PASS.
- Portrait replacement behavior remains an Owner decision gate.
- STABLE activation/promotion still requires mandatory BETA acceptance plus explicit Owner approval.
