# VHDCHY LAN TRANSPORT BETA V1

Status: SUPERSEDED AS PRODUCT AUTHORITY / RETAINED FOR LOW-LEVEL REFERENCE
Superseded by: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`, `docs/DELIVERY_PLAN_V2.md`
Legacy reference: `tamnv2supra/vanhanhdchungyen@7b4488a89f585812c1bccba5d07d86049482bf4c`

## Why this document is superseded

This document was written when LAN was being treated primarily as a transport/fallback lane around a Cloud-canonical Service. The Owner clarified on 2026-09-13 that this is incomplete.

The current product requirement is stronger:

- Website and APK are both first-class clients of the same VHDCHY business platform.
- Cloudflare Service is the normal online service runtime.
- LAN Service must be able to substitute for Cloud Service when site Internet is unavailable, Cloud Service is unavailable/degraded, or a client is forced to use the LAN path.
- During true Cloud/upstream loss, LAN must be able to perform reviewed offline-capable business processing locally and later reconcile to D1.
- The legacy repository is reference/evidence only and must never be copied as product authority.

Therefore this file is no longer allowed to define the product architecture by itself.

## Retained low-level patterns

The following mechanics remain useful inputs to the new LAN Service design after review:

- stable `deviceId` and monotonically increasing `deviceSeq`;
- stable idempotency identity across retries;
- durable queue before transmission where required;
- cached endpoint -> UDP discovery -> manual recovery order;
- no arbitrary subnet scan;
- health validation and anti-flapping hysteresis;
- `streamEpoch + sequence` realtime resync;
- bounded background work;
- diagnostics that exclude secrets/business payload by default;
- no-admin/user-mode operation;
- explicit distinction among transport receipt, local edge acceptance and global Cloud/D1 reconciliation.

## Retired assumptions

Do not continue implementation based on these former assumptions:

- LAN is only a relay/transport path and cannot locally execute business commands;
- a LAN Agent receipt can be treated as the final business success;
- the Android deliverable is primarily a LAN diagnostics/transport test application;
- the final Android product can be designed independently from the Web/domain surface;
- source/build progress is measured by reproducing the legacy pilot.

## Current authoritative references

Read in this order for new work:

1. `DECISIONS.md`
2. `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`
3. `docs/DELIVERY_PLAN_V2.md`
4. `docs/SERVICE_API_CONTRACT.md`
5. `docs/BETA_ACCEPTANCE_MATRIX.md`
6. this file only for low-level LAN transport mechanics.
