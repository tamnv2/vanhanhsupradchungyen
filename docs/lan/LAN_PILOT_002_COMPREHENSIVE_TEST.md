# LAN-PILOT-002 — COMPREHENSIVE TWO-PDA TEST

Status: `OWNER-APPROVED / BUILDING`
Environment: restricted corporate Windows laptop + exactly 2 Newland MT90 available for physical testing.

## Purpose

Move beyond basic connectivity and collect enough evidence to decide whether the laptop-as-LAN-Agent model is viable for VHDCHY.

The physical ceiling is currently 2 PDA. Synthetic load is used to test software headroom above two clients, but it MUST NOT be represented as Wi-Fi/RF evidence for an equivalent number of physical PDA.

## Build 0.2 measurement capabilities

### LAN hard-priority / reacquisition

- APK actively prefers a valid BETA LAN Agent whenever one is reachable.
- While the APK process is alive, a non-LAN state triggers aggressive discovery/reacquisition attempts.
- Agent health includes its start timestamp so APK diagnostics can estimate `Agent start -> LAN_ACTIVE` delay after laptop Agent restart.
- Diagnostics record discovery source (`UDP`, `CACHE`, or other observable source).
- This does not modify the Android OS route. It is application-level transport selection.
- Background/screen-off behavior must be measured. If Android kills/suspends the process, that is evidence for whether a foreground-service monitor is needed in the next build.

### Data transfer

APK can run bidirectional test payloads:

- 1 MB normal test;
- 25 MB heavy test;
- FULL suite: 1 MB, 10 MB and 25 MB upload + download.

Record Mbps, elapsed time, failure, and Agent transfer-byte counters.

### Realtime

Agent provides a bounded in-memory realtime event buffer and long-poll API.

- PDA can publish one message.
- Heavy mode publishes `200 x 2 KB` messages.
- Laptop Test Center continuously receives and reports display latency.
- Other PDA continuously receives and reports count, last display latency, p95 and sequence-gap count.
- Test payload only; not business data.

### Service capacity

Laptop Test Center can start local-only LoadGen for:

- 10 logical clients x 100 echo requests;
- 25 x 100;
- 50 x 100;
- 100 x 100.

Measure request success, req/s, p50/p95/p99, Agent CPU/RAM and whole-laptop CPU/RAM.

## Required physical sequence

### Test A — baseline two PDA

1. Start Agent on laptop as ordinary user.
2. Start both APKs.
3. Do not enter manual IP.
4. Verify both become `LAN_ACTIVE`.
5. Keep both running for at least 10 minutes.
6. Confirm failure streak remains zero or explain any transition.

### Test B — laptop Agent restart forces LAN reacquisition

1. Both PDA must already be running.
2. Exit/restart Agent from tray.
3. Do not touch either PDA.
4. Observe PDA leave LAN state while Agent is down.
5. Start Agent again.
6. Verify both PDA automatically return to `LAN_ACTIVE` without manual endpoint.
7. Capture `Agent-start -> LAN_ACTIVE` measurement in FULL diagnostics.
8. Repeat at least three times.

### Test C — Wi-Fi interruption and durable queue

For each PDA separately:

1. While LAN_ACTIVE, disable Wi-Fi if device policy permits.
2. Create several durable events.
3. Re-enable Wi-Fi.
4. Verify automatic LAN reacquisition.
5. Verify pending queue returns to zero.
6. Agent event count must increase without unexplained event loss.

### Test D — realtime normal

1. Keep laptop Test Center open.
2. PDA-A: press `Gửi 1 bản tin realtime` ten times.
3. Observe laptop and PDA-B.
4. Repeat from PDA-B toward PDA-A.
5. Record receiver count, last latency, p95 and sequence gaps.

### Test E — realtime heavy

1. PDA-A: run `Realtime nặng: 200 bản tin x 2 KB`.
2. PDA-B and laptop must remain responsive and receive the stream.
3. Export diagnostics.
4. Repeat in reverse direction.
5. Then trigger heavy realtime on both PDA near-simultaneously.
6. Record sequence gaps, publish fail count, p95 receiver latency, Agent CPU/RAM and error count.

### Test F — data transfer

On each PDA:

1. Run 1 MB test three times.
2. Run 25 MB test three times.
3. Run FULL suite once.
4. Record upload/download Mbps and errors.
5. Repeat FULL suite on both PDA near-simultaneously for the heavy case.

### Test G — synthetic Agent capacity

On laptop Test Center opened through `127.0.0.1`:

1. Run 10 clients.
2. Run 25 clients.
3. Run 50 clients.
4. Run 100 clients.
5. Stop increasing if the laptop becomes materially unstable or corporate endpoint protection intervenes.
6. Save LoadGen output shown on the page and export Agent diagnostics afterward.

This tests Agent software headroom only.

### Test H — background / screen-off

For each MT90:

1. Reach LAN_ACTIVE.
2. Turn screen off for 5 minutes with Agent online.
3. Turn screen on and inspect realtime/health continuity.
4. Repeat with Agent restarted during screen-off period.
5. If APK does not reacquire until foregrounded, record it; do not hide the result. It determines whether the business APK needs a foreground LAN monitor.

### Test I — soak

Run Agent + two PDA for:

- first target: 30 minutes;
- next: 2 hours;
- later if feasible: one work-shift window.

During soak, periodically send realtime and durable events. Check memory growth, errors, latency drift, queue and reconnect behavior.

## Evidence to export

After each major batch, collect:

- one Agent ZIP diagnostics;
- FULL diagnostics TXT from PDA-A;
- FULL diagnostics TXT from PDA-B.

Do not export business credentials or real operational data.

## Measurement fields used for decision

- automatic LAN reacquisition success and delay;
- UDP/cache discovery source;
- request error rate;
- p50/p95/p99 request latency;
- realtime display latency and sequence gaps;
- realtime publish success/failure;
- upload/download throughput;
- local queue recovery and event loss;
- duplicates handled deterministically;
- Agent CPU/RAM idle and loaded;
- laptop CPU/RAM loaded;
- synthetic req/s at 10/25/50/100 clients;
- process/background survival;
- stability over soak duration.

## Decision rule

With only two physical PDA available, LAN feasibility may be accepted for the current environment when both physical devices pass connection/reacquisition/realtime/transfer/queue/soak tests and synthetic load demonstrates clear Agent headroom. The report must explicitly state that RF/Wi-Fi behavior above two physical PDA remains unproven until more devices are available.
