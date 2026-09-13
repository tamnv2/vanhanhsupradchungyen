# ARCHITECTURE

Status: ACTIVE / V2 TARGET 2026-09-13
Primary detail: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`
Execution: `docs/DELIVERY_PLAN_V2.md`

## Product topology

```text
                       +-------------------+
                       |      Website      |
                       +-------------------+
                                 \
                                  \
                                   > one domain/API contract
                                  /
                       +-------------------+
                       |    Android APK    |
                       +-------------------+
                                  |
                    +-------------+-------------+
                    |                           |
             Cloud Service path           LAN Service path
                    |                           |
            Cloudflare Worker            local no-admin host
                    |                   /                  \
                   D1            Cloud reachable       Cloud unavailable
                    |                 LAN_RELAY          LAN_AUTONOMOUS
            immutable events               |                  |
              + outbox                     +-------> Cloud    + edge DB
                    |                                          + event journal
          Google Gateway                                      + sync outbox
            /          \                                       + staged media
       Sheets          Drive                                         |
                                                             reconcile to D1
                                                               after recovery
```

## Client rule

Web and APK are clients of the same business platform.

- Web: wider/full browser management and operational surface.
- APK: PDA-optimized compact operational surface.
- Both use the same authenticated command/query/error/permission semantics.
- UI/device differences do not create separate business rules or authorities.

## Cloud Service

Normal online runtime:
- Cloudflare Worker is the public Service entry point;
- D1 is the global canonical structured authority after normal commit/reconciliation;
- business mutations are immutable-event/idempotency oriented;
- Google integration is downstream/asynchronous.

## LAN Service

LAN is not merely a transport test.

Required modes:
- `LAN_RELAY`: client uses LAN endpoint while LAN can reach Cloud; Cloud/D1 remains the immediate canonical commit path;
- `LAN_AUTONOMOUS`: Cloud/upstream unavailable; LAN executes approved offline-capable commands locally, persists edge events and queues later D1 reconciliation.

Required use cases:
- site Internet unavailable but local Wi-Fi/LAN still works;
- Cloud Service unavailable/degraded;
- an individual client is forced to LAN because its direct Cloud path is problematic.

LAN remains no-admin/minimum-information and must not assume control of company router/firewall/internal DNS.

## Shared business core

Cloud and LAN must not maintain independently invented business logic.

Target separation:

```text
Domain command/query contract
        |
provider-neutral business validation / transition / event intent
        |
   +----+----+
   |         |
D1 adapter  LAN edge adapter
```

Both adapters must pass identical business acceptance vectors and machine error semantics.

## Offline authority and reconciliation

A hard partition cannot provide both guaranteed global single-writer consistency and uninterrupted local writes. Because the product requirement requires continued local operation, LAN autonomous mode uses explicit event reconciliation rather than pretending this limitation does not exist.

Rules:
- edge acceptance is durable and auditable;
- stable idempotency/device/event identities survive reconnect;
- non-conflicting events reconcile once;
- conflicting events are retained as `SYNC_CONFLICT` evidence and never silently overwritten/dropped;
- D1 becomes global canonical after successful reconciliation.

## Google rules

- Sheets is asynchronous projection/reconciliation/DR only.
- Drive is media/document/archive storage.
- LAN does not turn Sheets into a fallback database.
- During Internet/Cloud loss, projection and Drive upload are deferred/staged.
- Structured LAN events reconcile to D1 before normal Sheets projection.

## Local Web continuity

A public-cloud webpage is not sufficient during complete Internet loss. The LAN package must support a reviewed way to load the current Web client locally, preferably by serving the compatible Web bundle from the LAN Service. This allows browser clients on the same site network to continue against LAN Service.

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

## Current unresolved security policy

Do not invent these values/permissions:
- exact offline-auth credential/capability mechanism;
- exact offline-auth expiry/TTL;
- which privileged security/admin actions are allowed offline;
- who may explicitly enter emergency autonomous mode.

Until reviewed, unresolved privileged offline behavior remains fail-closed.
