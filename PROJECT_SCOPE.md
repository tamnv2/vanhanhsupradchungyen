# PROJECT SCOPE — VHDCHY

Status: ACTIVE / PRODUCT TARGET RECONCILED V7 2026-09-14
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

`BACKUP PICK PACK 1291` remains NON_AUTHORITY for business/data/runtime logic. `DECISIONS_V7.md` explicitly permits its actual App UI/UX source/artifacts to be used as a visual/interactions reference for the current Android/PDA App.

## Final product deliverables

The VHDCHY product is one platform with four first-class deliverables:

1. **Web product** — Online Web and LAN Web are two runtime modes/surfaces of one shared browser product/design system, not unrelated websites.
2. **Android/PDA App** — PDA-optimized business client using the same domain/API and permission semantics as Web.
3. **Cloud Service/Gateway** — Cloudflare Worker + D1 plus controlled provider integrations as the normal central online runtime.
4. **LAN Service** — full local substitute Service able to execute the same approved warehouse business model locally and serve the LAN Web bundle.

## UI product direction

- Web + App expose Vietnamese / English / Chinese; default English unless a later Owner decision overrides it.
- Android/PDA App UI/UX direction is taken from the actual Pick Pack 1291 App reference, adapted to current VHDCHY workflows/terminology/permissions/data/runtime. Old Pick Pack business logic does not become authority.
- Online Web + LAN Web use the Owner-supplied DNSHE screenshots as visual direction: dark navy navigation, light blue/white spacious backgrounds, white rounded cards, royal-blue primary actions, compact icon/status tiles and a clean enterprise console hierarchy.
- DNSHE branding/proprietary assets are not copied; VHDCHY keeps its own product identity and information architecture.
- Core LAN Web UI assets required during an outage must be available locally and may not depend on public Internet/CDNs.

## LAN Service scope

LAN must support:
- normal local operation when Internet is unavailable but local Wi-Fi/LAN remains usable;
- normal local operation when Cloud Service is unavailable/degraded;
- deliberate forced-LAN routing while Cloud is healthy, controlled by SUPERADMIN/ROOT;
- continuous/opportunistic synchronization to Cloud/D1 whenever Cloud is reachable, even if users remain routed through LAN;
- direct controlled Google Sheets projection and Drive upload whenever Internet/Google is reachable;
- queued/staged approved Google work when Internet/Google is unavailable;
- later Cloud reconciliation from the LAN immutable event journal/outbox rather than reconstruction from Sheets/Drive;
- explicit conflict evidence and ADMIN+ decision for unresolved business/data conflicts;
- ordinary-user/no-admin host operation without bypassing company router/firewall/network policy.

## Offline operation scope

LAN maintains the latest synchronized local authority/configuration state required for approved business operation.

Offline login is not time-expired merely because the outage is long under the current Owner requirement. Remote changes made during a hard partition are necessarily unknown until reconnect; after refresh, new authority/configuration applies to future operations while prior accepted local events remain auditable/reconcilable evidence.

Final continuity acceptance uses the current V6 minute-level target: after warmup and Internet cut while valid LAN connectivity remains, approved App + LAN Web + local workflow must remain usable for **Window 2 >=60 minutes**. Post-restoration synchronization and host-restart/power-loss recovery are separate test sections.

## Data authority

- D1 is the central consolidated structured store after synchronization.
- LAN edge state/event journal is the local operational authority for events accepted during LAN operation.
- Google Sheets remains projection/reconciliation/DR only.
- Google Drive remains media/document/archive storage.
- Google output may be produced by either authorized Cloud or LAN Service but must carry stable identities/receipts to prevent duplicates.
- Web and App never write D1/Sheets/Drive directly around the Service contract.

Detailed architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`.
Current full execution plan: `docs/DELIVERY_PLAN_V5.md`.
Current progress model: `docs/PROGRESS_TRACKING_V1.md`.

## Legacy/reference boundary

- Legacy repository `tamnv2supra/vanhanhdchungyen` is read-only NON_AUTHORITY reference.
- Fixed comparison commit: `7b4488a89f585812c1bccba5d07d86049482bf4c`.
- The pre-clarification transport-only APK/Agent prototype is NON_AUTHORITY reference only.
- No legacy schema/data/UI/protocol becomes current product authority by inheritance.
- Only explicitly reviewed/re-adopted mechanics or Owner-authorized reference use may enter the current product.

## Current execution mode

Build shared domain core, Cloud Service, LAN Service, Online/LAN Web, Android/PDA App and Google integration in dependency-aware parallel lanes.

Unavailable final company-network hardware may delay physical regression only; it must not pause independent source/contracts/build/UI work.

Current progress/position is recorded in `CURRENT_STATE.md`; current evidence-weighted baseline is 53.3% / displayed 53% as of 2026-09-14.

## Reset rule

The pre-reset repository is evidence/reference only. Source is restored/reused only after review against current authority. Historical behavior never becomes current product authority automatically.