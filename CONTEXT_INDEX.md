# CONTEXT INDEX — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`

GitHub `main` is persistent project authority. Current provider/runtime evidence may prove a GitHub status entry stale and must then be reconciled back to `main`.

## Authority precedence

For product/business/execution decisions read in this order:

1. `DECISIONS.md` — base decisions;
2. `DECISIONS_V3.md` — full LAN Service, direct LAN Google output, long-offline authority and reconciliation overrides;
3. `DECISIONS_V4.md` — no-admin LAN host, canonical LAN domains and BETA/STABLE preparation/promotion overrides;
4. `DECISIONS_V5.md` — recovered effective Owner rules from reviewed approved specs;
5. `DECISIONS_V6.md` — later authentication/account/offline/enterprise/camera and related Owner overrides;
6. `DECISIONS_V7.md` — current UI direction and prior ready-queue execution authority;
7. `DECISIONS_V9.md` — current execution authority: vertical acceptance slices, WIP limit, governance batching, single-source ownership, deterministic progress credits and layered CI.

Newest applicable override wins only where it conflicts. Unaffected older decisions remain active.

**All active decision layers above are mandatory resume reads.** A stale or incomplete `CHECKPOINT.md` must never be used to omit a newer decision layer.

`DECISIONS_V8.md` exists as a redundant historical record of the same 2026-09-14 Owner language/execution instruction and is not a separate required authority layer; effective V8-era rules were consolidated into V7 before V9.

## Current canonical execution guides

- `docs/OWNER_BUSINESS_RULES_V1.md`
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
- `docs/SERVICE_API_CONTRACT_V3.md`
- `docs/LAN_EDGE_STATE_V2.md`
- `docs/DATA_MODEL_GUIDE_V2.md`
- `docs/NON_FUNCTIONAL_BASELINE_V1.md`
- `docs/BETA_ACCEPTANCE_MATRIX.md`
- `docs/DELIVERY_PLAN_V6.md` — current acceptance-first delivery sequencing; no volatile percentage
- `docs/EXECUTION_MODEL_V1.md` — current vertical-slice/WIP/CI/governance operating procedure
- `docs/PROGRESS_TRACKING_V2.md` — current deterministic evidence-credit percentage authority
- `docs/LAN_HOST_DOMAIN_V1.md`
- `docs/RELEASE_PROMOTION_V1.md`
- `docs/LAN_SOURCE_REVIEW_20260913.md` — legacy/current LAN reuse boundary and physical-regression distinction

`docs/DELIVERY_PLAN_V5.md` remains historical/detail reference where not conflicting. `docs/PROGRESS_TRACKING_V1.md`, `docs/DELIVERY_PLAN_V4.md` and older V2 delivery/contract/edge documents remain historical/reference only where a current file above supersedes them.

## Volatile-state ownership

To prevent drift, read volatile truth only from its owner:

- current runtime/product/provider evidence -> `CURRENT_STATE.md`;
- READY/BLOCKED/WIP queue -> `NEXT_ACTIONS.md`;
- current exact/rounded project percentage -> `docs/PROGRESS_TRACKING_V2.md`;
- provider/resource identities and stable boundaries -> `SERVICE_AUTHORITY.md`;
- resume reconciliation SHA -> `CHECKPOINT.md`.

Do not treat a copied percentage/live-state sentence in another file as authority. Such duplicates should be removed when encountered.

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
- `docs/EXECUTION_MODEL_V1.md` for active delivery/WIP selection;
- `docs/PROGRESS_TRACKING_V2.md` when reporting project percentage/position;
- only task-specific current guide/source/evidence.

### FOCUSED

Use for a new/changed task inside active boundaries or when relevant paths changed after the checkpoint. Read FAST plus the relevant subset of:

- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- `docs/DELIVERY_PLAN_V6.md`
- current architecture/contract/data/acceptance guide
- relevant source/config/workflows/provider evidence.

### FULL

Mandatory for Owner full-audit commands, unresolved authority conflicts, cross-lane architecture changes, stale/unreconcilable checkpoint or STABLE promotion.

Read/reconcile all root authority/governance files, all active decision layers, current canonical guides, current source/config/workflows and directly relevant provider evidence. Historical/reference material is read only to resolve an identified discrepancy or deliberate reuse candidate.

## Lane routing

### REPO / GOVERNANCE

Read bootstrap/governance files, all active decisions, `PROJECT_SCOPE.md`, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, Delivery Plan V6, Execution Model V1, Progress V2 and relevant validation evidence.

