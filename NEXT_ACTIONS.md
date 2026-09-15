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
- canonical D1 marker/outbox -> ACK -> real Google Sheet readback -> replay dedupe exactly one logical row -> full cleanup: PASS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- active Cloudflare version exports `fetch` + `scheduled`, Cron is exactly `*/2 * * * *`, outbox/pending count is 0.

## B — Web Slice-1 — FIRST AUTHENTICATED EMPLOYEE CREATE MERGED PASS

PR #19 is merged at `db746a71ed80dafd288218f599ab3e990f87e439`.

- PR clean baseline `34928060646`: SUCCESS;
- post-merge clean baseline `34928090928`: SUCCESS;
- product foundations `34928090885`: Cloud/Web/Android/LAN SUCCESS.

Continue Web work only where current state/version contracts support safe UX. Do not invent a read/version model merely to expose update/status/MNV/attendance actions.

## C — Account/security V6 email OTP source slice — MERGED PASS / PROVIDER LIVE GATED

PR #30 is merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

Evidence:

- PR clean baseline `34930120980`: SUCCESS;
- post-merge clean baseline `34930171765`: SUCCESS;
- post-merge product foundations `34930171683`: Cloud/Web/Android/LAN SUCCESS.

Current source now provides:

- fixed Cloud HTTP email-OTP request/use paths;
- normal-account registered-email recovery -> restricted `MUST_CHANGE_PASSWORD` session;
- ROOT runtime-only recovery destination checked against the ACTIVE hashed allowlist;
- 4-digit / 5-minute validity / 5-minute resend cooldown / single-use semantics;
- injected delivery binding with fail-closed behavior when runtime provider inputs are absent;
- ROOT TOTP gate before OTP consumption when active verified TOTP exists;
- no client-supplied flag can claim that TOTP has been verified.

Still gated:

1. Do **not** deploy/claim live OTP delivery until the approved runtime delivery provider/binding, OTP pepper and ROOT destination are provisioned through a reviewed provider path.
2. Real ROOT + normal-account request/delivery/use E2E remains required.
3. Current authority does not lock enough TOTP verifier operational detail to invent algorithm/digits/time-step/window or `secret_ref` resolution. Keep TOTP-enabled ROOT fail-closed until that contract is resolved or an authoritative existing verifier is surfaced.
4. Do not invent an OTP failure-attempt limit; V6 does not define one.

Provider-independent auth work may continue only where those gates are not prerequisites.

## D — CURRENT PRIMARY READY: Android/PDA endpoint mechanics

Current main already has HTTPS endpoint validation, in-memory session expiry, bounded retry, same-runtime reconnect decisions, scanner normalization and authenticated request planning.

Delivery Plan Phase 6.2 locks the reusable endpoint order:

`cached healthy endpoint -> LAN discovery -> manual recovery`

Current in-progress branch: `ai/android-endpoint-acquisition-20260915`.

Exact next sequence:

1. complete pure-Java LAN endpoint acquisition policy with:
   - HTTPS-only normalized candidates;
   - cached endpoint selected only after health probe passes;
   - healthy cache prevents discovery/manual flapping;
   - invalid/unhealthy cache falls through to discovery;
   - untrusted invalid discovery candidates are discarded;
   - manual endpoint is considered only after cache/discovery fail;
   - no automatic Cloud/LAN authority switch;
   - no fabricated endpoint when all probes fail;
2. wire an automated harness into Android `preBuild`;
3. write checkpoint last so freshness validation sees all source changes before its reconciliation anchor;
4. open PR, run clean baseline + Android product build, fix until green, merge exact tested head if clean;
5. after merge, reconcile governance and continue the next source-testable Android gap:
   - session persistence/expiry;
   - scanner command handoff;
   - reconnect/resync and network lifecycle;
   - HTTPS/trust fail-closed behavior;
   - foreground/background recovery.

Do not fabricate Pick Pack 1291 final UI details. Current acceptance UI remains Vietnamese only.

## E — LAN/provider proof — PARTLY GATED

Retained source/HOSTED CI PASS:

- machine-authenticated operational snapshot/coverage route;
- signed LAN snapshot client and refresh coordinator;
- atomic authoritative import;
- post-reconciliation rebase confirmation;
- fail-closed readiness while canonical events remain unre-based;
- integration `34851773729` and clean baseline `34851772963`: SUCCESS.

Still pending and not inferable from CI:

- intended company Windows host acceptance;
- company-network/PDA/public-trust path;
- live LAN->Cloud machine/provider linkage where Owner-controlled setup is required;
- >=60-minute Internet-cut acceptance;
- reconnect/host restart/network-change physical regression;
- capacity/soak/UAT.

## F — Gateway / integrations

The projection path is live-PASS. Continue only genuinely open integration coverage:

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
- Projection E2E cleanup is clean; do not re-clean absent markers blindly.
- Read-only Cron observer supports manual dispatch; use it for evidence instead of mutating runtime merely to observe state.

## Owner decision / release boundaries

- Portrait replacement behavior remains `OWNER_DECISION_REQUIRED`.
- STABLE activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall 56% displayed / 56.2% exact | Phase 6 65% | Projection live PASS | Web employee-create merged PASS | V6 email-OTP source + post-merge CI PASS, provider live E2E gated | Android endpoint acquisition branch IN PROGRESS | Physical Windows/PDA/outage acceptance pending`
