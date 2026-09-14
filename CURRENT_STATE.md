# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 55.4% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

Phase 6 remains **60%** and Phase 7 remains **25%**. No completion increase is taken from governance cleanup or the newly validated auth route slice because the current progress model does not justify another phase-level increment from this source slice alone.

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App, account-security routes and Cloud reconciliation transport.

## Full-audit conclusion — 2026-09-14

The project implementation direction is consistent with current Owner authority:

- Cloud/LAN share one business/domain/API model; LAN is a full local Service substitute, not a relay-only fallback.
- D1 is the central consolidated structured store after synchronization; LAN edge state + immutable event journal is operational authority for commands accepted during LAN operation.
- Google Sheets/Drive remain downstream projection/storage and are not business authority.
- ordinary-user/no-admin Windows hosting remains a hard constraint;
- STABLE remains fail-closed pending mandatory BETA acceptance and explicit Owner promotion approval;
- current Web/App acceptance UI is **Vietnamese only**; multilingual implementation is deferred;
- Pick Pack 1291 is Android/PDA UI/UX reference only; legacy business logic/data/runtime remain NON_AUTHORITY;
- DNSHE screenshots are visual-direction reference only; DNSHE branding/assets are not copied.

The audit found governance drift, not an architecture drift. Stale canonical wording has been reconciled in `PROJECT_SCOPE.md`, `CONTEXT_INDEX.md`, `docs/OWNER_BUSINESS_RULES_V1.md`, `docs/DATA_MODEL_GUIDE_V2.md`, `docs/DELIVERY_PLAN_V5.md`, `docs/LAN_SECURE_HTTP_V1.md` and `docs/LAN_SOURCE_REVIEW_20260913.md` so that V6/V7 auth/language rules, current 55.4% progress and current LAN authority/certificate gates are no longer contradicted by those active guides.

## Account-security continuation — source/CI PASS for current route slice

Existing source already contained reviewed password/session primitives and the V6 email-OTP lifecycle. This continuation added and validated the public Cloud Worker route layer for the parts that can be implemented without inventing an email provider:

- `POST /api/v1/auth/login` for normal permanent-password login;
- ROOT username-only login bootstrap returns `ROOT_EMAIL_OTP_REQUIRED` and never creates a permanent-password session;
- `POST /api/v1/auth/change-password` for authenticated normal accounts, including restricted `MUST_CHANGE_PASSWORD` sessions;
- bounded JSON request parsing and route tests covering bearer-token persistence semantics, ROOT permanent-password exclusion and permanent-password establishment.

Latest ROOT-bootstrap source/test commits: `2bffb170319f9bf6d6d5f1953655ce960115d35e` and `1f2d328f21af90d09c5e3b260ad6452af05835c6`.

Clean-baseline evidence at checkpoint v38:

- HEAD `3bb22252a53ce25d69901c3261efd2ee3c54f57d`;
- workflow run `34830186926`;
- job/check `103931457439`;
- result: **SUCCESS**;
- authority/resume invariants: PASS;
- checkpoint freshness: PASS;
- Worker source syntax: PASS;
- auth crypto contract: PASS;
- Worker unit tests: PASS;
- clean D1 schema: PASS;
- schema/runtime contract: PASS.

Therefore the current public normal login + normal change-password + ROOT password-exclusion/bootstrap slice is **SOURCE/CI PASS**.

The public ROOT OTP request/delivery/verify flow is **not** claimed complete. Current OTP source correctly fails closed without a delivery callback, and current schema stores protected destination hashes/hints rather than a readable email address for Worker use. A reviewed delivery adapter + secure destination provisioning boundary and verified provider migration state are still required. Migration `0010_auth_v6_email_otp.sql` exists in source; live BETA application is not inferred until verified.

## Reconciled LAN transport/certificate/package evidence through `e4731983fa681541c317d853276abc9bf206366e`

### Secure LAN HTTPS login/session -> public Slice-1 route — source/CI PASS

Direct Kestrel HTTPS remains the reviewed credential transport. No TLS source => `HTTP_READ_ONLY`; secure login/business mutation routes remain unavailable. With reviewed TLS/readiness/security gates, paired signed normal-user login issues a durable LAN session and the signed-session Slice-1 route delegates through current authorization/domain gates.

