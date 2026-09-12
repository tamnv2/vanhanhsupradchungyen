# CHANGELOG

## 2026-09-12 — AI_AUTHORITY_RESUME_V2

- Added `AI_ENTRYPOINT.md` as the single project bootstrap entry.
- Added `CONTEXT_INDEX.md` with FAST / FOCUSED / FULL read routing to reduce unnecessary context/token use.
- Added `CHECKPOINT.md` as the short resume ledger for interruption/chat handoff.
- Upgraded `AI_OPERATING_CONTRACT.md` with a hard memory non-authority rule, Owner approval boundary, dependency/parallel execution model, checkpoint/interruption protocol, evidence-before-PASS and fail-closed resume behavior.
- Standardized Owner resume aliases: `Tiếp tục VHDCHY`, `Tiếp tục việc đang làm`, `Tiếp tục việc đang dở`.
- Standardized full-audit aliases: `Tiếp tục VHDCHY — full audit.`, `Rà soát toàn bộ dự án`, `Kiểm tra toàn bộ dự án`.
- Extended baseline CI to validate protocol files and invariants.

## 2026-09-12 — REPO-RESET-20260912-01

- Preserved the pre-zero state at `backup/pre-zero-20260912`.
- Cleared the active `main` tree to a zero marker and rebuilt authority from the confirmed current scope.
- Kept the current Drive and BETA Sheet identifiers.
- Retained the reviewed least-privilege Google Gateway foundation.
- Owner manually removed obsolete branches, LAN tags, GitHub Environments, and all Actions environment/repository secrets and variables.
- Current branches are only `main` + temporary `backup/pre-zero-20260912`; current tag namespace and Releases are empty.
- Reconciled the pre-reset Worker/D1 version conflict by creating a clean `business_core_v2` / `BUSINESS_CORE_V2` source contract.
- Consolidated the approved D1 model into `service/worker/migrations/0001_initial.sql` without legacy compatibility tables, historical business rows or credential seeds.
- Expanded baseline CI to validate authority files, JSON configuration, exact GAS OAuth scopes, Worker syntax, D1 schema application and Worker/schema version agreement.
- Provider deploy scripts/workflows remain intentionally absent until current Google/Cloudflare/signer resources are verified.
- LAN/Android source remains preserved only in the temporary pre-zero backup branch pending later review while Owner is off-site.
