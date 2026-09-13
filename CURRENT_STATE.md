# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- `AI_AUTHORITY_RESUME_V2` is active.
- `D-041` remains active: overall execution continues by default and a blocked single lane does not stop independent safe work.
- Android/APK build and LAN Agent/model development are now ACTIVE in parallel with Worker/Service/Google/Web work by current Owner instruction.
- The Owner has an Android device available for APK installation/testing. Final company-laptop/network LAN regression remains environment-dependent and is a later physical gate, not a reason to pause source/build work.
- Legacy reference repository: `tamnv2supra/vanhanhdchungyen`; fixed V4 comparison commit `7b4488a89f585812c1bccba5d07d86049482bf4c`. It is NON_AUTHORITY and cannot override current decisions/contracts.
- Secret values remain outside repository source/chat.

## GitHub BETA environment

- Environment `beta` is the provider execution boundary.
- Google and Cloudflare CI credentials remain in Environment secret/variable stores.
- Read-only Cloudflare verification, guarded D1 migration, guarded Worker deployment and GAS sync bridges exist.
- Platform write-safety currently blocks modification of the provider-mutating Worker workflow and sensitive Auth/runtime source through the connected GitHub write path. This is not classified as an Owner permission blocker and must not be bypassed through lower-level Git/API methods.

## Google / Drive / Sheets / GAS

- Current Google authority: `tam95.supra@gmail.com`.
- Project root: `VẬN HÀNH DC HƯNG YÊN` with `01_BETA` and `02_STABLE` roots.
- BETA cluster: `PICK_PACK_1291`.
- BETA workbook: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`.
- Workbook schema remains `PP1291_SHEETS_BETA_V1`; D1 remains canonical; projection remains `PROVISIONED_NOT_LIVE`.
- GAS managed deployment immutable version `3`, deployment run `34753872034`: PASS.
- Version 3 implements fail-closed `VHDCHY_PROJECTION_V1`, including fixed sheet/key mapping, bounded batches, unknown-column rejection, key upsert, ScriptLock, shared-token verifier gate and separate projection-enable gate.
- Secure coordinated provisioning design is persisted in `docs/PROJECTION_AUTH_PLAN.md`.
- Projection writes remain disabled until auth provisioning plus sender/ACK/retry/idempotency/degraded-Google acceptance pass.

## Cloudflare D1 BETA — BUSINESS_CORE_V3 PASS

- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- D1: `vhdchy-data-beta`.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- Provider schema: `business_core_v3`.
- Fresh read-only verification run `34754968801`: SUCCESS.
- Verification reconfirmed exact database/Worker identity, full V3 table set, required baseline seeds and zero business rows.

## Cloudflare Worker BETA — FOUNDATION LIVE / BUSINESS FAIL-CLOSED

- Worker: `vhdchy-beta`.
- Public origin: `https://beta.supra.cc.cd`.
- `workers.dev`: disabled.
- Expected bindings remain `DB`/D1, `APP_ENV`/plain text, `BUILD_SHA`/plain text and `GAS_EXEC_URL`/plain text.
- Fresh provider verification run `34754968801`: SUCCESS.
- Existing runtime health/meta/capability foundation remains the live contract.
- Protected business/admin APIs remain fail-closed.

## Worker packaging state

- Current active deploy workflow still uploads only `service/worker/src/index.js`.
- `service/worker/deploy.beta.json` now contains an exact reviewed seven-module manifest: `index.js`, `auth.js`, `auth-service.js`, `authorization.js`, `session.js`, `permission-store.js`, `projection.js`.
- Manifest commit `45ee0cbadee6e6817e2894a7cfddfa4b995da9a4`; validation run `34754738074`: SUCCESS.
- Packaging implementation contract is persisted in `docs/WORKER_PACKAGING_PLAN.md`.
- No Worker deploy has been triggered after this manifest-only change. Do not deploy until the workflow itself supports and verifies the reviewed module set.

