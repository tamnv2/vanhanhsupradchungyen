# CHECKPOINT — VHDCHY

checkpoint_version: 19
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V4_BETA
reconciled_through_commit: 517e767d150488af07f10dacdb8afa23930411e1
action_mode: AUTONOMOUS_PARALLEL
active_lanes: SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION / STABLE_PREPARATION / KNOWLEDGE_RECONCILIATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
product_architecture_ref: docs/TARGET_PRODUCT_ARCHITECTURE_V3.md
delivery_plan_ref: docs/DELIVERY_PLAN_V3.md
knowledge_audit_ref: docs/PROJECT_KNOWLEDGE_AUDIT_20260913.md
release_promotion_ref: docs/RELEASE_PROMOTION_V1.md
lan_host_domain_ref: docs/LAN_HOST_DOMAIN_V1.md
context_index_ref: CONTEXT_INDEX.md
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## Owner scope locked through V4

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

## Audit result

Full GitHub audit found that most Owner-locked business rules are persisted in `DECISIONS.md` and V3/V4 override files, and D1 `business_core_v3` already models the main business domains.

Documentation is not fully internally reconciled yet. The active audit records these remaining drifts:
- `docs/SERVICE_API_CONTRACT.md` remains V2 in several LAN/Google/offline-auth sections;
- `docs/CANONICAL_MUTATION_PLAN.md` remains V2 for LAN direct-Google behavior;
- `docs/LAN_EDGE_STATE_V1.md` remains V2 and lacks V3 authority/Google-receipt/cloud-sync requirements;
- `SERVICE_AUTHORITY.md` still contains V2 LAN authority wording and STABLE-reserved-only wording;
- `docs/ARCHITECTURE.md` contains older V2 text beneath partial V3 reconciliation;
- `docs/BETA_ACCEPTANCE_MATRIX.md` still includes some V2 expectations;
- current CI/provider setup remains BETA-focused; isolated STABLE provider/runtime preparation is not yet implemented;
- offline canonical LAN-domain resolution is not yet implemented/proven.

`CONTEXT_INDEX.md` now requires future AI to read V3/V4 overrides first.

## Provider/source state retained

- BETA D1 `business_core_v3`: previously verified PASS.
- BETA Worker foundation/health: previously verified PASS; business routes remain fail-closed.
- Google Gateway: deployed fail-closed; projection still not LIVE.
- Worker multi-module deployment path remains an implementation blocker for Cloud runtime integration.
- Legacy LAN repo and current transport-only prototype remain NON_AUTHORITY references.

## Immediate next execution

1. Reconcile stale V2 contract/design files to V3/V4 semantics.
2. Define V3 LAN/Cloud data additions for local authority snapshot, LAN event ingestion, Cloud-sync cursors and Google output receipts without inventing unapproved business rules.
3. Add isolated STABLE preparation lane/configuration while preserving Owner activation gate.
4. Continue shared domain-core and dual Cloud/LAN adapters.
5. Build Web/APK foundations against the same contract.
6. Start business vertical Slice 1 only after shared contracts/data boundaries are internally consistent.
7. Physical LAN/domain regression remains deferred only until the intended company environment is available.

## do_not_repeat:

Do not treat V2 wording as higher authority than V3/V4 overrides. Do not treat the legacy repo or transport-only prototype as product authority. Do not build LAN as only relay/discovery. Do not make Google outputs the business source. Do not wait for client route change before LAN syncs Cloud. Do not silently overwrite split-brain conflicts. Do not claim offline LAN domain PASS before physical evidence. Do not clone/rename BETA into STABLE. Do not copy BETA business/runtime data into STABLE. Do not promote/activate STABLE business traffic before explicit Owner approval. Do not promote accidental latest `main` instead of the exact accepted BETA release.
