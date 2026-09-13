# CHECKPOINT — VHDCHY

checkpoint_version: 4
protocol: AI_AUTHORITY_RESUME_V2
status: OWNER_UI_ONCE_REQUIRED
action_mode: AUTONOMOUS_CLOUD_CI
active_lane: SERVICE / BUSINESS_CORE_V3 BETA MIGRATION
approved_scope: Owner approved autonomous execution on 2026-09-13. Prefer direct connected actions, otherwise GitHub-hosted CI. Do not require laptop-local tooling for routine cloud/provider operations. Request only exact missing permissions/consent when needed. No STABLE promotion without explicit Owner approval.
reconciled_through_commit: 869c53d7f43d087346127dabdc207b3e80e77985
active_work_ref: NEXT_ACTIONS.md
current_state_ref: CURRENT_STATE.md
authority_ref: DECISIONS.md
service_authority_ref: SERVICE_AUTHORITY.md
context_router_ref: CONTEXT_INDEX.md

## Completed

- Owner-approved 2026-09-13 business/data decisions are persisted in repository authority.
- `business_core_v3` source was reviewed, validated and merged to `main` through PR #7.
- Main validation after merge completed SUCCESS (`34749181618`).
- Guarded BETA zero-business rebuild preamble is present as `service/worker/migrations/0000_beta_zero_business_rebuild.sql` and CI-valid with D1-compatible deferred foreign-key handling.
- Validation run `34749393544` completed SUCCESS after the rebuild guard adjustment.
- Automated final pre-write D1 inspection run `34749417468` completed SUCCESS.
- Verified BETA D1 identity remains `vhdchy-data-beta`, database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- Verified pre-write schema remains `business_core_v1`; all business table row counts remain zero; bookkeeping rows only (`d1_migrations=1`, `vhdchy_meta=3`).
- Final main validation run `34749417525` completed SUCCESS.
- Operating contract now explicitly requires direct connected execution first, GitHub-hosted CI second, and Owner-local tooling only for inherently local/physical work or explicit Owner request.
- `CURRENT_STATE.md` and `NEXT_ACTIONS.md` were reconciled to the merged V3/pre-write state and no-local-install execution policy.
- Disabled dispatch control `.github/dispatch/cloudflare-beta-migrate.json` now exists on `main`; it cannot mutate provider state while `enabled=false`.

## Current gate / blocker

A dedicated guarded D1 write executor is required in `.github/workflows`. An attempt to create the provider-write helper directly through the connected GitHub write action was blocked by the platform action-safety guard. This is not a missing Cloudflare/GitHub permission and must not be bypassed through lower-level Git/API mechanisms.

Owner must perform one GitHub Web UI source-creation step to place the reviewed fixed-purpose migration workflow in `.github/workflows`. No laptop installation or local CLI is required. After the workflow exists, AI resumes automation through the repository/dispatch path.

Required migration workflow behavior:
- GitHub-hosted runner + Environment `beta` only;
- fixed target D1 `vhdchy-data-beta`, exact database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`;
- fixed operation `migrate_business_core_v3`; no arbitrary SQL/provider command input;
- immediate preflight requires schema `business_core_v1`, exact expected legacy table set and zero business rows;
- apply only reviewed migrations `0000` through `0008`;
- fail closed on any mismatch/error;
- postflight requires `business_core_v3`, target tables, baseline seeds, foreign-key check and quick check PASS;
- credentials remain GitHub Environment `beta` secrets/variables.

## Execution policy

- Do not ask Owner to install Wrangler/Git/Node/Python/provider SDKs merely to operate cloud resources.
- If direct ChatGPT provider action is unavailable, use GitHub Actions/CI.
- If a required permission or UI consent is missing, ask Owner only for that exact grant/consent, then resume automation.
- Do not delegate routine D1 console/CLI commands to Owner while a safe CI path can be established.

## Evidence

- V3 source merge: PR #7 / merge commit `c4c6e43f25c78625ebd816d1bdf5c54393a0f812`.
- Post-merge validation: `34749181618` — SUCCESS.
- Rebuild-guard validation: `34749393544` — SUCCESS.
- Final pre-write Cloudflare/D1 inspection: `34749417468` — SUCCESS.
- Final main validation before migration-bridge work: `34749417525` — SUCCESS.
- Current provider schema: `business_core_v1`, zero business rows.
- Target provider schema: `business_core_v3`.

## Next actions

Owner creates the single reviewed GitHub Actions migration workflow through GitHub Web UI -> AI validates the committed workflow -> AI activates the fixed dispatch -> inspect run evidence -> independent post-migration read-only verification -> update authority/checkpoint -> continue Worker/API BETA integration.

## do_not_repeat:

Do not recreate verified Cloudflare resources. Do not require laptop-local tooling for routine cloud execution. Do not run arbitrary SQL from dispatch input. Do not apply a migration if provider preflight differs from the verified V1 zero-business state. Do not repeat a migration/deploy whose result is uncertain; verify evidence first. Do not bypass platform safety blocks through lower-level Git/API mechanisms. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
