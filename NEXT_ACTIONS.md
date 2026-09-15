# NEXT ACTIONS — VHDCHY

Updated: 2026-09-15
Progress: **56% displayed / 56.2% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A blocked lane does not stop unrelated ready work. `AI_TERMINATION_GUARD.md` forbids voluntary finalization while approved `READY` work remains.

## A — Projection Cron / Google Sheet live chain — CLOSED PASS

Do not continue the former Cron root-cause investigation unless new evidence reopens it.

Accepted evidence:

- isolated live projection E2E `34927511443`: SUCCESS;
- real canonical D1 marker/outbox -> ACK -> Google Sheet readback -> replay dedupe exactly one logical row -> full cleanup: PASS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- live BETA build `a501e4b7ae551cf1cd86f15786f4ca994032b522`, `BUSINESS_CORE_V3`, authority D1, Google not degraded;
- post-deploy observer `34927996370`: SUCCESS;
- active Cloudflare version 14 serves 100% with handlers `fetch` + `scheduled`;
- live Cron is exactly `*/2 * * * *`;
- outbox/pending count is 0;
- temporary scheduler-probe code/config has been removed.

The historical D1 `projection_scheduler_probe` row was written by the prior diagnostic build and is not evidence the current build still writes diagnostics.

## B — Web Slice-1 — FIRST AUTHENTICATED EMPLOYEE CREATE MERGED

PR #19 is merged at main commit `db746a71ed80dafd288218f599ab3e990f87e439`.

Evidence:

- reconciled against latest main with only 3 intended Web files differing;
- PR clean baseline `34928060646`: SUCCESS;
- post-merge clean baseline `34928090928`: SUCCESS;
- post-merge product foundations `34928090885`: Cloud/Web/Android/LAN SUCCESS.

Continue Web work only where current state/version contracts support safe UX. Do not expose update/status/MNV/attendance mutations by inventing a read/version model.

## C — NEXT READY: Account/security V6 source/contract work

Provider-independent work may proceed now.

Exact next sequence:

1. Inspect current Cloud + LAN auth routes/services/tests against `DECISIONS_V6.md`.
2. Map which V6 requirements already exist in source and which are missing:
   - ROOT normal login uses the fixed approved email one-time-password channel;
   - email OTP validity is 5 minutes and resend cooldown is 5 minutes;
   - normal-account forgot-password uses the registered recovery email and enters `MUST_CHANGE_PASSWORD` after successful OTP login;
   - ROOT TOTP is optional, but when it is enabled ROOT authentication must satisfy the TOTP factor in addition to the valid email OTP according to the current ROOT auth state machine;
   - request/use/failure/security events are auditable without OTP plaintext or provider secrets in logs/audit/source.
3. Implement the smallest contract-backed missing source slice that does **not** require secret/provider mutation.
4. Add/extend automated tests for request/use, expiry, cooldown, replay/consumption, audit redaction and ROOT TOTP gate semantics as applicable to the implemented slice.
5. Do **not** invent an OTP failure-attempt limit unless a later Owner decision or current authoritative security contract explicitly defines one.
6. Keep real email delivery/provider E2E separate until the required account/provider capability is available. Do not fake provider PASS and do not repeatedly retry equivalent blocked secret mutations.

## D — PARALLEL READY: Android/PDA mechanics

Continue mechanics independent of unavailable final visual references:

1. inspect current endpoint selection/cache/discovery/manual recovery behavior;
2. session/login persistence and expiry handling;
3. scanner input/command handoff;
4. retry/backoff/reconnect/resync under LAN/Wi-Fi loss;
5. HTTPS/trust/fail-closed behavior;
6. background/foreground recovery where source-testable.

Current APK build is green, but that is not final visual/workflow acceptance. Do not fabricate Pick Pack 1291 visual details without authorized current-product reference assets.

## E — LAN/provider proof — PARTLY GATED

Retained source/HOSTED CI PASS:

- machine-authenticated operational snapshot/coverage route;
- signed LAN snapshot client and refresh coordinator;
- atomic authoritative import;
- post-reconciliation rebase confirmation;
- fail-closed readiness while canonical events remain unre-based;
- integration run `34851773729` and clean baseline `34851772963`: SUCCESS.

Still pending and not inferable from CI:

- intended company Windows host acceptance;
- company-network/PDA/public-trust path;
- live LAN->Cloud machine/provider linkage where Owner-controlled setup is required;
- >=60-minute Internet-cut acceptance;
- reconnect/host restart/network-change physical regression;
- capacity/soak/UAT.

## F — Gateway / integrations

The projection path is now live-PASS. Continue only genuinely open integration coverage:

- bounded provider failure/recovery;
- receipts/idempotency/reconciliation for broader current flows;
- Drive/media/provider paths still lacking accepted live evidence;
- operator-visible status and safe retry behavior.

Do not rewrite the working projection processor/GAS path without a new failing evidence chain.

## G — Repo/governance

Keep synchronized with evidence:

- Issue #8;
- `CURRENT_STATE.md`;
- `NEXT_ACTIONS.md`;
- `CHECKPOINT.md`.

Progress remains **56.2% exact / 56% displayed** until a weighted phase threshold is defensibly changed under `docs/PROGRESS_TRACKING_V1.md`.

## Retained complete/current nodes

- Cloud schema parity migration `0014_employee_code_entity_version.sql`: LIVE BETA PASS; do not replay.
- LAN integration receipt/conflict/recovery source/HOSTED CI evidence remains PASS.
- Projection E2E cleanup is clean; do not re-clean absent E2E markers blindly.
- Read-only Cron observer now supports manual dispatch; use it for evidence instead of mutating runtime merely to observe state.

## Owner decision / release boundaries

- Portrait replacement behavior remains `OWNER_DECISION_REQUIRED`.
- STABLE activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall 56% displayed / 56.2% exact | Phase 6 65% | Projection live BETA PASS end-to-end | Web employee-create merged + post-merge CI PASS | Next READY: V6 account/security source/contract work + Android mechanics in parallel | Physical Windows/PDA/outage acceptance pending`
