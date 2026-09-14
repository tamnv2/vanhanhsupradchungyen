# CONTEXT INDEX — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`

GitHub `main` is persistent project authority. Current provider/runtime evidence may prove a GitHub status entry stale and must then be reconciled back to `main`.

## Authority precedence

For product/business decisions read in this order:

1. `DECISIONS.md` — base decisions;
2. `DECISIONS_V3.md` — full LAN Service, direct LAN Google output, long-offline authority and reconciliation overrides;
3. `DECISIONS_V4.md` — no-admin LAN host, canonical LAN domains and BETA/STABLE preparation/promotion overrides;
4. `DECISIONS_V5.md` — recovered effective Owner rules from reviewed approved specs;
5. `DECISIONS_V6.md` — later authentication/account/offline/enterprise/camera and related Owner overrides;
6. `DECISIONS_V7.md` — App UI uses Pick Pack 1291 UI/UX direction as reference; Online Web + LAN Web use the Owner-supplied DNSHE visual direction with VHDCHY identity and offline-safe shared design-system rules.

Newest applicable override wins only where it conflicts. Unaffected older decisions remain active.

**All active decision layers above are mandatory resume reads.** A stale or incomplete `CHECKPOINT.md` must never be used to omit a newer decision layer.

## Current canonical execution guides

- `docs/OWNER_BUSINESS_RULES_V1.md`
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
- `docs/SERVICE_API_CONTRACT_V3.md`
- `docs/LAN_EDGE_STATE_V2.md`
- `docs/DATA_MODEL_GUIDE_V2.md`
- `docs/NON_FUNCTIONAL_BASELINE_V1.md`
- `docs/BETA_ACCEPTANCE_MATRIX.md`
- `docs/DELIVERY_PLAN_V5.md` — full start-to-production plan and current phase mapping
- `docs/PROGRESS_TRACKING_V1.md` — evidence-weighted project percentage model
- `docs/LAN_HOST_DOMAIN_V1.md`
- `docs/RELEASE_PROMOTION_V1.md`
- `docs/LAN_SOURCE_REVIEW_20260913.md` — legacy/current LAN reuse boundary and physical-regression distinction

`docs/DELIVERY_PLAN_V4.md` and older V2 delivery/contract/edge documents remain historical/reference only where a current file above supersedes them.

## Freshness rule

Before selecting FAST/FOCUSED/FULL, compare current GitHub `main` HEAD with `CHECKPOINT.md.reconciled_through_commit`.

- If they match for the affected lanes, FAST may continue.
- If they differ, inspect changed paths/commits first.
- Any changed `DECISIONS*`, `CONTEXT_INDEX.md`, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, `SERVICE_AUTHORITY.md`, active canonical guide, source/config/workflow or provider-evidence path relevant to active work requires reconciliation before mutation.
- An outdated checkpoint is never authority over newer `main`.

## Read modes

### FAST

Read:

- `AI_ENTRYPOINT.md`
- `AI_OPERATING_CONTRACT.md`
- `CHECKPOINT.md`
- this index
- **all active decision layers listed under Authority precedence**
- `docs/PROGRESS_TRACKING_V1.md` when reporting project percentage/position
- only task-specific current guide/source/evidence.

### FOCUSED

Use for a new/changed task inside active boundaries or when relevant paths changed after the checkpoint. Read FAST plus the relevant subset of:

- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- `docs/DELIVERY_PLAN_V5.md`
- current architecture/contract/data/acceptance guide
- relevant source/config/workflows/provider evidence.

### FULL

Mandatory for Owner full-audit commands, unresolved authority conflicts, cross-lane architecture changes, stale/unreconcilable checkpoint or STABLE promotion.

Read/reconcile all root authority/governance files, all active decision layers, current canonical guides, current source/config/workflows and directly relevant provider evidence. Historical/reference material is read only to resolve an identified discrepancy or deliberate reuse candidate.

## Lane routing

### REPO / GOVERNANCE

Read:

- bootstrap/governance files;
- all active `DECISIONS*`;
- `PROJECT_SCOPE.md`;
- `CURRENT_STATE.md`;
- `NEXT_ACTIONS.md`;
- `docs/DELIVERY_PLAN_V5.md`;
- `docs/PROGRESS_TRACKING_V1.md`;
- relevant validation workflows/evidence.

