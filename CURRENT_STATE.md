# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence reconciled through live main before this state update: `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

The percentage remains unchanged. Projection live E2E, the first authenticated Web employee-create interaction and the provider-independent V6 email-OTP request/use source slice close useful implementation/evidence gaps, but physical LAN/PDA/outage acceptance, live OTP provider delivery, broader business/provider coverage and final Web/App coverage still prevent a defensible weighted phase-threshold increase.

## Projection / Cloudflare Cron / Google Sheet — LIVE BETA PASS

The earlier scheduled-event incident is closed by direct live evidence; do not continue treating it as an active root-cause lane.

Accepted chain:

- read-only observer `34926987360` proved the active Worker exported `fetch` + `scheduled`, live Cron was `*/2 * * * *`, and a real scheduled execution recorded `PROJECTION_IDLE`;
- isolated live E2E `34927511443`: `SUCCESS`;
- canonical D1 marker/outbox -> ACK -> real Google Sheet readback -> replay dedupe with exactly one logical row -> full D1/Sheet cleanup: PASS;
- successful ACK rows correctly retain `attempts=0`; attempts increment only on failure;
- temporary scheduler-probe runtime/config was removed;
- cleaned Worker deploy `34927869845`: `SUCCESS`;
- public BETA health converged to build `a501e4b7ae551cf1cd86f15786f4ca994032b522`, runtime `BUSINESS_CORE_V3`, authority `D1`, Google not degraded;
- post-deploy observer `34927996370`: `SUCCESS`;
- active version 14 serves 100% with handlers `fetch` + `scheduled` and Cron remains exactly `*/2 * * * *`;
- projection outbox/pending count is 0.

The historical `projection_scheduler_probe` D1 row was written by the prior diagnostic build and is not produced by the current Worker.

## Web Slice-1 — FIRST AUTHENTICATED MUTATION MERGED

PR #19 `Add authenticated Web employee-create interaction` is merged at `db746a71ed80dafd288218f599ab3e990f87e439`.

Evidence:

- fresh PR clean baseline `34928060646`: `SUCCESS`;
- post-merge clean baseline `34928090928`: `SUCCESS`;
- post-merge product foundations `34928090885`: Cloud Service / Web contract / Android APK / LAN Service all `SUCCESS`.

The personnel screen can create an employee through the authenticated shared Cloud/LAN business client. Update/status/MNV/attendance interactions remain fail-closed until safe current-state/version-backed UX exists.

## V6 account/security — EMAIL OTP SOURCE/CI PASS, LIVE PROVIDER GATED

PR #30 `Add V6 email OTP request/use auth slice` is merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

Accepted source/CI evidence:

- PR clean baseline `34930120980`: `SUCCESS`;
- post-merge clean baseline `34930171765`: `SUCCESS`;
- post-merge product foundations `34930171683`: Cloud Service / Web contract / Android APK / LAN Service all `SUCCESS`.

Implemented semantics:

- Cloud HTTP contract now includes fixed email-OTP request/use paths;
- normal-account recovery uses the registered `auth_users.email` and successful recovery login issues a restricted `MUST_CHANGE_PASSWORD` session;
- ROOT actual destination remains runtime-only and must hash-match an ACTIVE `root_recovery_allowlist` entry;
- existing four-digit, 5-minute validity, 5-minute resend cooldown and atomic single-use lifecycle is reused;
- delivery is injected through a runtime binding and fails closed if provider/runtime inputs are absent;
- no client request can self-assert TOTP success;
- if ROOT has an active verified TOTP enrollment, email-OTP use fails closed with `ROOT_TOTP_REQUIRED` before OTP consumption unless a trusted server-side TOTP verifier has already satisfied that factor;
- unit tests cover normal recovery, ROOT allowlist, single-factor ROOT, TOTP gate, `MUST_CHANGE_PASSWORD` and delivery-adapter redaction/fail-closed behavior.

Not yet accepted:

