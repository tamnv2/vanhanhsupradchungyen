# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`
Product architecture: `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`
Execution plan: `docs/DELIVERY_PLAN_V2.md`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- `AI_AUTHORITY_RESUME_V2` and D-041 non-stop/parallel execution remain active.
- Owner clarified final product scope on 2026-09-13: Website + Android APK + Cloud Service + LAN Service are four first-class deliverables built toward one system.
- APK is a PDA-optimized client of the same business/domain contract as Web, not a LAN diagnostics app.
- LAN Service must substitute for Cloud Service during site Internet loss, Cloud Service failure/degradation, and support per-client forced-LAN routing.
- Legacy repo `tamnv2supra/vanhanhdchungyen@7b4488a89f585812c1bccba5d07d86049482bf4c` remains NON_AUTHORITY reference only.
- The transport-only APK/Agent prototype created immediately before this clarification is also NON_AUTHORITY/disposable reference. It must not define product direction.
- Secret values remain outside repository source/chat.

## Product runtime target

Normal mode:

`Web/APK -> Cloudflare Worker -> D1 -> Google Gateway -> Sheets/Drive`

LAN modes:

- `LAN_RELAY`: Web/APK -> LAN Service -> Cloud Service/D1 when Cloud is reachable; used especially for per-client forced-LAN/direct-path problems.
- `LAN_AUTONOMOUS`: Web/APK -> LAN Service -> local edge DB/event journal/sync outbox when Cloud/upstream is unavailable; later reconcile to D1.
- `LOCAL_QUEUE_ONLY`: eligible command retained on the client when neither Cloud nor LAN can accept it.

D1 remains global canonical authority after normal commit/reconciliation. LAN autonomous edge events are durable business facts pending reconciliation, not mere transport receipts. Split-brain conflicts must be explicit; silent overwrite/drop is prohibited.

## Shared-domain requirement

Cloud and LAN may not become two independently invented backends.

Target implementation boundary:
- provider-neutral domain command/event validation and state-transition core;
- Cloud D1 adapter;
- LAN edge persistence/reconciliation adapter;
- identical business test vectors/machine error semantics across both.

Exact LAN packaging/runtime technology is not Owner-locked; it must be selected based on no-admin compatibility/footprint and must not change business semantics.

## Google / Drive / Sheets / GAS

- Current Google authority: `tam95.supra@gmail.com`.
- Project root: `VẬN HÀNH DC HƯNG YÊN` with `01_BETA` and `02_STABLE`.
- BETA cluster: `PICK_PACK_1291`.
- BETA workbook: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`.
- Workbook schema: `PP1291_SHEETS_BETA_V1`; projection remains `PROVISIONED_NOT_LIVE`.
- GAS managed deployment immutable version `3`, deployment run `34753872034`: PASS.
- Projection Gateway remains fail-closed until secure auth + sender/ACK/retry acceptance.
- During LAN autonomous mode, Sheets/Drive remain deferred/downstream. LAN must not use Sheets as a fallback database.
- Offline files may be staged with hashes/metadata; current document `DRAFT -> FINAL` durable-Drive rule remains unchanged until Owner explicitly changes it.

## Cloudflare D1 BETA — BUSINESS_CORE_V3 PASS

- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- D1: `vhdchy-data-beta`.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- Provider schema: `business_core_v3`.
- Fresh read-only verification run `34754968801`: SUCCESS.
- Required schema/seeds/integrity verified; business rows remained zero at verification.

## Cloudflare Worker BETA — FOUNDATION LIVE / BUSINESS FAIL-CLOSED

- Worker: `vhdchy-beta`.
- Public origin: `https://beta.supra.cc.cd`.
- `workers.dev`: disabled.
- Existing runtime health/meta/capability foundation is live.
- Protected business/admin APIs remain fail-closed.

### Worker packaging blocker

- `service/worker/deploy.beta.json` declares reviewed modules: `index.js`, `auth.js`, `auth-service.js`, `authorization.js`, `session.js`, `permission-store.js`, `projection.js`.
- Manifest validation `34754738074`: SUCCESS.
- Active provider-mutating deploy workflow still uploads only `index.js`; do not deploy until multi-module workflow support is safely updated/verified.
- Connected write-safety currently blocks that sensitive workflow/source mutation path. Do not bypass the guard through lower-level APIs.

## Cloud Service source foundation

Already present/tested at source level:
- password/hash/bearer/TOTP primitives;
- session expiry/revocation/device-security-epoch checks;
- scoped role/direct permission evaluation with DENY precedence;
- non-ROOT login/session issuance;
- ROOT password-only fail-closed to `ROOT_MFA_REQUIRED`;
- projection outbox read/retry/dead-letter primitives;
- shared Service API contract;
- canonical mutation design.

Not yet runtime-live:
- integrated protected routes;
- reusable canonical mutation helper;
- business APIs;
- secure projection sender/auth;
- Drive flow;
- Web business UI.

## Android / LAN clarification state

### Prototype evidence retained, product authority removed

First transport-only paired build run `34756569016` was green and proves basic CI/package/no-admin feasibility only.

Prototype paths:
- `android-pilot/`
- `lan-agent/`
- `.github/workflows/build-lan-dev.yml`

These paths are temporarily retained for selective low-level extraction/reference. They are not the final product structure and must not be extended as the business APK/LAN Service without review/rework against V2 architecture.

### Active product-level Android/LAN work

Current work is now Phase 1 from `DELIVERY_PLAN_V2`:
1. shared domain/API contract suitable for both Cloud and LAN;
2. exact offline/reconciliation envelope and commit-status model;
3. real LAN edge DB/event/outbox/snapshot design;
4. Cloud-direct / LAN-relay / LAN-autonomous endpoint-state model;
5. security/pairing boundary design without inventing unresolved offline-auth policy;
6. then real vertical business slices across Cloud + LAN + Web + APK.

## Important unresolved policy — do not invent

The Owner has not yet locked:
- exact offline authentication/capability mechanism;
- exact offline auth TTL/staleness limit;
- which privileged security/admin operations are allowed in autonomous LAN mode;
- who may explicitly enter/exit emergency autonomous mode.

These must remain explicit TBD/fail-closed boundaries, not assumptions hidden in code.

## Current exact project position

Infrastructure/provider foundations are substantially prepared. The project is now correcting the implementation shape before deeper business coding:

```text
Cloud/D1/Google foundation
        |
        +--> NOW: shared domain + dual-runtime Service/LAN contract
                 |
          +------+------+
          |             |
     Cloud runtime   LAN runtime
          |             |
          +------+------+
                 |
        vertical business slices
          /      |       \
        Web     APK   Sheets/Drive
                 |
        failover/reconciliation
                 |
            BETA acceptance
```

The next milestone is NOT installing/polishing the transport test APK. It is establishing the shared business core/contracts so Cloud Service and LAN Service can be built simultaneously without divergence.

## Physical dependencies / STABLE

- Android product source/build work is active now; final PDA UX/physical behavior can be tested as product slices become installable.
- Final corporate-network/no-admin LAN regression still requires the intended company laptop/network/PDA environment.
- Lack of that final environment blocks only physical evidence, not source/contracts/build work.
- STABLE remains blocked until full BETA product acceptance + explicit Owner approval.