### PRODUCT / SHARED DOMAIN

Read:

- all active decisions;
- Owner business rules;
- V3 target architecture;
- V3 Service contract;
- data-model guide;
- non-functional baseline;
- current BETA acceptance matrix;
- Delivery Plan V5;
- relevant migrations/source.

Do not let stale `docs/SERVICE_API_CONTRACT.md`, `docs/CANONICAL_MUTATION_PLAN.md`, `docs/LAN_EDGE_STATE_V1.md` or older delivery text override the current files.

### CLOUDFLARE / D1 / WORKER

Read:

- `SERVICE_AUTHORITY.md`;
- all active decisions plus current state/next actions;
- current Service/data guides;
- `service/worker/src/` and migrations;
- Cloudflare workflows/scripts/dispatch;
- latest directly relevant provider/deploy evidence.

Never migrate/replace/recreate D1/Worker from name alone. Verify exact identity/state first.

### GOOGLE GAS / DRIVE / SHEETS

Read:

- `PROJECT_SCOPE.md`;
- `SERVICE_AUTHORITY.md`;
- all active decisions;
- Owner business rules and Service contract;
- projection config/current gateway source;
- current Drive/Sheets/provider evidence.

Sheets/Drive are downstream outputs/storage and never business authority.

### LAN SERVICE

Read:

- all active decisions;
- target architecture;
- V3 Service contract;
- LAN edge state;
- LAN host/domain guide;
- non-functional baseline;
- Delivery Plan V5 / progress model;
- active LAN source/evidence, including current Slice-1/materialization/media tests and latest physical evidence.

Hard constraints: full local Service, portable/no-admin host, no corporate-policy bypass, canonical LAN domains, current offline authority model, controlled Google output when reachable, event/outbox reconciliation to Cloud, explicit conflicts and fail-closed mutation readiness.

CI/source evidence and physical company-network/PDA evidence remain distinct.

### WEBSITE

Read:

- all active decisions, especially V5/V6/V7;
- Owner business rules;
- V3 Service contract;
- non-functional baseline;
- Delivery Plan V5;
- current Web source/build evidence.

V7 rules: Online/LAN Web are one shared design system/product; DNSHE screenshots provide visual direction only; VHDCHY identity/assets must be used; LAN-critical UI assets must remain local/offline-capable.

### ANDROID APK / PDA

Read:

- all active decisions, especially V5/V6/V7;
- Owner business rules;
- V3 Service contract;
- non-functional baseline;
- Delivery Plan V5;
- current Android source/build evidence;
- Pick Pack 1291 UI source/artifacts only as an authorized reference, never business/runtime authority.

The earlier transport-only Android prototype and legacy LAN repository are NON_AUTHORITY except for deliberately reviewed/re-adopted low-level mechanics.

### BETA / STABLE / RELEASE PROMOTION

Read:

- all active decisions;
- `docs/RELEASE_PROMOTION_V1.md`;
- Delivery Plan V5;
- progress model;
- `SERVICE_AUTHORITY.md`;
- exact accepted release/provider evidence.

Prepare isolated STABLE during development, keep business traffic fail-closed until explicit Owner promotion approval, promote the exact accepted BETA release, and never copy BETA runtime/business data by default.

## Current progress authority

The current baseline is defined by `docs/PROGRESS_TRACKING_V1.md`: **53.3% exact / 53% displayed** as of 2026-09-14. `CURRENT_STATE.md` and `docs/DELIVERY_PLAN_V5.md` must be reconciled whenever this percentage changes.

Do not raise progress from plans, chat discussion, mockups, or legacy evidence alone. Runtime/test/physical/provider evidence must match the acceptance level required by the relevant phase.

## Current Owner decision gate

One material product-semantic conflict remains open: previous-portrait immediate deletion versus offline/LAN media staging when Drive is unavailable. Do not silently resolve or weaken either rule. Decision-independent durable media work may continue while the actual portrait replacement behavior stays fail-closed.

## Escalation

FAST -> FOCUSED when relevant paths changed after checkpoint, provider identity matters, a new lane/task is introduced or evidence is missing.

Escalate to FULL for authority conflict, unreconcilable checkpoint, cross-cutting architecture change, Owner full audit or STABLE promotion.