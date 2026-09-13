# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13

## Execution rule

The active operating mode is autonomous and parallel. The default state is `CONTINUE`.

Overall progress requests Owner interaction only for:
- `OWNER_PERMISSION_REQUIRED`: an Owner-controlled permission/access/consent/secret-store action is required to continue the affected operation; or
- `OWNER_DECISION_REQUIRED`: a material business/authority contradiction has multiple valid outcomes and cannot be resolved from current Owner instruction, provider evidence or GitHub authority.

Android/APK build and LAN Agent/model development are ACTIVE in parallel with Worker/Service, Projection, D1, Google Sheets/Drive and Web preparation. The final company-laptop/network LAN regression remains a later physical evidence gate, not a source/build blocker. The Owner currently has an Android device available for APK installation/testing.

## Provider baseline — PASS

Cloudflare BETA:
- D1 `vhdchy-data-beta` is `business_core_v3`.
- Worker `vhdchy-beta` remains at `https://beta.supra.cc.cd` with `workers.dev` disabled.
- Fresh read-only verification run `34754968801` PASS: exact Worker/D1 identity, expected bindings and zero business rows reconfirmed.
- Fresh baseline validation run `34754968807` PASS.
- Existing deployed business/admin APIs remain intentionally fail-closed.

Google BETA:
- Projection workbook remains `PP1291_SHEETS_BETA_V1` / `PROVISIONED_NOT_LIVE`.
- Managed Apps Script deployment version `3`; run `34753872034` PASS.
- Gateway retains fail-closed verifier + enable gates.

## Gate 1 — Worker multi-module packaging

Completed:
- `service/worker/deploy.beta.json` now contains an exact reviewed seven-module manifest.
- Manifest validation run `34754738074` PASS.
- Reviewed implementation contract persisted in `docs/WORKER_PACKAGING_PLAN.md`.

Next:
1. apply the reviewed multi-module change to `.github/workflows/cloudflare-beta-deploy.yml` using an allowed high-level repository write path;
2. validate exact module allowlist, syntax and provider preconditions;
3. perform a packaging-only BETA deployment while leaving `index.js` business/admin behavior fail-closed;
4. verify domain/bindings/health plus anonymous business-route rejection after deploy;
5. only then integrate runtime imports/routes.

Do not trigger Worker deployment while the active workflow still uploads only `index.js`.

Current connected write path blocks the provider-mutating workflow/source changes by platform safety. Do not bypass that guard through lower-level Git/API methods.

## Gate 2 — Auth / session / permission

Completed foundation:
- password policy/hash, bearer utilities and TOTP verifier;
- session token resolution, expiry/revocation/device-security-epoch checks;
- scoped role/direct permission loading;
- ROOT/SUPERADMIN boundary and explicit DENY precedence;
- non-ROOT password login/session issuance;
- ROOT password stage fails closed to `ROOT_MFA_REQUIRED`;
- unit/contract validation PASS.

Next after sensitive-source write is available:
1. complete ROOT TOTP/recovery only where current authority is explicit;
2. integrate authenticated context into protected Worker routes after packaging PASS;
3. add login/logout/me/change-password acceptance tests;
4. add account-administration grantor/self-protection enforcement and tests;
5. provision exact BETA bootstrap identities only when locked identity inputs are authoritative.

## Gate 3 — Projection / outbox authentication

Completed foundation:
- Worker outbox envelope/retry/dead-letter source;
- GAS version 3 allowed-sheet/key upsert contract;
- Gateway verifier + enable fail-closed checks;
- secure provisioning design persisted in `docs/PROJECTION_AUTH_PLAN.md`.

Next:
1. add a management-plane Apps Script API-executable path restricted to the deploying identity and verify `scripts.run` with the existing BETA OAuth identity;
2. use GitHub Environment `beta` secret `VHDCHY_PROJECTION_SHARED_TOKEN` as the raw-token authority; compute only its SHA-256 verifier for GAS Script Properties;
3. bind the raw token to Worker as a secret, not plain text;
4. verify Gateway `authConfigured=true` while `enabled=false`;
5. implement Worker sender/ACK/failure behavior;
6. test wrong token, Google unavailable, retry/dead-letter and no rollback of canonical D1 state;
7. enable projection only as a separate evidenced gate.

