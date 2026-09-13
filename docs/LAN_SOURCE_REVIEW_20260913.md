# LAN SOURCE REVIEW — 2026-09-13

Status: ACTIVE SOURCE REUSE / PHYSICAL TEST STILL PAUSED
Current authority: `DECISIONS.md`, `AI_OPERATING_CONTRACT.md`, `docs/SERVICE_API_CONTRACT.md`

## Reference sources

Two non-authority LAN references are now available:

1. retained snapshot `backup/pre-zero-20260912` in the current project;
2. legacy public repository `tamnv2supra/vanhanhdchungyen`, read-only reference only.

The legacy repository is accessible for read/review and must never override current VHDCHY authority. The fixed final V4 source reference for comparison is commit `7b4488a89f585812c1bccba5d07d86049482bf4c` (`lan-pilot-beta-v0.3.36`). Do not depend on a moving `main` reference when restoring code.

## Verified legacy evidence

The legacy checkpoint records a restricted ordinary-user corporate Windows laptop and exactly two Newland NLS-MT90 Android 11 devices reaching basic LAN feasibility before the final V4 regression:

- Agent ran without Administrator/network-policy changes;
- both physical PDA reached `LAN_ACTIVE` automatically against the LAN Agent;
- no manual endpoint was required in the successful session;
- health success streaks reached 73/67 with failure streak 0;
- observed echo samples were about 10–57 ms;
- Agent request p50/p95/p99 were about 47/67/90 ms;
- durable events reached Agent SQLite and pending queues recovered to zero;
- duplicate event rejection was deterministic;
- V4 automated build/package/sign/release gates reached PASS at release `lan-pilot-beta-v0.3.36`.

This evidence proves the old pilot was materially implemented and physically exercised. It does not prove the current VHDCHY LAN business path because current auth/domain contracts differ and the final V4 physical regression was not completed.

## Reusable implementation patterns confirmed from legacy source

### Windows Agent

`lan-agent/Vhdchy.LanAgent/PilotV4.cs` provides reusable patterns for:

- portable per-user Agent with single-instance guard and no-Admin operation;
- local Kestrel listener on a high user-space port;
- UDP discovery request/reply with explicit service/environment/protocol identity;
- health endpoint carrying instance/version/stream epoch/capabilities;
- `streamEpoch + sequence` realtime cursor and explicit resync after restart/buffer gap;
- bounded realtime buffer and signal-driven long polling;
- local SQLite persistence and duplicate-safe event acceptance;
- latency/error/client-cancel/network/resource metrics;
- transfer/load-test/diagnostic endpoints;
- diagnostics export that deliberately excludes canonical business payload/database;
- staged no-admin update design with hash verification, health check and rollback path.

### Android/PDA

Legacy `MainActivityV4.java` and `PilotRepository.java` confirm reusable client patterns:

- transport state machine: `CLOUD_ONLY / LAN_AVAILABLE / LAN_ACTIVE / LAN_LOST / RECONNECTING / LOCAL_QUEUE_ONLY`;
- endpoint resolution order: cached healthy endpoint -> UDP discovery -> manual recovery endpoint;
- anti-flapping hysteresis: two successful health samples before activation and two failures before fallback;
- stable per-device identifier plus monotonically increasing `device_seq`;
- SQLite durable pending queue keyed by `event_id` and ordered by `device_seq`;
- ACK-driven deletion only after successful acceptance;
- automatic queue recovery after LAN reacquisition;
- explicit epoch reset/resync after Agent restart;
- foreground-only realtime and bounded finish-only background service for unfinished work/queue;
- clock calibration for cross-device latency evidence rather than direct unsynchronized wall-clock subtraction.

## What may be adopted now

These concepts are compatible with the current architecture and can be adapted without physical devices:

- durable command queue and persistent device sequence;
- LAN endpoint cache/discovery/hysteresis state machine;
- Agent health/capability/epoch contract;
- reconnect/resync logic;
- diagnostics/metrics/export model;
- synthetic Agent load generator approach;
- no-admin packaging/runtime constraints;
- bounded Android background-work policy as a future client requirement.

The source must be adapted to the current shared Service/domain contract rather than copied wholesale.

## Items that remain pilot-only or must change

`VHDCHY_LAN_PILOT_V1` and cleartext `/api/pilot/*` endpoints remain test-only. They must not carry business credentials, employee PII or canonical mutations.

Legacy service/environment/protocol string checks are identity hints, not cryptographic authentication. Before business LAN activation, current VHDCHY requires reviewed pairing/authentication, security epoch handling, permission enforcement and the same idempotent command/event semantics used by the Service API.

Legacy local SQLite is appropriate for client/Agent queueing and transport evidence. It must not become a second canonical business authority; D1 remains canonical under current decisions.

Legacy Cloud health probing may remain diagnostic, but LAN/cloud selection must not fork business semantics or create separate authorities.

## Restoration work that can resume immediately

Physical availability is no longer a reason to pause source/model work. The following work is executable now from the legacy reference:

1. define a transport-neutral LAN command envelope aligned with `docs/SERVICE_API_CONTRACT.md` and canonical `idempotency_key + device_id + device_seq` semantics;
2. extract/adapt endpoint discovery, health validation and hysteresis into a current BETA LAN transport design;
3. extract/adapt durable queue/device-sequence semantics without importing pilot payload/business assumptions;
4. define `streamEpoch + sequence` reconnect/resync behavior for non-canonical realtime/status traffic;
5. define diagnostics and synthetic-capacity acceptance compatible with the current BETA acceptance matrix;
6. map Agent/PDA authentication and pairing requirements before any business-data transport is opened;
7. prepare source/build structure for later physical regression without claiming physical PASS.

## Physical gate that still remains

The real company laptop/network and physical MT90 devices are still required to verify:

- inbound reachability and UDP discovery under current company network policy;
- automatic reacquisition after Agent restart/network changes;
- real PDA realtime/transfer latency and Wi-Fi behavior;
- queue recovery under Wi-Fi loss;
- background lifecycle/battery footprint;
- no-admin update behavior on the actual laptop;
- synthetic 10/25/50/100 Agent capacity plus soak in the target environment.

Until those tests occur, classify LAN as `SOURCE/MODEL ACTIVE — PHYSICAL REGRESSION PENDING`, not final PASS.

## Authority boundary

The legacy repository is reference/evidence only. Current repo decisions, security rules and Service API contract remain authoritative. Any old behavior that conflicts with current authority must be discarded or adapted; no old repository resource may become runtime fallback or write target.
