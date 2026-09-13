# CHECKPOINT — VHDCHY

checkpoint_version: 9
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_SERVICE_BETA
action_mode: AUTONOMOUS_CLOUD_CI
active_lanes: WORKER_PACKAGING / SERVICE_AUTH / PROJECTION_OUTBOX / SERVICE_API
paused_lanes: ANDROID / PHYSICAL_LAN
approved_scope: Continue Website-Service-Google backend path autonomously. Android/PDA build and physical LAN/model work are paused by current Owner instruction until the required Android/company-laptop environment is available. Overall Owner interaction remains limited to OWNER_PERMISSION_REQUIRED or OWNER_DECISION_REQUIRED. STABLE remains separately gated by explicit Owner approval after BETA PASS.
reconciled_through_commit: c03c62b7dc95f312e7e285f471a22ffda769fdf7
active_work_ref: NEXT_ACTIONS.md
current_state_ref: CURRENT_STATE.md
authority_ref: DECISIONS.md
service_authority_ref: SERVICE_AUTHORITY.md
context_router_ref: CONTEXT_INDEX.md
packaging_plan_ref: docs/WORKER_PACKAGING_PLAN.md
projection_auth_plan_ref: docs/PROJECTION_AUTH_PLAN.md
canonical_mutation_plan_ref: docs/CANONICAL_MUTATION_PLAN.md
beta_acceptance_ref: docs/BETA_ACCEPTANCE_MATRIX.md

## Completed provider gates

- D1 BETA `business_core_v3`: PASS.
- Worker BETA foundation/public health: PASS.
- Google Gateway immutable version 3 with fail-closed `VHDCHY_PROJECTION_V1`: PASS.
- Projection workbook remains intentionally `PROVISIONED_NOT_LIVE`.
- Fresh Cloudflare read-only verification run `34754968801`: SUCCESS; exact Worker/D1 identity, schema, bindings, `workers.dev` state, custom domain and zero business rows reconfirmed.
- Fresh clean-baseline validation run `34754968807`: SUCCESS.
- Canonical mutation design validation run `34755193469`: SUCCESS.

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

## BETA acceptance contract

- `docs/BETA_ACCEPTANCE_MATRIX.md` now defines dependency-ordered acceptance for Provider, Auth, Authorization, canonical mutation, attendance/presence, PICK/PACK/resources, labor/dropped goods, Drive/documents, Google projection, Web E2E and pre-STABLE durability.
- Source presence alone is explicitly not runtime PASS; every PASS claim requires CI/provider/readback evidence.
- Android/PDA and physical LAN acceptance are excluded from the current execution scope until Owner resumes those lanes.

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

## Next execution order

1. Safely enable reviewed multi-module Worker packaging through an allowed high-level write path; perform packaging-only deployment with business routes still fail-closed.
2. Complete/runtime-integrate Auth/session/permission and acceptance tests.
3. Provision Worker->GAS projection authentication, then sender/ACK/retry/dead-letter E2E while keeping Google non-canonical.
4. Implement/test canonical mutation helper and business commands in dependency order using `BETA_ACCEPTANCE_MATRIX.md` as the acceptance contract.
5. Implement Drive media/document flow.
6. Start same-origin Web against real BETA Auth/business APIs.

## Owner stop conditions

- `OWNER_PERMISSION_REQUIRED`: exact Owner-controlled permission/access/consent/secret-store action is required and no safe connected/CI alternative exists.
- `OWNER_DECISION_REQUIRED`: material business/authority contradiction cannot be resolved from current Owner instruction, provider evidence or GitHub authority.

Neither condition is currently established. Continue safe actionable work.

## do_not_repeat:

Do not rerun V1->V3 migration. Do not recreate verified provider resources. Do not trigger Worker deployment while active deploy workflow is single-module. Do not open business APIs anonymously. Do not make Sheets canonical. Do not enable projection before secure auth + E2E PASS. Do not expose secrets in source/chat. Do not bypass platform safety guards. Do not build Android/PDA or physical LAN until Owner resumes those lanes. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