If the Environment secret cannot be created through an approved connected provider action, request only the minimum Owner secret-store action at that point; never request the raw token in chat.

## Gate 4 — Shared Service API / canonical mutation model

Completed foundation:
- `docs/SERVICE_API_CONTRACT.md` defines authentication, permissions, idempotency and required state + immutable event + outbox semantics.

Next:
1. implement and test a reusable D1 atomic mutation helper;
2. prove guarded state change + `domain_events` + `projection_outbox` rollback together on conflict/failure;
3. implement business commands in dependency order after Auth enforcement is executable;
4. cover BETA scenarios from `DECISIONS.md`: IN/OUT/repeated IN, MNV reuse, PICK/PACK mixed tasks, resource changes/reissue/borrow, labor, dropped goods, documents and degraded projection.

## Gate 5 — Android APK + LAN Agent/model — ACTIVE

Reference boundary:
- current authority remains this repository;
- legacy repo `tamnv2supra/vanhanhdchungyen` is read-only NON_AUTHORITY evidence/reference;
- fixed V4 comparison commit: `7b4488a89f585812c1bccba5d07d86049482bf4c`;
- source review: `docs/LAN_SOURCE_REVIEW_20260913.md`;
- current contract: `docs/LAN_TRANSPORT_BETA_V1.md`;
- build status: `docs/LAN_DEV_BUILD_STATUS.md`.

Completed in the current repository:
- Android project bootstrapped under `android-pilot/` with isolated DEV package `vn.vhdchy.transport.beta`;
- Android transport state implements cached Agent -> UDP discovery and two-sample anti-flapping activation;
- Android transport-test queue is durable SQLite and allocates stable `deviceId + deviceSeq + idempotencyKey` before transmission;
- portable .NET 8 LAN Agent bootstrapped under `lan-agent/` with HTTP `17891` and UDP discovery `17892`;
- Agent persists its instance ID, rotates `streamEpoch` on restart, stores test-event receipt metadata and rejects idempotency-payload or device-sequence collisions;
- cleartext endpoints are explicitly transport-test only; business credentials/PII/canonical mutations remain prohibited;
- `.github/workflows/build-lan-dev.yml` builds Android and Agent in independent parallel CI jobs without provider mutation/release signing secrets;
- first build run is `34756569016`; final result must be read before BUILD PASS is claimed.

Next Android/LAN work, in parallel with Service work:
1. resolve any failures from build run `34756569016` until both Android and Agent jobs are green;
2. publish/retain the green DEV APK + Agent ZIP artifacts and SHA256 evidence;
3. use the Owner's available Android device for APK install/open/basic offline queue behavior once the APK is green;
4. continue pairing/authenticated-channel design so transport discovery/health identity is upgraded before any business LAN payload is enabled;
5. add realtime `streamEpoch + sequence` resync and bounded background-finish behavior from the proven legacy V4 patterns;
6. add transport source-level tests/acceptance for duplicate retry, idempotency collision, device-sequence collision, queue ordering and restart/resync;
7. later run the final company-network/laptop/PDA regression without assuming Administrator/router/DNS/firewall changes.

A DEV Agent response `TEST_ACCEPTED_AGENT_ONLY` is transport-test evidence only. Future business commands must remain queued until `ACCEPTED_CANONICAL` from the Service/D1 authority.

## Gate 6 — Drive media/documents

After initial business API primitives are executable:
1. implement durable upload flow;
2. retain Drive file identity/checksum/metadata in D1;
3. enforce document DRAFT -> FINAL and replacement history;
4. implement employee portrait replacement semantics;
5. test upload failure/duplicate/retry and durable-readback gates.

## Gate 7 — Web

Do not start a mock-heavy frontend before the Auth/runtime and initial business API are usable.

Then:
1. create same-origin Web shell/login;
2. integrate session and permission-aware navigation;
3. add business modules against real BETA APIs;
4. add Drive/media flows;
5. run Web -> Service -> D1 -> Projection/Drive E2E acceptance.

## Physical dependencies

- Android APK installation/testing can proceed on the currently available test device once CI produces a green APK.
- Final corporate LAN PASS still depends on the real company laptop/network and intended PDA environment.
- Lack of that final physical environment does not pause Android source/build, LAN Agent source/build, LAN protocol/model work or independent Service/Web/Google work.

## STABLE

No promotion until full BETA PASS plus explicit Owner approval.
