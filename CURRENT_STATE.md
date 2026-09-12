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
- Actions Repository secrets/variables remain unused.
- Clean validation workflow remains active.

## GitHub BETA environment — GOOGLE READY

- Environment `beta` exists.
- Owner UI verified environment secret names:
  - `GOOGLE_OAUTH_CLIENT_SECRET`
  - `GOOGLE_OAUTH_REFRESH_TOKEN`
- Owner UI verified environment variable names:
  - `APP_ENV=BETA`
  - `OWNER_EMAIL=tam95.supra@gmail.com`
  - `GOOGLE_OAUTH_CLIENT_ID`
  - `GAS_SCRIPT_ID`
  - `GOOGLE_SHEETS_PROJECTION_ID`
- No secret values are recorded in repository files.
- `.github/workflows/gas-beta-sync.yml` supports guarded dispatch from `main` by changes to `.github/dispatch/gas-beta-sync.json`; manual `workflow_dispatch` remains fallback only.
- `.github/scripts/gas-sync.mjs` refreshes the OAuth access token, syncs/reads back Apps Script HEAD, and contains versioned deployment + `/exec` verification logic for the post-bootstrap gate.
- Initial sync run `34693049129` completed SUCCESS.
- Bootstrap-health source was synchronized in run `34695050157`, which also completed SUCCESS.

## Drive / Sheet — VERIFIED CURRENT

- Project root: `VẬN HÀNH DC HƯNG YÊN`.
- Active environment roots: `01_BETA` and `02_STABLE`.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`.
- Legacy setup is outside the project runtime root and remains reference-only.

## Source baseline

- Google Gateway foundation: restored after least-privilege review and synchronized from GitHub to Apps Script HEAD.
- Gateway `doGet()` now reports only non-secret bootstrap health booleans (`authorized`, `configMatch`) so deployment verification can prove that `bootstrapAuthorize()` successfully opened the designated workbook and stored the expected environment/owner/projection properties.
- Worker foundation: restored under `service/worker/` with reconciled contract `business_core_v2` / `BUSINESS_CORE_V2`.
- D1 schema: consolidated into clean zero-baseline `service/worker/migrations/0001_initial.sql`; no legacy compatibility `resources` table and no historical business rows/credentials are seeded.
- Android/LAN source and evidence: preserved only in `backup/pre-zero-20260912` pending later task-specific restoration.

## Provider state

- Google Cloud/OAuth BETA: `OAUTH_READY`.
  - Standard Cloud project `VHDCHY-BETA` confirmed.
  - Apps Script API enabled and account-level Apps Script API access enabled.
  - OAuth app is External / In production; branding published.
  - CI OAuth client exists with `script.projects` + `script.deployments` only.
  - Refresh token obtained and successfully used by GitHub Actions.
  - Real `projects.create`, `projects.getContent` and `projects.updateContent` API operations succeeded under the BETA automation path.
- GAS BETA: `BOOTSTRAP_OWNER_REPORTED_PENDING_REMOTE_VERIFY`.
  - `VHDCHY BETA - Google Gateway` exists and is linked to standard Cloud project `VHDCHY-BETA`.
  - GitHub source is synchronized to Apps Script HEAD and read-back verification passed.
  - Owner reports that interactive runtime authorization for `bootstrapAuthorize()` has completed under `tam95.supra@gmail.com`.
  - This owner report is not yet treated as final PASS; the next versioned deployment must verify `/exec` with `bootstrap.authorized=true` and `bootstrap.configMatch=true`.
  - Immutable version, active versioned deployment and `/exec` verification remain pending.
- Cloudflare BETA resources: `VERIFY_REQUIRED`.
- Android BETA signer: `VERIFY_REQUIRED`.
- STABLE: blocked until BETA PASS + explicit Owner approval.

## LAN

Physical regression is paused while Owner is off-site. Existing source/evidence remains preserved in the pre-zero backup branch. When Owner is at the company, LAN and Service may proceed in parallel where independent.