Evidence: workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**. Detailed boundary: `docs/LAN_SECURE_HTTP_V1.md`.

### Windows user-space DPAPI TLS — source/CI PASS

DPAPI CurrentUser protected PFX plus Schannel-compatible current-user key handling works without certificate-store installation/admin. Evidence at commit `376cb0976f481acf68e8e21654d3d6593e98a81b`: DPAPI TLS workflow `34817069447` and same-line secure HTTP/baseline regressions: **SUCCESS**.

### LAN ACME/DNS-01 certificate manager — source/CI PASS

Separate user-space certificate manager exists at `lan-certificate-manager/`. It verifies exact account/zone before DNS mutation, restricts DNS-01 to the canonical challenge namespace, cleans exact challenge records, uses ECDSA P-256, stores the final PFX and ACME account key under DPAPI CurrentUser, defaults to staging and requires explicit production opt-in.

Evidence: workflow `34819836554`, job/check `103898656531`, commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`: **SUCCESS**. This proves source/storage/renewal behavior, not live issuance. Detailed boundary: `docs/LAN_CERTIFICATE_MANAGER_V1.md`.

### Portable ordinary-user Windows LAN-host package — source/CI PASS

BETA package `0.2.2` combines self-contained `win-x64` LAN Service, certificate manager and ordinary-user launch/protection scripts. It starts fail-closed as `HTTP_READ_ONLY` without a certificate and rejects key/certificate leakage in the package boundary.

Evidence at commit `e4731983fa681541c317d853276abc9bf206366e`: portable Windows-host workflow `34821013175`, job/check `103902355956`: **SUCCESS**; same-HEAD validator `34821013167`: **SUCCESS**. Package SHA256 `87a5d8d9ce14852204ba757a7b1cf7a716af79675c1bf78f36f9f44934286167`; artifact ID `10338301582`.

### Cloudflare trust prerequisites — read-only provider PASS only

Read-only workflow `34816004518`, check `103886701628` verified the intended project account, active zone `supra.cc.cd`, and no existing `lan-beta.supra.cc.cd` / ACME challenge records at inspection time. DNS Edit permission for the real host credential is **not proven** and is not inferred.

### Cloud reconciliation foundation — source/CI PASS; network authentication pending

Cloud ingestion core and durable LAN reconciliation queue foundations remain source/CI PASS, including stable event/source/actor evidence, collision handling, receipt dedupe and restart/retry/conflict mechanics. Machine/service authentication is still required before exposing LAN -> Cloud ingestion over the network; payload actor evidence is not authentication.

### Web / Android foundations

- Shared V7 Online/LAN Web shell remains PASS in product-foundation workflow `34801611019` and is Vietnamese-only under current authority.
- Android foundation builds, but actual final Pick Pack UI source/artifacts remain insufficiently surfaced to justify invented screen details.

## Current primary execution direction

Primary physical/provider chain:

1. intended ordinary-user Windows host;
2. dedicated least-privilege DNS credential with exact scope verification;
3. ACME staging DNS-01 issuance/cleanup;
4. DPAPI PFX + LAN Service HTTPS startup/restart;
5. explicit production public-CA issuance only after staging PASS;
6. canonical company-LAN reachability + normal Windows/browser trust;
7. real NLS-MT90/PDA HTTPS/reconnection/Wi-Fi recovery;
8. >=60-minute Internet-cut continuity;
9. separate post-restoration reconciliation evidence.

Independent source lanes continue in parallel: ROOT OTP provider/public flow, normal forgotten-password flow, Cloud reconciliation machine-auth/network E2E, authenticated Web/business screens, Android endpoint/session/scanner/retry/HTTPS and broader Gateway/Google receipt/retry/readback coverage.

## Owner decision gate

The only current material product-semantic Owner decision gate is portrait replacement: immediate deletion of the previous remote portrait conflicts with offline/LAN staging while Drive is unavailable. Decision-independent media infrastructure may continue; actual offline portrait replacement remains fail-closed.

## Physical / STABLE boundary

Publicly trusted BETA issuance, canonical company-LAN DNS/reachability/trust, intended ordinary-user Windows acceptance and real NLS-MT90/PDA evidence are **not yet PASS**. Canonical offline continuity still requires **Window 2 >=60 minutes**. STABLE business activation/promotion remains blocked until mandatory BETA acceptance plus explicit Owner approval.
