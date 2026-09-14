# CHECKPOINT — VHDCHY

checkpoint_version: 43
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 955ac7f05008059240d5dca06433f565d8a9b5a3
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB_ONLINE_LAN / ANDROID_PDA / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
context_index_ref: CONTEXT_INDEX.md

## Current project progress

- Evidence-weighted total: **55.4% exact / 55% displayed**.
- Phase 6 LAN continuity/offline/reconcile: **60%**.
- Phase 7 Online/LAN Web UI: **25%**.
- No percentage increase is recorded in this block yet. Live D1 schema activation and new source/security acceptance are material prerequisites, but signed LAN->Cloud finality has not passed live E2E.

## Reconciled state

FOCUSED reconciliation is complete through `955ac7f05008059240d5dca06433f565d8a9b5a3`.

`DECISIONS_V8.md` is ACTIVE as a record, but `CONTEXT_INDEX.md` explicitly classifies it as a redundant record of rules already consolidated into mandatory `DECISIONS_V7.md`; there is no unresolved authority conflict from V8.

## BETA D1 reconciliation schema — LIVE PASS

The legacy `0009_edge_reconciliation.sql` was not replayed because it also contains obsolete unprefixed permission seeds. A compatibility migration was introduced instead:

- `service/worker/migrations/0013_edge_reconciliation_backfill_v2.sql`;
- source commit `1e83f591dcdcccc2bf0c5fb46328ae1aef62fcbe`;
- migration blob `0056e9a5e8a1818c13241810fbd35e967f2d13f7`;
- no permission-catalog seed mutation.

Guarded BETA provider migration run `34840071968`, job `103962722822`: **SUCCESS**.
Evidence from the run:

- exact account/D1/schema preflight PASS;
- prestate `ABSENT`;
- migration apply PASS (`results=12`);
- poststate `ABSENT -> FINAL`;
- 59 tables, six required reconciliation indexes;
- foreign-key/integrity checks PASS;
- pre-existing table row counts unchanged.

The migration dispatch was immediately returned to `enabled=false` after PASS.

Post-migration read-only verify run `34840179761`, job `103963070455` proves:

- D1 reconciliation tables PASS;
- V2 `edge_event_ingest` columns PASS;
- Worker still lacks only the two reconciliation machine-auth bindings.

## Reconciliation source/security — SOURCE + CI PASS

`EMPLOYEE_CREATE` canonical reconciliation finality remains honest and fail-closed:

- current state + immutable canonical event + projection outbox + edge linkage are treated as canonical finality;
- retry/idempotency returns the compatible canonical event;
- mismatch/conflict remains explicit;
- unsupported commands remain `RECEIVED` rather than fabricated finality.

Additional integrity work now PASS:

- LAN canonicalizes `payloadJson`, stores that exact canonical string and stores SHA-256 over its UTF-8 bytes;
- Cloud ingress now independently recomputes SHA-256 and rejects a signed envelope when `payloadHash` does not match `payloadJson`;
- a valid HMAC cannot bypass this logical payload-integrity check;
- Worker module packaging is now tested transitively so a local imported JS module cannot be omitted from `deploy.beta.json` unnoticed;
- finality store tests lock receipt linkage, no-duplicate Google projection (`ACKED` when LAN receipt already completed), canonical linkage/checkpoint batching, and conflict evidence batching.

Evidence:

- aggregate `Build product foundations` run `34840763966`: **SUCCESS** for Cloud Service, LAN Service, Web and Android;
- `Validate clean baseline` run `34840763964`: **SUCCESS**;
- later baseline run `34841158647`: **SUCCESS**, including Worker unit tests and clean D1 schema.

## BETA Worker credential provisioning — PREPARED / PROVIDER NOT YET MUTATED

Current live Worker `vhdchy-beta` still lacks:

- `LAN_RECONCILIATION_KEY_ID`;
- `LAN_RECONCILIATION_SHARED_SECRET`.

No secret value is stored in the public repository or chat.

Prepared deployment model:

- public key-id fixed as `vhdchy-beta-lan-reconciliation-01`;
- shared secret must come from GitHub Environment `beta` secret `LAN_RECONCILIATION_SHARED_SECRET` with minimum length 32;
- uploader sends it to Cloudflare as a `secret_text` binding without logging the value;
- finality feature flag remains `LAN_RECONCILIATION_FINALITY_V1=disabled` during credential provisioning;
- new read-only predeploy verifies exact BETA Worker/D1 identity, reconciliation D1 schema, workers.dev disabled and custom domain before upload;
- normal full Cloudflare verifier is required post-upload and must prove both machine bindings + D1 reconciliation schema.

Cloudflare current documentation confirms multipart Worker metadata supports `secret_text` bindings and strict inherit semantics; new provisioning uses explicit secret binding because the binding does not yet exist.

Deployment dispatch `.github/dispatch/cloudflare-beta-deploy-v2.json` is still `enabled=false` at this checkpoint.

## Exact next ready actions

1. Toggle the reviewed BETA Worker deploy dispatch once.
2. The workflow first checks whether GitHub Environment `beta` contains `LAN_RECONCILIATION_SHARED_SECRET`; if absent/too short it fails before any Cloudflare mutation.
3. If present, require predeploy identity/schema PASS, upload/package PASS, full binding/D1 postflight PASS and public BETA health convergence.
4. Return deploy dispatch to `enabled=false` immediately after outcome is known.
5. Only after Worker machine-auth bindings are proven may finality activation/live signed LAN->Cloud E2E proceed.
6. If the GitHub Environment secret is absent, classify this single lane as `OWNER_PERMISSION_REQUIRED`; ask only for the minimum GitHub secret-store action and never ask the Owner to paste secret material into chat.
7. Continue independent reconciliation acceptance/source work while that gate is blocked.

## Retained boundaries

- Physical company-network/PDA acceptance and >=60-minute Internet-cut acceptance remain pending.
- ROOT email delivery/recovery live E2E remains incomplete; do not fake delivery.
- Portrait replacement semantics remain an Owner decision gate.
- STABLE promotion remains blocked until mandatory BETA acceptance plus explicit Owner approval.

do_not_repeat:
Do not treat memory as authority. Do not replay uncertain provider mutations. Do not replay legacy migration `0009` on current BETA. Do not infer secret-store values. Do not put secret material in source/chat/logs. Do not enable finality until D1 + bindings + live signed E2E gates pass. Do not treat `RECEIVED` as final Cloud reconciliation. Do not inflate progress without acceptance evidence. Do not promote STABLE without explicit Owner approval.
