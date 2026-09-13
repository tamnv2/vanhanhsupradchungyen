# CONTEXT INDEX — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`

GitHub `main` is persistent project authority. Current provider/runtime evidence may prove a GitHub status entry stale and must then be reconciled back to `main`.

## Authority precedence

For product/business decisions read in this order:
1. `DECISIONS.md` — base decisions;
2. `DECISIONS_V3.md` — full LAN Service, direct LAN Google output, long-offline authority and reconciliation overrides;
3. `DECISIONS_V4.md` — no-admin LAN host, canonical LAN domains and BETA/STABLE preparation/promotion overrides;
4. `DECISIONS_V5.md` — recovered effective Owner rules from the reviewed approved specs;
5. `DECISIONS_V6.md` — ROOT email one-time login, optional TOTP, five-minute credential lifecycle and normal-account forgot-password overrides.

Newest applicable override wins only where it conflicts. Unaffected older decisions remain active. The former unresolved ROOT-factor block in V5 is resolved by V6 and must not be reopened from stale documents.

**All five active decision layers above are mandatory resume reads.** A stale or incomplete `CHECKPOINT.md` must never be used to omit a newer decision layer.

Canonical consolidation/implementation guides:
- `docs/OWNER_BUSINESS_RULES_V1.md`
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`
- `docs/SERVICE_API_CONTRACT_V3.md`
- `docs/LAN_EDGE_STATE_V2.md`
- `docs/DATA_MODEL_GUIDE_V2.md`
- `docs/NON_FUNCTIONAL_BASELINE_V1.md`
- `docs/BETA_ACCEPTANCE_MATRIX.md`
- `docs/DELIVERY_PLAN_V4.md`
- `docs/LAN_HOST_DOMAIN_V1.md`
- `docs/RELEASE_PROMOTION_V1.md`

Older V2 contract/delivery/edge documents remain historical/reference only where a newer file above supersedes them.

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
- only task-specific current guide/source/evidence.

### FOCUSED
Use for a new/changed task inside active boundaries or when relevant paths changed after the checkpoint. Read FAST plus the relevant subset of:
- `PROJECT_SCOPE.md`
- `SERVICE_AUTHORITY.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- current architecture/contract/data/acceptance guide
- relevant source/config/workflows/provider evidence.

### FULL
Mandatory for Owner full-audit commands, unresolved authority conflicts, cross-lane architecture changes, stale/unreconcilable checkpoint or STABLE promotion.

Read/reconcile all root authority/governance files, all active decision layers, current canonical guides, current source/config/workflows and directly relevant provider evidence. Historical/reference material is read only to resolve an identified discrepancy or reuse candidate.

## Lane routing

### REPO / GOVERNANCE
Read:
- `AI_ENTRYPOINT.md`
- `AI_OPERATING_CONTRACT.md`
- `CHECKPOINT.md`
- `PROJECT_SCOPE.md`
- `CURRENT_STATE.md`
- `NEXT_ACTIONS.md`
- all active `DECISIONS*`
- `docs/OWNER_BUSINESS_RULES_V1.md`
- `docs/DELIVERY_PLAN_V4.md`
- `SECURITY.md`
- `.github/workflows/validate.yml`

### PRODUCT / SHARED DOMAIN
Read:
- all active decision layers;
- `docs/OWNER_BUSINESS_RULES_V1.md`;
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`;
- `docs/SERVICE_API_CONTRACT_V3.md`;
- `docs/DATA_MODEL_GUIDE_V2.md`;
- `docs/NON_FUNCTIONAL_BASELINE_V1.md`;
- `docs/BETA_ACCEPTANCE_MATRIX.md`;
- `docs/DELIVERY_PLAN_V4.md`;
- relevant D1 migrations/source.

Do not let `docs/SERVICE_API_CONTRACT.md`, `docs/CANONICAL_MUTATION_PLAN.md`, `docs/LAN_EDGE_STATE_V1.md` or V2 delivery text override the current files above.

### CLOUDFLARE / D1 / WORKER
Read:
- `SERVICE_AUTHORITY.md`;
- all active decision layers plus current state/next actions;
- `docs/SERVICE_API_CONTRACT_V3.md`;
- `docs/DATA_MODEL_GUIDE_V2.md`;
- `service/worker/src/`;
- `service/worker/migrations/`;
- Cloudflare workflows/scripts/dispatch;
- latest directly relevant provider/deploy evidence.

Never migrate/replace/recreate D1/Worker from name alone. Verify exact identity/state first.

### GOOGLE GAS / DRIVE / SHEETS
Read:
- `PROJECT_SCOPE.md`;
- `SERVICE_AUTHORITY.md`;
- all active decision layers;
- `docs/OWNER_BUSINESS_RULES_V1.md`;
- `docs/SERVICE_API_CONTRACT_V3.md`;
- `config/projections.beta.json`;
- `service/google-gateway/`;
- current Drive/Sheets/provider evidence.

Sheets/Drive are downstream outputs/storage and are never reconstructed as business authority.

### LAN SERVICE
Read:
- all active decision layers;
- `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`;
- `docs/SERVICE_API_CONTRACT_V3.md`;
- `docs/LAN_EDGE_STATE_V2.md`;
- `docs/LAN_HOST_DOMAIN_V1.md`;
- `docs/NON_FUNCTIONAL_BASELINE_V1.md`;
- active LAN source/evidence.

Hard constraints: full local Service, portable/no-admin host, no corporate-policy bypass, canonical LAN domains, long-offline operation using last synchronized authority, direct controlled Google output when reachable, event/outbox reconciliation to Cloud, explicit conflicts.

Physical evidence and synthetic-capacity evidence remain distinct.

### WEBSITE / APK
Read:
- all active decision layers;
- business rules;
- V3 Service contract;
- non-functional baseline;
- delivery plan;
- current client source/build evidence.

Website and APK share the same domain authority. The earlier transport-only Android prototype is NON_AUTHORITY except for deliberately re-adopted low-level mechanics.

### BETA / STABLE / RELEASE PROMOTION
Read:
- all active decision layers;
- `docs/RELEASE_PROMOTION_V1.md`;
- `docs/DELIVERY_PLAN_V4.md`;
- `SERVICE_AUTHORITY.md`;
- exact accepted release/provider evidence.

Prepare isolated STABLE during development, keep business traffic fail-closed until explicit Owner promotion approval, promote the exact accepted BETA release, never BETA runtime/business data.

## Current Owner decision gates

No previously known ROOT factor/lifetime gate remains open: `DECISIONS_V6.md` resolves it. Future work must only raise a new `OWNER_DECISION_REQUIRED` when implementation reaches a genuinely unspecified product/security choice that cannot be derived from current authority.

## Escalation

FAST -> FOCUSED when relevant paths changed after checkpoint, provider identity matters, a new lane/task is introduced or evidence is missing.

Escalate to FULL for authority conflict, unreconcilable checkpoint, cross-cutting architecture change, Owner full audit or STABLE promotion.
