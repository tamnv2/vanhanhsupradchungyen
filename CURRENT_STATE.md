# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 55.4% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

Phase 6 remains **60%** and Phase 7 remains **25%**. The newly implemented LAN->Cloud authenticated reconciliation transport is source/CI validated, but the evidence-weighted percentage remains unchanged until the remaining live network/provider E2E/finality acceptance justifies a phase-level increment.

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

The audit found governance drift, not architecture drift. Stale canonical wording was reconciled in `PROJECT_SCOPE.md`, `CONTEXT_INDEX.md`, `docs/OWNER_BUSINESS_RULES_V1.md`, `docs/DATA_MODEL_GUIDE_V2.md`, `docs/DELIVERY_PLAN_V5.md`, `docs/LAN_SECURE_HTTP_V1.md` and `docs/LAN_SOURCE_REVIEW_20260913.md` so V6/V7 auth/language rules, current progress and current LAN authority/certificate gates are no longer contradicted by those active guides.

## Account-security continuation — source/CI PASS for current route slice

Existing source already contained reviewed password/session primitives and the V6 email-OTP lifecycle. The current validated public Cloud Worker route layer includes:

- `POST /api/v1/auth/login` for normal permanent-password login;
- ROOT username-only login bootstrap returns `ROOT_EMAIL_OTP_REQUIRED` and never creates a permanent-password session;
- `POST /api/v1/auth/change-password` for authenticated normal accounts, including restricted `MUST_CHANGE_PASSWORD` sessions;
- bounded JSON request parsing and route tests covering bearer-token persistence semantics, ROOT permanent-password exclusion and permanent-password establishment.

Latest ROOT-bootstrap source/test commits: `2bffb170319f9bf6d6d5f1953655ce960115d35e` and `1f2d328f21af90d09c5e3b260ad6452af05835c6`.

Accepted clean-baseline evidence:

- HEAD `3bb22252a53ce25d69901c3261efd2ee3c54f57d`;
- workflow run `34830186926`;
- job/check `103931457439`;
- result: **SUCCESS**.

Therefore public normal login + normal change-password + ROOT password-exclusion/bootstrap is **SOURCE/CI PASS**.

The public ROOT OTP request/delivery/verify flow is **not** claimed complete. Current OTP source correctly fails closed without a real delivery callback. A reviewed delivery adapter + secure destination provisioning boundary and verified provider migration state are still required. Migration `0010_auth_v6_email_otp.sql` exists in source; live BETA application is not inferred until verified.

## LAN transport/certificate/package accepted evidence

### Secure LAN HTTPS login/session -> public Slice-1 route — source/CI PASS

Direct Kestrel HTTPS remains the reviewed credential transport. No TLS source => `HTTP_READ_ONLY`; secure login/business mutation routes remain unavailable. With reviewed TLS/readiness/security gates, paired signed normal-user login issues a durable LAN session and the signed-session Slice-1 route delegates through current authorization/domain gates.

Evidence: workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**.

### Windows user-space DPAPI TLS — source/CI PASS

DPAPI CurrentUser protected PFX plus Schannel-compatible current-user key handling works without certificate-store installation/admin. Evidence at commit `376cb0976f481acf68e8e21654d3d6593e98a81b`: workflow `34817069447`, check `103889883955`: **SUCCESS**.

### LAN ACME/DNS-01 certificate manager — source/CI PASS

Separate user-space certificate manager exists at `lan-certificate-manager/`. It verifies exact account/zone before DNS mutation, restricts DNS-01 to the canonical challenge namespace, cleans exact challenge records, uses ECDSA P-256, stores the final PFX and ACME account key under DPAPI CurrentUser, defaults to staging and requires explicit production opt-in.

