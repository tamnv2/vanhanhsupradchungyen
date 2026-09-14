# NEXT ACTIONS — VHDCHY

Updated: 2026-09-14
Delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress: `docs/PROGRESS_TRACKING_V1.md`
Current overall: **55% displayed / 55.4% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A failed lane does not stop unrelated ready work.

## A — Aggregate LAN regression — IMMEDIATE

At HEAD `4b202351c62d7439569fb72b9aa39794c34769c7`:

- `Validate LAN Cloud reconciliation queue` run `34833117039`: **SUCCESS**.
- `Validate clean baseline` run `34833117051`: **SUCCESS**.
- `Build portable LAN Windows host package` run `34833117192`: **SUCCESS**.
- aggregate `Build product foundations` run `34833117353`: **FAILURE** only in job `lan-service` / check `103940791504`, step `Build and smoke-test LAN Service`.
- sibling jobs `android-apk`, `web-contract`, `cloud-service`: **SUCCESS**.

Next: inspect and repair the aggregate LAN smoke-test regression, then require both the aggregate LAN job and the dedicated reconciliation harness to PASS at the repaired HEAD. Do not raise project % from CI repair alone unless the progress model justifies it.

## B — Cloud/LAN reconciliation — SOURCE/CI PASS, LIVE E2E PENDING

Current PASS source boundary:

- durable LAN reconciliation queue and immutable event envelope;
- isolated Worker route `POST /api/v1/reconciliation/events`;
- signed machine-request verification bound to request metadata and body hash;
- stale/unsigned/mismatched requests rejected;
- LAN HTTPS sender compatible with the Worker verifier;
- background sender starts only with complete runtime configuration;
- `RECEIVED` is durable inbox receipt only and is not treated as final `RECONCILED`/canonical commit.

Evidence:

- Worker route coverage commit `a2932d95fb8289ad78be7102d597d3b4a95561ce`;
- clean baseline run `34832612667`: **SUCCESS**;
- LAN reconciliation run `34833117039`: **SUCCESS**;
- same-HEAD baseline `34833117051`: **SUCCESS**.

Next: complete real BETA network E2E, add/prove final reconciliation acknowledgement semantics, prove retry/restart/idempotency over real HTTP transport, verify downstream receipt no-duplicate behavior, retain explicit conflict handling, then add cursor/delta/rebase after transport is stable.

## C — Physical LAN trust chain — PRIMARY PHYSICAL/PROVIDER GATE

Source/CI foundations for HTTPS, DPAPI-protected TLS state, ACME DNS-01 manager and portable ordinary-user Windows host are already PASS.

Next dependency chain: target ordinary-user Windows host -> least-privilege DNS access -> ACME staging -> protected PFX + HTTPS restart -> production public CA only after staging PASS -> canonical company-LAN reachability/browser trust -> real NLS-MT90/PDA HTTPS/reconnect -> >=60-minute Internet-cut acceptance -> restoration reconciliation.

Do not treat GitHub-runner certificate state as target-laptop acceptance. Do not bypass browser trust warnings or company policy.

## D — Account security — PARALLEL

Current SOURCE/CI PASS: normal login, ROOT username-only bootstrap to `ROOT_EMAIL_OTP_REQUIRED`, normal change-password including `MUST_CHANGE_PASSWORD`. Evidence run `34830186926`, check `103931457439`: **SUCCESS**.

Next: real email delivery integration, ROOT OTP request/verify route, V6 lifecycle tests, normal forgotten-password flow, session security behavior, leakage checks, and verified BETA migration state before live E2E claims.

## E — Web / Android / Gateway — PARALLEL

Web: continue authenticated login/recovery, then employee/attendance Slice-1 and later ADMIN+ conflict-resolution surfaces. Preserve Online/LAN parity and Vietnamese-only current UI.

Android: continue endpoint/session/scanner/retry/HTTPS/reconnect work independent of missing final Pick Pack visual evidence. Latest aggregate `android-apk` job at HEAD `4b202351...`: **SUCCESS**.

Gateway/Google: continue projection/upload receipt, retry and readback behavior. Sheets/Drive remain downstream only.

## Owner decision gate — portrait only

Immediate deletion of the previous portrait conflicts with offline staging while Drive is unavailable. Keep actual offline portrait replacement fail-closed until Owner decides; independent media infrastructure may continue.

## STABLE boundary

STABLE activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall: 55% displayed / 55.4% exact | Phase 6: 60% | Immediate: repair aggregate LAN smoke-test regression | Reconciliation signed transport: SOURCE/CI PASS, live/finality E2E pending | Physical: target Windows trust chain + real PDA + >=60-minute outage | Parallel: account security + V7 Web/App + Gateway/Google`
