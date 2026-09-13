# CHECKPOINT — VHDCHY

checkpoint_version: 17
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V3_BETA
action_mode: AUTONOMOUS_PARALLEL
active_lanes: SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / GOOGLE_SYNC / WEB / ANDROID_APK / RECONCILIATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION
approved_scope: Build one VHDCHY product with Website + PDA-optimized APK + Cloud Service + full LAN Service in parallel. LAN Service executes the same approved business model locally, may project/upload to Google directly when Internet/Google is reachable, queues/stages Google work when offline, and synchronizes its immutable local events to Cloud/D1 whenever Cloud becomes reachable. Offline login remains available from the latest synchronized local authority snapshot without a time-based expiry solely because the outage is long. Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy. Unresolved business/data synchronization conflicts are decided by ADMIN or higher after automatic retry/deduplication/reconciliation has been exhausted.
reconciled_through_commit: a5bf918781c45ac6640ed768fdf7ac6b09b4c798
product_architecture_ref: docs/TARGET_PRODUCT_ARCHITECTURE_V3.md
delivery_plan_ref: docs/DELIVERY_PLAN_V3.md
authority_ref: DECISIONS_V3.md
base_authority_ref: DECISIONS.md
service_contract_ref: docs/SERVICE_API_CONTRACT.md
lan_edge_state_ref: docs/LAN_EDGE_STATE_V1.md
legacy_lan_reference_repo: tamnv2supra/vanhanhdchungyen
legacy_lan_reference_commit: 7b4488a89f585812c1bccba5d07d86049482bf4c

## V3 clarification

- LAN is a full local Service substitute, not transport-only.
- Client route and background synchronization are separate concerns: users may remain on LAN while LAN synchronizes to Cloud in the background whenever possible.
- If Internet/Google is available, LAN may write controlled Sheets projection and Drive media directly.
- Cloud reconciliation consumes LAN event/outbox records, not Sheets/Drive as a business source.
- Existing Google output receipts are carried into reconciliation to prevent duplicate rows/files.
- If Internet is unavailable, LAN continues local business operation and queues Google work.
- Offline operation uses the most recently synchronized authority/configuration state; reconnect refresh applies new state to later operations while preserving already-accepted local event evidence.
- SUPERADMIN/ROOT-only manual LAN activation is audited.
- Technical/provider retries are automatic; unresolved business conflicts go to ADMIN+.

## Technical constraints accepted

- A fully disconnected LAN cannot immediately know remote permission/account changes made after its last sync.
- Independent Cloud/LAN writes during a partition can conflict.
- Therefore V3 requires immutable IDs/versions, explicit reconciliation and conflict evidence; silent overwrite/drop is prohibited.

## Provider foundation retained

- D1 BETA `business_core_v3`: PASS.
- Worker BETA health/foundation: PASS; business routes remain fail-closed.
- Google Gateway v3: deployed fail-closed; projection currently NOT_LIVE.
- Worker multi-module deployment remains blocked until the reviewed deploy workflow can safely package the module set.

## Current implementation position

1. Architecture/product scope is now V3.
2. `DECISIONS_V3.md` is the active override authority over older conflicting product decisions.
3. Shared domain/event contract remains the common dependency.
4. Cloud adapter and LAN edge adapter must implement the same business rules.
5. LAN edge design must now add synchronized authority state, Google projection/upload receipts and continuous Cloud-sync behavior.
6. Website/APK client foundations must consume the same runtime/status contract.
7. Vertical business slices then proceed across Cloud + LAN + Web + APK + Google outputs.

## Immediate execution order

1. Extend shared Service/API contract for LAN local acceptance, Google receipts and sync status.
2. Extend LAN edge schema/design for local authority snapshot, Google receipts and Cloud sync cursors.
3. Define provider-neutral domain-core interfaces and shared acceptance vectors.
4. Continue independent Cloud packaging/auth/projection work where allowed.
5. Build real LAN Service skeleton against the shared contract, not the earlier transport prototype.
6. Build Website/APK foundations against the same contract.
7. Start Slice 1 only after Cloud/LAN parity boundaries are explicit.

## do_not_repeat:

Do not treat the old repo or transport-only prototype as product authority. Do not build LAN as only relay/discovery. Do not make Sheets/Drive the business source. Do not wait for a client route switch before LAN syncs Cloud. Do not silently recreate Cloud truth from Google outputs. Do not make Cloud/LAN business rules diverge. Do not silently overwrite split-brain conflicts. Do not send ordinary transient errors to ADMIN for manual handling. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
