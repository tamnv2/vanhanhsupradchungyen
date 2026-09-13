# CHECKPOINT — VHDCHY

checkpoint_version: 22
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: e7f8b3d5cceb0dc1fd7f8cb2f94bb0e158cab30a
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
product_architecture_ref: docs/TARGET_PRODUCT_ARCHITECTURE_V3.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
delivery_plan_ref: docs/DELIVERY_PLAN_V4.md
release_promotion_ref: docs/RELEASE_PROMOTION_V1.md
lan_host_domain_ref: docs/LAN_HOST_DOMAIN_V1.md
context_index_ref: CONTEXT_INDEX.md
external_project_bootstrap_ref: CHATGPT_PROJECT_BOOTSTRAP.md
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Resume authority state

- Fresh-chat resume must fetch `AI_ENTRYPOINT.md` from GitHub `main` in the current chat; memory/project/chat summaries are NON_AUTHORITY.
- Resume must compare current `main` HEAD with `reconciled_through_commit` before acting.
- All active decision layers V1/V3/V4/V5/V6 are mandatory resume reads regardless of what an older checkpoint listed.
- V6 resolves the former ROOT factor/lifetime decision gate from V5.
- `docs/SERVICE_API_CONTRACT_V3.md`, `docs/DELIVERY_PLAN_V4.md` and `CURRENT_STATE.md` are reconciled through V6.
- `CHATGPT_PROJECT_BOOTSTRAP.md` now records the exact external ChatGPT Project instruction required to make fresh-chat live GitHub bootstrap deterministic.
- `NEXT_ACTIONS.md` is still stale in several references; two high-level replacement attempts were blocked by platform action-safety. Do not bypass through lower-level Git/GitHub routes. Current authority is defined by the active decision layers, current guides, `CURRENT_STATE.md`, and this checkpoint until a permitted high-level update succeeds.

## Owner scope locked through V6

- Website, APK, Cloud Service and full LAN Service are one product and are developed in parallel.
- LAN executes the same approved business model locally and synchronizes Cloud whenever Cloud is reachable.
- Authorized LAN may write controlled Google outputs directly when Google is reachable; Cloud reconciliation consumes LAN event/outbox records, not Google as business source.
- Offline login uses the latest synchronized LAN authority snapshot without duration-only expiry; refreshed information applies prospectively after reconnect.
- Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy.
- Unresolved business/data conflicts are escalated to ADMIN+ only after automatic retry/deduplication/reconciliation.
- LAN host must remain portable/no-admin and compatible with restricted company-laptop operation.
- Canonical LAN URLs are `lan-beta.supra.cc.cd` and `lan.supra.cc.cd`.
- Offline use of the canonical LAN domain is a required technical/physical acceptance gate; LAN IP/discovery is fallback, not the intended normal UX.
- STABLE infrastructure is prepared during development but remains dormant/fail-closed for business traffic until explicit Owner promotion approval.
- STABLE promotion uses the exact accepted BETA release and never clones/copies BETA operational data into STABLE.
- Current authentication authority is `DECISIONS_V6.md`; implementation/runtime PASS remains pending.

## Provider/source state retained

- BETA D1 `business_core_v3`: previously verified PASS.
- BETA Worker foundation/health: previously verified PASS; business routes remain fail-closed.
- Google Gateway: deployed fail-closed; projection still not LIVE.
- Worker multi-module deployment path remains an implementation blocker for Cloud runtime integration.
- Legacy LAN repo and current transport-only prototype remain NON_AUTHORITY references.

## Governance correction completed

The earlier resume defect was caused by a materially stale checkpoint and an incomplete FAST-read dependency on checkpoint-listed authority. `main` had advanced through V5/V6 while the checkpoint still named only V1/V3/V4.

Repository-side corrections now in place:
- `AI_ENTRYPOINT.md` requires a live GitHub read in the current chat, HEAD-vs-checkpoint comparison, and reconciliation before mutation;
- `CONTEXT_INDEX.md` requires all active decision layers independently of checkpoint contents;
- `CHATGPT_PROJECT_BOOTSTRAP.md` documents the exact external ChatGPT Project instruction needed before a fresh chat can know to start the live GitHub bootstrap;
- `docs/SERVICE_API_CONTRACT_V3.md`, `docs/DELIVERY_PLAN_V4.md`, and `CURRENT_STATE.md` are reconciled through V6;
- `.github/workflows/validate.yml` checks active decision layers, current guide references and checkpoint freshness.

Validation evidence before the external-bootstrap documentation change:
- workflow `Validate clean baseline` run `34763849700`: SUCCESS;
- authority file checks: PASS;
- resume invariant checks: PASS;
- checkpoint freshness check: PASS;
- Worker syntax/unit/schema baseline checks: PASS.

A new validation run after this checkpoint must be verified before declaring the governance correction fully PASS at the new HEAD.

## Immediate next execution

1. Verify CI at the new governance/checkpoint HEAD.
2. Install the `CHATGPT_PROJECT_BOOTSTRAP.md` instruction in ChatGPT Project settings; this is the only layer that cannot be changed from repository contents alone.
3. Continue the existing multi-module Worker packaging lane through allowed high-level paths only.
4. Reconcile/implement V6 authentication runtime and tests without claiming runtime PASS from documentation alone.
5. Continue shared domain, Cloud/LAN adapters, Web/APK foundations and isolated STABLE preparation in dependency-aware parallel lanes.
6. Reconcile `NEXT_ACTIONS.md` when the high-level write path permits; do not bypass the current action-safety block.
7. Physical LAN/domain regression remains deferred only until the intended company environment is available.

## do_not_repeat:

Do not use memory/project/chat summaries as project authority. Do not resume a fresh chat without fetching `AI_ENTRYPOINT.md` and comparing GitHub HEAD with the checkpoint. Do not omit V5/V6 because an older checkpoint failed to list them. Do not reopen V6-resolved ROOT factor/lifetime decisions from stale V5 text. Do not treat V2 wording as higher authority than newer overrides. Do not treat the legacy repo or transport-only prototype as product authority. Do not build LAN as only relay/discovery. Do not make Google outputs the business source. Do not wait for client route change before LAN syncs Cloud. Do not silently overwrite split-brain conflicts. Do not claim offline LAN domain PASS before physical evidence. Do not clone/rename BETA into STABLE. Do not copy BETA business/runtime data into STABLE. Do not promote/activate STABLE business traffic before explicit Owner approval. Do not promote accidental latest `main` instead of the exact accepted BETA release. Do not bypass platform action-safety guards.
