# NON-FUNCTIONAL BASELINE V1 — VHDCHY

Status: ACTIVE BASELINE / MEASURE IN BETA
Updated: 2026-09-13

This file consolidates compatible performance, usability, quota and resilience requirements from the reviewed Owner material. Numeric values are BETA planning/acceptance targets, not claims of current provider capacity.

## Workload and stress envelope

Normal planning: about 20 laptop/admin clients plus 20–30 PDA clients across roughly 4–8 clusters; about 300 attendance people/day; about 500 labor records/day; about 50 evidence images/day; about 10–30 new employees/day.

Stress cases to measure:
- 65 devices;
- 20-hour soak;
- roughly 390 attendance operations in a 15-minute burst;
- roughly 300–400 labor start/finish operations in a 15-minute burst;
- around 20 concurrent image uploads near 1.3 MB each;
- around 20 laptop history/search users plus several long-history/archive readers.

Capacity decisions use measured burst, reconnect, archive and upload behavior rather than daily averages alone.

## Latency targets

Healthy-network BETA targets:
- LAN reviewed same-cluster operations: >=90% under 500 ms and 100% under 2 seconds;
- Cloud fast-path operations: p95 target under 800 ms.

## Free-first policy

Optimize before Paid without weakening data safety.

Required practices include indexed/bounded reads, transaction/batch/idempotency, batched Google projection, retry/backoff, avoiding polling-heavy designs where delta/event delivery is suitable, and avoiding repeated whole-Drive/whole-Sheet scans for known resources.

Quota monitoring should expose current use plus trend/forecast. Operational warning bands around 50/70/90% may be used where a provider offers a meaningful measurable limit.

After STABLE is live, BETA should remain a bounded test consumer of shared account-wide limits except during deliberate Owner-approved stress testing.

## UI baseline

Website and APK share the same business authority and permission semantics.

APK/PDA baseline:
- scanner-first interaction;
- large scan/search targets suitable for warehouse use;
- hardware scan/Enter fast path with manual-entry fallback;
- compact permission-aware surfaces for business work, people, history, synchronization and settings.

Website baseline:
- broader permission-aware surfaces for dashboard, business work, people, attendance/labor, resources, documents, import/export, history, synchronization, administration and settings;
- bounded/paginated long-history views;
- wider UI capability never means stronger business authority than APK.

Normal UI updates should use bounded state/delta refresh where practical instead of avoidable full reload/flicker.

## Realtime direction

Polling-heavy architecture is not the target. Realtime implementation must preserve environment/cluster isolation, bounded deltas, reconnect safety and command idempotency. Business success is defined by durable Service commit, not by a realtime transport acknowledgement.

The older Master proposed Durable Objects/WebSocket Hibernation as a Cloud implementation candidate; it is not a locked business rule and may be optimized after measurement.

## Client update / compatibility

- BETA and STABLE package/signing/update channels are separate.
- A device that has learned a mandatory incompatible release may block new business mutations until update while keeping update/network/LAN/diagnostic/pending-recovery surfaces usable.
- A truly disconnected device cannot learn a new release requirement until connectivity returns.
- Update handling preserves pending work, verifies the downloaded artifact, migrates local state and self-checks before normal mutation resumes.
- Rollback uses a newer installable build carrying the previous accepted behavior rather than a lower Android version number.
- Old pending events are preserved under the compatibility rules in `DECISIONS_V5.md`.

## Backup / restore targets

Before STABLE promotion, evidence must cover D1 snapshot/restore, LAN edge recovery with unsynchronized events, staged-media recovery, and archive readback/checksum verification.

Planning recovery targets retained from the approved Master are one-cluster recovery within about 30 minutes and whole-environment recovery within about 2 hours when required recovery sources/connectivity are healthy. These must be measured rather than assumed.

A corrupt local database is quarantined/preserved before rebuild; recovery must not overwrite the only remaining evidence in place.

## LAN physical gate

LAN host remains portable/no-admin and must not require changing corporate firewall/router/AP/route table/internal DNS/certificate policy.

Final PASS requires physical evidence on the intended company laptop/network/PDA for peer reachability, local service access, sleep/reconnect, local Web/API, canonical LAN-domain target, and no-admin update/recovery behavior.

Synthetic tests prove software headroom only; they do not replace physical network evidence.

## Required soak/failure families

Test at minimum: Cloud direct; Cloud unavailable with Internet/Google still reachable; full Internet loss with LAN; Cloud recovery while clients remain on LAN; LAN restart with pending work; retry/idempotency; Cloud/LAN conflict; Google outage/recovery; long offline operation; quota/burst/20-hour soak; BETA/STABLE isolation; update/rollback; archive/backup/restore.
