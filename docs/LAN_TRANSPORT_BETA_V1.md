# VHDCHY LAN TRANSPORT BETA V1

Status: ACTIVE DESIGN / SOURCE-LEVEL ACCEPTANCE ONLY
Authority: `DECISIONS.md`, `AI_OPERATING_CONTRACT.md`, `docs/SERVICE_API_CONTRACT.md`
Reference only: legacy repo `tamnv2supra/vanhanhdchungyen` at commit `7b4488a89f585812c1bccba5d07d86049482bf4c`

## Purpose

Define the LAN transport contract that can be implemented and tested without treating LAN as a second business backend.

D1 remains canonical. The Cloud Service contract remains the business authority. LAN exists to improve local availability/latency and to relay retry-safe commands when the approved LAN path is available.

## Non-goals

This contract does not:

- make legacy `VHDCHY_LAN_PILOT_V1` a production protocol;
- allow cleartext business credentials or employee PII over unauthenticated pilot endpoints;
- allow the Windows Agent to become canonical business authority;
- allow local SQLite state to replace D1 canonical state;
- claim physical LAN feasibility for the current implementation before company-network/MT90 regression.

## Roles

### PDA/client

- owns a stable registered `device_id`;
- owns a monotonically increasing local `device_seq` for retry/offline ordering;
- stores pending commands durably before transmission when required;
- may discover and use an authenticated LAN Agent transport;
- retains retry-safe command identity across LAN/cloud transport changes.

### LAN Agent

- runs as a normal per-user process on the approved company laptop; no Administrator/router/DNS/firewall bypass assumptions;
- advertises/discloses transport health and capabilities;
- authenticates/authorizes paired devices before accepting business traffic;
- relays current Service command envelopes without changing business meaning;
- may persist bounded relay/ACK metadata for continuity, but is not business authority;
- exposes safe operational diagnostics separate from business payloads.

### Cloud Service

- authenticates session/device context;
- enforces effective permissions and cluster/module scope;
- applies canonical mutation contract in D1;
- owns idempotency reconciliation and canonical event identity.

## Command identity

A retryable business command has one stable identity independent of transport:

- `requestId`: UUID for request tracing;
- `idempotencyKey`: opaque stable key reused for retries of the same intended mutation;
- `deviceId`: registered device identifier;
- `deviceSeq`: monotonically increasing sequence allocated durably by the originating device;
- `appVersion`: originating client build;
- `commandType`: stable Service/domain command code;
- `clusterId` / `moduleId`: scope when required;
- `entityId`: target entity when applicable;
- `expectedEntityVersion`: optimistic-concurrency precondition when applicable;
- `payload`: command-specific body;
- `payloadHash`: SHA-256 of canonicalized command payload for retry/collision diagnostics.

Actor identity is not trusted from this envelope. Authenticated session/device context remains authoritative.

## Required retry semantics

1. Allocate `idempotencyKey` and `deviceSeq` before first transmission and persist them with the pending command.
2. Retries across LAN/cloud transport reuse the same command identity.
3. ACK/removal from the local pending queue occurs only after a response proves the command is accepted/reconciled by the authoritative Service path.
4. A duplicate retry must reconcile the existing canonical event rather than create another business mutation.
5. A reused `idempotencyKey` with a conflicting payload hash is a hard conflict, not a second command.
6. A reused `(deviceId, deviceSeq)` for a different command identity is a device-sequence collision and must fail closed.

## Discovery and activation

Legacy source evidence supports the following order, retained as BETA design:

1. cached Agent endpoint, if its authenticated health check succeeds;
2. UDP discovery on the approved discovery port;
3. manual recovery endpoint only as a diagnostic/recovery path.

Do not scan arbitrary subnets.

Discovery response is only a candidate locator. Before `LAN_ACTIVE`, the client must verify:

- expected service family/environment;
- supported LAN transport protocol version;
- Agent instance identity;
- Agent health freshness;
- required capability set;
- authenticated pairing/channel state.

Service/environment/protocol strings alone are insufficient authentication.

## Anti-flapping state machine

Recommended states adapted from the legacy pilot:

- `RECONNECTING`
- `LAN_AVAILABLE`
- `LAN_ACTIVE`
- `LAN_LOST`
- `CLOUD_ONLY`
- `LOCAL_QUEUE_ONLY`

BETA baseline hysteresis:

- require at least two consecutive valid Agent health/auth samples before entering `LAN_ACTIVE` from an untrusted/inactive state;
- do not abandon a previously active Agent on a single failed health sample;
- require at least two consecutive failures before normal transport fallback;
- record loss/reacquire time and discovery source for diagnostics.

Exact timing may be tuned by measurement, but a one-sample success/failure switch is prohibited.

## Transport selection

Transport selection does not alter command semantics.

- If authenticated LAN is healthy, LAN may be preferred.
- If LAN is unavailable but Cloud Service is reachable, the same command may use Cloud.
- If neither path is available, the durable command remains local and unacknowledged.
- Switching transport must not allocate a new idempotency key/device sequence for the same intended command.

