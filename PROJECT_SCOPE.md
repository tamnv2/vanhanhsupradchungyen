# PROJECT SCOPE — VHDCHY

Status: ACTIVE / CLEAN BASELINE 2026-09-12
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

## Architecture retained

- Cloudflare Worker is the public service layer.
- D1 is canonical structured authority for service-first business data.
- Google Sheets is projection / human-readable / reconciliation / DR surface, not canonical authority.
- Projection uses controlled writer + outbox + idempotency + retry + ACK + checkpoint.
- BETA and STABLE are isolated in provider resources, credentials and runtime data.
- Android/PDA, LAN Agent and LAN Web remain official deliverables.
- LAN uses no-admin/minimum-information assumptions and must not depend on router/firewall/DNS changes.
- Physical LAN evidence currently covers exactly two real Newland MT90; synthetic clients are service-capacity evidence only.

## Current execution mode

Owner is off-site. Continue Service/BETA setup now. Resume physical LAN regression when Owner is at the company; from then on Service and LAN may proceed in parallel when independent.

## Reset rule

The pre-reset repository is evidence/reference only. Source is restored to the active tree only after review against the clean baseline. Old provider IDs, deployment claims and historical branch/tag state never become current authority by inheritance.
