# VẬN HÀNH DC HƯNG YÊN — OWNER DECISIONS V8

Status: ACTIVE
Decision date: 2026-09-14
Authority: explicit Owner instruction in current session
Supersedes: only conflicting language-delivery and execution/reporting details in older authority; all non-conflicting decisions remain active.

## V8-001 — UI language is Vietnamese only for the current delivery stage

Until a later Owner decision explicitly starts multilingual delivery, all current user-facing Web and Android/PDA App implementation shall be **Vietnamese only**.

Rules:
- do not build or expose Vietnamese / English / Chinese language switching now;
- do not spend current implementation time on translation catalogs, language selectors, locale persistence or multilingual acceptance;
- current screens, labels, messages and operator-facing help use Vietnamese;
- architecture may avoid choices that would make future internationalization unnecessarily difficult, but future multilingual support is deferred work and must not block current delivery;
- any older requirement that current BETA must expose multiple languages or default to English is superseded for the current stage.

## V8-002 — Mandatory factual progress report before tool/session interruption

When a tool/session budget is near exhaustion, a long execution block is being ended, or the AI must stop using tools for any practical limit, it must first checkpoint current project truth where possible and then report the result to the Owner.

The report must state, based on evidence only:
- what actually reached PASS in the current execution block;
- what FAILED, remains IN_PROGRESS, is BLOCKED, or was not started;
- direct evidence identifiers where available (commit, workflow run, provider readback, test result or equivalent);
- current evidence-weighted project percentage and the delta from the start of the execution block, if the percentage legitimately changed;
- the exact next ready work items.

Do not present issued commands, planned work, source edits without verification, or elapsed/tool time as completed progress. Do not inflate percentage merely because many tool calls were made.

If no material product progress was proven, say so explicitly and keep the percentage unchanged.

## V8-003 — Ready-queue parallel execution is mandatory

The dependency graph is operational, not descriptive.

At any execution point:
1. identify all currently ready work nodes whose prerequisites are satisfied;
2. execute every independent safe ready node in parallel where the available tools permit;
3. serialize only dependency-bound work or writes that target the same file/ref/database/provider resource;
4. when one prerequisite chain advances, immediately unlock and execute its next dependent node without waiting for unrelated lanes;
5. if a node fails or blocks, isolate it and continue every independent ready node;
6. continuously refill the ready queue from unfinished work rather than waiting for a whole lane to finish before starting another unrelated lane.

Example scheduling rule: if `A -> B -> C` is one dependency chain and `D`, `E`, `F` are independent, execute `A + D + E + F`; after A finishes, execute `B + remaining D/E/F`; then `C + remaining independent work`.

Tool limitations may prevent literal simultaneous API calls, but execution ordering must still emulate the same ready-queue behavior: no independent work may be intentionally held behind an unrelated dependency chain.

## V8-004 — Progress percentage remains evidence-weighted

`docs/PROGRESS_TRACKING_V1.md` remains the percentage model unless separately revised.

Progress may increase only when newly completed work satisfies the evidence rule for the affected phase. Governance/document edits alone do not increase product completion unless they close a weighted acceptance requirement.
