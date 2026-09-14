# LAN SECURE HTTP V1

Status: IMPLEMENTED_AUTOMATED — source/CI PASS; physical/provider acceptance pending
Effective date: 2026-09-14
Scope: LAN credential transport, primary login/session HTTP adapter, reviewed Slice-1 public business route, Windows user-space TLS key custody
Authority dependencies: `DECISIONS.md`, `DECISIONS_V3.md`, `DECISIONS_V4.md`, `DECISIONS_V5.md`, `DECISIONS_V6.md`, `DECISIONS_V7.md`, `docs/SERVICE_API_CONTRACT_V3.md`, `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`, `docs/LAN_EDGE_STATE_V2.md`, `docs/LAN_HOST_DOMAIN_V1.md`, `docs/NON_FUNCTIONAL_BASELINE_V1.md`

## 1. Security decision

Reusable user credentials must not cross plaintext HTTP. P-256 request signing authenticates/binds the paired device, method, target and exact body, but it does not encrypt a password. The reviewed transport is direct HTTPS in the LAN Service Kestrel listener.

Canonical production-trust names:

- BETA: `lan-beta.supra.cc.cd`
- STABLE: `lan.supra.cc.cd`

Production requires a publicly trusted certificate. CI self-signed certificates with explicit thumbprint pinning are test-only and are never an accepted browser/PDA trust mechanism.

No design step here authorizes changes to company certificate stores, firewall, router/AP, internal DNS or other corporate policy.

## 2. TLS key custody on Windows

The preferred Windows ordinary-user runtime input is:

- `VHDCHY_LAN_TLS_PROTECTED_PFX_PATH`

The file contains a complete PFX encrypted at rest with Windows DPAPI `CurrentUser`. The entropy binds the protected blob to VHDCHY, the target environment and canonical LAN hostname. The decrypted PFX exists only in process memory and plaintext buffers are zeroed after import.

Before Kestrel starts, the loader verifies:

- a private key exists;
- current certificate validity window is valid;
- exact SAN matches the canonical hostname;
- wildcard matching and CN fallback are not accepted;
- TLS Server Authentication EKU is accepted when EKU restrictions exist.

On Windows the decrypted certificate is imported with `UserKeySet` and without `PersistKeySet`. This gives Schannel temporary current-user key material without installing the certificate into a Windows certificate store and without administrator rights. The earlier `EphemeralKeySet` approach was rejected by real Windows CI because Schannel could not complete the server TLS handshake with that private-key form.

Compatibility/test inputs remain available:

- `VHDCHY_LAN_TLS_PFX_PATH`
- `VHDCHY_LAN_TLS_PFX_PASSWORD`

Only one source may be configured. Raw production PFX/private-key material must not be committed to GitHub.

## 3. Runtime modes

### No certificate configured

The LAN Service remains `HTTP_READ_ONLY`:

- read-only health/meta/capability/sync-status remain available;
- login and reviewed business mutation routes are not registered;
- generic mutation paths remain fail-closed with HTTP 503;
- `businessMutationEnabled=false`.

### Valid certificate configured

Kestrel serves HTTPS on the configured ordinary-user high port. TLS alone does not enable business mutation: synchronized authority + operational snapshots, paired-client security, executable primary credential authority, secure route wiring and authorization/domain/command gates must also pass.

Health/meta expose certificate storage mode and expiration so renewal can be monitored without exposing private material.

## 4. Reviewed HTTP routes

### `POST /api/v1/auth/login`

Requires HTTPS, an active paired client/device, current security epoch, valid P-256 proof over method/target/body/timestamp/nonce, replay/timestamp checks, synchronized primary-login authority and valid normal-user primary credentials.

The body accepts only `username` and `password`. Passwords are verification input only and are not intentionally logged. Successful normal-user login issues a durable LAN session bound to device, security epoch and authority generation.

ROOT remains email-OTP authority; no permanent ROOT password is introduced. `mustChangePassword=true` remains session evidence and blocks ordinary business mutation until the reviewed public password-change/recovery path exists.

### `POST /api/v1/data/commands`

Requires HTTPS, valid Bearer LAN session, valid paired-device signature over the exact raw body, session/device/security-epoch binding, current authority/session freshness, clear `MUST_CHANGE_PASSWORD`, current permission/domain/command authorization and reviewed Slice-1 command support.

