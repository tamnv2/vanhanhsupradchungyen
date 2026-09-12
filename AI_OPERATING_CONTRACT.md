# AI OPERATING CONTRACT

Status: ACTIVE / OWNER-APPROVED 2026-09-12

Purpose: deterministic continuation, minimum rereading/token waste, strict dependency order and parallel execution of independent work.

## A. Minimal bootstrap

At the start of a new chat, read in order:

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Do not read the full history/changelog/reference archive by default. Drill down only to files relevant to the current task.

If task touches provider/account/resource ID/permission/secret name, `SERVICE_AUTHORITY.md` must be read before mutation.

## B. Single-source facts

Each current fact should have one primary authority. Other files should link/summarize rather than create competing copies.

Provider facts use statuses defined in `SERVICE_AUTHORITY.md`. Never call a resource DONE/LIVE/PASS unless it is `VERIFIED_CURRENT` and its required integration gate passes.

## C. Setup reset semantics

Baseline `SETUP-RESET-20260912-01` resets setup/provider state, not approved code/logic/history.

- Keep architecture, decisions, code, migrations and test evidence unless explicitly superseded.
- Old provider IDs/credentials/deployments/workbooks are historical until re-verified.
- Reuse current-account resources if verification proves ownership, access and correct purpose.
- Do not recreate valid resources merely to mimic a “from zero” checklist.

## D. Dependency-first execution

Before execution:

1. decompose into work items;
2. identify true dependency/gates;
3. run independent items in parallel where tools permit;
4. serialize writes to the same file/ref/resource;
5. never idle waiting for one provider if another independent lane can progress.

A numbered runbook expresses **gate order**, not a ban on parallel work.

## E. Current setup lane model

```text
GitHub authority baseline
  -> Drive/Sheet baseline
      -> Google Cloud/OAuth -> GAS
      -> Cloudflare verify          (parallel)
      -> Android signer verify      (parallel)
  -> verified provider outputs
  -> GitHub beta Environment
  -> BETA CI/deploy/health
  -> LAN physical regression
  -> Owner approval -> STABLE
```

## F. Provider preflight

Before any provider write/deploy:

- verify active account/profile with available tool;
- compare against `SERVICE_AUTHORITY.md`;
- verify target environment BETA/STABLE;
- identify whether operation is reuse, provision, migration or deploy;
- use least privilege;
- if tool cannot verify a required UI fact, mark it `OWNER_CONFIRMED_NOT_TOOL_VERIFIED`/`UNKNOWN` rather than guessing.

## G. Google security discipline

Current Google owner is `tam95.supra@gmail.com`.

CI OAuth current minimum:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

GAS runtime current minimum:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

Do not add Drive/Gmail/Calendar/Contacts/mail/trigger/external-fetch scopes until a current approved feature actually uses them.

Refresh token is created for intended environment/client and reused; do not mint a new token each run. Never expose token/client secret in logs/chat/repo.

## H. GitHub safety

- Repo is PUBLIC intentionally.
- Repository default Actions token remains least privilege/read by default.
- Workflows elevate only what their job needs.
- Environment values are populated only from verified provider outputs; placeholders never become current facts.
- `beta` and `stable` are live pointers, not scratch branches.

## I. BETA/STABLE isolation

BETA first. STABLE only after BETA PASS and explicit Owner approval. Do not copy BETA Google/GAS/Sheet/OAuth secrets/IDs into STABLE to save time.

## J. Checkpoint discipline

Update `SESSION_CHECKPOINT.md` and `NEXT_ACTIONS.md`:

- after provider/account/resource changes;
- after a major PASS/FAIL;
- before switching chat;
- after a blocker;
- before potentially risky migration/release.

Checkpoint records only concise state + locators: timestamp, authority commit/ref, statuses, exact next actions, dependencies, blockers, run IDs/resource IDs. Do not paste raw long logs.

## K. Smart inheritance / token optimization

- Prefer compact authority/current-state/index files.
- Use path/ID/module targeted reads.
- Re-read a file only when its revision/state may have changed or current task needs details not already grounded.
- Use commit SHA/checksum/provider ID as continuity locators.
- Do not re-load immutable historical docs each session.

## L. Owner interaction

AI performs everything supported by current tools. Ask Owner only for provider UI/2FA/consent, secret entry, local keystore access, business conflict, or explicit STABLE approval.

Never ask Owner to paste secret/token/password/private key/keystore content into chat.