Evidence: workflow `34819836554`, job/check `103898656531`, commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`: **SUCCESS**. This proves source/storage/renewal behavior, not live issuance.

### Portable ordinary-user Windows LAN-host package — source/CI PASS

BETA package `0.2.2` combines self-contained `win-x64` LAN Service, certificate manager and ordinary-user launch/protection scripts. It starts fail-closed as `HTTP_READ_ONLY` without a certificate and rejects key/certificate leakage in the package boundary.

Evidence at commit `e4731983fa681541c317d853276abc9bf206366e`: workflow `34821013175`, job/check `103902355956`: **SUCCESS**; same-HEAD validator `34821013167`: **SUCCESS**. Package SHA256 `87a5d8d9ce14852204ba757a7b1cf7a716af79675c1bf78f36f9f44934286167`; artifact ID `10338301582`.

### Cloudflare trust prerequisites — read-only provider PASS only

Read-only workflow `34816004518`, check `103886701628` verified the intended project account, active zone `supra.cc.cd`, and no existing `lan-beta.supra.cc.cd` / ACME challenge records at inspection time. DNS Edit permission for the real host credential is **not proven** and is not inferred.

## Cloud/LAN reconciliation transport — source/CI PASS; live network/finality E2E pending

The current source now includes an authenticated LAN->Cloud transport boundary instead of relying on payload actor evidence:

- dedicated `POST /api/v1/reconciliation/events` Worker route, isolated from unrelated public mutation routes;
- machine authentication using HMAC-SHA256 over the canonical request contract, binding method/path/timestamp/nonce/body hash/environment/key identity;
- bounded timestamp window and rejection of unsigned, malformed, stale or body/signature-mismatched requests;
- key material supplied only through runtime secret/config boundaries, not public source;
- LAN HTTPS sender emits the compatible signed request contract;
- LAN reconciliation background pump starts only when the complete Cloud endpoint + machine credential configuration is present; partial configuration fails closed;
- `RECEIVED` means durable Cloud inbox receipt only and is **not** treated as `RECONCILED`/canonical Cloud commit; the LAN event stays pending with backoff until final reconciliation evidence exists.

Worker route/source validation evidence:

- commit `a2932d95fb8289ad78be7102d597d3b4a95561ce` (`test: cover authenticated reconciliation route`);
- clean-baseline workflow `34832612667`: **SUCCESS**.

Latest LAN sender/pump evidence at HEAD `4b202351c62d7439569fb72b9aa39794c34769c7`:

- `Validate LAN Cloud reconciliation queue` run `34833117039`: **SUCCESS**;
- `Validate clean baseline` run `34833117051`: **SUCCESS**;
- `Build portable LAN Windows host package` run `34833117192`: **SUCCESS**.

A separate aggregate regression remains open on that same HEAD:

- `Build product foundations` run `34833117353`: **FAILURE**;
- failed job `lan-service` / check `103940791504`, step `Build and smoke-test LAN Service`;
- sibling jobs `android-apk`, `web-contract` and `cloud-service` in the same workflow all **SUCCESS**.

Because the dedicated reconciliation harness and baseline validator pass while the aggregate LAN product smoke-test fails, the reconciliation source slice is recorded **SOURCE/CI PASS**, but the aggregate LAN regression must be diagnosed/repaired before using the aggregate product-foundation workflow as release evidence.

Still not PASS: real deployed Cloud network E2E with provisioned machine credential, final `RECONCILED`/canonical-commit acknowledgement path, provider-environment verification, Google/Drive receipt no-duplicate E2E, and physical company-network/PDA reconciliation acceptance.

## Web / Android foundations

- Shared V7 Online/LAN Web shell remains source/CI PASS and Vietnamese-only under current authority.
- Android foundation builds; in the latest aggregate product workflow at HEAD `4b202351...`, the `android-apk` job passed.
- Actual final Pick Pack UI source/artifacts remain insufficiently surfaced to justify invented final screen details.

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

Independent source lanes continue in parallel: repair the aggregate LAN product smoke-test regression; complete live Cloud reconciliation machine-auth/network/finality E2E; ROOT OTP provider/public flow; normal forgotten-password flow; authenticated Web/business screens; Android endpoint/session/scanner/retry/HTTPS; broader Gateway/Google receipt/retry/readback coverage.

## Owner decision gate

The only current material product-semantic Owner decision gate is portrait replacement: immediate deletion of the previous remote portrait conflicts with offline/LAN staging while Drive is unavailable. Decision-independent media infrastructure may continue; actual offline portrait replacement remains fail-closed.

## Physical / STABLE boundary

Publicly trusted BETA issuance, canonical company-LAN DNS/reachability/trust, intended ordinary-user Windows acceptance and real NLS-MT90/PDA evidence are **not yet PASS**. Canonical offline continuity still requires **Window 2 >=60 minutes**. STABLE business activation/promotion remains blocked until mandatory BETA acceptance plus explicit Owner approval.
