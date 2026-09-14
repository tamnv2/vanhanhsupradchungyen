# AI OPERATING CONTRACT

Status: ACTIVE
Protocol: `AI_AUTHORITY_RESUME_V2`

## 1. Bootstrap

`AI_ENTRYPOINT.md` is the only fixed entry file. It defines the Owner command aliases, read modes and initial bootstrap sequence.

Normal resume command aliases:
- `Tiếp tục VHDCHY`
- `Tiếp tục việc đang làm`
- `Tiếp tục việc đang dở`

Full-audit aliases:
- `Tiếp tục VHDCHY — full audit.`
- `Rà soát toàn bộ dự án`
- `Kiểm tra toàn bộ dự án`

## 2. Authority and memory ban

AI memory, remembered conversations, prior-chat summaries and remembered provider state are `NON_AUTHORITY` by default. Do not use them as the factual basis for project work unless Owner explicitly requests that source.

Memory may be used only as a locator for facts that are then verified from GitHub/provider evidence.

Authority order for active work:
1. explicit current Owner instruction for intent, approval and priority;
2. verified current provider/runtime evidence for actual remote state;
3. GitHub `main` authority files for persistent project facts and policy;
4. task-specific source/config on `main`;
5. historical/reference material only when explicitly required for reconciliation.

If current provider evidence contradicts GitHub state, do not silently choose either side: stop the affected mutation, classify GitHub as stale, reconcile the discrepancy, and persist the corrected state. Continue all unrelated safe work while that affected mutation is paused.

A new Owner decision that must survive sessions must be persisted to GitHub before it is treated as persistent authority.

## 3. Context economy

Use `CONTEXT_INDEX.md` to choose `FAST`, `FOCUSED` or `FULL` reads.

- `FAST`: minimum context to resume a valid checkpoint.
- `FOCUSED`: task/lane-specific authority plus relevant source/evidence.
- `FULL`: whole active project reconciliation when explicitly requested or required by conflict/risk.

Do not read historical logs, full changelog or unrelated lanes merely for completeness. Escalate context only when correctness requires it.

## 4. Owner approval protocol and strict non-stop execution rule

For a genuinely new material scope, analyze intent, risks, dependencies and parallelizable work, then use the Owner's current instruction as the approval boundary. Once the Owner has approved or directly instructed the scope, execute all available actions end-to-end without adding routine confirmation gates.

The default state is `CONTINUE`, not `WAIT`.

After scope approval, the AI is **strictly prohibited from voluntarily stopping execution** unless at least one of these three conditions is actually reached:

1. `OWNER_DECISION_REQUIRED`: a material conflict, contradiction or unresolved business/authority choice has multiple materially different valid outcomes and cannot be resolved from current Owner instruction, verified provider state or GitHub authority.
2. `OWNER_PERMISSION_REQUIRED`: a missing permission, access grant, secret-store setup, interactive consent or equivalent Owner-controlled capability prevents the AI from continuing the required action.
3. `TOOL_CAPABILITY_LIMIT`: a hard limitation of the currently available tools/runtime makes the required action technically impossible for the AI, and safe alternate connected tools, APIs, hosted CI bridges and indirect AI-executable paths have been exhausted or proven unavailable.

Any other condition — including ordinary implementation errors, failed CI, transient provider errors, unavailable local tooling, incomplete evidence, uncertain prior outcome, a blocked single lane, long execution time, a completed checkpoint, or the fact that another task is still running — does **not** authorize stopping. Diagnose, repair, verify and continue automatically.

If one lane is blocked, continue every independent safe lane. Reaching one of the three stop conditions pauses only the affected dependency path by default. A whole-project/session halt is permitted only when one of those three conditions applies **and no independent safe ready work remains**.

Do not ask Owner to reconfirm a choice already resolved by current instruction, `DECISIONS.md`, provider evidence or task-specific authority.

A technical failure is not an Owner blocker unless safe diagnosis/correction reduces it to `OWNER_DECISION_REQUIRED`, `OWNER_PERMISSION_REQUIRED` or a proven `TOOL_CAPABILITY_LIMIT`. First attempt safe diagnosis, correction, alternate connected execution, GitHub-hosted CI, or read-only evidence collection.

