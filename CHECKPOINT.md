# CHECKPOINT — VHDCHY

checkpoint_version: 3
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING
action_mode: AUTONOMOUS_CLOUD_CI
active_lane: SERVICE / BUSINESS_CORE_V3 BETA MIGRATION
approved_scope: Owner approved autonomous execution on 2026-09-13. Prefer direct connected actions, otherwise GitHub-hosted CI. Do not require laptop-local tooling for routine cloud/provider operations. Request only exact missing permissions/consent when needed. No STABLE promotion without explicit Owner approval.
reconciled_through_commit: e32951aeb7988572b379574d4737c1f4197ba3cb
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

## Current gate

Build and execute a guarded GitHub Actions BETA migration bridge. The bridge must run on GitHub-hosted infrastructure using Environment `beta`, not on the Owner laptop.

Required migration workflow behavior:
- fixed target only: Cloudflare account from approved environment, D1 name `vhdchy-data-beta`, exact database ID `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`;
- preflight must verify current schema `business_core_v1`, expected legacy table set and zero business rows immediately before mutation;
- migration operation is fixed to the reviewed ordered source `0000` through `0008`; dispatch payload must not accept arbitrary SQL/commands;
- fail closed on any identity/precondition mismatch;
- postflight must verify `business_core_v3`, required target tables/catalog seeds and integrity checks before PASS;
- credentials remain GitHub Environment secrets/variables; do not expose raw token values.

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

Create/review CI migration executor -> validate workflow/source -> trigger guarded BETA migration -> inspect run evidence -> run independent post-migration read-only verification -> update authority/checkpoint -> continue Worker/API BETA integration.

## do_not_repeat:

Do not recreate verified Cloudflare resources. Do not require laptop-local tooling for routine cloud execution. Do not run arbitrary SQL from dispatch input. Do not apply a migration if provider preflight differs from the verified V1 zero-business state. Do not repeat a migration/deploy whose result is uncertain; verify evidence first. Do not bypass platform safety blocks through lower-level Git/API mechanisms. Do not promote STABLE before full BETA PASS plus explicit Owner approval.
