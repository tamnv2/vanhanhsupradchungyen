# PROJECT SCOPE — VHDCHY

Status: ACTIVE / PRODUCT TARGET RECONCILED 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## Scope

`VẬN HÀNH DC HƯNG YÊN` is the platform scope. `PICK_PACK_1291` is the first operational cluster/module and does not define the universe of DC-wide business types.

## Current identities

- Google Drive / Sheets / GAS: `tam95.supra@gmail.com`
- Cloudflare: `nguyenvantam050595@gmail.com`
- GitHub user: `tamnv2`
- Source repository: `tamnv2/vanhanhsupradchungyen`
- `automation@supra.cc.cd`: suspended recovery candidate; not current authority

## Drive boundary

Project root: `VẬN HÀNH DC HƯNG YÊN` — `19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`

Active roots:
- BETA `01_BETA` — `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`
- STABLE `02_STABLE` — `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI`

BETA cluster:
- `01_BETA/01_CLUSTERS/PICK_PACK_1291`
- ID `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`

Reference-only siblings:
- `BACKUP PICK PACK 1291` — `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`
- `VHDCHY_LEGACY_SETUP_20260908` — `1NysNtmsMAxFA5JgsYwEJNgMVvbogvf9R`

## Final product deliverables

The VHDCHY product is developed as one platform with four first-class deliverables:

1. Website — full browser business/administration client.
2. Android APK — PDA-optimized client using the same domain/API and permission model as Web.
3. Cloud Service — Cloudflare Worker + D1 as the normal online service runtime.
4. LAN Service — local substitute runtime for approved business continuity when Internet/Cloud Service is unavailable, plus a local relay/front door for forced-LAN clients when Cloud remains reachable.

Google Sheets and Google Drive remain downstream storage/projection integrations, not separate business authorities.

## Architecture retained and expanded

- D1 is the global canonical structured authority after normal Cloud commit/reconciliation.
- Google Sheets is projection / human-readable / reconciliation / DR surface, never canonical authority.
- Projection uses controlled writer + outbox + idempotency + retry + ACK + checkpoint.
- BETA and STABLE are isolated in provider resources, credentials and runtime data.
- Web and APK use one business/domain contract; APK is a compact PDA-oriented surface rather than a separate application logic branch.
- Cloud and LAN must share one business command/event model and acceptance semantics.
- LAN must work without admin/router/firewall/internal-DNS dependency.
- LAN autonomous mode uses a local edge state/event/outbox model and later reconciles to D1; conflicts are explicit and never silently overwritten.
- During Internet/Cloud loss, Sheets/Drive work is deferred rather than becoming a parallel authority.
- Physical LAN evidence currently covers exactly two real Newland MT90 from the legacy reference; synthetic clients are capacity evidence only and legacy evidence is not current-product PASS.

Detailed authority: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`.
Execution plan: `docs/DELIVERY_PLAN_V2.md`.

## Legacy/reference boundary

- Legacy repository `tamnv2supra/vanhanhdchungyen` is read-only NON_AUTHORITY reference.
- Fixed comparison commit: `7b4488a89f585812c1bccba5d07d86049482bf4c`.
- The pre-clarification transport-only APK/Agent prototype in the current repo is also NON_AUTHORITY and may contribute only deliberately re-adopted low-level mechanics.
- No legacy schema/data/UI/protocol becomes current product authority by inheritance.

## Current execution mode

Build Service, LAN Service, Web and APK in parallel by shared vertical business slices rather than completing disconnected products sequentially.

Unavailable final company-network hardware may delay physical regression only. It must not pause source/contracts/build work in Android/LAN or independent Cloud/Web/Google work.

## Reset rule

The pre-reset repository is evidence/reference only. Source is restored or reused in the active tree only after review against current authority. Old provider IDs, deployment claims, historical branch/tag state and legacy behavior never become current authority by inheritance.
