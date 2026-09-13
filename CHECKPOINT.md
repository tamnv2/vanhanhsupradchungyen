# CHECKPOINT — VHDCHY

checkpoint_version: 12
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_SERVICE_BETA
action_mode: AUTONOMOUS_CLOUD_CI
active_lanes: WORKER_PACKAGING / SERVICE_AUTH / PROJECTION_OUTBOX / SERVICE_API / ANDROID_BUILD / LAN_AGENT / LAN_SOURCE_REUSE
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION
approved_scope: Continue Website-Service-Google backend work and build Android APK plus LAN Agent/model in parallel. The Owner has an Android device available for APK installation/testing. Final company-laptop/network LAN regression remains a later physical evidence gate, but it must not pause Android/LAN source/build work. Legacy repository remains NON_AUTHORITY read-only reference. Overall Owner interaction remains limited to OWNER_PERMISSION_REQUIRED or OWNER_DECISION_REQUIRED. STABLE remains separately gated by explicit Owner approval after BETA PASS.
reconciled_through_commit: 9dbd7197a46efb45f988f9e6f8417d588efdd2c3
active_work_ref: NEXT_ACTIONS.md
current_state_ref: CURRENT_STATE.md
authority_ref: DECISIONS.md
service_authority_ref: SERVICE_AUTHORITY.md
context_router_ref: CONTEXT_INDEX.md
packaging_plan_ref: docs/WORKER_PACKAGING_PLAN.md
projection_auth_plan_ref: docs/PROJECTION_AUTH_PLAN.md
canonical_mutation_plan_ref: docs/CANONICAL_MUTATION_PLAN.md
beta_acceptance_ref: docs/BETA_ACCEPTANCE_MATRIX.md
lan_source_review_ref: docs/LAN_SOURCE_REVIEW_20260913.md
lan_transport_contract_ref: docs/LAN_TRANSPORT_BETA_V1.md
lan_dev_build_status_ref: docs/LAN_DEV_BUILD_STATUS.md
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Completed provider gates

- D1 BETA `business_core_v3`: PASS.
- Worker BETA foundation/public health: PASS.
- Google Gateway immutable version 3 with fail-closed `VHDCHY_PROJECTION_V1`: PASS.
- Projection workbook remains intentionally `PROVISIONED_NOT_LIVE`.
- Fresh Cloudflare read-only verification run `34754968801`: SUCCESS; exact Worker/D1 identity, schema, bindings, `workers.dev` state, custom domain and zero business rows reconfirmed.
- LAN/Android first parallel build run `34756569016`: SUCCESS for both jobs.

## Worker packaging progress

- `service/worker/deploy.beta.json` declares exact reviewed modules: `index.js`, `auth.js`, `auth-service.js`, `authorization.js`, `session.js`, `permission-store.js`, `projection.js`.
- Manifest commit `45ee0cbadee6e6817e2894a7cfddfa4b995da9a4`; validation `34754738074`: SUCCESS.
- Multi-module packaging contract is persisted in `docs/WORKER_PACKAGING_PLAN.md`.
- Active deploy workflow still uploads only `index.js`; Worker deploy dispatch MUST NOT run until that workflow is safely updated and verified.

## Projection authentication design

- Current GAS version already fails closed on missing verifier and disabled projection state.
- Preferred management-plane provisioning is persisted in `docs/PROJECTION_AUTH_PLAN.md`.
- No projection secret or LIVE state is claimed yet.

## Canonical mutation design

- Atomic mutation contract is persisted in `docs/CANONICAL_MUTATION_PLAN.md`.
- Required invariant: guarded current state + immutable `domain_events` + `projection_outbox` commit together in one D1 batch or all roll back.
- Runtime helper/tests are not yet source-implemented because sensitive runtime-source writes are currently platform-blocked.

## Android + LAN lane active

