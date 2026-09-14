# SERVICE AUTHORITY

Status: ACTIVE / OWNER RECONCILED 2026-09-14
Baseline: `REPO-RESET-20260912-01`

This file records known service/provider identities and authority boundaries. Provider facts below are the latest project-recorded verified state; **live-check the exact provider state before any provider mutation/deploy**.

## GitHub

- Repo: `tamnv2/vanhanhsupradchungyen`
- Visibility: PUBLIC
- Current managing identity: `tamnv2` / `nguyenvantam050595@gmail.com`
- Default/authority branch: `main`
- Pre-zero snapshot: `backup/pre-zero-20260912`

GitHub `main` is persistent project authority for decisions/source/checkpoints. Provider runtime evidence may prove a status entry stale and must then be reconciled back to `main`.

## Google

Current authorized owner/operator for project Drive / Sheets / GAS: `tam95.supra@gmail.com`.

The separate domain Google account previously used/attempted for automation is not current project Drive/Sheets/GAS authority while unavailable. Its future recovery does not automatically transfer authority or data; any later transfer must be explicit and reviewed.

Current BETA projection workbook recorded by the project:

- Title: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`
- ID: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`
- Schema: `PP1291_SHEETS_BETA_V1`
- Status: `PROVISIONED_NOT_LIVE`

Current recorded Google runtime scopes:

- `https://www.googleapis.com/auth/spreadsheets`
- `https://www.googleapis.com/auth/userinfo.email`

Current recorded CI OAuth scopes:

- `https://www.googleapis.com/auth/script.projects`
- `https://www.googleapis.com/auth/script.deployments`

Google Cloud / OAuth BETA recorded state:

- Standard Cloud project: `VHDCHY-BETA`
- OAuth app: External / In production
- OAuth owner/operator: `tam95.supra@gmail.com`
- CI client: `VHDCHY BETA CI`
- Last recorded status: `PASS`

Google Apps Script BETA recorded authority:

- Script title: `VHDCHY BETA - Google Gateway`
- Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`
- Canonical managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`
- Canonical Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`
- Current recorded immutable version: `3`
- Source authority: `service/google-gateway/`
- Projection protocol: `VHDCHY_PROJECTION_V1`
- Projection write state: `FAIL_CLOSED / NOT_LIVE`

Sheets/Drive are projection/file-storage outputs, **never canonical business authority**. V3 requires stable identities/receipts so Cloud and LAN controlled output paths do not duplicate rows/files.

## Cloudflare

Managing email: `nguyenvantam050595@gmail.com`.
Intended zone/domain: `supra.cc.cd`.

Latest project-recorded BETA account/resource authority:

- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`
- API token: stored only in GitHub Environment `beta`, never source/chat
- BETA Worker: `vhdchy-beta`
- BETA origin: `https://beta.supra.cc.cd`
- BETA Worker `workers.dev`: disabled in last recorded verification
- BETA D1: `vhdchy-data-beta`
- BETA D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`
- D1 schema marker: `business_core_v3`

Recorded verification history includes guarded D1 migration/provider inspection and guarded Worker deployment/post-deploy inspection. Before new provider mutations, verify exact current identity/bindings/schema/routing rather than relying only on this historical record.

D1 remains the central consolidated canonical business store after synchronization under current architecture. LAN local authority accepted during local operation reconciles into this model; Sheets/Drive do not replace it.

Expected STABLE names remain reserved/preparatory until final promotion:

- STABLE Worker: `vhdchy-stable`
- STABLE D1: `vhdchy-data-stable`

STABLE business activation remains blocked until mandatory BETA acceptance plus explicit Owner promotion approval. Promotion must use the exact accepted BETA release/artifacts and must not copy BETA runtime/business data by default.

## LAN Service authority model — V3/V4/V6 current

Current LAN product authority is defined by the latest applicable decisions and these current guides:

- `DECISIONS_V3.md` through `DECISIONS_V7.md`;
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`;
- `docs/SERVICE_API_CONTRACT_V3.md`;
- `docs/LAN_EDGE_STATE_V2.md` plus later current source/evidence;
- `docs/LAN_HOST_DOMAIN_V1.md`;
- `docs/DELIVERY_PLAN_V5.md`.

The former V2 `LAN_RELAY` / `LAN_AUTONOMOUS` framing is historical and must not override V3.

Current authority:

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

Current source has materially progressed beyond the old transport prototype: local operational state materialization, replay/actor evidence, employee/MNV/attendance Slice-1 behavior, atomic active-code uniqueness and durable staged-media primitives have automated evidence. Public LAN business mutations remain fail-closed until current readiness/auth/domain coverage is accepted.

Physical company-network/PDA validation is still separate and pending for the current product path.

## Offline authority/security — current V6 direction

The former unresolved duration-only offline TTL gate is obsolete. Current offline behavior uses the latest synchronized local authority snapshot under the Owner-approved availability/security tradeoff; remote authority changes made during disconnection cannot be known locally until reconnect/refresh.

This does **not** remove security requirements:

- current authenticated client <-> LAN channel remains required;
- LAN pairing/auth/security epoch and permission enforcement must be completed before broader public business mutation exposure;
- the authority-snapshot version accepting an operation must remain auditable;
- privileged/security operations keep their current explicit permission/recovery boundaries;
- only allowed retry-safe client work may exist as client-only queue.

Canonical continuity acceptance is minute-level: Window 2 must sustain approved local workflow for **>=60 minutes** after warmup and Internet cut while valid LAN connectivity remains. Post-restoration synchronization is a separate acceptance section; host restart/power loss is also separate.

## Website authority — V7

- Online Web and LAN Web are **one product with one shared design-system/artifact direction**, not unrelated interfaces.
- Both consume the same current Service/domain semantics.
- V7 visual direction comes from the Owner-supplied DNSHE screenshots: dark navy navigation, light blue/white field, white rounded cards, royal-blue CTA and clean enterprise-console hierarchy.
- DNSHE branding/proprietary assets must not be copied.
- LAN-critical fonts/icons/scripts/styles/images must be locally available so the shell remains usable without Internet.
- Network/sync/queue state must be visible where it affects behavior.
- V5 language rule remains Vietnamese / English / Chinese, default English.

## Android/PDA authority — V7

- Android/PDA App is the compact operational client of the same current business/domain contract.
- V7 authorizes Pick Pack 1291 as **UI/UX reference only**, adapted to VHDCHY.
- Pick Pack business logic/data/credentials/runtime architecture do not become VHDCHY authority.
- Exact visual details must be based on actual accessible reference source/artifacts, not invented.
- Scanner/QR actions must invoke current domain commands rather than bypass Service rules.
- Reviewed legacy transport mechanics such as discovery/cache/hysteresis, durable queue/device sequence, ACK deletion and reconnect/resync may be deliberately re-adopted after adaptation to current contracts.

## Android signing

Signing material remains sensitive. Do not commit keystore contents or passwords. Do not use retired Pick Pack legacy signing material merely because Pick Pack is now a UI reference.

Current final App signing/release acceptance must use the reviewed VHDCHY signing path and exact accepted artifact.

## Current unresolved product-semantic gate

Portrait replacement remains fail-closed where current Owner rules conflict:

- previous portrait deletion is required immediately by one current rule;
- LAN/offline media semantics allow staging when Drive is unavailable.

Durable media infrastructure may proceed, but do not decide offline remote-portrait replacement behavior without explicit Owner authority.

## Secrets

Secret values live only in provider secret stores / GitHub Environments or reviewed local secure storage when LAN security is implemented. Repository files may contain only secret names, non-secret resource IDs, verification state and non-sensitive policy.