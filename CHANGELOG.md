# CHANGELOG

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
