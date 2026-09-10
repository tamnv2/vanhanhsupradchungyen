# VHDCHY LAN PILOT — TEST RESULT

Status: `TEMPLATE`
Environment: `BETA`

## Test identity

- Date/time:
- Release tag:
- LAN Agent version:
- APK version:
- Laptop Windows version if visible without elevated/admin tools:
- Laptop CPU/RAM as known: ~2C/4T, 8 GB baseline; exact model optional
- PDA count physically tested:

## PDA inventory

| PDA | Manufacturer/model shown by app | Android version | Device ID | Install PASS/FAIL |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |

## Laptop Agent start

- Normal-user EXE start: PASS / FAIL
- Local dashboard `127.0.0.1`: PASS / FAIL
- Corporate execution block observed: YES / NO
- LAN IP(s) reported by Agent:
- User data directory:
- HKCU auto-start: PASS / BLOCKED / NOT TESTED

## Discovery and LAN activation

| PDA | UDP auto-discovery | Manual-IP health | LAN_AVAILABLE | LAN_ACTIVE | Time to LAN_ACTIVE |
|---|---|---|---|---|---|
| 1 | | | | | |
| 2 | | | | | |
| 3 | | | | | |

Failure classification if applicable:
- `DISCOVERY_BLOCKED_OR_FILTERED`
- `INBOUND_TCP_BLOCKED`
- `PDA_AP_ISOLATION`
- `AGENT_UNREACHABLE`
- `AGENT_IDENTITY_INVALID`
- `UNSTABLE_WIFI`
- `CLIENT_FALLBACK_ERROR`
- `RESOURCE_LIMIT`
- `EXECUTION_POLICY_BLOCKED`
- `APK_INSTALL_POLICY_BLOCKED`
- `UPDATE_POLICY_BLOCKED`
- other:

## Physical 1/2/3-PDA results

| Test | Requests/events | Success rate | p50 | p95 | p99 | Event loss | Unexpected duplicate |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 PDA | | | | | | | |
| 2 PDA | | | | | | | |
| 3 PDA | | | | | | | |

## Recovery tests

| Scenario | Expected | Observed | PASS/FAIL/NOT TESTABLE |
|---|---|---|---|
| App restart | cached endpoint/re-discovery recovers | | |
| Agent restart | PDA leaves LAN then reconnects | | |
| PDA Wi-Fi off/on | queue preserved and later flushes | | |
| Internet unavailable but LAN alive | LAN continues | | |
| LAN unavailable but Internet alive | Cloud fallback | | |
| Both unavailable | local queue | | |
| Laptop reboot/login | Agent user auto-start or documented manual start | | |

## Laptop footprint

| Load | Agent RAM | Agent CPU | Errors | Notes |
|---|---:|---:|---:|---|
| idle ≥5 min | | | | |
| 1 PDA | | | | |
| 3 PDA | | | | |
| synthetic 10 | | | | |
| synthetic 25 | | | | |
| synthetic 50 | | | | |
| synthetic 100 | | | | |

## Synthetic LoadGen

| Logical clients | requests/client | success | throughput req/s | p50 | p95 | p99 |
|---:|---:|---:|---:|---:|---:|---:|
| 10 | 100 | | | | | |
| 25 | 100 | | | | | |
| 50 | 100 | | | | | |
| 100 | 100 | | | | | |

Reminder: synthetic logical-client test is not physical Wi-Fi/RF evidence.

## Update tests

- Android manual update check: PASS / FAIL
- Android automatic notification: PASS / FAIL / PENDING NEW RELEASE
- Android actual update install: PASS / FAIL / PENDING NEW RELEASE
- LAN Agent manual update check: PASS / FAIL
- LAN Agent automatic notification: PASS / FAIL / PENDING NEW RELEASE
- LAN Agent actual staged self-update: PASS / FAIL / NOT YET IMPLEMENTED
- LAN Agent failed-update rollback: PASS / FAIL / NOT YET IMPLEMENTED

## Final evidence classification

- `LAN-PILOT`: PASS / SOFTWARE_REWORK_REQUIRED / NOT_FEASIBLE_CURRENT_POLICY / INCOMPLETE
- Blocking evidence:
- Software fixes required:
- Corporate/network restrictions observed:
- Owner decision required:
- Exact next action:

Do not mark final PASS until all mandatory gates in `LAN_PILOT_001.md` are satisfied.