## Parallel Service implementation state

### Auth/session/permission
- Password/hash, bearer-token and TOTP foundation exists.
- Session resolution, expiry/revocation/device-security-epoch checks exist.
- Scoped role/direct permission loading and explicit DENY precedence exist.
- Non-ROOT password login/session issuance exists and is unit-tested.
- ROOT password-only login remains fail-closed to `ROOT_MFA_REQUIRED`.
- Runtime integration remains pending packaging PASS and an allowed sensitive-source write path.

### Projection/outbox
- `projection.js` contains bounded pending-outbox reads, processing/ACK/failure states, exponential retry and DEAD transition foundation.
- GAS projection version 3 is deployed but not write-live.
- Management-plane provisioning design prefers authenticated Apps Script API execution rather than provisioning through the anonymous Web App data plane.
- D1 remains authoritative if Google is degraded; projection failure may not roll back committed D1 business state.

### Shared API / mutation
- `docs/SERVICE_API_CONTRACT.md` defines bearer/session rules, effective permissions, idempotency, errors and canonical state + immutable event + outbox semantics.
- Reusable canonical mutation helper remains to be implemented/tested when sensitive runtime-source write is available.
- No Web business surface is live yet.

## Android + LAN build lane — ACTIVE

- Current LAN restoration review: `docs/LAN_SOURCE_REVIEW_20260913.md`.
- Current transport contract: `docs/LAN_TRANSPORT_BETA_V1.md`.
- Active build/status record: `docs/LAN_DEV_BUILD_STATUS.md`.
- Legacy repo `tamnv2supra/vanhanhdchungyen` remains read-only NON_AUTHORITY reference at fixed V4 commit `7b4488a89f585812c1bccba5d07d86049482bf4c`.
- Current Android source now exists under `android-pilot/`; package `vn.vhdchy.transport.beta` is an isolated DEV transport pilot so it can evolve without claiming current business-client authority.
- Android DEV source implements cached endpoint -> UDP discovery, two-sample anti-flap activation, LAN state display, echo test and a durable SQLite transport-test queue using stable `deviceId + deviceSeq + idempotencyKey` per queued event.
- Current Windows Agent source now exists under `lan-agent/`; it is a portable .NET 8 user-mode process using HTTP `17891` and UDP discovery `17892`, with persistent Agent instance ID, fresh `streamEpoch` per start and durable transport-test receipt metadata.
- Agent transport-test receipt enforces idempotency payload collision and `(deviceId, deviceSeq)` collision guards.
- DEV Agent ACK is intentionally `TEST_ACCEPTED_AGENT_ONLY`; it is not a canonical D1 business ACK.
- Current cleartext DEV endpoints are transport-test only and must not carry business credentials, employee PII or canonical business mutations.
- `.github/workflows/build-lan-dev.yml` builds the Android APK and portable win-x64 Agent in two independent parallel jobs with no provider mutation and no release-signing secret.
- First parallel build run: `34756569016` (started from commit `bd3e3f379fc954c781f37ede958bed53b961c0a8`; verify final result before claiming BUILD PASS).

## Target architecture

- D1 is canonical structured business authority.
- Google Sheets is projection/reconciliation/DR only.
- Google Drive stores media/documents/archive; D1 stores identifiers, metadata, hashes and state.
- Web and later production APK use one Service/domain contract.
- LAN is a transport/fallback path over the same command/event model, with local durable queue and explicit authentication/pairing before business activation.
- Web implementation follows usable Auth + initial business API runtime rather than a mock-heavy early frontend.

## Remaining physical dependencies / STABLE

- Android source/build is active and APK installation testing may proceed on the available device once the current artifact is green.
- Final corporate-network LAN regression still requires the intended company laptop/network and PDA environment; this remains a physical evidence gate only.
- LAN business traffic remains disabled until pairing/authenticated-channel design is implemented and accepted.
- STABLE remains blocked until full BETA PASS plus explicit Owner approval.
