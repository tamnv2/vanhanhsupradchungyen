# AI OPERATING CONTRACT

Status: ACTIVE / CLEAN BASELINE 2026-09-12

## Minimal bootstrap

Read in order:
1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS.md`

Then read only task-specific files.

## Authority order

Newest Owner decision > project scope > service authority > active decisions > current state > task-specific design/source > historical snapshot/reference.

## Execution

- Build a dependency graph before work.
- Run independent work in parallel when tools/UI permit.
- Serialize writes to the same file/ref/resource.
- Do not block Service work because physical LAN testing is unavailable off-site.
- When Owner is at the company, LAN and Service may run in parallel where independent.

## Provider discipline

Before provider changes, verify current account, environment and resource identity. Use least privilege. Never infer current provider state from pre-reset history.

## Source restoration

`backup/pre-zero-20260912` is evidence only. Restore a component only after reviewing it against current scope. Correct stale assumptions before it returns to `main`.

## Repository discipline

- `main` is the clean authority baseline.
- `beta` and `stable` are deployment pointers, not scratch branches.
- Move `beta` only after the BETA provider baseline is ready.
- Move `stable` only after BETA PASS and Owner approval.
- Keep current-state documents concise and avoid duplicate ledgers.

## Security

Sensitive access material and signing material must stay outside source history. Repository files record only names, ownership and verification state.
