# SERVICE AUTHORITY

Status: ACTIVE / OWNER RECONCILED V9 2026-09-15
Baseline: `REPO-RESET-20260912-01`

This file owns **provider/resource identities and stable authority boundaries**. It intentionally does not own volatile provider liveness, current deployment PASS/FAIL or current projection queue state. Read those only from `CURRENT_STATE.md` and exact provider evidence before mutation/deploy.

## GitHub

- Repo: `tamnv2/vanhanhsupradchungyen`
- Visibility: PUBLIC
- Current managing identity: `tamnv2` / `nguyenvantam050595@gmail.com`
- Default/authority branch: `main`
- Pre-zero snapshot: `backup/pre-zero-20260912`

GitHub `main` is persistent project authority for decisions/source/checkpoints. Provider runtime evidence may prove a status entry stale and must then be reconciled into `CURRENT_STATE.md` without duplicating volatile status here.

## Google

Current authorized owner/operator for project Drive / Sheets / GAS: `tam95.supra@gmail.com`.

The separate domain Google account previously used/attempted for automation is not current project Drive/Sheets/GAS authority while unavailable. Its future recovery does not automatically transfer authority or data; any later transfer must be explicit and reviewed.

BETA projection workbook identity:

- Title: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`
- ID: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`
- Schema: `PP1291_SHEETS_BETA_V1`
- Operational/live status: **see `CURRENT_STATE.md`**.

Approved Google runtime scopes:

- `https://www.googleapis.com/auth/spreadsheets`
- `https://www.googleapis.com/auth/userinfo.email`

Approved CI OAuth scopes:

- `https://www.googleapis.com/auth/script.projects`
- `https://www.googleapis.com/auth/script.deployments`

Google Cloud / OAuth BETA resource identities:

- Standard Cloud project: `VHDCHY-BETA`
- OAuth app: External / In production
- OAuth owner/operator: `tam95.supra@gmail.com`
- CI client: `VHDCHY BETA CI`

Google Apps Script BETA resource authority:

- Script title: `VHDCHY BETA - Google Gateway`
- Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`
- Canonical managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`
- Canonical Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`
- Source authority: `service/google-gateway/`
- Projection protocol: `VHDCHY_PROJECTION_V1`
- Current deployment version/write/liveness evidence: **see `CURRENT_STATE.md` and exact provider evidence**.

Sheets/Drive are projection/file-storage outputs, **never canonical business authority**. V3 requires stable identities/receipts so Cloud and LAN controlled output paths do not duplicate rows/files.

## Cloudflare

Managing email: `nguyenvantam050595@gmail.com`.
Intended zone/domain: `supra.cc.cd`.

BETA resource identities:

- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`
- API token: stored only in GitHub Environment `beta`, never source/chat
- BETA Worker: `vhdchy-beta`
- BETA origin: `https://beta.supra.cc.cd`
- BETA D1: `vhdchy-data-beta`
- BETA D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`
- D1 schema marker: `business_core_v3`

Before any provider mutation, verify exact current bindings/schema/routing/deployment state rather than relying on the identity list alone. Operational results belong in `CURRENT_STATE.md`.

D1 remains the central consolidated canonical business store after synchronization. LAN local authority accepted during local operation reconciles into this model; Sheets/Drive do not replace it.

Expected STABLE resource names remain reserved/preparatory until final promotion:

- STABLE Worker: `vhdchy-stable`
- STABLE D1: `vhdchy-data-stable`

STABLE business activation remains blocked until mandatory BETA acceptance plus explicit Owner promotion approval. Promotion must use the exact accepted BETA release/artifacts and must not copy BETA runtime/business data by default.

## LAN Service authority model — V3/V4/V6/V9 current

Current LAN product authority is defined by the latest applicable decisions and current guides including:

- `DECISIONS_V3.md` through active `DECISIONS_V7.md` plus `DECISIONS_V9.md`;
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`;
- `docs/SERVICE_API_CONTRACT_V3.md`;
- `docs/LAN_EDGE_STATE_V2.md`;
- `docs/LAN_HOST_DOMAIN_V1.md`;
- `docs/DELIVERY_PLAN_V5.md`;
- `docs/EXECUTION_MODEL_V1.md`.