A failed LAN attempt cannot be hidden as LAN PASS merely because cloud fallback succeeded.

## Local durable queue

Minimum client queue record:

- `idempotency_key` PRIMARY/UNIQUE;
- `device_id`;
- `device_seq`;
- `command_type`;
- `cluster_id` / `module_id` where applicable;
- `entity_id` / expected entity version where applicable;
- serialized payload or protected payload reference;
- `payload_hash`;
- creation time;
- retry count / next retry time;
- last transport/error code;
- state: `PENDING`, `SENDING`, `ACKED`, `DEAD_LOCAL_REVIEW` as applicable.

Queue ordering is normally by `device_seq`. One failed command must not silently cause later conflicting commands to overtake it when domain order matters.

Raw bearer/session tokens must not be written into the durable command payload.

## Agent relay boundary

The Agent may keep transient/bounded relay metadata needed for retry and diagnostics, but business acceptance is not complete merely because the Agent received a command.

A business ACK to the PDA must distinguish at least:

- `RECEIVED_BY_AGENT`: transport receipt only, not safe for deleting a canonical pending mutation;
- `ACCEPTED_CANONICAL`: Service/D1 committed or reconciled the command; safe for normal queue deletion;
- `REJECTED_FINAL`: authoritative validation/permission/conflict rejection;
- `RETRY_LATER`: transient Agent/Cloud/provider failure.

The client must not delete a business command solely on `RECEIVED_BY_AGENT`.

## Authentication and pairing requirements

Before business LAN traffic is enabled, the design must provide a reviewed pairing/authenticated-channel mechanism. Minimum properties:

- device must already be a current registered device or enter an explicit bootstrap/pairing flow;
- Agent and PDA mutually bind the connection to the intended BETA environment/Agent instance;
- replay of stale pairing material fails after security-epoch rotation/revocation;
- Agent cannot mint user business authority; user/session permission remains Service-owned;
- secrets/private keys are never committed to the public repository or diagnostics;
- diagnostics expose identifiers/hashes/status, not raw auth material.

Until this is implemented and accepted, business payloads remain prohibited on LAN pilot endpoints.

## Realtime/status stream

For non-canonical realtime/status traffic, retain the proven legacy pattern:

- each Agent start owns a fresh `streamEpoch`;
- events within an epoch have monotonically increasing `sequence`;
- client cursor is `(streamEpoch, sequence)`;
- epoch change requires explicit resync/reset;
- if client cursor falls behind the retained buffer, Agent signals `requiresResync` rather than pretending continuity;
- realtime stream position is never canonical business state.

Canonical business recovery reads D1-derived current state/events through the Service contract, not the transient realtime buffer.

## Background policy

Current client policy should preserve the legacy resource-safe principle:

- foreground app may keep realtime LAN active;
- background app should stop continuous realtime polling;
- unfinished in-flight command/transfer or durable pending recovery may use a bounded finish-only background mechanism;
- background work stops when required work is complete;
- unavailable network must not keep the app awake indefinitely;
- durable pending work resumes on the next allowed execution/app-open cycle.

Exact Android implementation remains pending the later client build lane.

## Diagnostics

Transport diagnostics should include:

- current transport state and state transition reason;
- Agent endpoint/discovery source without credentials;
- Agent instance ID/protocol/capability summary;
- success/failure streaks and reacquire latency;
- queue pending/peak/enqueued/ACK/failure counters;
- last device sequence;
- request counts/errors/client cancellation;
- latency percentiles;
- realtime epoch/sequence/gap/resync counters;
- transfer/load metrics when test mode is enabled;
- process CPU/RAM/thread/handle/DB/log growth where available;
- app foreground/background/finish-work state.

Business payloads, raw bearer tokens, pairing secrets and credential verifiers must be excluded from normal diagnostic export.

## Source-level acceptance before physical test

The LAN source/model lane is acceptable for the next stage only when automated/source review can demonstrate:

1. one command keeps the same `idempotencyKey + deviceId + deviceSeq` across retry/transport fallback;
2. durable queue insert happens before first transmission where offline resilience applies;
3. queue deletion requires authoritative canonical ACK/reconciliation;
4. cached endpoint -> UDP -> manual order is preserved with authenticated health verification;
5. hysteresis prevents single-sample flapping;
6. Agent restart changes epoch and client resync is deterministic;
7. buffer-gap handling explicitly requests resync;
8. cleartext pilot/test routes cannot carry business credentials/canonical business mutation in current mode;
9. background work is bounded;
10. diagnostics omit raw secrets/business payload by default.

## Physical acceptance still required later

Source/CI PASS is not physical LAN PASS. The company laptop/network and physical MT90 devices are still required for:

- inbound reachability/discovery under actual policy;
- reconnect/reacquisition timing;
- real Wi-Fi latency/throughput/loss;
- offline queue recovery;
- background/battery behavior;
- no-admin update/rollback;
- multi-client synthetic headroom and soak.

Final status until then: `LAN SOURCE/MODEL ACTIVE — PHYSICAL REGRESSION PENDING`.
