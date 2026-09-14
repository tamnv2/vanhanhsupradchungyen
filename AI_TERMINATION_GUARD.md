# AI EXECUTION TERMINATION GUARD — VHDCHY

Status: ACTIVE
Protocol: `AI_AUTHORITY_RESUME_V2`
Owner approval: 2026-09-15

## Purpose

This file is a mandatory execution/finalization authority extension for VHDCHY. It exists to prevent premature voluntary stops while approved work remains executable.

When this file conflicts with older wording in `AI_OPERATING_CONTRACT.md` about voluntary stopping/finalization, this file controls that execution/finalization question. All unrelated contract rules remain active.

## Core rule

The default execution state after Owner approval is `CONTINUE`, not `WAIT` and not `FINAL`.

A final response that ends the current approved execution is permitted only when `CAN_FINAL = TRUE` under this guard.

`CAN_FINAL = TRUE` only when one of the following applies:

1. `APPROVED_SCOPE_COMPLETE`: every required action inside the approved scope is complete to the level currently required and verifiable, required verification has been performed, and no approved `READY` work remains.
2. `OWNER_DECISION_REQUIRED`: an unresolved material Owner/business/authority choice has multiple materially different valid outcomes and cannot be resolved from current instruction or authority.
3. `OWNER_PERMISSION_REQUIRED`: a missing Owner-controlled permission, consent, access grant or secret-store setup blocks the affected dependency path.
4. `TOOL_CAPABILITY_LIMIT`: the required action is technically impossible with the current runtime after materially relevant safe direct and indirect AI-executable paths have been exhausted or proven unavailable.

Conditions 2–4 authorize ending the whole session only when no independent approved safe `READY` work remains. Otherwise only the affected path is paused and execution continues elsewhere.

Ordinary errors, failed CI, transient provider errors, incomplete evidence, uncertain prior outcome, a blocked single lane, long execution time, a completed checkpoint, a useful intermediate result, or a progress report are not finalization conditions.

## Mandatory PRE_FINAL_TERMINATION_GUARD

Before any final response that would end approved execution:

1. rebuild the dependency graph and current ready queue from verified state;
2. classify remaining approved work as:
   - `COMPLETE`: required work and verification for the node are finished;
   - `READY`: an AI-executable next action exists now and its dependencies are satisfied;
   - `BLOCKED`: a specific dependency/gate prevents the next action;
3. if any approved `READY` work exists, `FINAL` is **FORBIDDEN** and execution must continue;
4. if no approved `READY` work exists and all approved scope is complete, classify `APPROVED_SCOPE_COMPLETE` and finalization is allowed;
5. if scope is incomplete and no `READY` work exists, prove the exact blocking condition as `OWNER_DECISION_REQUIRED`, `OWNER_PERMISSION_REQUIRED` or `TOOL_CAPABILITY_LIMIT`;
6. if none of those blockers can be proven, search alternate execution paths, rebuild the ready queue and continue.

A progress update, milestone report, checkpoint write or useful evidence finding is not a substitute for this guard and does not authorize finalization.

## READY-work rule

`READY` is intentionally broad. It includes any safe AI-executable next action such as:

- source/config implementation or repair;
- diagnosis and log inspection;
- evidence collection and CI verification;
- repository/provider API reads;
- documentation/governance updates required by approved work;
- safe hosted CI or fixed-operation workflow execution;
- independent work in another approved lane whose dependencies are already satisfied.

If one lane is blocked, rebuild the queue and continue all independent `READY` nodes.

## TOOL_CAPABILITY_LIMIT proof standard

Do not classify a task as `TOOL_CAPABILITY_LIMIT` because one preferred tool/query/path is unavailable, inconvenient or did not immediately return the needed evidence.

Before using this classification, attempt or explicitly evaluate all materially relevant safe categories:

1. preferred connected tool/path;
2. alternate connected tools;
3. direct repository/provider APIs available to the AI;
4. GitHub-hosted CI or a reviewed fixed-operation workflow bridge;
5. other safe indirect AI-executable paths.

Record the exact hard limitation and alternatives checked. If any safe AI-executable path remains, continue.

Do not bypass platform safety guards. A path blocked by a platform guard must be abandoned or redesigned through a safe alternate route.

## Checkpoint and interruption behavior

Checkpointing is a save boundary, not a stop boundary.

`CHECKPOINT_WRITTEN != SESSION_COMPLETE`.

After writing a checkpoint:

1. rebuild/reconfirm the ready queue;
2. continue all approved `READY` work;
3. apply `PRE_FINAL_TERMINATION_GUARD` before any later finalization.

Time is only an interruption-safety/checkpointing signal. Do not extend a finished task merely to reach a time target, and do not stop unfinished approved work merely because a time threshold has elapsed.

For interruption-safe continuation, checkpoint enough exact state to resume without repeating investigation: last completed node, current investigation, next evidence/tool action where useful, independent `READY` nodes, blocked nodes, direct evidence references and `do_not_repeat` safeguards.

## Reporting behavior

Intermediate findings should be reported as progress updates when useful, then execution continues. Reporting and stopping are separate decisions.

A final response must state which valid finalization condition was reached whenever the approved scope was not fully completed.