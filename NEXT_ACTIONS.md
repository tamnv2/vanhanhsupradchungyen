# NEXT ACTIONS

Checkpoint: `LAN-PILOT-20260911-02`

## Priority objective

`LAN-PILOT-001 — compile/release no-admin pilot artifacts, then validate LAN feasibility on restricted corporate laptop + up to 3 Newland MT90 PDA.`

Owner approved 2026-09-11. Deep business feature work remains behind the LAN feasibility gate unless independent and directly supportive.

## Stream A — LAN CONTRACT + AUTO-LAN [DONE FOR PILOT BASELINE]

- Versioned protocol: `docs/lan/LAN_PROTOCOL_V1.md`.
- TCP local API/Web default: `17891`; UDP discovery: `17892`.
- PDA states: `CLOUD_ONLY`, `LAN_AVAILABLE`, `LAN_ACTIVE`, `LAN_LOST`, `RECONNECTING`, `LOCAL_QUEUE_ONLY`.
- Candidate discovery: cached endpoint -> UDP broadcast -> manual endpoint recovery; internal DNS is not required.
- LAN activation requires Agent identity/environment/protocol health verification + hysteresis.
- Test-only durable event envelope uses event ID + device ID + device sequence/idempotency.

## Stream B — WINDOWS LAN AGENT BETA [IN PROGRESS]

Implemented baseline:

- portable per-user `.exe`, no Administrator/SCM service;
- `asInvoker`, no HKLM/Program Files/firewall/router/DNS changes;
- tray/settings runtime;
- local HTTP API, UDP discovery, embedded diagnostics Web;
- SQLite WAL test store + duplicate protection;
- selectable writable data directory with integrity verify/rollback;
- manual update check + automatic release notification;
- optional HKCU user auto-start if policy permits;
- synthetic LoadGen tool.

Next:

1. finish CI/release artifact gate;
2. add/verify CPU metric on target laptop;
3. implement and test actual no-admin staged self-update + health rollback before final pilot PASS;
4. harden tray/update threading and bounded logs/metrics if measurement requires;
5. run physical laptop feasibility tests without bypassing corporate policy.

## Stream C — ANDROID LAN TEST APP [IN PROGRESS]

Implemented baseline:

- lightweight native app, `minSdk 21`, no Newland scanner SDK dependency;
- signed BETA build pipeline using existing BETA signer;
- auto LAN discovery, health validation and hysteresis;
- Cloud fallback + local queue mode;
- durable SQLite test-event queue;
- echo/latency test, transport status and manual endpoint recovery;
- automatic release discovery + manual update entry point.

Next:

1. finish signed APK CI/release gate;
2. install on Newland MT90 and capture exact Android/device revision from the app;
3. test auto `LAN_AVAILABLE -> LAN_ACTIVE` and all fallback transitions;
4. verify update/install behavior on real MT90;
5. only after physical evidence tune timeout/hysteresis/poll cadence.

## Stream D — LAN WEB BETA [BASELINE IMPLEMENTED]

- Served directly by LAN Agent at `http://<laptop-LAN-IP>:17891/`.
- Shows health, Agent IP(s), connected devices, request/errors, latency percentiles, event/duplicate counts, RAM/DB size, data directory and update check.
- `beta-lan.supra.cc.cd` is not a pilot dependency and will only be added if legitimate internal DNS later exists.

Next: add CPU/status evidence and tune dashboard refresh only if target-laptop measurement shows need.

## Stream E — TEST HARNESS / QUALITY [IN PROGRESS]

1. CI must produce immutable prerelease artifacts:
   - `VHDCHY-LAN-Agent-BETA-win-x64.zip` + SHA256;
   - `VHDCHY-LAN-Pilot-BETA.apk` + SHA256/signature verification.
2. Physical test: 1 -> 2 -> 3 MT90 simultaneously.
3. Synthetic LoadGen: 10 -> 25 -> 50 -> 100+ logical clients on laptop; this measures Agent software capacity only.
4. Scenarios: same Wi-Fi discovery, manual-IP diagnostic, Internet off/LAN on, LAN off/Internet on, both off, Agent restart, PDA/app restart, laptop restart/login, Wi-Fi reconnect/AP transition where possible.
5. Measure success/error rates, p50/p95/p99, throughput, reconnect, queue recovery, event loss/duplicates, RAM/CPU, DB growth and uptime.
6. Classify corporate restrictions explicitly: discovery blocked, inbound TCP blocked, AP isolation, executable/update policy block, unstable Wi-Fi or resource limit.
7. Produce evidence-based PASS/FAIL report.

## LAN-PILOT gate

PASS requires at minimum:

- Agent runs as ordinary user without security-policy bypass;
- 3 physical MT90 concurrently connect and automatically enter LAN mode;
- deterministic fallback/reconnect with zero unexplained test-event loss;
- synthetic capacity shows clear software headroom above three real PDA;
- restart/recovery behavior is predictable;
- automatic update notification and independent manual update work; actual Agent self-update + rollback is verified before final PASS;
- tray/settings/Web operate within acceptable measured CPU/RAM footprint on the target laptop.

If corporate policy prevents PDA-to-laptop inbound LAN without legitimate user privileges, report `FAIL/NOT-FEASIBLE` for laptop-as-LAN-Agent under current conditions instead of requiring router/admin changes.

## Deferred until LAN-PILOT PASS or clearly independent

- deep Pick Pack business UI;
- protected business mutations;
- full Sheets projection activation;
- LAN HA/Master-Backup/fencing;
- Durable Objects/R2 unless measurement proves need.

Existing `CORE-REFINE-001`, `SHEETS-001`, `AUTH-001` source remains checkpointed.

## Owner action

No action while CI/build is being finished. Once known-good artifacts exist, Owner only needs to copy/run the portable Agent as a normal user and install the signed APK on the MT90. The runbook will not require Administrator, router, route, Wi-Fi or DNS changes. No STABLE promotion.
