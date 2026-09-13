# CHATGPT PROJECT BOOTSTRAP — VHDCHY

Status: REQUIRED EXTERNAL BOOTSTRAP

Purpose: ensure a fresh ChatGPT Project chat loads current GitHub authority before resuming VHDCHY work.

## Required Project instruction

Add this rule to the ChatGPT Project instructions:

> For VHDCHY, when the user says `Tiếp tục VHDCHY`, `Tiếp tục việc đang làm`, or `Tiếp tục việc đang dở`, first use the connected GitHub repository `tamnv2/vanhanhsupradchungyen`, branch `main`, and live-read `AI_ENTRYPOINT.md` in the current chat. Then execute the bootstrap defined there before answering or changing project state. Memory, project summaries, previous chats, and remembered provider state are NON_AUTHORITY and may only help locate what must be verified. If GitHub HEAD differs from `CHECKPOINT.md.reconciled_through_commit`, reconcile the changed authority/current-state/source paths before continuing.

## Repository-side guarantees

The repository enforces the following:
- `AI_ENTRYPOINT.md` defines the live GitHub bootstrap sequence.
- `CONTEXT_INDEX.md` requires all active decision layers.
- `CHECKPOINT.md` is a resume ledger, not higher authority than newer `main`.
- `.github/workflows/validate.yml` checks authority-file presence, resume invariants, and checkpoint freshness against active authority/current-guide changes.

## Acceptance test

A fresh chat passes bootstrap acceptance only if it can demonstrate this order before project mutation:
1. live fetch `AI_ENTRYPOINT.md` from GitHub `main`;
2. read `AI_OPERATING_CONTRACT.md`, `CHECKPOINT.md`, and `CONTEXT_INDEX.md`;
3. obtain current `main` HEAD and compare it with the checkpoint reconciliation commit;
4. read all currently active decision layers and relevant changed paths;
5. rebuild the active dependency/parallel-work view;
6. resume unfinished approved work without relying on remembered chat state as authority.

This file documents the external Project-setting requirement. The repository cannot edit ChatGPT Project settings by itself.