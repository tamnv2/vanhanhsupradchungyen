# CHECKPOINT — VHDCHY

checkpoint_version: 13
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V2_BETA
action_mode: AUTONOMOUS_PARALLEL
active_lanes: SHARED_DOMAIN / CLOUD_SERVICE / LAN_SERVICE / SERVICE_AUTH / PROJECTION_OUTBOX / WEB / ANDROID_APK / GOOGLE_DRIVE_SHEETS
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION
approved_scope: Build one VHDCHY product with Website + PDA-optimized APK + Cloud Service + LAN Service in parallel. LAN must substitute for Cloud Service during site Internet loss, Cloud Service failure/degradation, and support per-client forced-LAN routing. Legacy repo and the pre-clarification transport-only APK/Agent prototype are NON_AUTHORITY reference only. Do not extend diagnostic/pilot architecture as the product. Overall Owner interaction remains limited to OWNER_PERMISSION_REQUIRED or OWNER_DECISION_REQUIRED. STABLE remains separately gated by explicit Owner approval after full BETA PASS.
reconciled_through_commit: 856cc8cdc0089514177a8176becb63ce6e63e429
active_work_ref: NEXT_ACTIONS.md
current_state_ref: CURRENT_STATE.md
authority_ref: DECISIONS.md
service_authority_ref: SERVICE_AUTHORITY.md
product_architecture_ref: docs/TARGET_PRODUCT_ARCHITECTURE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V2.md
service_contract_ref: docs/SERVICE_API_CONTRACT.md
beta_acceptance_ref: docs/BETA_ACCEPTANCE_MATRIX.md
canonical_mutation_plan_ref: docs/CANONICAL_MUTATION_PLAN.md
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

## Architecture correction completed

Created/updated:
- `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md` — authoritative product topology and failover semantics;
- `docs/DELIVERY_PLAN_V2.md` — parallel vertical-slice delivery plan;
- `DECISIONS.md` D-042..D-048 — Owner clarification persisted;
- `PROJECT_SCOPE.md` — four first-class deliverables;
- `docs/ARCHITECTURE.md` — Cloud + LAN dual runtime;
- `docs/SERVICE_API_CONTRACT.md` V2 — Cloud direct / LAN relay / LAN autonomous / reconciliation statuses;
- `docs/BETA_ACCEPTANCE_MATRIX.md` — Web/APK/LAN/failover/split-brain acceptance added;
- `docs/LAN_TRANSPORT_BETA_V1.md` — transport-only product authority superseded;
- `docs/LAN_DEV_BUILD_STATUS.md` — earlier green paired build reclassified as disposable prototype evidence.

## Current exact implementation position

Phase 0 architecture correction is complete.

Phase 1 is ACTIVE NOW: shared domain/API + dual-runtime contract/source boundary.

Required immediate implementation sequence:
1. define provider-neutral domain core interface and shared business acceptance vectors;
2. define Cloud D1 adapter boundary;
3. define LAN edge persistence/snapshot/event/outbox/reconciliation adapter boundary;
4. extend canonical mutation design for equivalent LAN edge transactions and reconciliation identity;
5. only then begin real vertical business Slice 1 across Cloud + LAN + Web + APK.

Android/LAN lanes are active; they are not paused. Their next work is product architecture/domain integration, not test-APK polishing.

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
- Continue shared-domain/LAN/Web/APK/acceptance/design/source lanes that are independent of this blocker.

## Cloud source foundation

Source/CI foundation exists for:
- password/hash/bearer/TOTP;
- session expiry/revocation/device-security epoch;
- scoped permissions + DENY precedence;
- normal login/session issuance;
- ROOT password-only fail-closed to MFA;
- projection outbox retry/dead-letter;
- canonical mutation design.

Not yet runtime-live:
- integrated protected Worker routes;
- canonical mutation helper/business APIs;
- projection sender/auth;
- Drive flow;
- Web business UI.

## LAN runtime target

Required modes:
- `LAN_RELAY`: client -> LAN -> Cloud/D1, preserving command identity;
- `LAN_AUTONOMOUS`: local edge state + immutable edge event + sync outbox while Cloud unavailable;
- recovery/reconciliation into D1 exactly once where non-conflicting;
- explicit `SYNC_CONFLICT` for real partition conflicts;
- locally served compatible Web bundle for complete Internet outage;
- no-admin/user-mode constraints preserved.

A hard partition cannot provide both global strict single-writer consistency and uninterrupted local writes. The design therefore must retain evidence and reconcile conflicts, never silently overwrite/drop.

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

Do not hide assumptions for these in code. Keep unresolved privileged cases fail-closed and surface `OWNER_DECISION_REQUIRED` only when implementation reaches a point where one of these choices is unavoidable.

## Next execution order

1. Reconcile/implement shared domain-core boundary and acceptance vectors.
2. Define LAN edge DB/event/outbox/snapshot/reconciliation contract.
3. Extend canonical mutation model for Cloud/LAN parity.
4. In parallel continue every unblocked Cloud auth/projection/package task.
5. Build client endpoint-state foundation for Web/APK against the shared contract.
6. Implement vertical Slice 1: identity/employee/attendance/presence across Cloud + LAN + Web + APK.
7. Continue Slice 2 resources/PICK/PACK, Slice 3 labor/dropped goods, Slice 4 documents/media.
8. Run failover/recovery/split-brain acceptance.
9. Run final company-network/PDA physical regression when environment is available.
10. No STABLE promotion before full BETA PASS + explicit Owner approval.

## Owner stop conditions

- `OWNER_PERMISSION_REQUIRED`: exact Owner-controlled permission/access/consent/secret-store action is required and no safe connected/CI alternative exists.
- `OWNER_DECISION_REQUIRED`: a material unresolved business/security policy must be chosen before safe implementation can proceed.

Neither condition is currently established for Phase 1 shared-domain/LAN edge design work. Continue.

## do_not_repeat:

Do not rerun V1->V3 migration. Do not recreate verified provider resources. Do not trigger Worker deploy while active deploy workflow is single-module. Do not open business APIs anonymously. Do not make Sheets canonical or use Sheets as LAN fallback storage. Do not expose secrets in source/chat. Do not bypass platform safety guards. Do not treat legacy repo or the temporary transport prototype as product authority. Do not build APK as merely a LAN test app. Do not build LAN as merely discovery/echo/transport relay. Do not create separate Cloud/LAN business-rule implementations that can drift. Do not report LAN local acceptance as D1/Cloud commit. Do not silently drop/overwrite split-brain conflicts. Do not invent unresolved offline-auth TTL/privileged policy. Do not claim physical corporate-LAN PASS from legacy/CI evidence. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
