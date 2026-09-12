# AI ENTRYPOINT — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`
Repository: `tamnv2/vanhanhsupradchungyen`
Authority branch: `main`

## Owner commands

The following phrases are equivalent resume commands:
- `Tiếp tục VHDCHY`
- `Tiếp tục việc đang làm`
- `Tiếp tục việc đang dở`

Resume mode means: restore the active checkpoint and continue the unfinished approved work with the minimum sufficient GitHub context.

The following phrases are equivalent full-audit commands:
- `Tiếp tục VHDCHY — full audit.`
- `Rà soát toàn bộ dự án`
- `Kiểm tra toàn bộ dự án`

Full-audit mode means: reconcile the whole active project from GitHub `main`, verify current evidence where available, and report drift/conflicts before making changes.

## Hard authority rule

AI memory, remembered chat content, prior-chat summaries and remembered provider state are never project authority unless Owner explicitly asks to use them. They may help locate what to verify, but must not be used as the factual basis for project work.

An explicit instruction from Owner in the current conversation controls intent and approval. Persistent project facts, IDs, decisions and state must be verified from GitHub `main` and/or current provider evidence, then persisted back to GitHub when changed.

## Bootstrap

Always read:
1. `AI_OPERATING_CONTRACT.md`
2. `CHECKPOINT.md`
3. `CONTEXT_INDEX.md`

Then reconcile the checkpoint against current `main`. Read only the task-specific authority/source files selected by `CONTEXT_INDEX.md` unless escalation to FULL is required.

Before a state-changing action, confirm the current plan/approval scope, dependency gate, resource identity and last evidence. Never repeat a mutation merely because its prior outcome is uncertain; verify first.

## Context modes

- `FAST`: normal resume of a still-valid checkpoint; minimum files and evidence.
- `FOCUSED`: a new or changed task within one or more named lanes; read only relevant authority/source/evidence.
- `FULL`: full audit, authority conflict, stale/unreconcilable checkpoint, cross-cutting architecture change, major reset, or STABLE promotion gate.

`FAST` is the default for `Tiếp tục VHDCHY`. `FULL` is mandatory for the full-audit command aliases above.
