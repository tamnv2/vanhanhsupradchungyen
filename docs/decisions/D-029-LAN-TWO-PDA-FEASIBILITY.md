# D-029 — LAN feasibility with two physical MT90

Status: ACTIVE
Owner approved: 2026-09-11

## Context

Earlier LAN-PILOT planning assumed availability of up to three physical Newland MT90 devices. Owner confirmed the actual current physical test inventory is exactly two MT90.

## Decision

The current LAN feasibility decision may use:

- exactly 2 physical MT90 for Wi-Fi/LAN behavior;
- synthetic 10/25/50/100 logical clients on the laptop for Agent software-capacity headroom.

A feasibility PASS in the current environment requires both real PDA to pass the relevant auto-LAN/reacquisition, realtime, transfer, queue/recovery and soak scenarios, while synthetic load shows clear Agent headroom.

Synthetic clients MUST NOT be represented as proof that the corporate Wi-Fi/RF layer can support an equivalent number of physical PDA. Any final report must explicitly say RF/Wi-Fi behavior above two physical PDA remains unproven until more real devices are available.

## Related evidence plan

`docs/lan/LAN_PILOT_002_COMPREHENSIVE_TEST.md`
