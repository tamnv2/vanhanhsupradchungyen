# CHECKPOINT — VHDCHY

checkpoint_version: 20
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V6_BETA
reconciled_through_commit: 02cf8cb198a1c562a6f2b864b81e0bbc8812d12f
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
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Resume authority state

- Fresh-chat resume must fetch `AI_ENTRYPOINT.md` from GitHub `main` in the current chat; memory/project/chat summaries are NON_AUTHORITY.
- Resume must compare current `main` HEAD with `reconciled_through_commit` before acting.
- All active decision layers V1/V3/V4/V5/V6 are mandatory resume reads regardless of what an older checkpoint listed.
- V6 resolves the former ROOT factor/lifetime decision gate from V5.
- `docs/SERVICE_API_CONTRACT_V3.md`, `docs/DELIVERY_PLAN_V4.md` and `CURRENT_STATE.md` have been reconciled through V6.
- `NEXT_ACTIONS.md` still needs a governance-only reconciliation update; the attempted update was blocked by platform action-safety and must not be bypassed through a lower-level route.

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

## Governance audit finding

The previous checkpoint was materially stale: it reconciled only through `517e767d150488af07f10dacdb8afa23930411e1`, while `main` had advanced by 34 commits to V6 authority. It also named only V1/V3/V4 decision layers while `CONTEXT_INDEX.md` had already promoted V5/V6. That made a FAST resume vulnerable to reading incomplete authority even if GitHub was consulted.

Repository-side corrections completed in this audit:
- `AI_ENTRYPOINT.md` now requires a live GitHub fetch in the current chat and HEAD-vs-checkpoint comparison;
- `CONTEXT_INDEX.md` now requires all active decision layers independently of checkpoint contents;
- `docs/SERVICE_API_CONTRACT_V3.md` reconciled through V6;
- `docs/DELIVERY_PLAN_V4.md` reconciled through V6;
- `CURRENT_STATE.md` reconciled through V6.

The remaining cross-chat guarantee requires one external bootstrap instruction in the ChatGPT Project configuration, because a repository file cannot cause itself to be fetched before a new chat knows to read it.

## Immediate next execution

1. Verify CI after the governance reconciliation commits.
2. Strengthen baseline validation so missing active decision layers/current guide references cannot pass silently.
3. Continue the existing multi-module Worker packaging lane through allowed high-level paths only.
4. Reconcile/implement V6 authentication runtime and tests without claiming runtime PASS from documentation alone.
5. Continue shared domain, Cloud/LAN adapters, Web/APK foundations and isolated STABLE preparation in dependency-aware parallel lanes.
6. Physical LAN/domain regression remains deferred only until the intended company environment is available.

## do_not_repeat:

Do not use memory/project/chat summaries as project authority. Do not resume a fresh chat without fetching `AI_ENTRYPOINT.md` and comparing GitHub HEAD with the checkpoint. Do not omit V5/V6 because an older checkpoint failed to list them. Do not reopen V6-resolved ROOT factor/lifetime decisions from stale V5 text. Do not treat V2 wording as higher authority than newer overrides. Do not treat the legacy repo or transport-only prototype as product authority. Do not build LAN as only relay/discovery. Do not make Google outputs the business source. Do not wait for client route change before LAN syncs Cloud. Do not silently overwrite split-brain conflicts. Do not claim offline LAN domain PASS before physical evidence. Do not clone/rename BETA into STABLE. Do not copy BETA business/runtime data into STABLE. Do not promote/activate STABLE business traffic before explicit Owner approval. Do not promote accidental latest `main` instead of the exact accepted BETA release. Do not bypass platform action-safety guards.
