# LAN SECURE HTTP V1

Status: IMPLEMENTED_AUTOMATED — source/CI PASS; physical/provider acceptance pending
Effective date: 2026-09-14
Scope: LAN credential transport, primary login/session HTTP adapter, reviewed Slice-1 public business route
Authority dependencies: `DECISIONS.md`, `DECISIONS_V3.md`, `DECISIONS_V4.md`, `DECISIONS_V5.md`, `DECISIONS_V6.md`, `DECISIONS_V7.md`, `docs/SERVICE_API_CONTRACT_V3.md`, `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`, `docs/LAN_EDGE_STATE_V2.md`, `docs/LAN_HOST_DOMAIN_V1.md`, `docs/NON_FUNCTIONAL_BASELINE_V1.md`

## 1. Security decision

Reusable user credentials must not cross plaintext HTTP. P-256 client request signing authenticates the paired device and binds method, target and exact request body, but it does not provide confidentiality for a password.

The reviewed transport is therefore direct HTTPS in the LAN Service Kestrel listener. The production trust target is a publicly trusted certificate for the canonical LAN hostname:

- BETA: `lan-beta.supra.cc.cd`
- STABLE: `lan.supra.cc.cd`

The certificate/private key is loaded from a user-space PFX with ephemeral key loading. The design does not require installing a certificate into the company Windows certificate store and does not authorize changes to company firewall, router/AP, internal DNS or other corporate policy.

A self-signed certificate with explicit thumbprint pinning is used only inside CI to prove the runtime TLS and HTTP behavior. It is not an accepted production/browser trust mechanism and must not be converted into a user click-through exception.

## 2. Runtime modes

### No TLS PFX configured

The LAN Service remains `HTTP_READ_ONLY`.

- read-only health/meta/capability/sync-status endpoints remain available;
- login and reviewed business mutation routes are not registered;
- generic mutation paths remain fail-closed with HTTP 503;
- `businessMutationEnabled=false`.

This is the safe default and preserves the earlier fail-closed behavior.

### TLS PFX configured and valid

Kestrel listens using HTTPS on the configured ordinary-user high port. The service validates that the PFX exists, contains a private key and is within its certificate validity period.

The reviewed secure routes are registered, but business mutation becomes available only when the complete readiness chain is also satisfied: synchronized authority + operational snapshots, paired signed-client security, executable primary credential authority, secure route wiring and the existing command/domain authorization gates.

TLS configuration alone never bypasses readiness.

## 3. Reviewed HTTP routes

### `POST /api/v1/auth/login`

Requirements:

1. HTTPS;
2. active paired client/device;
3. current security epoch;
4. valid P-256 signature over method + exact route target + exact body hash + timestamp + nonce;
5. replay/timestamp checks;
6. synchronized primary-login authority;
7. valid normal-user primary credentials.

The request body accepts only `username` and `password`. The password is used only for verification and is not returned or intentionally logged.

A successful normal-user login issues a durable LAN session bound to device, security epoch and authority snapshot generation.

ROOT semantics are unchanged: a permanent ROOT password is not introduced. The current primary credential verifier returns `ROOT_EMAIL_OTP_REQUIRED`; the public ROOT email-OTP flow is outside this V1 route and remains pending.

If authority marks `mustChangePassword=true`, that state is retained in the session and ordinary business mutation remains blocked. A reviewed public password-change route is still pending.

### `POST /api/v1/data/commands`

Requirements:

1. HTTPS;
2. valid Bearer LAN session;
3. valid paired-device signed request over the exact raw business body;
4. session/device/security-epoch binding;
5. current authority snapshot/session freshness;
6. `MUST_CHANGE_PASSWORD` clear;
7. current permission/domain/command authorization;
8. reviewed Slice-1 command support.

The HTTP adapter passes the exact signed raw request body into `LanBusinessRouteCoordinator`; authenticated actor identity comes from server-side session evidence, never client-supplied actor fields.

`EMPLOYEE_PORTRAIT_REPLACE` remains fail-closed at the unresolved portrait lifecycle semantic gate.

## 4. Request proof headers

The secure routes use the existing paired-client proof:

- `X-VHDCHY-Device-Id`
- `X-VHDCHY-Security-Epoch`
- `X-VHDCHY-Timestamp-Ms`
- `X-VHDCHY-Nonce`
- `X-VHDCHY-Signature`

Business requests additionally carry the LAN session using `Authorization: Bearer <token>`.

Signed request bodies are bounded to 128 KiB and parsed as strict UTF-8. Query strings are rejected on the exact reviewed signed routes so the signed route target cannot differ from the executed route.

## 5. Runtime configuration

Current runtime inputs:

- `VHDCHY_LAN_TLS_PFX_PATH`
- `VHDCHY_LAN_TLS_PFX_PASSWORD`
- existing `VHDCHY_ENV`
- existing `VHDCHY_CLUSTER_ID`
- existing `VHDCHY_LAN_PORT`
- existing `VHDCHY_LAN_DATA_ROOT`

The PFX must remain outside source control. No production private key or certificate password may be committed to GitHub.

## 6. Automated evidence

Source chain includes:

- explicit secure-route readiness input;
- TLS-gated login/session adapter;
- TLS-gated public Slice-1 business adapter;
- HTTP read-only fallback when TLS is absent;
- E2E harness that launches the real LAN Service process.

Dedicated workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**.

The E2E proof covers:

- plaintext HTTP does not successfully reach the credential route on the HTTPS listener;
- paired signed normal-user login;
- wrong-password rejection;
- exact signed-body tamper rejection;
- login replay rejection;
- durable LAN session issuance;
- signed authorized `EMPLOYEE_CREATE` execution through the public HTTP adapter;
- business replay rejection;
- LAN Service restart followed by successful reuse of the durable valid session;
- password absent from captured service diagnostics;
- SQLite foreign-key and quick integrity checks.

The same HEAD also passed baseline workflow `34811861613`.

## 7. What this does not prove

This V1 source/CI PASS does not prove:

- issuance/renewal of a publicly trusted production certificate;
- canonical DNS resolution and certificate hostname acceptance on the actual company LAN;
- ordinary-user Windows browser trust in the company environment;
- real NLS-MT90/PDA HTTPS acceptance and reconnection behavior;
- ROOT email-OTP HTTP flow;
- public password-change/recovery route;
- Cloud reconciliation machine/service authentication;
- physical >=60-minute Internet-cut continuity acceptance;
- STABLE production acceptance.

These boundaries remain separate gates and must not be inferred from CI TLS success.

## 8. Next acceptance gate

The next LAN transport acceptance is to provision a publicly trusted BETA certificate for `lan-beta.supra.cc.cd` without requiring company-admin changes, configure canonical DNS/reachability as permitted by the approved architecture, and prove HTTPS from the intended ordinary-user company Windows environment and real PDA/NLS-MT90 path.

Until that physical/provider evidence exists, status remains `IMPLEMENTED_AUTOMATED`, not `ACCEPTED`.
