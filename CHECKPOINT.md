# CHECKPOINT — VHDCHY

checkpoint_version: 11
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_SERVICE_BETA
action_mode: AUTONOMOUS_CLOUD_CI
active_lanes: WORKER_PACKAGING / SERVICE_AUTH / PROJECTION_OUTBOX / SERVICE_API / LAN_SOURCE_REUSE
paused_lanes: ANDROID_BUILD / PHYSICAL_LAN
approved_scope: Continue Website-Service-Google backend path autonomously and resume LAN source/model adaptation using the legacy repository as NON_AUTHORITY read-only reference. Android/PDA packaging/signing/physical regression and physical company-network LAN regression remain pending the required environment. Overall Owner interaction remains limited to OWNER_PERMISSION_REQUIRED or OWNER_DECISION_REQUIRED. STABLE remains separately gated by explicit Owner approval after BETA PASS.
reconciled_through_commit: 1dcb78afc0b4d9cc26fce6b507c7ff8d60c88a34
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
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Completed provider gates

- D1 BETA `business_core_v3`: PASS.
- Worker BETA foundation/public health: PASS.
- Google Gateway immutable version 3 with fail-closed `VHDCHY_PROJECTION_V1`: PASS.
- Projection workbook remains intentionally `PROVISIONED_NOT_LIVE`.
- Fresh Cloudflare read-only verification run `34754968801`: SUCCESS; exact Worker/D1 identity, schema, bindings, `workers.dev` state, custom domain and zero business rows reconfirmed.
- Validation run `34756136119`: SUCCESS after LAN source-reuse scope/checkpoint update.

## Packaging progress

- `service/worker/deploy.beta.json` declares exact reviewed modules: `index.js`, `auth.js`, `auth-service.js`, `authorization.js`, `session.js`, `permission-store.js`, `projection.js`.
- Manifest commit `45ee0cbadee6e6817e2894a7cfddfa4b995da9a4`; validation `34754738074`: SUCCESS.
- Multi-module packaging contract is persisted in `docs/WORKER_PACKAGING_PLAN.md`.
- Active deploy workflow still uploads only `index.js`; Worker deploy dispatch MUST NOT run until that workflow is safely updated and verified.

## Projection authentication design

- Current GAS version already fails closed on missing verifier and disabled projection state.
- Preferred management-plane provisioning is persisted in `docs/PROJECTION_AUTH_PLAN.md`:
  - raw high-entropy token authority in GitHub Environment `beta` secret;
  - raw token becomes a Worker secret binding;
  - GAS stores only SHA-256 verifier in Script Properties;
  - provisioning should use authenticated Apps Script API execution restricted to the deploying identity rather than the anonymous Web App data plane;
  - projection enable remains a separate explicit gate after auth + sender/ACK/retry acceptance.
- No projection secret or LIVE state is claimed yet.

## Canonical mutation design

- Atomic mutation contract is persisted in `docs/CANONICAL_MUTATION_PLAN.md`.
- Required invariant: guarded current state + immutable `domain_events` + `projection_outbox` commit together in one D1 batch or all roll back.
- Design includes in-transaction guarded-row assertion, idempotency race reconciliation, device-sequence collision handling and degraded-Google behavior.
- Runtime helper/tests are not yet source-implemented because sensitive runtime-source writes are currently platform-blocked.

## LAN source/model lane resumed

- Legacy repo `tamnv2supra/vanhanhdchungyen` is accessible for read/reference; it is not current authority and has no required write role.
- Fixed legacy V4 reference commit `7b4488a89f585812c1bccba5d07d86049482bf4c` is verified.
- Legacy physical evidence confirms the old pilot materially reached two-MT90 `LAN_ACTIVE`, durable queue recovery, duplicate rejection and measured LAN performance, but final V4 physical regression remained pending.
- Direct source review confirms reusable patterns: no-admin Agent, cached endpoint -> UDP -> manual recovery, health + anti-flapping hysteresis, durable `event_id + device_seq` queue, ACK-driven deletion, epoch/sequence resync, diagnostics/load/transfer instrumentation and bounded Android background work.
- `docs/LAN_SOURCE_REVIEW_20260913.md` records the adoption boundary and keeps cleartext pilot endpoints/non-cryptographic identity checks out of current business transport.
- `docs/LAN_TRANSPORT_BETA_V1.md` is now the current transport design: stable retry identity across LAN/cloud, canonical ACK requirement before queue deletion, authenticated pairing requirement, anti-flap state machine, durable queue contract, Agent relay boundary and `streamEpoch + sequence` resync semantics.
- LAN source/model/protocol adaptation is ACTIVE; only Android build/signing and physical company-network/PDA regression remain paused.