- Legacy repo `tamnv2supra/vanhanhdchungyen` remains read-only NON_AUTHORITY reference at fixed V4 commit `7b4488a89f585812c1bccba5d07d86049482bf4c`.
- Current Android source is under `android-pilot/`, package `vn.vhdchy.transport.beta`.
- Current Agent source is under `lan-agent/`, portable .NET 8 user-mode, HTTP `17891`, UDP discovery `17892`.
- Android current DEV behavior: cached endpoint -> UDP discovery, two-sample anti-flap activation, echo and durable SQLite transport-test queue with stable `deviceId + deviceSeq + idempotencyKey`.
- Agent current DEV behavior: persistent instance ID, fresh `streamEpoch` on restart, durable test-event receipt, idempotency-payload collision guard and device-sequence collision guard.
- Cleartext DEV endpoints are explicitly transport-test only; they do not carry business credentials, PII or canonical mutations.
- Agent ACK `TEST_ACCEPTED_AGENT_ONLY` is not canonical acceptance and cannot be reused for future business queue deletion.
- Build workflow `.github/workflows/build-lan-dev.yml` executes Android and Agent jobs independently/in parallel.
- Run `34756569016`: SUCCESS.
- APK file SHA256: `bf311b44ae1d514f897678aeac97c23e34a366762a94d82d410a1f4f6a5f7629`.
- Agent ZIP file SHA256: `6681d7beec406cad34366074b998c5be70f046d1dc2d99db14c7c4a21a94b2a0`.
- APK install/open/basic local queue test can proceed on the currently available Android device.
- Final corporate-network LAN PASS still requires later company laptop/network + intended PDA evidence.

## BETA acceptance contract

- `docs/BETA_ACCEPTANCE_MATRIX.md` remains the dependency-ordered acceptance authority.
- Source/build presence alone is not physical/runtime PASS.
- Android/LAN acceptance must distinguish CI BUILD PASS, available-device APK behavior, generic Agent transport behavior and final corporate-network physical evidence.

## Platform write-safety constraint

The connected GitHub write path blocked modification of the provider-mutating Worker workflow and sensitive Auth/runtime source writes. This is a platform action-safety constraint, not an Owner permission blocker. Do not bypass it through lower-level Git/API methods or local-tooling workarounds merely to evade the guard.

Current Worker runtime remains safe: no post-manifest Worker deployment was triggered and protected business/admin routes remain fail-closed.

## Active source/CI foundation

- Auth crypto/password/token/TOTP foundation: source + CI PASS.
- Session expiry/revocation/device-security-epoch foundation: source + CI PASS.
- Scoped role/direct permission evaluation with DENY precedence: source + CI PASS.
- Non-ROOT password login/session issuance: source + CI PASS.
- ROOT password-only login remains fail-closed to `ROOT_MFA_REQUIRED`.
- Projection outbox envelope/retry/dead-letter foundation: source + CI PASS.
- Shared Service API contract remains authoritative for client/runtime semantics.
- Android/LAN source/build lane is now active and has its first green paired artifacts.

## Next execution order

1. Continue Worker/Service packaging/auth/projection work where allowed.
2. In parallel, install/test the current DEV APK on the available Android device.
3. Continue Android/LAN source work: authenticated pairing/channel binding, realtime epoch/sequence resync, bounded background finish and transport source tests.
4. When any suitable Windows machine is available, run the matching Agent and test discovery -> `LAN_ACTIVE` -> echo -> durable test-event flush.
5. Complete/runtime-integrate Auth/session/permission when sensitive-source write is available.
6. Provision Worker->GAS projection authentication and complete sender/ACK/retry/dead-letter E2E.
7. Implement/test canonical mutation helper and business commands.
8. Implement Drive media/document flow and then Web against real BETA APIs.
9. Later run final company-network/laptop/PDA LAN regression; do not confuse earlier generic/device testing with final corporate-network PASS.

## Owner stop conditions

- `OWNER_PERMISSION_REQUIRED`: exact Owner-controlled permission/access/consent/secret-store action is required and no safe connected/CI alternative exists.
- `OWNER_DECISION_REQUIRED`: material business/authority contradiction cannot be resolved from current Owner instruction, provider evidence or GitHub authority.

Neither condition is currently established. Continue safe actionable work.

## do_not_repeat:

Do not rerun V1->V3 migration. Do not recreate verified provider resources. Do not trigger Worker deployment while active deploy workflow is single-module. Do not open business APIs anonymously. Do not make Sheets canonical. Do not enable projection before secure auth + E2E PASS. Do not expose secrets in source/chat. Do not bypass platform safety guards. Do not treat legacy repo as authority or runtime fallback. Do not use cleartext transport-test endpoints for business credentials/PII/canonical mutations. Do not delete a future business command from a local queue on `TEST_ACCEPTED_AGENT_ONLY`; canonical Service/D1 acceptance is required. Do not pause Android/LAN source/build solely because final corporate-network hardware is unavailable. Do not claim final physical LAN PASS from CI or generic-device evidence alone. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
