# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- `AI_AUTHORITY_RESUME_V2` is active.
- `D-041` remains active: overall execution continues by default and a blocked single lane does not stop independent safe work.
- Current Owner scope pauses Android/PDA build and physical LAN/model work; active priority is Worker/Service -> D1 -> Google Sheets/Drive -> Web.
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

## Target architecture

- D1 is canonical structured business authority.
- Google Sheets is projection/reconciliation/DR only.
- Google Drive stores media/documents/archive; D1 stores identifiers, metadata, hashes and state.
- Web and later APK use one Service/domain contract.
- Web implementation follows usable Auth + initial business API runtime rather than a mock-heavy early frontend.

## Paused lanes / STABLE

- Android/PDA build and physical LAN/model work are paused by current Owner instruction until required environments are available.
- STABLE remains blocked until full BETA PASS plus explicit Owner approval.
