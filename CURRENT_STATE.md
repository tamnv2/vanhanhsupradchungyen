# CURRENT STATE — VHDCHY

Updated: 2026-09-14
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active decisions: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`

## Progress

`Overall: 55% | Exact weighted baseline: 55.4% | Primary current phase: Phase 6 — LAN continuity/offline/reconcile`

Phase 6 remains **60%**. The newer Windows DPAPI TLS and ACME certificate-manager evidence materially improves implementation readiness, but no additional product-completion credit is taken yet because publicly trusted live issuance, canonical company-LAN reachability/trust and real PDA acceptance remain open.

Parallel active lanes: broader core business Service/API, Gateway/integrations, Online+LAN Web V7 UI, Android/PDA App and Cloud reconciliation transport.

## Current product authority

- VHDCHY is one platform: Online Web + LAN Web, Android/PDA App, Cloud Service/Gateway and full LAN Service substitute.
- Web/App current user-facing implementation language is **Vietnamese only**. Multilingual work is deferred until a later explicit Owner decision.
- Online Web and LAN Web share one V7 design system; LAN-critical UI assets must remain locally available without Internet.
- Pick Pack 1291 is authorized only as Android/PDA UI/UX reference; legacy business logic/data/credentials/runtime remain NON_AUTHORITY.
- DNSHE screenshots are visual-direction reference only; do not copy DNSHE branding/proprietary assets.
- Sheets/Drive remain downstream projection/storage, never canonical business authority.
- D1 remains central consolidated structured authority after synchronization; LAN edge state + immutable event journal is local operational authority for events accepted by LAN.
- Ordinary-user/no-admin Windows operation remains a hard LAN-host constraint. Do not depend on company certificate-store installation, firewall/router/AP modification or unauthorized internal-DNS changes.

## Reconciled LAN transport/certificate evidence through `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`

### Secure LAN HTTPS login/session -> public Slice-1 HTTP route — source/CI PASS

The reviewed credential transport remains direct Kestrel HTTPS. Paired P-256 signatures authenticate/bind requests but do not replace TLS confidentiality.

Runtime behavior remains fail-closed:

- no TLS source => `HTTP_READ_ONLY`; secure login/business mutation routes are unavailable;
- valid TLS source => reviewed HTTPS routes may be exposed only when the complete LAN readiness chain is also satisfied;
- `POST /api/v1/auth/login` verifies the paired signed request before normal-user credential verification and issues a durable LAN session;
- `POST /api/v1/data/commands` requires HTTPS + Bearer LAN session + exact signed request body and delegates through the reviewed coordinator/domain gates;
- ROOT remains email-OTP authority; no permanent ROOT password exists;
- `mustChangePassword` remains fail-closed until reviewed change/recovery routes exist;
- `EMPLOYEE_PORTRAIT_REPLACE` remains fail-closed at the unresolved portrait lifecycle gate.

Secure HTTP evidence remains workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**. Same-HEAD clean baseline `34811861613`: **SUCCESS**.

Detailed boundary: `docs/LAN_SECURE_HTTP_V1.md`.

### Windows user-space DPAPI TLS — source/CI PASS

LAN TLS private material can now be stored on Windows as a DPAPI CurrentUser protected PFX. The raw PFX is decrypted only for loading and is not intentionally persisted.

Windows Schannel requires a usable current-user private-key container rather than `EphemeralKeySet`; therefore Windows imports the decrypted certificate with `UserKeySet` and without `PersistKeySet`. This does **not** install the certificate into the Windows certificate store and does not require administrator rights. Non-Windows compatibility/CI paths retain ephemeral loading.

Dedicated Windows evidence at commit `376cb0976f481acf68e8e21654d3d6593e98a81b`:

- DPAPI TLS workflow `34817069447`, check `103889883955`: **SUCCESS**;
- secure HTTP regression `34817069438`: **SUCCESS**;
- baseline `34817069440`: **SUCCESS**.

Proven markers include DPAPI CurrentUser protection, real Kestrel/Schannel HTTPS startup, canonical SAN acceptance, wrong-host rejection, corrupt-blob rejection and no raw PFX disk leak.

### LAN ACME/DNS-01 certificate manager — source/CI PASS

A separate user-space certificate manager now exists at `lan-certificate-manager/`. It is intentionally separate from LAN Service so DNS-provider credentials do not enter the business-service runtime.

Implemented source behavior:

- BETA/STABLE canonical host derivation;
- exact Cloudflare account/zone verification before DNS mutation;
- DNS-01 restricted to `_acme-challenge.<canonical-host>`;
- exact challenge-record readback and record-ID cleanup in `finally`;
- ECDSA P-256 certificate request;
- SAN/validity verification before protected-PFX replacement;
- DPAPI CurrentUser protected PFX + atomic replacement;
- DPAPI-protected ACME account key;
- 30-day renewal threshold;
- Let’s Encrypt staging by default;
- production issuance requires both `--production` and `VHDCHY_ACME_ALLOW_PRODUCTION=YES`;
- sanitized provider failures; no intentional token/private-key/PFX logging.

Dedicated certificate-manager workflow at commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`:

