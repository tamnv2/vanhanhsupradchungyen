# ARCHITECTURE

Status: ACTIVE / V3 TARGET 2026-09-13
Primary detail: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
Execution: `docs/DELIVERY_PLAN_V3.md`

## Product topology

```text
Website / APK
      |
      +-------------------------------+
      |                               |
Cloud Service                    LAN Service
Worker + D1                 edge DB/event journal
      |                         |        |
      |                         |        +-> Cloud sync/reconcile when reachable
      |                         +----------> Google projection/Drive when reachable
      |
Google Gateway -> Sheets / Drive
```

Website and APK use one business/domain contract. Cloud Service and LAN Service use the same business command/event semantics with different persistence/runtime adapters.

## Cloud path

Normal path:

`Web/APK -> Cloud Service -> D1 -> Google`

D1 is the central consolidated structured store after synchronization.

## LAN path

LAN is a full local Service substitute, not transport-only.

It supports:
- user-forced LAN routing under the approved elevated-control rule;
- Cloud Service unavailable/degraded while Internet remains available;
- complete Internet loss while local Wi-Fi/LAN remains usable.

During LAN operation:
- accepted business operations commit to local edge state + immutable local events;
- if Cloud is reachable, LAN synchronizes to D1 in the background without requiring a route switch;
- if Google is reachable, LAN may project to Sheets/upload Drive directly using stable IDs/receipts;
- if Google is unavailable, Google work is queued/staged locally;
- later Cloud reconciliation consumes LAN event/outbox records, not Sheets/Drive as a business source.

## Offline authority

LAN maintains the latest synchronized local authority/configuration snapshot required for offline business operation. Offline duration alone does not expire login under the current Owner requirement.

Remote changes that occur during a true partition cannot be known until reconnect. After refresh, new authority/configuration applies to later operations. Already accepted offline events remain immutable evidence and reconcile explicitly.

Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy. Forced routing is audited and does not disable background Cloud synchronization when Cloud is reachable.

## Conflict model

Hard partitions can create independent Cloud/LAN changes. Therefore:
- stable event/idempotency/device/version identity is mandatory;
- non-conflicting reconciliation is automatic;
- technical/provider retries are automatic;
- silent last-write-wins/drop is prohibited;
- unresolved business conflicts are escalated to ADMIN+ with evidence;
- ROOT-security conflicts retain the existing ROOT-exclusive boundary.

## Google rules

- Sheets remains projection/reconciliation/DR, not business authority.
- Drive remains media/document/archive storage.
- Authorized Cloud and LAN Service writers must use controlled Google integration and stable projection/file identities.
- Google output may precede D1 reconciliation for LAN-accepted events when Internet/Google is available.
- Cloud sync still consumes LAN event/outbox records; Google receipts only prevent duplicate output.

## Local Web continuity

The LAN Service must support a reviewed local-loading path for the compatible Website build so browser clients can continue operating during complete Internet loss.

## Environment mapping

| Component | BETA | STABLE |
|---|---|---|
| Public host | `beta.supra.cc.cd` | `supra.cc.cd` |
| Worker intent | `vhdchy-beta` | `vhdchy-stable` |
| D1 intent | `vhdchy-data-beta` | `vhdchy-data-stable` |
| Drive root | `01_BETA` | `02_STABLE` |
| GAS | isolated project | isolated project |
| Signing | isolated signer | isolated signer |
| LAN runtime state | isolated BETA local state | isolated STABLE local state |

## Shared business core

Cloud and LAN must not maintain independently invented business logic.

```text
Domain command/query contract
        |
provider-neutral validation / transition / event intent
        |
   +----+----+
   |         |
D1 adapter  LAN edge adapter
```

Both adapters must pass the same business acceptance vectors and machine error semantics.
