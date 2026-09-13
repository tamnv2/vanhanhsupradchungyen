# LAN SOURCE REVIEW — 2026-09-13

Status: REVIEWED FOR RESTORATION / PHYSICAL TEST STILL PAUSED
Source reviewed: `backup/pre-zero-20260912`
Current authority: `DECISIONS.md`, `AI_OPERATING_CONTRACT.md`, `docs/SERVICE_API_CONTRACT.md`

## Result

The retained LAN pilot is useful transport/feasibility evidence, but it is not a business backend and must not be restored wholesale as current business authority.

Retainable concepts/source patterns:
- user-mode Windows Agent with no Administrator/router/DNS/firewall-rule assumption;
- cached endpoint -> UDP discovery -> manual endpoint recovery order;
- health identity verification before LAN activation;
- hysteresis-based LAN state machine instead of switching on one success/failure;
- local durable queue with `event_id` + monotonically increasing `device_seq`;
- idempotent retry/ACK semantics;
- explicit foreground/background diagnostics and bounded logs;
- transport/resource test endpoints and metrics for feasibility measurement;
- physical failure classification rather than forcing network-policy changes.

These are consistent with current D-008/D-009 because LAN remains a transport/fallback lane using the shared command/event model and must operate under the real corporate-network permission boundary.

## Items that remain pilot-only

The retained `VHDCHY_LAN_PILOT_V1` protocol and its cleartext HTTP test endpoints are test-only. They must not carry business credentials, employee PII or canonical business mutations.

The retained source identifies LAN Agent by service/environment/protocol strings. That identity check is sufficient only for pilot feasibility and is not cryptographic authentication.

Cloud health probing in the Android pilot may remain diagnostic, but cloud vs LAN transport selection must not create separate business authorities. Canonical business commands eventually use the same authenticated/idempotent Service-domain semantics as `docs/SERVICE_API_CONTRACT.md`.

## Restoration plan

Source restoration is split to avoid coupling code review with physical availability:

1. Restore/reuse transport-neutral queue/device-sequence/diagnostic primitives after namespace/version cleanup.
2. Preserve pilot discovery/health/state-machine components behind a BETA-only transport adapter.
3. Do not wire pilot `POST /api/pilot/event` into canonical D1 business tables.
4. Before any real business LAN path, add reviewed pairing/authentication, security-epoch handling and shared command/event envelope compatible with Service auth/idempotency rules.
5. Keep physical discovery/inbound/AP-isolation regression pending until Owner is on-site with the actual company network/PDA devices.

## Current gate

No Owner action is required for source review. Physical LAN testing is a `PHYSICAL` dependency only and does not block Service/Auth/Projection/API work.