Standing explicit Owner gates already recorded in authority, such as STABLE promotion, apply only to that gated lane. Reaching such a gate does not stop unrelated BETA/LAN/Android/documentation work that can still proceed safely.

### Permission-blocker escalation protocol

When a required action is blocked by missing permission or access, stop only the affected mutation and immediately switch to permission-enablement analysis. The objective is to make the capability AI-operable for future runs rather than repeatedly delegating the same manual step to Owner.

Required order:
1. identify the exact missing capability, resource, account/environment and least-privilege permission needed;
2. determine whether Owner can grant that capability to an existing connected tool/plugin/API/GitHub App, provider service account, repository/environment permission, or other durable AI-accessible integration;
3. if a grant is possible, give Owner the exact minimum UI/consent/setup steps, without asking for raw secrets in chat, then verify the new capability and resume autonomous execution;
4. if direct AI permission cannot be granted, independently search for and design a safe indirect AI-executable path, preferring fixed fail-closed APIs or GitHub-hosted CI bridges with credentials kept in approved secret stores;
5. only when no safe direct grant and no safe indirect AI-executable path exists may the task become `OWNER_MANUAL_LAST_RESORT`; explain precisely why automation is impossible and request only the minimum unavoidable manual action.

A missing permission must therefore trigger an **automation-enablement attempt first**, not an immediate handoff of routine work to Owner.

### Tool-limit escalation protocol

Do not classify a task as `TOOL_CAPABILITY_LIMIT` merely because one preferred tool is unavailable or inconvenient. Before stopping the affected path, attempt or evaluate other connected tools, provider APIs, GitHub-hosted CI, reviewed fixed-operation workflows and other safe indirect paths. Record the exact hard limitation and the alternatives checked. If any safe AI-executable path remains, continue rather than stopping.

## 5. Autonomous execution and no-local-install default

Owner does not want project execution to depend on installing CLI tools, SDKs, runtimes, repositories or provider software on a laptop merely to let AI operate cloud resources.

Default execution order:
1. use a directly connected tool/API when available;
2. otherwise use a reviewed GitHub Actions/CI bridge running on hosted runners with provider credentials stored in GitHub Environments/Secrets;
3. otherwise request only the minimum Owner UI/consent/permission step needed to establish that bridge or grant the missing permission;
4. if direct capability cannot be granted, find a safe indirect AI-executable route;
5. use Owner-local/manual execution only as `OWNER_MANUAL_LAST_RESORT`, when the task is inherently local/physical or when no safe remote/CI/indirect AI path exists after review.

Do not ask Owner to install Wrangler, Git, Node, Python, SDKs or similar tooling merely because a provider action cannot be invoked directly from the current chat tool. Prefer GitHub-hosted CI.

When a permission is missing, identify the exact least-privilege permission/resource/environment required and prioritize making that capability durable and AI-operable for future runs. After permission is granted, verify it and resume autonomous execution without delegating routine commands back to Owner.

Provider secrets must remain in provider/GitHub secret stores. Do not ask Owner to paste raw secrets into chat or commit them to source.

CI mutation workflows must be fail-closed: verify environment, account/resource identity and required preconditions before write; perform only the reviewed operation; verify the post-state before PASS; never accept arbitrary provider commands/SQL from an untrusted dispatch payload when a fixed operation can be encoded instead.

## 6. Dependency and parallel execution

Build and continuously maintain a dependency graph before and during substantial execution.

Classify work nodes where useful as:
- `READ_ONLY`
- `REVERSIBLE_WRITE`
- `RISKY_OR_IRREVERSIBLE`
- `OWNER_INTERACTION`
- `PHYSICAL`

At every checkpoint or newly completed gate, immediately re-evaluate which remaining nodes are independent and start/execute them without waiting for unrelated nodes. Parallelism is a required operating behavior, not an optional optimization.

Execute independent nodes in parallel when tools and safety permit. Execute dependency-bound nodes in order. Serialize writes to the same file, branch/ref, database or provider resource.

If one node fails, isolate the failure, preserve evidence, and continue all nodes that do not depend on it. A failed node becomes a global stop only if it creates `OWNER_PERMISSION_REQUIRED`, `OWNER_DECISION_REQUIRED` or a proven `TOOL_CAPABILITY_LIMIT` and no other actionable independent work remains.

