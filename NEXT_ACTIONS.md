# NEXT ACTIONS

Checkpoint: `LAN-PILOT-20260911-01`

## Priority objective

`LAN-PILOT-001 — build and validate LAN feasibility before deep business implementation.`

Owner approved 2026-09-11. Do not continue deep business feature work before the LAN pilot gate unless the work is independent and clearly supports the pilot.

## Stream A — LAN CONTRACT + AUTO-LAN

1. Define versioned LAN health/discovery contract.
2. Define PDA transport states: `CLOUD_ONLY`, `LAN_AVAILABLE`, `LAN_ACTIVE`, `LAN_LOST`, `RECONNECTING`, `LOCAL_QUEUE_ONLY`.
3. Define service identity/environment validation, timeouts, hysteresis/backoff and fallback rules.
4. Define event/request envelope suitable for reconnect/retry/idempotency testing.
5. Define internal-only hostname contract for `beta-lan.supra.cc.cd`; no public DNS.

## Stream B — WINDOWS LAN AGENT BETA

1. Choose implementation/runtime after comparing footprint + maintainability for Windows service/tray/update.
2. Build lightweight background service skeleton.
3. Add local HTTP API + health + metrics + local durable queue/database.
4. Add tray/settings console showing Service/LAN/Internet, client count, latency/error/queue, CPU/RAM, versions.
5. Add safe local data-directory selection, logs/diagnostics and controlled service start/stop/restart.
6. Add updater contract with automatic discovery/notification plus manual `Kiểm tra cập nhật`; stage/verify/health-check/rollback.
7. Produce BETA artifact/install instructions.

## Stream C — ANDROID LAN TEST APP

1. Create/continue native Android BETA foundation using existing signing/environment separation.
2. Add auto-discovery and auto-enter LAN mode.
3. Show current transport and fallback reason.
4. Add request/realtime/queue/reconnect test controls and metrics.
5. Add local durable queue so test events survive network/app restart.
6. Add automatic update discovery/notification and independent manual update controls.
7. Build signed BETA APK suitable for 1/2/3 physical PDA testing.

## Stream D — LAN WEB BETA

1. Serve a lightweight local diagnostics site from LAN Agent/local host.
2. Target internal hostname `beta-lan.supra.cc.cd` after Owner configures/accepts internal DNS/routing instructions.
3. Display service health, connected PDA, p50/p95/p99 latency, request/error rate, queue/backlog, CPU/RAM/local DB, Internet/Cloud status and build versions.
4. Keep frontend contract reusable for future operational/admin Web.

## Stream E — TEST HARNESS / QUALITY

1. Physical test profiles for 1, 2 and 3 PDA simultaneously.
2. Synthetic laptop clients for 10/25/50/100+ logical-client service capacity tests; never represent these as equivalent Wi-Fi evidence.
3. Network scenarios: LAN only, Internet only, both, neither, reconnect, service restart, laptop restart, app restart.
4. Measure success/error rates, p50/p95/p99, throughput, reconnect time, queue recovery, event loss, CPU/RAM, DB/log growth and uptime.
5. Verify updater automatic notification + manual recovery path for APK and LAN Agent.
6. Record evidence and PASS/FAIL report.

## LAN-PILOT gate

PASS requires at minimum:

- 3 physical PDA concurrently connect and automatically enter LAN mode under correct policy;
- deterministic fallback/reconnect with zero unexplained test-event loss;
- synthetic capacity provides clear headroom above 3 physical PDA;
- LAN Agent restart/laptop restart recover predictably;
- automatic + manual update paths work;
- tray/settings requirements work with low measured idle footprint;
- LAN Web reports consistent status/metrics.

If FAIL: fix LAN transport/runtime/architecture first.

## Deferred until LAN-PILOT PASS or clearly independent

- deep Pick Pack business UI;
- protected business mutations;
- full Sheets projection activation;
- LAN HA/Master-Backup/fencing;
- Durable Objects/R2 unless pilot measurement proves need.

Existing `CORE-REFINE-001`, `SHEETS-001`, `AUTH-001` source work may remain checkpointed but does not outrank this gate.

## Owner action

At implementation start: NONE.

When LAN artifacts are ready, Owner will need to run/install them on the test laptop/PDA and, where required, apply internal DNS/hosts/router settings provided by the test runbook. No STABLE promotion.
