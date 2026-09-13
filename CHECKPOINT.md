# CHECKPOINT — VHDCHY

checkpoint_version: 14
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V2_BETA
action_mode: AUTONOMOUS_PARALLEL
active_lanes: SHARED_DOMAIN / CLOUD_SERVICE / LAN_SERVICE / SERVICE_AUTH / PROJECTION_OUTBOX / WEB / ANDROID_APK / GOOGLE_DRIVE_SHEETS
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION
approved_scope: Build one VHDCHY product with Website + PDA-optimized APK + Cloud Service + LAN Service in parallel. LAN must substitute for Cloud Service during site Internet loss, Cloud Service failure/degradation, and support per-client forced-LAN routing. Legacy repo and the pre-clarification transport-only APK/Agent prototype are NON_AUTHORITY reference only. Do not extend diagnostic/pilot architecture as the product. Overall Owner interaction remains limited to OWNER_PERMISSION_REQUIRED or OWNER_DECISION_REQUIRED. STABLE remains separately gated by explicit Owner approval after full BETA PASS.
reconciled_through_commit: 3bd2e438f65c6da0b5084192f6ba97aa0f772046
active_work_ref: NEXT_ACTIONS.md
current_state_ref: CURRENT_STATE.md
authority_ref: DECISIONS.md
service_authority_ref: SERVICE_AUTHORITY.md
product_architecture_ref: docs/TARGET_PRODUCT_ARCHITECTURE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V2.md
service_contract_ref: docs/SERVICE_API_CONTRACT.md
beta_acceptance_ref: docs/BETA_ACCEPTANCE_MATRIX.md
canonical_mutation_plan_ref: docs/CANONICAL_MUTATION_PLAN.md
lan_edge_state_ref: docs/LAN_EDGE_STATE_V1.md
projection_auth_plan_ref: docs/PROJECTION_AUTH_PLAN.md
worker_packaging_plan_ref: docs/WORKER_PACKAGING_PLAN.md
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Owner clarification reconciled

The previous interpretation was too narrow. The Owner does NOT want:
- a standalone LAN diagnostics APK as the product;
- LAN only as a transport/relay proof;
- wholesale copying of the legacy repo;
- sequential completion of Cloud/Web first and Android/LAN later.

Current locked product direction:
1. Website and APK are clients of the same VHDCHY business/domain platform.
2. APK is a compact PDA-oriented business client, not separate business logic.
3. Cloudflare Worker + D1 is the normal online Service runtime.
4. LAN Service is a real substitute runtime when Internet/Cloud Service is unavailable.
5. Forced-LAN per client is supported; when Cloud is reachable, LAN relay is preferred to avoid unnecessary split-brain.
6. True Cloud/upstream loss uses LAN autonomous local edge execution for reviewed offline-capable commands, followed by D1 reconciliation.
7. Google Sheets/Drive remain downstream/deferred, never LAN fallback databases.
8. Legacy/prototype mechanics are selectively adapted only after review.

## Architecture/contract correction completed

Created/updated:
- `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md` — product topology/failover semantics;
- `docs/DELIVERY_PLAN_V2.md` — parallel vertical-slice delivery;
- `DECISIONS.md` D-042..D-048 — Owner clarification persisted;
- `PROJECT_SCOPE.md`, `docs/ARCHITECTURE.md`, `SERVICE_AUTHORITY.md` — four-deliverable/dual-runtime authority;
- `docs/SERVICE_API_CONTRACT.md` V2 — Cloud direct / LAN relay / LAN autonomous / commit-status/reconciliation semantics;
- `docs/CANONICAL_MUTATION_PLAN.md` V2 — Cloud D1 transaction + LAN edge transaction + reconciliation model;
- `docs/LAN_EDGE_STATE_V1.md` — edge snapshot/current-state/event/outbox/conflict/staged-media contract;
- `docs/BETA_ACCEPTANCE_MATRIX.md` — Cloud/LAN/Web/APK/failover/split-brain acceptance;
- `docs/LAN_TRANSPORT_BETA_V1.md` — transport-only product authority superseded;
- `docs/LAN_DEV_BUILD_STATUS.md` — earlier green paired build reclassified as disposable prototype evidence;
- `CHANGELOG.md` — V2 architecture correction recorded.

Validation run `34757604568` for the latest edge-state contract commit: SUCCESS.

## Current exact implementation position

Phase 0 architecture correction is complete.

Phase 1 shared dual-runtime contract is substantially defined at design level:
- Service API V2: defined;
- Cloud/LAN mutation/reconciliation semantics: defined;
- LAN edge state/snapshot/event/outbox model: defined;
- acceptance matrix: defined.

Next implementation work:
1. materialize provider-neutral domain-core source boundary and shared business acceptance vectors;
2. materialize Cloud D1 adapter interface/source around the existing D1 model;
3. materialize LAN edge adapter/interface/source around `LAN_EDGE_STATE_V1`;
4. then start real vertical Slice 1 identity/employee/attendance/presence across Cloud + LAN + Web + APK.

