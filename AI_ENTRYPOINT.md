# AI ENTRYPOINT — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`
Repository: `tamnv2/vanhanhsupradchungyen`
Authority branch: `main`
External bootstrap reference: `CHATGPT_PROJECT_BOOTSTRAP.md`

## External bootstrap requirement

This repository is persistent project authority, but repository files do not load themselves into a fresh chat.

For every fresh-chat resume, the AI must fetch this file from GitHub `main` in the current chat before using remembered project facts. A remembered copy, project/chat summary or prior conversation is not a substitute for a live GitHub read.

If this file has not been fetched in the current chat, the project is not considered resumed yet.

`CHATGPT_PROJECT_BOOTSTRAP.md` contains the exact one-time ChatGPT Project instruction required to trigger this live read from a fresh chat. Repository-side resume correctness is not considered end-to-end complete until that external Project instruction is installed.

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

`AI_TERMINATION_GUARD.md` is the mandatory execution/finalization authority extension. Where older wording in `AI_OPERATING_CONTRACT.md` conflicts with that guard about voluntary stopping or finalization, the guard controls that question; all unrelated contract rules remain active.

Hard finalization invariant: **while approved `READY` work remains, a final response that ends execution is forbidden.**

## Bootstrap

Always perform these steps in the current chat:
1. fetch `AI_OPERATING_CONTRACT.md` from GitHub `main`;
2. fetch `AI_TERMINATION_GUARD.md` from GitHub `main`;
3. fetch `CHECKPOINT.md` from GitHub `main`;
4. fetch `CONTEXT_INDEX.md` from GitHub `main`;
5. obtain the current GitHub `main` HEAD SHA;
6. compare HEAD against `CHECKPOINT.md.reconciled_through_commit`;
7. if HEAD differs, inspect the changed paths/commits before continuing and read every changed authority/current-state/canonical-guide file relevant to the active lanes;
8. read every active decision layer listed by `CONTEXT_INDEX.md`, even if an older checkpoint forgot to name one;
9. only then continue the task-specific source/evidence selected by `CONTEXT_INDEX.md`.

A stale checkpoint is a locator, not current truth. It must never override a newer active decision layer or newer canonical guide on `main`.

Before a state-changing action, confirm the current plan/approval scope, dependency gate, resource identity and last evidence. Never repeat a mutation merely because its prior outcome is uncertain; verify first.

Before any final response that would end approved execution, run the mandatory `PRE_FINAL_TERMINATION_GUARD` defined in `AI_TERMINATION_GUARD.md`. A checkpoint, progress update, useful intermediate result, incomplete evidence or elapsed time is not by itself permission to stop.

## Context modes

- `FAST`: normal resume of a still-valid checkpoint after the HEAD/checkpoint comparison above; minimum files and evidence.
- `FOCUSED`: a new or changed task within one or more named lanes, or a checkpoint with relevant changes after its reconciliation point; read only relevant authority/source/evidence.
- `FULL`: full audit, authority conflict, stale/unreconcilable checkpoint, cross-cutting architecture change, major reset, or STABLE promotion gate.

`FAST` is the default for `Tiếp tục VHDCHY` only when the checkpoint is still current for the affected lanes. A stale checkpoint automatically becomes `FOCUSED` or `FULL` as required by the changed paths.