The former V2 `LAN_RELAY` / `LAN_AUTONOMOUS` framing is historical and must not override V3.

Stable authority boundaries:

- LAN Service is a **full first-class local Service substitute**, not merely a transport relay.
- Cloud/LAN share one business command/event meaning and may use different runtime/persistence adapters only.
- While routed through LAN, LAN may accept approved business operations durably into local current state + immutable event/outbox under the current authority snapshot.
- If Cloud is reachable, LAN synchronization may run continuously/opportunistically even while users remain routed through LAN.
- If Google is reachable, LAN may perform controlled approved Sheets projection/Drive upload with stable identities/receipts.
- If Internet/Google is unavailable, approved Google work/media may be queued/staged locally.
- Cloud synchronization consumes local events/outbox, never Sheets/Drive as source authority.
- Conflicts remain explicit evidence; silent last-write-wins/drop is forbidden.
- LAN must not use Sheets as a fallback database.
- Host requirement remains ordinary-user/no-admin; do not bypass corporate router/firewall/network policy.

Current source/CI/physical completion of these mechanics is volatile and belongs in `CURRENT_STATE.md` and `docs/PROGRESS_TRACKING_V2.md`.

## Offline authority/security

The former unresolved duration-only offline TTL gate is obsolete. Current offline behavior uses the latest synchronized local authority snapshot under the Owner-approved availability/security tradeoff; remote authority changes made during disconnection cannot be known locally until reconnect/refresh.

This does **not** remove security requirements:

- current authenticated client <-> LAN channel remains required;
- LAN pairing/auth/security epoch and permission enforcement are mandatory before affected public business mutation exposure;
- the authority-snapshot version accepting an operation must remain auditable;
- privileged/security operations keep their explicit permission/recovery boundaries;
- only allowed retry-safe client work may exist as client-only queue.

Canonical continuity acceptance is minute-level: Window 2 must sustain approved local workflow for **>=60 minutes** after warmup and Internet cut while valid LAN connectivity remains. Post-restoration synchronization is a separate acceptance section; host restart/power loss is also separate.

## Website authority — V7/V9

- Online Web and LAN Web are **one product with one shared design-system/artifact direction**, not unrelated interfaces.
- Both consume the same current Service/domain semantics.
- V7 visual direction comes from the Owner-supplied DNSHE screenshots without copying DNSHE branding/proprietary assets.
- LAN-critical fonts/icons/scripts/styles/images must be locally available so the shell remains usable without Internet.
- Network/sync/queue state must be visible where it affects behavior.
- Current product language is Vietnamese only; multilingual support is deferred.
- V9 permits Web client work to advance in its independent WIP lane against locked contracts/fixtures without inventing business semantics.

## Android/PDA authority — V7/V9

- Android/PDA App is the compact operational client of the same current business/domain contract.
- Pick Pack 1291 is **UI/UX reference only**, adapted to VHDCHY.
- Pick Pack business logic/data/credentials/runtime architecture do not become VHDCHY authority.
- Exact visual details must be based on accessible reference source/artifacts, not invented.
- Scanner/QR actions invoke current domain commands rather than bypassing Service rules.
- Reviewed legacy transport mechanics may be deliberately re-adopted only after adaptation to current contracts.
- Current App UI language is Vietnamese only.

## Android signing

Signing material remains sensitive. Do not commit keystore contents or passwords. Do not use retired Pick Pack legacy signing material merely because Pick Pack is a UI reference.

Final App signing/release acceptance must use the reviewed VHDCHY signing path and exact accepted artifact.

## Current unresolved product-semantic gate

Portrait replacement remains fail-closed where current Owner rules conflict:

- previous portrait deletion is required immediately by one current rule;
- LAN/offline media semantics allow staging when Drive is unavailable.

Durable media infrastructure may proceed, but do not decide offline remote-portrait replacement behavior without explicit Owner authority.

## Secrets

Secret values live only in provider secret stores / GitHub Environments or reviewed local secure storage when LAN security is implemented. Repository files may contain only secret names, non-secret resource IDs, verification references and non-sensitive policy.
