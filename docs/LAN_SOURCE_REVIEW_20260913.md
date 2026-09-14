# LAN SOURCE REVIEW — 2026-09-13

Status: ACTIVE SOURCE-REUSE BOUNDARY / CURRENT SOURCE MATERIALIZED / PHYSICAL ACCEPTANCE PENDING
Reconciled: 2026-09-14
Current authority: `DECISIONS.md`, `DECISIONS_V3.md`, `DECISIONS_V4.md`, `DECISIONS_V5.md`, `DECISIONS_V6.md`, `DECISIONS_V7.md`, `AI_OPERATING_CONTRACT.md`, `docs/SERVICE_API_CONTRACT_V3.md`, `docs/LAN_EDGE_STATE_V2.md`.

## Reference sources

Two non-authority LAN references remain available:

1. retained snapshot `backup/pre-zero-20260912` in the current project;
2. legacy public repository `tamnv2supra/vanhanhdchungyen`, read-only reference only.

The legacy repository is evidence/reference only and never overrides current VHDCHY authority. The fixed final V4 source reference for comparison is commit `7b4488a89f585812c1bccba5d07d86049482bf4c` (`lan-pilot-beta-v0.3.36`). Do not depend on a moving legacy `main` reference when reviewing old mechanics.

## Verified legacy evidence

The legacy checkpoint recorded a restricted ordinary-user corporate Windows laptop and two Newland NLS-MT90 Android 11 devices reaching basic LAN feasibility before the final V4 regression:

- Agent ran without Administrator/network-policy changes;
- both physical PDA reached `LAN_ACTIVE` automatically against the LAN Agent;
- no manual endpoint was required in the successful session;
- health success streaks reached 73/67 with failure streak 0;
- observed echo samples were about 10–57 ms;
- Agent request p50/p95/p99 were about 47/67/90 ms;
- durable events reached Agent SQLite and pending queues recovered to zero;
- duplicate event rejection was deterministic;
- V4 automated build/package/sign/release gates reached PASS at release `lan-pilot-beta-v0.3.36`.

This proves the old pilot was materially implemented and physically exercised. It does **not** prove the current VHDCHY LAN business path because current auth/domain/authority contracts differ and the final V4 physical regression was not completed.

## Reusable implementation patterns confirmed from legacy source

### Windows Agent patterns

Legacy `lan-agent/Vhdchy.LanAgent/PilotV4.cs` supplied reviewed reference patterns for:

- portable per-user single-instance/no-admin host operation;
- local Kestrel listener on a high user-space port;
- LAN discovery with explicit service/environment/protocol identity;
- health/capability/instance/epoch reporting;
- `streamEpoch + sequence` realtime resync concepts;
- bounded realtime buffering;
- durable SQLite persistence and duplicate-safe event handling;
- latency/error/network/resource metrics and diagnostics export;
- staged no-admin update/hash/health/rollback mechanics.

### Android/PDA patterns

Legacy `MainActivityV4.java` and `PilotRepository.java` supplied reviewed reference patterns for:

- transport state machine and endpoint cache/discovery/recovery ordering;
- anti-flapping activation/fallback hysteresis;
- stable device identity and monotonic `device_seq`;
- durable pending queue ordered by device sequence;
- ACK-driven deletion and automatic recovery after LAN reacquisition;
- explicit epoch reset/resync after Agent restart;
- foreground-first realtime and bounded background completion work;
- calibrated latency evidence rather than naive cross-device wall-clock subtraction.

## Current reuse outcome

These concepts have now moved beyond “restoration work that can resume” into current VHDCHY source foundations where reviewed:

- no-admin LAN host/runtime packaging;
- LAN health/readiness and endpoint/domain contract foundations;
- durable local operational state and immutable edge events;
- idempotent replay and authenticated actor evidence;
- durable staged media;
- durable Cloud reconciliation queue/restart/conflict mechanics;
- secure Kestrel HTTPS normal-user login/session + reviewed Slice-1 business route;
- Windows DPAPI CurrentUser PFX custody;
- separate user-space ACME DNS-01 certificate-manager source.

Current accepted automated evidence is tracked by `docs/PROGRESS_TRACKING_V1.md`, `docs/LAN_SECURE_HTTP_V1.md` and `docs/LAN_CERTIFICATE_MANAGER_V1.md`. Legacy evidence itself does not add current-product completion credit.

## Items that remain pilot-only or forbidden as current authority

`VHDCHY_LAN_PILOT_V1` and cleartext `/api/pilot/*` behavior remain test/reference-only. They must not carry current business credentials, employee PII or canonical mutations.

Legacy service/environment/protocol string checks are identity hints, not cryptographic authentication. Current VHDCHY requires reviewed pairing/authentication, security epoch handling, permission enforcement and the same domain/idempotency/error semantics as the current Service contract.

Legacy payload/business assumptions, old provider IDs, old Sheets authority, hard-coded shifts/auth rules and old runtime state/data are not inherited.

## Correct current Cloud/LAN authority interpretation

The earlier statement “local SQLite must not become a second canonical business authority; D1 remains canonical” is too coarse under V3/V4 and must not be used to reject the approved LAN model.

Current authority is context-specific:

- Cloud normal operation: Cloud Service/D1 is the normal central structured authority.
- LAN operation: the LAN Service commits local current state + immutable edge event + required sync/output work durably; for commands accepted locally under the synchronized authority snapshot, that LAN edge state/event journal is the operational authority during the partition/LAN period.
- After connectivity returns: Cloud reconciliation consumes the LAN immutable events/outbox and D1 becomes the central consolidated structured store after synchronization.
- Google Sheets/Drive are downstream projection/storage and never reconstruction source truth.
- Cloud/LAN do not fork business semantics; route changes preserve the same logical command/event identity.

This is the required full local Service model, not the legacy relay/pilot model.

## Current physical/provider gate

Source work is not paused by physical availability. The remaining target-environment acceptance still requires:

1. intended ordinary-user company Windows host;
2. verified least-privilege DNS write credential outside GitHub/LAN Service;
3. ACME staging DNS-01 issuance/cleanup for `lan-beta.supra.cc.cd`;
4. production public-CA issuance only after staging passes;
5. canonical DNS reachability and Windows/browser trust;
6. real NLS-MT90/PDA HTTPS, reconnection and Wi-Fi/LAN reacquisition;
7. host restart/network-change recovery and no-admin update/rollback;
8. >=60-minute Internet-cut local continuity plus separate post-restoration reconciliation;
9. synthetic 10/25/50/100 capacity and soak when the target environment is ready.

Until these tests occur, classify the physical lane as `IMPLEMENTED_AUTOMATED / PHYSICAL-PROVIDER ACCEPTANCE PENDING`, not final PASS. Independent auth/business/provider/Web/App source lanes continue in parallel.

## Authority boundary

The legacy repository is reference/evidence only. Current repo decisions, security rules and `docs/SERVICE_API_CONTRACT_V3.md` remain authoritative. Any old behavior that conflicts with current authority is discarded or deliberately adapted; no old repository resource becomes runtime fallback, write target or business authority automatically.