Execution rule: enforce the V9 WIP limit. One integrating vertical slice plus at most two independent preparation/client lanes may run. Blocked provider/physical nodes do not occupy WIP. Reconcile governance once at meaningful boundaries rather than after every helper/mechanic.

### PRODUCT / SHARED DOMAIN

Read all active decisions, Owner business rules, V3 target architecture, V3 Service contract, data-model guide, non-functional baseline, BETA acceptance matrix, Delivery Plan V6, Execution Model V1 and relevant migrations/source.

Every technical task must map to an acceptance item or mandatory architecture/security/migration/provider/release prerequisite. Do not let stale `docs/SERVICE_API_CONTRACT.md`, `docs/CANONICAL_MUTATION_PLAN.md`, `docs/LAN_EDGE_STATE_V1.md` or older delivery text override current files.

### CLOUDFLARE / D1 / WORKER

Read `SERVICE_AUTHORITY.md`, all active decisions plus current state/next actions, current Service/data guides, `service/worker/src/`, migrations, Cloudflare workflows/scripts/dispatch and latest directly relevant provider/deploy evidence.

Never migrate/replace/recreate D1/Worker from name alone. Verify exact identity/state first.

### GOOGLE GAS / DRIVE / SHEETS

Read `PROJECT_SCOPE.md`, `SERVICE_AUTHORITY.md`, all active decisions, Owner business rules and Service contract, projection config/current gateway source and current Drive/Sheets/provider evidence.

Sheets/Drive are downstream outputs/storage and never business authority. Operational liveness is read from `CURRENT_STATE.md`, not from identity records.

### LAN SERVICE

Read all active decisions, target architecture, V3 Service contract, LAN edge state, LAN host/domain guide, non-functional baseline, Delivery Plan V6, Execution Model V1, Progress V2 and active LAN source/evidence.

Hard constraints: full local Service, portable/no-admin host, no corporate-policy bypass, canonical LAN domains, current offline authority model, controlled Google output when reachable, event/outbox reconciliation to Cloud, explicit conflicts and fail-closed mutation readiness.

Physical company-network/PDA acceptance remains a distinct gate and must not pause independent source/business/client work.

### WEBSITE

Read all active decisions especially V7/V9, Owner business rules, V3 Service contract, non-functional baseline, Execution Model V1 and current Web source/build evidence.

Current rules:
- Online/LAN Web are one shared design system/product;
- DNSHE screenshots provide visual direction only; VHDCHY identity/assets are required;
- LAN-critical UI assets must remain local/offline-capable;
- current UI is **Vietnamese only**;
- Web may advance in Lane B against locked contracts/fixtures without inventing business semantics.

### ANDROID APK / PDA

Read all active decisions especially V7/V9, Owner business rules, V3 Service contract, non-functional baseline, Execution Model V1, current Android source/build evidence and Pick Pack 1291 UI source/artifacts only as authorized reference.

Current rules:
- current UI is **Vietnamese only**;
- do not invent final Pick Pack 1291 screen details until actual source/artifact evidence is surfaced;
- scanner/QR actions use current Service/domain commands, never direct storage/provider writes;
- earlier transport-only prototypes are NON_AUTHORITY except deliberately reviewed/re-adopted mechanics.

### BETA / STABLE / RELEASE PROMOTION

Read all active decisions, `docs/RELEASE_PROMOTION_V1.md`, Delivery Plan V6, Execution Model V1, Progress V2, `SERVICE_AUTHORITY.md` and exact accepted release/provider evidence.

Prepare isolated STABLE during development, keep business traffic fail-closed until explicit Owner promotion approval, promote the exact accepted BETA release, and never copy BETA runtime/business data by default.

## Current progress authority

The exact current percentage, phase credits and evidence delta are defined **only** by `docs/PROGRESS_TRACKING_V2.md`.

Do not copy the percentage into this index. Do not raise progress from plans, chat discussion, mockups, governance-only edits, tool-call volume or legacy evidence alone.

## Current Owner decision gate

One material product-semantic conflict remains open: previous-portrait immediate deletion versus offline/LAN media staging when Drive is unavailable. Do not silently resolve or weaken either rule. Decision-independent durable media work may continue while the actual portrait replacement behavior stays fail-closed.

## Escalation

FAST -> FOCUSED when relevant paths changed after checkpoint, provider identity matters, a new lane/task is introduced or evidence is missing.

Escalate to FULL for authority conflict, unreconcilable checkpoint, cross-cutting architecture change, Owner full audit or STABLE promotion.
