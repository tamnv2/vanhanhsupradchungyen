# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Execution plan: `docs/DELIVERY_PLAN_V3.md`

## Active product direction

VHDCHY is one platform with four first-class deliverables:
- Website;
- Android APK for PDA;
- Cloud Service;
- full LAN Service substitute.

APK and Website use the same domain/API/business semantics. LAN and Cloud use the same business command/event rules through different persistence/runtime adapters.

Legacy repository and the earlier transport-only APK/Agent prototype remain NON_AUTHORITY reference only.

## V3 LAN behavior

LAN is now defined as a full local Service runtime.

### CLOUD_DIRECT
`Web/APK -> Cloud Service -> D1 -> Google`

### LAN_PRIMARY_ONLINE
`Web/APK -> LAN Service -> edge state/event journal`

If Cloud is reachable, LAN synchronizes accepted events to Cloud/D1 continuously/opportunistically even while users remain routed through LAN.

If Google is reachable, LAN may write controlled Sheets projection and upload Drive media directly.

### LAN_PRIMARY_CLOUD_UNAVAILABLE
Internet remains available but Cloud Service is unavailable/degraded. LAN continues normal local business processing. Google projection/upload may continue if Google is reachable. Cloud synchronization waits for Cloud recovery.

### LAN_OFFLINE
Public Internet is unavailable but local Wi-Fi/LAN remains usable. LAN continues normal local business processing using the latest synchronized local authority/configuration snapshot. Google work is queued/staged locally.

### LOCAL_QUEUE_ONLY
Only explicitly retry-safe commands may remain on a client when neither Service is reachable.

## Synchronization and authority

- D1 is the central consolidated store after synchronization.
- LAN edge state + immutable event journal is the local operational authority for operations accepted by LAN.
- Cloud synchronization consumes LAN event/outbox records, not Sheets/Drive as a business source.
- LAN Google writes/uploads retain stable event/logical-file IDs and receipts so Cloud reconciliation can avoid duplicate rows/files.
- A client does not need to switch back to Cloud before LAN starts syncing backlog.
- Remote authority/configuration changes become active for subsequent operations after reconnect/refresh.
- Previously accepted offline events remain immutable evidence and reconcile explicitly rather than being silently removed.

## Offline operation tradeoff

Offline login has no duration-only expiry under the Owner requirement and uses the latest synchronized local authority snapshot.

A fully disconnected LAN therefore cannot immediately know about remote account/permission changes made after the last sync. This is recorded as an explicit availability/security consistency tradeoff and must be visible in audit evidence.

Independent Cloud/LAN writes during a partition can also conflict. V3 requires stable identities/versions, deterministic reconciliation and explicit conflict evidence; silent last-write-wins is prohibited.

## Manual LAN control

Only SUPERADMIN/ROOT may deliberately force LAN routing while Cloud is healthy. The action must be audited. Forced LAN routing does not stop background Cloud synchronization when Cloud is reachable.

Technical/provider retry errors remain automatic. Unresolved business/data synchronization conflicts go to ADMIN or higher. ROOT-security conflicts preserve the existing ROOT-exclusive boundary.

## Google / Drive / Sheets / GAS

- Current Google authority remains `tam95.supra@gmail.com`.
- Project root remains `VẬN HÀNH DC HƯNG YÊN` with isolated BETA/STABLE roots.
- BETA cluster remains `PICK_PACK_1291`.
- BETA workbook remains `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`.
- Current projection remains `PROVISIONED_NOT_LIVE`.
- GAS managed deployment v3 remains deployed fail-closed.
- V3 requires the controlled Google integration to support authorized Cloud and LAN writers with stable projection/file identities and deduplication.
- Sheets remains projection/reconciliation/DR, never business authority.
- Drive remains file/media storage.

## Cloudflare BETA

- D1 `vhdchy-data-beta` remains `business_core_v3` PASS.
- Worker `vhdchy-beta` foundation/health remains live at `https://beta.supra.cc.cd`.
- Protected business/admin APIs remain fail-closed.
- Reviewed multi-module manifest exists, but the active provider-mutating deploy workflow still needs safe multi-module packaging support before Worker runtime integration/deploy.
- Do not bypass platform write-safety guards.

## Cloud source foundation

Source-level foundations already exist for:
- authentication/session/permission primitives;
- projection outbox retry/dead-letter primitives;
- Service API/canonical mutation design.

Still pending runtime/product implementation:
- shared provider-neutral domain core;
- D1 business adapter;
- LAN reconciliation ingestion;
- business APIs;
- secure Google sender path;
- Drive business flow;
- Website/APK business surfaces.

## LAN full-Service work

Required product components:
- edge current-state DB;
- immutable local event journal;
- Cloud sync/reconciliation outbox;
- synchronized local authority/configuration snapshot;
- direct Google projection/upload path + receipts;
- offline Google queue/staged media;
- Cloud sync cursor/delta refresh;
- explicit conflict queue;
- locally served Web bundle;
- no-admin diagnostics/update/recovery.

## Website/APK

- Website is the wider management/administration surface.
- APK is the compact PDA operational surface.
- Both consume the same business API/domain contract.
- Both require Cloud/LAN endpoint selection and meaningful sync/Google/conflict states.
- APK scanner/QR functions must invoke domain commands rather than bypass Service logic.

## Current exact project position

```text
Cloud/D1/Google foundation
        |
        +--> NOW: V3 shared domain + LAN local authority/sync/Google receipt contracts
                 |
          +------+------+
          |             |
     Cloud runtime   LAN full Service runtime
          |             |
          +------+------+
                 |
        vertical business slices
          /      |       \
        Web     APK   Google outputs
                 |
       failover/sync/conflict resolution
                 |
            BETA acceptance
```

Immediate work: extend shared Service/API + LAN edge contracts for V3, materialize shared domain/adapters, then build Cloud/LAN/Web/APK vertical slices in parallel.

## Physical dependencies / STABLE

Final company-network/no-admin/PDA evidence still requires the intended physical environment, but source/product work continues now.

STABLE remains blocked until full BETA PASS + explicit Owner approval.
