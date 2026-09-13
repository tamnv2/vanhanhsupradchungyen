# LAN / APK DEV BUILD STATUS

Updated: 2026-09-13
Status: DISPOSABLE PROTOTYPE BUILD PASS / NOT PRODUCT AUTHORITY
Authority: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`, `docs/DELIVERY_PLAN_V2.md`
Legacy reference: `tamnv2supra/vanhanhdchungyen@7b4488a89f585812c1bccba5d07d86049482bf4c` — NON_AUTHORITY

## Correction

The earlier `android-pilot/` + `lan-agent/` pair was built before the Owner clarified the final product role of APK and LAN.

It is now classified as a disposable transport prototype/reference only.

It must NOT be extended as if:
- APK were only a LAN diagnostics app;
- LAN were only discovery/echo/transport relay;
- success meant reproducing the legacy pilot.

The actual product target is:
- Web + APK as clients of one business/domain platform;
- Cloud Service as normal runtime;
- LAN Service as a real substitute runtime for Internet/Cloud Service failure and forced-LAN cases;
- shared business command/event semantics and dual-runtime reconciliation.

## What the prototype evidence still proves

First build run `34756569016` passed both jobs and therefore proves only these low-level facts:

- GitHub CI can compile/package an Android debug APK in the current repository;
- GitHub CI can publish a portable self-contained win-x64 .NET process;
- no-admin Agent packaging is technically feasible at a basic build level;
- the reviewed low-level discovery/queue/idempotency patterns can be implemented in current source.

This is useful engineering evidence, but it is not counted as completed business APK/LAN Service functionality.

## Retained artifacts/source

Current prototype paths remain temporarily for selective extraction/reference:
- `android-pilot/`
- `lan-agent/`
- `.github/workflows/build-lan-dev.yml`

Do not treat these paths as the final product structure. Any reused component must first be reviewed against `TARGET_PRODUCT_ARCHITECTURE_V2` and moved/rewritten behind the current shared domain/service contracts.

## Next product-level work

The Android/LAN lane continues without pause, but its next work is not "install and polish the test APK".

Next sequence:
1. establish the shared domain/API/offline-reconciliation contract;
2. define the real LAN Service edge database/event/outbox model;
3. define Cloud-direct / LAN-relay / LAN-autonomous runtime states;
4. define offline security/pairing boundaries without inventing unresolved policy;
5. begin real business vertical slices across Cloud + LAN + Web + APK;
6. use legacy/prototype code only where a low-level mechanic is deliberately adopted.
