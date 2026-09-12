# AI BOOTSTRAP

Purpose: continue VHDCHY across chats without relying on model memory and without repeatedly loading full project history.

## Every new session — read exactly these 5 files first

1. `PROJECT_SCOPE.md`
2. `SERVICE_AUTHORITY.md`
3. `CURRENT_STATE.md`
4. `NEXT_ACTIONS.md`
5. `DECISIONS_INDEX.md`

Then read only task-specific docs/code. Read `AI_OPERATING_CONTRACT.md` when execution/governance rules are needed.

## Baseline marker

Current setup baseline: `SETUP-RESET-20260912-01`.

Any provider/resource DONE/LIVE statement dated before the baseline is historical evidence only unless `SERVICE_AUTHORITY.md` explicitly marks it current.

## Current identity quick map

- Drive / Sheets / GAS: `tam95.supra@gmail.com`.
- Cloudflare / GitHub account: `nguyenvantam050595@gmail.com`.
- GitHub user/repo: `tamnv2` / `tamnv2/vanhanhsupradchungyen`.
- `automation@supra.cc.cd`: suspended recovery candidate, not current.
- Drive root: `VẬN HÀNH DC HƯNG YÊN`.
- Reference: `BACKUP PICK PACK 1291` — reference-only.

## Hard rules

- Owner decision newest > project scope > service authority > active decisions > current state/spec > references > history.
- Never infer current account/ID/token/deployment from chat memory, old changelog, backup, old repo or screenshot.
- Distinguish `code/evidence preserved` from `provider live verified`; preserved source does not mean current environment is deployed.
- Before work, build dependency graph and run independent lanes in parallel when possible.
- Do not repeatedly read full repo/history. Use paths/IDs/indexes and only drill down when needed.
- Update `SESSION_CHECKPOINT.md` + `NEXT_ACTIONS.md` after meaningful state changes, blockers or before switching sessions.
- `CHANGELOG.md` and `docs/changelog/` are append-only history; correction is a new record, not deletion of history.
- Never move `beta`/`stable` until corresponding provider gate passes.
- STABLE requires explicit Owner approval.
- Never commit/log/paste secrets/private keys/keystore bytes/passwords.
- OAuth/provider permissions are least-privilege and feature-gated.

## Reference loading

Do not load `BACKUP PICK PACK 1291` by default. Only read it for a task that needs legacy evidence. Any pattern must be classified `REFERENCE -> EVALUATED -> ADOPTED | ADAPTED | REJECTED` before it affects current design.