- no BETA deploy was performed for this auth slice because reviewed OTP runtime provider bindings/secrets are not provisioned;
- real ROOT/normal email delivery request/use E2E is still pending;
- repo has TOTP enrollment schema/policy but no current trusted TOTP verifier. Active authority does not yet lock enough operational verifier detail (algorithm/digits/time-step/window and `secret_ref` resolution) to safely invent an implementation; the current path therefore remains fail-closed when TOTP is enabled.

`NEXT_ACTIONS.md` was corrected so it no longer invents an OTP failure-attempt limit and no longer treats TOTP as an alternative to ROOT email OTP when TOTP is enabled.

## Android/PDA mechanics — CURRENT FOUNDATION + ENDPOINT ACQUISITION WORK IN PROGRESS

Current main source already has HTTPS-only Cloud/LAN endpoint validation, in-memory session expiry, bounded idempotent retry/backoff, same-runtime reconnect decisions, scanner payload normalization and authenticated request planning. APK/product-foundation build is green.

Delivery Plan Phase 6.2 explicitly approves the reusable endpoint order `cached healthy endpoint -> LAN discovery -> manual recovery` plus anti-flapping/reconnect behavior. A separate branch `ai/android-endpoint-acquisition-20260915` is implementing this ordering with health-probed HTTPS-only candidates and automated harness coverage. That branch is **IN PROGRESS / UNVERIFIED** until its PR/build evidence passes and must not be counted as accepted product progress yet.

Final visual/workflow fidelity remains separate and must not invent Pick Pack 1291 details without authorized source/artifact evidence.

## LAN continuity / reconciliation — SOURCE/HOSTED CI PASS, PHYSICAL ACCEPTANCE PENDING

Retained accepted evidence:

- signed Cloud operational snapshot/coverage route;
- LAN signed snapshot client, authoritative import, refresh/rebase coordinator and fail-closed readiness while canonical events remain unre-based;
- durable reconciliation queue/retry/restart/conflict mechanics;
- integration `34851773729`: `SUCCESS`;
- clean baseline `34851772963`: `SUCCESS`;
- Google/Drive integration receipt conflict/recovery `34853854932`, `34853938581`, `34879543693`; clean baselines `34853938669`, `34879543810`: `SUCCESS` at source/HOSTED CI level.

Physical company-network/PDA/public-trust, intended Windows host acceptance, live LAN->Cloud machine/provider linkage and >=60-minute Internet-cut acceptance remain separate gates.

## Cloud schema parity — LIVE BETA PASS

Migration `0014_employee_code_entity_version.sql` remains accepted via `34844597357`, `34844821406`, `34844932823`. Do not replay migration 0014.

## Current ready lanes

1. **Android/PDA mechanics:** finish and validate endpoint acquisition ordering, then continue session persistence/expiry, scanner handoff, reconnect/resync, HTTPS/trust and lifecycle mechanics independent of final visuals.
2. **Account/security:** real OTP provider/binding E2E is gated; provider-independent auth work may continue only where current authority is sufficient. TOTP verifier crypto/runtime details must not be invented.
3. **Web/business UI:** continue only interactions backed by safe current-state/version contracts and V6 auth semantics.
4. **Gateway/integrations:** broader bounded failure/recovery/receipt/provider coverage may continue without disturbing the proven projection path.
5. **Repo/governance:** keep Issue #8, this file, `NEXT_ACTIONS.md` and `CHECKPOINT.md` synchronized with accepted evidence.

## Current blockers / owner gates

- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance: physical environment required;
- real ROOT/normal email-OTP provider delivery/request/use E2E: runtime provider/bindings/secrets required;
- ROOT TOTP verifier implementation: current operational verifier parameters/secret resolution are not sufficiently locked to invent safely;
- final Android/PDA visual fidelity: requires authorized current-product Pick Pack reference assets;
- portrait replacement behavior: `OWNER_DECISION_REQUIRED`;
- STABLE activation/promotion: explicit Owner approval only after mandatory BETA acceptance.

## Resume instruction

A fresh chat must live-read `AI_ENTRYPOINT.md` from GitHub `main`, execute its bootstrap, reconcile HEAD against `CHECKPOINT.md`, then continue `NEXT_ACTIONS.md`. Memory/chat summaries are locators only, not authority.