## BETA acceptance contract

- `docs/BETA_ACCEPTANCE_MATRIX.md` defines dependency-ordered acceptance for Provider, Auth, Authorization, canonical mutation, attendance/presence, PICK/PACK/resources, labor/dropped goods, Drive/documents, Google projection, Web E2E and pre-STABLE durability.
- Source presence alone is explicitly not runtime PASS; every PASS claim requires CI/provider/readback evidence.
- LAN source-level acceptance must now be extended to cover command retry identity, durable queue ordering/ACK semantics, discovery/auth/anti-flap and epoch resync without claiming physical PASS.

## Platform write-safety constraint

The connected GitHub write path blocked modification of the provider-mutating deploy workflow and sensitive Auth/runtime source writes. This is a platform action-safety constraint, not an Owner permission blocker. Do not bypass it through lower-level Git/API methods or local-tooling workarounds merely to evade the guard.

Current runtime remains safe: no post-manifest Worker deployment was triggered and protected business/admin routes remain fail-closed.

## Active source/CI foundation

- Auth crypto/password/token/TOTP foundation: source + CI PASS.
- Session expiry/revocation/device-security-epoch foundation: source + CI PASS.
- Scoped role/direct permission evaluation with DENY precedence: source + CI PASS.
- Non-ROOT password login/session issuance: source + CI PASS.
- ROOT password-only login remains fail-closed to `ROOT_MFA_REQUIRED`.
- Projection outbox envelope/retry/dead-letter foundation: source + CI PASS.
- Shared Service API contract remains authoritative for client/runtime semantics.
- LAN source/model adaptation continues in parallel from the fixed legacy V4 reference and current `LAN_TRANSPORT_BETA_V1` contract.

## Next execution order

1. Safely enable reviewed multi-module Worker packaging through an allowed high-level write path; perform packaging-only deployment with business routes still fail-closed.
2. In parallel, design the authenticated Agent/PDA pairing/channel binding and exact durable queue/ACK state contract under `LAN_TRANSPORT_BETA_V1`.
3. Extend BETA acceptance with LAN source-level tests and prepare current LAN adapters/source structure where safe.
4. Complete/runtime-integrate Auth/session/permission and acceptance tests when sensitive-source write is available.
5. Provision Worker->GAS projection authentication, then sender/ACK/retry/dead-letter E2E while keeping Google non-canonical.
6. Implement/test canonical mutation helper and business commands in dependency order using `BETA_ACCEPTANCE_MATRIX.md`.
7. Implement Drive media/document flow.
8. Start same-origin Web against real BETA Auth/business APIs.
9. When the real company laptop/PDA environment is available, run physical LAN/Android regression against the current adapted implementation.

## Owner stop conditions

- `OWNER_PERMISSION_REQUIRED`: exact Owner-controlled permission/access/consent/secret-store action is required and no safe connected/CI alternative exists.
- `OWNER_DECISION_REQUIRED`: material business/authority contradiction cannot be resolved from current Owner instruction, provider evidence or GitHub authority.

Neither condition is currently established. Continue safe actionable work.

## do_not_repeat:

Do not rerun V1->V3 migration. Do not recreate verified provider resources. Do not trigger Worker deployment while active deploy workflow is single-module. Do not open business APIs anonymously. Do not make Sheets canonical. Do not enable projection before secure auth + E2E PASS. Do not expose secrets in source/chat. Do not bypass platform safety guards. Do not treat legacy repo as authority or runtime fallback. Do not copy legacy cleartext pilot endpoints into business transport. Do not delete a business command from a local queue merely because the LAN Agent received it; canonical acceptance/reconciliation is required. Do not claim physical LAN PASS from legacy evidence alone. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