Authenticated actor identity is server-derived. `EMPLOYEE_PORTRAIT_REPLACE` remains fail-closed at the unresolved portrait lifecycle gate.

## 5. Request proof headers

- `X-VHDCHY-Device-Id`
- `X-VHDCHY-Security-Epoch`
- `X-VHDCHY-Timestamp-Ms`
- `X-VHDCHY-Nonce`
- `X-VHDCHY-Signature`
- business route additionally: `Authorization: Bearer <token>`

Signed bodies are bounded to 128 KiB and strict UTF-8. Query strings are rejected on the exact reviewed signed routes.

## 6. Automated evidence

### Secure HTTP E2E

Workflow `34811861697`, job/check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**.

Proven on the real LAN Service process: HTTP read-only bootstrap, TLS listener, plaintext HTTP not reaching credentials, signed normal-user login, wrong-password rejection, signed-body tamper rejection, login/business replay rejection, durable session, authorized `EMPLOYEE_CREATE`, session reuse after restart, no password in captured diagnostics, SQLite FK/quick-integrity checks.

### Windows DPAPI/Schannel TLS E2E

Commit `376cb0976f481acf68e8e21654d3d6593e98a81b`:

- workflow `34817069447`, job/check `103889883955` (`dpapi-tls`): **SUCCESS**;
- secure-http regression workflow `34817069438`, job/check `103889884043`: **SUCCESS**;
- clean baseline workflow `34817069440`, job/check `103889883747`: **SUCCESS**.

Windows harness markers:

- `dpapiCurrentUser=PASS`;
- `userSpacePfx=PASS`;
- `canonicalSan=PASS`;
- `wrongHostRejected=PASS`;
- `corruptBlobRejected=PASS`;
- `rawPfxDiskLeak=PASS`.

This proves DPAPI-protected PFX at rest plus a working Schannel/Kestrel TLS handshake under the current Windows user without a certificate-store install. It does not prove the intended company laptop/network or a publicly trusted CA certificate.

### User-space certificate manager

`docs/LAN_CERTIFICATE_MANAGER_V1.md` records the separate ACME DNS-01 certificate manager as **IMPLEMENTED_AUTOMATED**. Dedicated workflow `34819836554` at commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655` completed **SUCCESS**. This closes the earlier source-construction gate but does not prove live DNS write permission or public CA issuance on the intended host.

## 7. Provider trust status

Read-only Cloudflare prerequisite inspection at workflow `34816004518`, job `103886701628`: **SUCCESS**.

Verified live on 2026-09-14:

- exact configured Cloudflare account token is active;
- zone `supra.cc.cd` is active under the expected account;
- `lan-beta.supra.cc.cd` has no existing DNS record;
- `_acme-challenge.lan-beta.supra.cc.cd` has no existing DNS record.

This is **read-only evidence only**. DNS Edit permission has not been proven and must not be inferred.

## 8. What this does not prove

Still open:

- DNS write capability with a reviewed least-privilege credential on the intended Windows host;
- successful ACME staging issuance and cleanup for `lan-beta.supra.cc.cd`;
- successful production public-CA issuance/renewal;
- canonical DNS resolution and certificate hostname acceptance on the company LAN;
- ordinary-user company Windows browser trust;
- real NLS-MT90/PDA HTTPS/reconnection behavior;
- ROOT email-OTP LAN HTTP flow;
- public LAN password-change/recovery route;
- Cloud reconciliation machine/service authentication;
- physical >=60-minute Internet-cut continuity acceptance;
- STABLE production acceptance.

## 9. Next gate

The certificate-manager source gate is already complete. The next gate is a controlled target-host acceptance chain, not more speculative certificate-manager design:

1. use the intended ordinary-user Windows LAN host;
2. supply a dedicated least-privilege DNS credential whose exact account/zone/write scope is verified before mutation;
3. prove ACME TXT create/read/delete behavior in the challenge namespace and complete **staging** issuance first;
4. verify the DPAPI-protected PFX, exact SAN/validity and LAN Service HTTPS startup;
5. only then allow production public-CA issuance;
6. verify canonical DNS reachability and normal Windows/browser trust;
7. verify real NLS-MT90/PDA HTTPS/reconnection behavior;
8. continue to the >=60-minute Internet-cut acceptance and post-restoration reconciliation checks.

The DNS credential remains outside the LAN Service and outside GitHub. Until these live/physical gates pass, status remains `IMPLEMENTED_AUTOMATED`, not `ACCEPTED`.