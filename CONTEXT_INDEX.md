# CONTEXT INDEX — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`

Use this file to minimize reads while preserving correctness. GitHub `main` is the persistent project authority. Current provider/runtime evidence may show that a GitHub status entry needs reconciliation.

## Read modes

### FAST
Use for normal resume when `CHECKPOINT.md` is current and the requested work remains inside the recorded approved scope.

Read `AI_ENTRYPOINT.md`, `AI_OPERATING_CONTRACT.md`, `CHECKPOINT.md`, only the lane files listed below, and the latest directly relevant execution evidence. Do not read `CHANGELOG.md` or historical backup material by default.

### FOCUSED
Use when Owner gives a new task, changes scope inside active project boundaries, or the checkpoint needs limited reconciliation.

Read the FAST set plus `PROJECT_SCOPE.md` when boundaries matter, `SERVICE_AUTHORITY.md` when provider/resource identity matters, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, relevant decisions, and relevant source/config/workflows.

### FULL
Use for the full-audit command, authority conflicts, major reset, cross-lane architecture change, stale/unreconcilable checkpoint, or STABLE promotion.

Read and reconcile all root authority/governance files, active source/config/workflows on `main`, and latest relevant Actions/provider evidence. Historical/reference material is read only where current authority identifies an unresolved dependency or discrepancy.

Do not ingest the entire pre-zero backup merely for completeness; inspect only the historical components needed to resolve an identified issue.

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
- `SECURITY.md`
- `.github/workflows/validate.yml`

### CLOUDFLARE / D1 / WORKER
Read:
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- relevant `DECISIONS.md`
- `service/worker/src/`
- `service/worker/migrations/`
- Cloudflare-related workflows/scripts/dispatch files
- latest directly relevant Cloudflare verification/deploy evidence

Critical gate: never migrate, replace or recreate D1/Worker from name alone. Verify exact authority and existing state first.

### GOOGLE GAS / GATEWAY
Read:
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- `config/projections.beta.json`
- `service/google-gateway/`
- GAS-related workflows/scripts/dispatch files
- latest directly relevant GAS sync/deployment evidence

### DRIVE / SHEETS / PROJECTION
Read:
- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `config/projections.beta.json`
- Google Gateway source when projection transport is involved
- current Drive/Sheets evidence when remote structure/data matters

### ANDROID / SIGNING
Read:
- `PROJECT_SCOPE.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- `SECURITY.md`
- active Android source/workflows once restored to `main`
- retained backup only for the specific component being reviewed

### LAN
Read:
- `PROJECT_SCOPE.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- active LAN source/evidence once restored to `main`
- retained backup only for the specific LAN component/evidence being reviewed

Physical evidence and synthetic-capacity evidence must remain distinct.

## Escalation rules

Escalate FAST -> FOCUSED if current `main` changed after the checkpoint in relevant paths, a provider/resource identity must be used, a new task is introduced, or a checkpoint claim lacks current evidence.

Escalate to FULL if authority files conflict, the checkpoint cannot be reconciled safely, architecture/invariants change across lanes, Owner requests a full audit, or STABLE promotion is considered.
