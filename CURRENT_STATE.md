# CURRENT STATE

Updated: 2026-09-12
Baseline: `REPO-RESET-20260912-01`

## GitHub cleanup — PASS

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC).
- Default branch: `main`.
- Pre-zero snapshot retained temporarily at `backup/pre-zero-20260912`.
- All obsolete working/recovery/beta/stable branches were manually removed by Owner.
- All old LAN tags were manually removed by Owner; current tag namespace is empty.
- Releases: none.
- GitHub Environments: none — Owner UI verified after cleanup.
- Actions Environment secrets/variables: none — Owner UI verified after cleanup.
- Actions Repository secrets/variables: none — Owner UI verified after cleanup.
- Clean validation workflow remains the only active workflow required at this stage.

## Drive / Sheet — VERIFIED CURRENT

- Project root: `VẬN HÀNH DC HƯNG YÊN`.
- Active environment roots: `01_BETA` and `02_STABLE`.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`.
- Legacy setup is outside the project runtime root and remains reference-only.

## Source baseline

- Google Gateway foundation: restored after least-privilege review.
- Worker foundation: restored under `service/worker/` with reconciled contract `business_core_v2` / `BUSINESS_CORE_V2`.
- D1 schema: consolidated into clean zero-baseline `service/worker/migrations/0001_initial.sql`; no legacy compatibility `resources` table and no historical business rows/credentials are seeded.
- Provider deployment scripts/workflows: intentionally absent until current provider identities/resources are verified.
- Android/LAN source and evidence: preserved only in `backup/pre-zero-20260912` pending later task-specific restoration.

## Provider state

- Google Cloud/OAuth BETA: `SETUP_REQUIRED`.
- GAS BETA: `SETUP_REQUIRED`.
- Cloudflare BETA resources: `VERIFY_REQUIRED`.
- Android BETA signer: `VERIFY_REQUIRED`.
- GitHub `beta` Environment: intentionally absent; rebuild only from verified provider outputs.
- STABLE: blocked until BETA PASS + explicit Owner approval.

## LAN

Physical regression is paused while Owner is off-site. Existing source/evidence remains preserved in the pre-zero backup branch. When Owner is at the company, LAN and Service may proceed in parallel where independent.
