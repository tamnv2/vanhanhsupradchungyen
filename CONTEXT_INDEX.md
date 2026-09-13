# CONTEXT INDEX — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`

Use this file to minimize reads while preserving correctness. GitHub `main` is the persistent project authority. Current provider/runtime evidence may show that a GitHub status entry needs reconciliation.

## Authority precedence inside GitHub

For product/business decisions read in this order:
1. `DECISIONS.md` — base decisions;
2. `DECISIONS_V3.md` — active overrides for full LAN Service, direct LAN Google output, offline authority and reconciliation;
3. `DECISIONS_V4.md` — active overrides for no-admin LAN host, canonical LAN domains and BETA/STABLE preparation/promotion.

Newest applicable override wins only where it conflicts; unaffected older decisions remain active.

Active architecture/execution:
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
- `docs/DELIVERY_PLAN_V3.md`
- `docs/PROJECT_KNOWLEDGE_AUDIT_20260913.md`

V2 architecture/delivery files are historical superseded references, not active product authority.

## Read modes

### FAST
Use for normal resume when `CHECKPOINT.md` is current and the requested work remains inside recorded scope.

Always read:
- `AI_ENTRYPOINT.md`
- `AI_OPERATING_CONTRACT.md`
- `CHECKPOINT.md`
- this index
- the active decision override file(s) named by the checkpoint
- only task-specific lane files/evidence.

### FOCUSED
Use for a new/changed task inside active project boundaries.

Read FAST plus the relevant subset of:
- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- active decision files
- V3 architecture/delivery
- relevant source/config/workflows/provider evidence.

### FULL
Mandatory for full-audit commands, authority conflicts, cross-lane architecture changes, stale/unreconcilable checkpoint or STABLE promotion.

Read/reconcile all root authority/governance files, active V3/V4 decision layers, active architecture/contract/data/source/config/workflows and current execution/provider evidence. Historical/reference material is read only where needed to resolve a specific dependency/discrepancy.

## Lane routing

### REPO / GOVERNANCE
Read:
- `AI_ENTRYPOINT.md`
- `AI_OPERATING_CONTRACT.md`
- `CHECKPOINT.md`
- `PROJECT_SCOPE.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- `DECISIONS.md`
- `DECISIONS_V3.md`
- `DECISIONS_V4.md`
- `docs/PROJECT_KNOWLEDGE_AUDIT_20260913.md`
- `SECURITY.md`
- `.github/workflows/validate.yml`

### PRODUCT ARCHITECTURE / BUSINESS CONTRACT
Read:
- all active decision layers above
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
- `docs/DELIVERY_PLAN_V3.md`
- `docs/SERVICE_API_CONTRACT.md`
- `docs/CANONICAL_MUTATION_PLAN.md`
- `docs/BETA_ACCEPTANCE_MATRIX.md`
- relevant D1 migrations for business/data truth.

Important: `SERVICE_API_CONTRACT`, `CANONICAL_MUTATION_PLAN` and `LAN_EDGE_STATE_V1` currently require V3 reconciliation where the knowledge-audit file marks drift. Do not let older V2 wording override V3/V4 decisions.

### CLOUDFLARE / D1 / WORKER
Read:
- `SERVICE_AUTHORITY.md`
- active decisions
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- `service/worker/src/`
- `service/worker/migrations/`
- Cloudflare workflows/scripts/dispatch
- latest directly relevant verification/deploy evidence.

Never migrate/replace/recreate D1/Worker from name alone. Verify exact authority and existing state first.

### GOOGLE GAS / GATEWAY / DRIVE / SHEETS
Read:
- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- active decisions, especially V3 direct-LAN Google rules
- `CURRENT_STATE.md`
- `config/projections.beta.json`
- `service/google-gateway/`
- GAS workflows/scripts/dispatch
- current Drive/Sheets evidence when remote structure/data matters.

Sheets/Drive are downstream outputs/storage and are never reconstructed as business authority.

### ANDROID / APK
Read:
- `PROJECT_SCOPE.md`
- active decisions
- V3 architecture/delivery
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- active Android source/workflows.

The current transport-only prototype is NON_AUTHORITY unless a low-level mechanic is deliberately re-adopted.

### LAN SERVICE
Read:
- active decisions V3/V4
- V3 architecture/delivery
- `docs/LAN_SOURCE_REVIEW_20260913.md`
- `docs/LAN_EDGE_STATE_V1.md` with V3 drift warning
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- active LAN source/evidence.

Hard constraints: portable/no-admin company-laptop operation, no corporate-policy bypass, canonical LAN domains `lan-beta.supra.cc.cd` / `lan.supra.cc.cd`, and offline domain resolution is an acceptance gate rather than an assumed fact.

Physical evidence and synthetic-capacity evidence remain distinct.

### BETA / STABLE / RELEASE PROMOTION
Read:
- `DECISIONS_V4.md`
- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- V3 delivery plan
- current release/build evidence.

Prepare STABLE infrastructure independently during development, but keep business traffic fail-closed until explicit Owner promotion approval. Promotion uses the exact accepted BETA release and never copies BETA runtime/business data into STABLE.

## Escalation rules

Escalate FAST -> FOCUSED if relevant `main` paths changed after checkpoint, a provider/resource identity must be used, a new task is introduced, or a checkpoint claim lacks evidence.

Escalate to FULL if authority files conflict, checkpoint cannot be reconciled safely, architecture/invariants change across lanes, Owner requests a full audit, or STABLE promotion is considered.