- run `34819836554`, job/check `103898656531`: **SUCCESS**;
- local markers: `LAN_CERTIFICATE_MANAGER_SELF_TEST_PASS`, DPAPI PFX atomic write PASS, DPAPI account-secret PASS, renewal metadata PASS, raw-PFX disk-leak PASS;
- same-HEAD baseline run `34819836553`, check `103898626431`: **SUCCESS**.

The workflow intentionally receives no GitHub `secrets.*` values. It proves local source/storage/renewal safety only; it does not issue the final laptop certificate.

Detailed boundary: `docs/LAN_CERTIFICATE_MANAGER_V1.md`.

### Cloudflare LAN trust prerequisites — read-only provider PASS

Read-only inspection at commit `3588bbcc29b665be453e3988e8f5898ed35a9fa8`, run `34816004518`, job/check `103886701628`: **SUCCESS**.

At that verification point:

- the configured token verified successfully as an account token;
- the expected project account matched;
- zone `supra.cc.cd` matched and was `active`;
- `lan-beta.supra.cc.cd` had zero DNS records;
- `_acme-challenge.lan-beta.supra.cc.cd` had zero DNS records.

This does **not** prove DNS Edit capability for the credential that will run on the intended Windows LAN host. No provider mutation is inferred from this read-only evidence.

### Cloud reconciliation foundation — source/CI PASS; network authentication pending

The Cloud LAN-reconciliation ingestion core remains source/CI PASS, including event/actor/source evidence preservation, collision handling, completed integration receipt ingestion/deduplication and Worker packaging.

Evidence remains workflows `34803221835` and `34803221873` at `591c4083973141155530357f5bddd4ee58efbdc7`: **SUCCESS**.

A reviewed machine/service authentication mechanism is still required before the LAN -> Cloud network route is exposed. Payload actor evidence is not authentication.

### LAN durable local foundation — PASS

Operational snapshots/materialized Slice-1, immutable local event/outbox, idempotent replay, actor evidence, employee/MNV/attendance, atomic active-code uniqueness, staged media and durable Cloud-sync queue remain PASS foundations. Dedicated durable queue workflow `34801533266` at `013d5b310ae0f068510c56cdbfe7cf4ea7ffec66`: **SUCCESS**.

### Earlier LAN security/readiness foundations — PASS

- primary credential/session workflow `34805395079`: **SUCCESS**;
- integrated readiness workflow `34808274815`: **SUCCESS**;
- paired readiness baseline `34808274819`: **SUCCESS**;
- signed route-wiring workflow `34808936937`: **SUCCESS**;
- route-wiring baseline `34808937077`: **SUCCESS**.

### Web / Android foundations

- Shared V7 Online/LAN Web shell remains PASS in product-foundation workflow `34801611019`.
- Android foundation continues to build; exact final Pick Pack screen/layout evidence remains insufficiently surfaced, so exact visual details must not be invented.

## Current primary execution direction

The source-level certificate lifecycle is now ready for live-host acceptance. The next dependency chain is:

1. on the intended ordinary-user Windows LAN host, use a dedicated least-privilege Cloudflare DNS credential or explicitly approved equivalent;
2. live-verify exact account/zone identity and TXT create/read/delete capability only in the ACME challenge namespace;
3. run ACME **staging** issuance first on that Windows user/machine context;
4. verify DPAPI protected PFX, canonical SAN and LAN Service HTTPS startup;
5. only after staging PASS, run explicit production issuance;
6. prove canonical hostname resolution/reachability and public certificate trust on the actual company network/browser;
7. prove NLS-MT90/PDA HTTPS login/business/reconnection/Wi-Fi recovery;
8. complete remaining account-security routes: ROOT email OTP and normal-user must-change/password change/recovery;
9. later include the approved secure public subset in physical continuity acceptance.

A GitHub-hosted runner must **not** be used to create the final DPAPI PFX for the laptop because DPAPI CurrentUser protection belongs to the target Windows user/machine context.

## Parallel execution lanes

Independent safe work remains ready:

- Cloud reconciliation machine/service authentication and LAN -> Cloud network E2E;
- Web authenticated login/recovery + employee/attendance Slice-1 screens;
- Android endpoint/session/scanner/durable-retry/HTTPS work that does not depend on missing visual evidence;
- broader Cloud/Gateway/Google projection receipt/retry/readback coverage;
- public ROOT email-OTP and normal-user password recovery/change routes.

## Portrait semantic gate — OWNER_DECISION_REQUIRED only when implementation reaches it

The current portrait rule conflict remains unresolved: immediate deletion of the previous remote portrait versus LAN/offline staging while Drive is unavailable. Decision-independent media infrastructure may continue. Actual offline portrait replacement remains fail-closed until explicit Owner authority resolves the conflict.

## Physical / STABLE boundary

Publicly trusted BETA certificate issuance, canonical company-LAN DNS/reachability/trust, ordinary-user Windows acceptance and real NLS-MT90/PDA evidence are **not yet PASS**.

Canonical offline continuity acceptance still requires **Window 2 >=60 minutes** after Internet cut while valid LAN remains.

STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.
