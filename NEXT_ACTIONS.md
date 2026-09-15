# NEXT ACTIONS — VHDCHY

Updated: 2026-09-15
Progress: **56% displayed / 56.2% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A blocked lane does not stop unrelated ready work. `AI_TERMINATION_GUARD.md` forbids voluntary finalization while approved `READY` work remains.

## A — FIRST PRIORITY: Cloudflare projection Cron Trigger delivery — ACTIVE INVESTIGATION

The live projection E2E run `34924438129` proved the current failure occurs before the projection processor/GAS/Sheet path:

- Worker build verification PASS;
- Google management helper/readback PASS;
- isolated D1 marker insert PASS;
- outbox stayed `PENDING`, `attempts=0` for the full wait;
- scheduler diagnostic stayed `null`;
- cleanup PASS with no marker/probe residue and immutable trigger restored.

Independent observer run `34924945395` then proved the Cloudflare Cron Trigger exists as `*/2 * * * *`, created `2026-09-15T02:32:41.788727Z`, modified `2026-09-15T02:49:45.112867Z`; the E2E ran much later (`03:17–03:24Z`) and still saw no scheduled-handler entry.

### Exact next work

1. Bootstrap from GitHub `main` and verify latest HEAD/checkpoint before mutation.
2. Inspect the Cloudflare Worker module/entrypoint/provider configuration specifically for Scheduled Handler compatibility; do not rewrite processor/GAS logic without evidence.
3. Verify whether the uploaded ES module Worker form and configured Cron Trigger target the exact live script/version expected by Cloudflare Scheduled Events.
4. Use read-only provider evidence first. If a minimal isolated fix is justified, deploy guardedly without changing D1 identity, custom domain, workers.dev state or secret values.
5. Re-run the isolated projection live E2E only after a concrete root-cause change or provider-state correction. Require:
   - scheduler probe observed;
   - outbox claim/attempt observed;
   - ACKED state;
   - real GAS/Sheet row readback;
   - replay dedupe keeps exactly one logical row;
   - complete D1/Sheet/probe cleanup.
6. Only then remove temporary scheduler diagnostics/observer machinery that is no longer needed and run clean baseline + product foundations again.

Do not mark Google projection live PASS until this full chain passes.

## B — Cloud Worker / Google projection source state — PASS WHERE PROVEN

- Cloud Slice-1 reviewed mutations are source/hosted-CI PASS; portrait replace remains fail-closed.
- Projection materializer/retry/recovery/error isolation is source-tested.
- Google Gateway helper deployment/activation readback run `34923668193`: PASS.
- Worker deploy run `34924438089`: PASS.
- Deploy uploader no longer PUTs Cron Trigger schedules when unchanged; log recorded `WORKER_CRON_SCHEDULES_UNCHANGED`.
- Main clean baseline run `34924438117`: SUCCESS.
- Build product foundations run `34924438250`: Web/Cloud/Android/LAN SUCCESS.

These facts do not override the failed live Cron invocation evidence.

## C — Web Slice-1 — READY AFTER/ALONGSIDE ISOLATED CRON WORK

PR #19 remains open intentionally:

- title: `Add authenticated Web employee-create interaction`;
- head: `50d4be6bde5927eb8cc64ef3a851b7997c2886b0`;
- clean baseline `34924120821`: SUCCESS.

Before merge:

1. rebase/reconcile against current `main`;
2. verify projection diagnostic changes do not conflict;
3. rerun clean baseline and relevant Web/product CI;
4. merge only with fresh evidence.

Do not invent current-state/version read UX merely to expose update/status/MNV/attendance mutations; those remain fail-closed until a safe contract-backed UX exists.

## D — LAN/provider proof — PARTLY GATED

Retained source/HOSTED CI PASS:

- machine-authenticated operational snapshot/coverage route;
- signed LAN snapshot client and refresh coordinator;
- atomic authoritative import;
- post-reconciliation rebase confirmation;
- fail-closed readiness while canonical events remain unre-based;
- integration run `34851773729` and clean baseline `34851772963`: SUCCESS.

Physical company-network/PDA proof, intended Windows host acceptance, public-trust path and >=60-minute Internet-cut acceptance remain pending. Do not infer physical PASS from GitHub CI.

## E — Parallel lanes that remain READY where independent

1. **Account/security:** continue only provider-independent source/contract work. Previous secret/account mutation attempts hit tool capability/safety limits; do not repeatedly retry equivalent blocked mutations.
2. **Android/PDA:** continue endpoint/session/scanner/retry/HTTPS/reconnect mechanics that are independent of unavailable final visual assets. Do not fabricate Pick Pack UI details.
3. **Gateway/Google:** source-level bounded sender/error/retry/readback work may continue, but avoid changing already-proven logic just to explain the Cron no-invocation incident.
4. **Repo/governance:** keep Issue #8, `CURRENT_STATE.md`, `NEXT_ACTIONS.md` and `CHECKPOINT.md` synchronized with evidence before another long-session handoff.

## Retained complete/current nodes

- Cloud schema parity migration `0014_employee_code_entity_version.sql`: LIVE BETA PASS; do not replay.
- LAN integration receipt/conflict/recovery source/HOSTED CI evidence remains PASS.
- Projection E2E cleanup from failed runs is clean; do not re-clean absent markers blindly.

## Owner decision / release boundaries

- Portrait replacement behavior remains `OWNER_DECISION_REQUIRED`.
- STABLE activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall 56% displayed / 56.2% exact | Phase 6 65% | Projection source/deploy foundations PASS where evidenced | Live projection E2E FAIL: Cloudflare Cron never entered scheduled() | Exact next: root-cause Scheduled Event delivery, then rerun isolated E2E | Web PR #19 open+CI PASS but unmerged | Physical Windows/PDA/outage acceptance pending`
