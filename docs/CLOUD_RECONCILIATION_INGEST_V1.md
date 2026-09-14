# CLOUD RECONCILIATION INGEST V1 — VHDCHY

Status: ACTIVE IMPLEMENTATION BOUNDARY
Updated: 2026-09-14
Authority: `DECISIONS.md` + `DECISIONS_V3.md` through `DECISIONS_V7.md` + `docs/SERVICE_API_CONTRACT_V3.md`

## Purpose

Define the current Cloud-side boundary for ingesting immutable business evidence that was already durably accepted by the LAN Service during LAN operation.

This document does not authorize public LAN business mutations by itself and does not make Google a reconciliation source.

## Required event evidence

Each LAN reconciliation envelope must preserve:

- immutable edge event ID;
- request ID and idempotency key;
- environment and cluster;
- device ID + monotonic device sequence where applicable;
- edge instance ID + edge epoch;
- command code + event code;
- entity type + entity ID;
- base version where applicable + resulting local version;
- full normalized payload JSON and its stable payload hash;
- local acceptance timestamp;
- authority-snapshot version used for local authorization;
- domain-contract version;
- edge-schema version;
- immutable authenticated actor user ID captured at LAN acceptance;
- completed Google Sheets/Drive receipt evidence where LAN already completed approved downstream work.

Payload hash alone is not sufficient for durable Cloud ingestion. The full payload must be retained so a received item can be safely inspected/retried/reconciled after process restart.

Actor identity supplied by a client request body is never authoritative. The actor field in this envelope must come from immutable LAN acceptance evidence.

## Current Cloud persistence

`edge_event_ingest` is the durable Cloud inbox for LAN-originated events before final canonical reconciliation.

The V2 ingestion schema preserves payload JSON, actor evidence and resulting local entity version in addition to the original identity/hash fields.

`integration_receipts` preserves already-completed Google output evidence by stable `(environment, target_kind, logical_key)` identity.

## Collision and replay rules

- Same edge event identity with the same immutable business identity is an idempotent replay.
- Same idempotency identity with different command/payload/evidence is `IDEMPOTENCY_PAYLOAD_CONFLICT`.
- Reusing one device sequence for another edge event is `DEVICE_SEQUENCE_COLLISION`.
- Reusing an edge event ID for different evidence is explicit `SYNC_CONFLICT`.
- An existing Google receipt with the same stable logical identity and same provider evidence is reused/attached without duplicate output.
- An existing receipt with conflicting provider evidence is explicit `SYNC_CONFLICT`.
- Environment/domain-contract/edge-schema mismatch is fail-closed.

No silent last-write-wins is allowed.

## Current activation boundary

Current implementation work may complete and test:

1. Cloud ingestion validation and durable D1 inbox storage;
2. event/idempotency/device/source collision behavior;
3. actor/payload/schema evidence preservation;
4. completed Google receipt deduplication/attachment;
5. stable retry/conflict result mapping;
6. LAN construction of the exact transport envelope.

The public network transport remains fail-closed until its machine-to-machine authentication/pairing/security boundary is reviewed and implemented. Creating the ingestion core does not authorize an unauthenticated HTTP reconciliation endpoint.

Final canonical reconciliation still must pass the LAN event through the normal current Cloud business mutation semantics before returning `LAN_RECONCILED_CLOUD_COMMITTED`.
