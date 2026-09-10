# VHDCHY MODULE MAP

Status: `ACTIVE_BASELINE`
Date: 2026-09-10

## Platform core — reusable across DC

| Domain | Core responsibility |
|---|---|
| Identity & Access | users, roles, permissions, sessions, device identity, audit; privileged credential method is a separate policy adapter |
| People | employee master and lifecycle |
| Cluster Membership | employee ↔ cluster assignment/effective periods |
| Shift Configuration | cluster-scoped shift definitions and time rules |
| Resource Registry | generic resource identity/type/catalog/state; no cluster-specific hard-coded global enum |
| Work Session | employee activity/session lifecycle and resource bindings |
| Event Ledger | immutable domain events, idempotency, entity version, device sequence |
| Conflict/Correction | evidence, resolver decision, correction/reversal as new event |
| Document/Media Metadata | Drive references/hashes/metadata; binary stays in object/file storage |
| Projection | outbox, target catalog, retry/ACK/checkpoint, rebuildability |
| Import/Export Audit | import provenance, counts, hashes and result references |
| Master Data/Catalogs | configurable shared/cluster catalogs such as site, warehouse, vendor, department, positions and module-specific types |

## Cluster module — PICK_PACK_1291

The first cluster reuses the core but owns its operational semantics.

### Resource types

- PDA
- USER_PICK
- BAN_PACK
- USER_PACK

These are registered/configured for the module; they do not define the resource universe for the whole DC.

### Operational domains/projections

- personnel projection for cluster use;
- resource availability/projection;
- RA/VÀO trong ca;
- employee operational user assignment;
- công nhật/support work tracking;
- vị trí;
- nhận hàng rớt / dropped-goods exception flow;
- operational event history;
- document links relevant to the cluster;
- conflict/correction and import-audit projections.

## Optional infrastructure modules

The following are not enabled simply because the retired Pick Pack 1291 project once used them:

- Durable Objects/WebSocket realtime;
- LAN Agent / Windows service;
- LAN authority/failover;
- emergency Google fallback ledgers;
- R2 cache/object storage.

Each requires a current VHDCHY use case/measurement gate.

## Boundary rule

A field/table/tab is not `GENERIC_DC` merely because more than one Pick Pack screen uses it. Core ownership requires a plausible reusable DC-wide invariant. Cluster-specific workflow/status/catalog semantics stay in the module or configurable catalogs.