Android/LAN lanes remain active. Their next work is product/domain integration, not transport-test APK polishing.

## Provider foundation — retained PASS

- D1 BETA `business_core_v3`: PASS.
- Worker BETA foundation/public health: PASS.
- Google Gateway immutable version 3 with fail-closed `VHDCHY_PROJECTION_V1`: PASS.
- Projection workbook remains intentionally `PROVISIONED_NOT_LIVE`.
- Fresh Cloudflare read-only verification run `34754968801`: SUCCESS.

## Cloud Worker packaging blocker

- `service/worker/deploy.beta.json` contains reviewed seven-module manifest.
- Manifest validation `34754738074`: SUCCESS.
- Active provider-mutating deploy workflow still uploads only `index.js`.
- Connected write-safety blocks the sensitive workflow/runtime write path.
- Do NOT bypass through lower-level Git/API methods.
- Continue shared-domain/LAN/Web/APK/acceptance/design/source lanes independent of this blocker.

## Cloud source foundation

Source/CI foundation exists for:
- password/hash/bearer/TOTP;
- session expiry/revocation/device-security epoch;
- scoped permissions + DENY precedence;
- normal login/session issuance;
- ROOT password-only fail-closed to MFA;
- projection outbox retry/dead-letter.

Not yet runtime-live:
- integrated protected Worker routes;
- provider-neutral domain core + D1 adapter implementation;
- business APIs;
- projection sender/auth;
- Drive flow;
- Web business UI.

## LAN runtime target

Required modes:
- `LAN_RELAY`: client -> LAN -> Cloud/D1, preserving command identity;
- `LAN_AUTONOMOUS`: local edge current state + immutable edge event + sync outbox while Cloud unavailable;
- recovery/reconciliation into D1 exactly once where non-conflicting;
- explicit `SYNC_CONFLICT` for real partition conflicts;
- locally served compatible Web bundle for complete Internet outage;
- no-admin/user-mode constraints preserved.

A hard partition cannot provide both global strict single-writer consistency and uninterrupted local writes. The design therefore retains evidence and reconciles conflicts; silent overwrite/drop is prohibited.

## Android target

APK is the real PDA operational client:
- same user/permission/domain contract as Web;
- PDA-optimized UI only;
- same Cloud/LAN runtime state model;
- scanner/QR input through domain commands;
- client-local durable queue only where explicitly allowed;
- no direct D1/Sheets shortcut.

The current `android-pilot/` is NON_AUTHORITY prototype/reference only.

## Unresolved policy — do_not_invent

The Owner has not yet locked:
- exact offline authentication/capability mechanism;
- exact offline authorization TTL/staleness;
- which privileged/security admin operations are allowed offline;
- who may explicitly activate/deactivate emergency autonomous mode.

Do not hide assumptions for these in code. Keep unresolved privileged cases fail-closed and surface `OWNER_DECISION_REQUIRED` only when implementation reaches a point where one choice is unavoidable.

## Next execution order

1. Implement shared domain-core source boundary and runtime-neutral acceptance vectors.
2. Implement LAN edge schema/adapter/source from `LAN_EDGE_STATE_V1` while preserving no-admin constraints.
3. Implement Cloud D1 adapter/canonical helper through allowed source path; continue packaging/auth/projection work independently.
4. Build Web/APK endpoint/runtime-state foundation against the shared contract.
5. Implement vertical Slice 1 identity/employee/attendance/presence across Cloud + LAN + Web + APK.
6. Continue Slice 2 resources/PICK/PACK, Slice 3 labor/dropped goods, Slice 4 documents/media.
7. Run failover/recovery/split-brain acceptance.
8. Run final company-network/PDA physical regression when environment is available.
9. No STABLE promotion before full BETA PASS + explicit Owner approval.

## Owner stop conditions

- `OWNER_PERMISSION_REQUIRED`: exact Owner-controlled permission/access/consent/secret-store action is required and no safe connected/CI alternative exists.
- `OWNER_DECISION_REQUIRED`: a material unresolved business/security policy must be chosen before safe implementation can proceed.

Neither condition is currently established for shared-domain/LAN edge source work. Continue.

## do_not_repeat:

Do not rerun V1->V3 migration. Do not recreate verified provider resources. Do not trigger Worker deploy while active deploy workflow is single-module. Do not open business APIs anonymously. Do not make Sheets canonical or use Sheets as LAN fallback storage. Do not expose secrets in source/chat. Do not bypass platform safety guards. Do not treat legacy repo or temporary transport prototype as product authority. Do not build APK as merely a LAN test app. Do not build LAN as merely discovery/echo/transport relay. Do not create separate Cloud/LAN business-rule implementations that can drift. Do not report LAN local acceptance as D1/Cloud commit. Do not silently drop/overwrite split-brain conflicts. Do not invent unresolved offline-auth TTL/privileged policy. Do not claim physical corporate-LAN PASS from legacy/CI evidence. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
