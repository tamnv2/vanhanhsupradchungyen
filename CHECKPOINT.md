# CHECKPOINT — VHDCHY

checkpoint_version: 3
protocol: AI_AUTHORITY_RESUME_V2
status: IN_PROGRESS
active_lane: SERVICE / CLOUDFLARE D1 BETA PRE-MIGRATION INSPECTION
approved_scope: Owner approved autonomous execution on 2026-09-13. Add and run a read-only D1 inspection gate through GitHub Actions; do not mutate D1, deploy Worker, or apply migration until inspection evidence is classified.
current_state_ref: CURRENT_STATE.md
authority_ref: SERVICE_AUTHORITY.md
context_router_ref: CONTEXT_INDEX.md

## Current gate

Inspect the already verified BETA D1 `vhdchy-data-beta` using metadata-only/read-only SQL. Evidence required:
- user table names and schema metadata;
- row count per user table;
- `vhdchy_meta.schema_version` only when that table exists;
- classification: `EMPTY`, `BUSINESS_CORE_V2`, or `UNKNOWN_NONEMPTY/SCHEMA_MISMATCH`.

## Safety boundary

- No INSERT / UPDATE / DELETE / DDL.
- No business-row contents.
- Verify Cloudflare account, D1 name and exact D1 database ID before querying.
- If identity mismatches or inspection is uncertain, fail closed.
- Do not apply `service/worker/migrations/0001_initial.sql` merely because resource identity matches.

## Execution plan

1. Add a dedicated D1 read-only inspection script and GitHub Actions workflow.
2. Trigger it from `main` using a controlled dispatch file.
3. Read workflow evidence and classify the database.
4. Persist PASS/FAIL/UNKNOWN evidence before any subsequent mutation.
5. Continue automatically only through the safe branch implied by the classification and current Owner-approved scope.

## do_not_repeat

Use completed/PASS items in `CURRENT_STATE.md` as the skip set. Never recreate verified Cloudflare resources. Never repeat a migration/deploy/provider mutation to resolve uncertainty; inspect evidence first.
