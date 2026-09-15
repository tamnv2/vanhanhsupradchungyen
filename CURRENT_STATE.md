# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence reconciled through live main before handoff: `c6dba66b00723baba65c78327b167227f606ef40`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

Do not increase the percentage from source-only work, diagnostic tooling, provider polling, or unaccepted live tests. Phase 6 remains 65% until acceptance-backed evidence closes additional weighted scope.

## Current critical lane — Google projection / Cloudflare Cron Trigger

### Source and provider state already proven

- Cloud Slice-1 canonical mutation source is implemented and hosted-CI PASS for the reviewed employee/attendance commands; `EMPLOYEE_PORTRAIT_REPLACE` remains fail-closed.
- Projection outbox processor includes bounded retry, stale `PROCESSING` recovery, materialization isolation, sender/readback behavior and no fabricated employee-code identity.
- Google Gateway management/E2E helpers were deployed; activation/readback run `34923668193` passed.
- Cloudflare Worker deploy run `34924438089` passed on build `6c4313aeb9539a1b99bbe751e24b24179f6a1fd7`.
- Deploy evidence proves `PROJECTION_DELIVERY_ENABLED=true`, the projection secret binding exists without exposing its value, Worker runtime is `BUSINESS_CORE_V3`, custom domain remains `beta.supra.cc.cd`, and Worker health/postflight passed.
- Uploader now reads current Cron Triggers first and does not rewrite the schedule when unchanged. The deploy log recorded `WORKER_CRON_SCHEDULES_UNCHANGED schedules=["*/2 * * * *"]`.
- Build product foundations run `34924438250`: Web/Cloud/Android/LAN all SUCCESS.
- Clean baseline run `34924438117`: SUCCESS.

### Live E2E result — FAIL isolated to scheduler invocation

Run `34924438129` tested the real chain with a synthetic, isolated marker and mandatory cleanup.

Evidence:

- exact Worker build check passed;
- Google management helper/readback passed;
- marker inserted into canonical D1 `domain_events` + `projection_outbox`;
- after the full wait, outbox remained `PENDING`, `attempts=0`, `next_attempt_at=null`;
- diagnostic scheduler probe remained `null`;
- therefore no evidence exists that Cloudflare invoked the Worker's `scheduled()` handler during the test window;
- the failure occurred before projection processor/GAS/Sheet delivery, so do not misclassify this as a Google Gateway or outbox-processor failure.

Cleanup evidence from the same failed run:

- `PROJECTION_E2E_CLEANUP_PASS`;
- Sheet marker rows remaining: 0;
- D1 E2E marker rows remaining: 0;
- scheduler probe rows remaining: 0;
- immutable domain-event delete trigger restored and verified PASS.

### Independent read-only Cron observer

PR #21 merged read-only diagnostic workflow at main `c6dba66b00723baba65c78327b167227f606ef40`.

Observer run `34924945395`: SUCCESS and recorded:

- schedule: `*/2 * * * *`;
- trigger `created_on=2026-09-15T02:32:41.788727Z`;
- trigger `modified_on=2026-09-15T02:49:45.112867Z`;
- at observation time after E2E cleanup: scheduler probe empty, projection outbox empty, pending count 0.

The trigger had therefore existed well beyond the expected propagation window before the `03:17–03:24Z` E2E, yet the Worker scheduler probe never recorded entry. The next root-cause work must focus on Cloudflare Cron Trigger delivery/entrypoint/provider behavior, not on rewriting projection business logic blindly.

## Web Slice-1 parallel lane

PR #19 `Add authenticated Web employee-create interaction` remains OPEN and intentionally unmerged while the projection Cron incident is being isolated.

- PR head: `50d4be6bde5927eb8cc64ef3a851b7997c2886b0`.
- Clean-baseline run `34924120821`: SUCCESS.
- It adds the first real authenticated employee-create interaction through the shared Cloud/LAN business client without inventing a read/version contract.
- Update/status/MNV/attendance interactions remain fail-closed until safe current-state/version UX exists.
- Before merging PR #19, rebase/reconcile it against latest `main` and rerun CI; do not merge solely to make progress while the provider diagnostic lane is unresolved.

## Retained accepted evidence

### LAN reconciliation / operational refresh — SOURCE/HOSTED CI PASS

- Durable LAN reconciliation queue, retry/restart/conflict mechanics and signed Cloud ingest foundations remain PASS.
- Post-reconciliation rebase tracker, canonical coverage rules, authoritative snapshot import and readiness recovery remain PASS.
- Dedicated integration run `34851773729`: SUCCESS.
- Same-source clean baseline `34851772963`: SUCCESS.

### Google/Drive integration receipt conflict + recovery — SOURCE/HOSTED CI PASS

- Durable local Google/Drive work + receipts, idempotent matching receipt replay, conflict to `REVIEW_REQUIRED`, interrupted-claim restart recovery and production status visibility remain PASS at source/hosted-CI level.
- Dedicated runs `34853854932`, `34853938581`, `34879543693` and clean baselines `34853938669`, `34879543810`: SUCCESS.
- This does not by itself prove live Google provider projection delivery.

### Cloud schema parity — BETA live PASS

Migration `0014_employee_code_entity_version.sql` remains accepted. Evidence `34844597357`, `34844821406`, `34844932823` PASS. Do not replay migration 0014.

## Current blockers / gates

- Cloudflare projection Cron Trigger delivery: ACTIVE ROOT-CAUSE INVESTIGATION; trigger exists but scheduled handler invocation was not observed.
- Physical company-network/PDA acceptance and >=60-minute Internet-cut acceptance remain pending.
- ROOT OTP real delivery/recovery E2E remains incomplete.
- Account/security connector-secret mutation attempts previously hit tool capability/safety limits; do not repeatedly hammer equivalent blocked mutations.
- Android/PDA final visual/workflow fidelity remains blocked on authorized current-product reference assets; do not fabricate visuals.
- Portrait replacement behavior remains an Owner decision gate.
- STABLE activation/promotion still requires mandatory BETA acceptance plus explicit Owner approval.

## Resume instruction

A fresh chat must live-read `AI_ENTRYPOINT.md` from GitHub `main`, execute the required bootstrap, reconcile HEAD against `CHECKPOINT.md`, then continue the exact next actions in `NEXT_ACTIONS.md`. Memory/chat summaries are locators only, not authority.