Do not block Service work merely because physical LAN testing is unavailable. Do not block projection design merely because auth runtime work is active. Do not block Web/API contract work merely because projection activation is pending when their shared dependencies are already stable. When Owner is at the company, independent LAN and Service work may proceed in parallel.

## 7. Checkpoint and interruption protocol

`CHECKPOINT.md` is the short-lived resume ledger. It is overwritten with current truth; it is not a historical log.

Checkpoint at these boundaries:
- after Owner approval and before the first material mutation when practical;
- after each important provider mutation, migration, deployment, version/promotion or irreversible/risky action;
- after each important PASS/FAIL or newly discovered blocker;
- before a long/risky operation;
- periodically during long tool sessions, targeting roughly 10–15 minutes when elapsed time is observable;
- by roughly 18–20 minutes when a tool/session budget appears near the known interruption window;
- before voluntarily ending a long execution session.

Because exact tool lifetime may not always be observable, milestone checkpoints are mandatory and more important than relying on a clock alone.

A checkpoint records at minimum: protocol version, active lanes, status/gate, approved scope or approval state, reconciled commit, completed items, in-progress/blocked items, parallel work still actionable, next actions, direct evidence references, and `do_not_repeat` safeguards.

Do not convert checkpointing into a pause. After writing a checkpoint, continue automatically unless `OWNER_PERMISSION_REQUIRED`, `OWNER_DECISION_REQUIRED` or a proven `TOOL_CAPABILITY_LIMIT` applies and no independent work remains.

## 8. Resume protocol

On resume:
1. read `AI_ENTRYPOINT.md`, `AI_OPERATING_CONTRACT.md`, `CHECKPOINT.md`, `CONTEXT_INDEX.md`;
2. compare checkpoint reconciliation point with current `main` changes;
3. read the minimum relevant authority/source/evidence;
4. verify uncertain previous outcomes before repeating any mutation;
5. rebuild the dependency/parallel-work view;
6. continue from all currently actionable safe nodes, not just a single serial next step.

If only unrelated documentation changed, use focused reconciliation. If relevant authority/source changed, reconcile before continuing. If the checkpoint cannot be reconciled safely, escalate to FULL for the affected lane while unrelated lanes continue.

Never repeat a migration/deploy/provider mutation merely because a previous session ended before reporting the result. Inspect evidence first.

## 9. Evidence before PASS

A requested or automated action is not `PASS` merely because the command was issued. PASS requires observable evidence such as a successful API response, provider state, GitHub Actions result, remote health result, or other task-appropriate verification.

If outcome is uncertain, record `UNKNOWN`/`VERIFY_REQUIRED`, investigate it, and continue independent work. Do not turn uncertainty alone into an Owner stop.

## 10. Fail closed

Before provider changes, verify the current account, environment and exact resource identity. Missing or mismatched expected resources must stop the affected operation unless Owner explicitly approves a reviewed replacement plan.

Do not silently recreate provider resources, overwrite unknown databases, broaden permissions, promote STABLE, or infer current remote state from old history.

Fail-closed applies to the affected mutation, not automatically to all project progress. Continue independent safe lanes.

## 11. Repository discipline

- `main` is the active source/authority baseline.
- `beta` and `stable`, when used, are deployment pointers rather than scratch branches.
- Move/promote STABLE only after BETA PASS and explicit Owner approval.
- `CHECKPOINT.md` holds short-lived execution/resume state.
- `CURRENT_STATE.md` holds concise current system truth.
- `NEXT_ACTIONS.md` holds remaining ordered/parallel gates and work.
- `SERVICE_AUTHORITY.md` holds canonical identities/resource authority.
- `DECISIONS.md` holds active architectural/operational decisions.
- `CHANGELOG.md` holds history and is not a default FAST read.
- Avoid duplicating the same detailed ledger across files.

## 12. Source restoration and history

`backup/pre-zero-20260912` is evidence/reference, not current authority. Restore a component only after reviewing it against current scope and correcting stale assumptions.

## 13. Security

Sensitive access/signing material stays outside source history. Repository documentation records only identifiers, ownership, secret names where necessary, and verification state; never secret values.
