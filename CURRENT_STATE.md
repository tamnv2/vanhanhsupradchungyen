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

## GitHub BETA environment — GOOGLE + CLOUDFLARE READY

- Environment `beta` exists.
- Google CI credentials/variables are configured and proven by successful Apps Script automation.
- Cloudflare credentials are configured:
  - `CLOUDFLARE_API_TOKEN` is stored as an Environment secret.
  - `CLOUDFLARE_ACCOUNT_ID` is stored as an Environment variable.
- No secret values are recorded in repository files.
- `.github/workflows/gas-beta-sync.yml` supports guarded Google Gateway synchronization/deployment.
- `.github/workflows/cloudflare-beta-verify.yml` verifies Cloudflare account/token identity and expected BETA resource identity without creating resources.

## Drive / Sheet — VERIFIED CURRENT

- Project root: `VẬN HÀNH DC HƯNG YÊN`.
- Active environment roots: `01_BETA` and `02_STABLE`.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`; Gateway deployment does not by itself make business projection live.
- Legacy setup is outside the project runtime root and remains reference-only.

## Source baseline

- Google Gateway foundation: restored after least-privilege review and synchronized from GitHub to Apps Script HEAD.
- Gateway `doGet()` reports only non-secret bootstrap health booleans (`authorized`, `configMatch`) and deployment identity.
- Worker foundation: restored under `service/worker/` with reconciled contract `business_core_v2` / `BUSINESS_CORE_V2`.
- D1 schema: consolidated into clean zero-baseline `service/worker/migrations/0001_initial.sql`; no legacy compatibility `resources` table and no historical business rows/credentials are seeded by source.
- Android/LAN source and evidence: preserved only in `backup/pre-zero-20260912` pending later task-specific restoration.

## Provider state

- Google Cloud/OAuth BETA: `PASS`.
  - Standard Cloud project `VHDCHY-BETA` confirmed.
  - OAuth app is External / In production; CI OAuth automation is operational.
- GAS BETA foundation: `PASS`.
  - Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`.
  - Managed immutable version: `2`.
  - Canonical managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`.
  - Canonical Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`.
  - `/exec` identity + bootstrap verification PASS.
  - Business `doPost()` remains `FOUNDATION_ONLY`; Google projection is not yet business-live.
- Cloudflare BETA identity/resources: `PASS`.
  - Verification run: `34699120539`.
  - Token type: account-owned API token; token verification PASS.
  - Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
  - Worker `vhdchy-beta`: FOUND.
  - D1 `vhdchy-data-beta`: FOUND.
  - D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
  - Verification created no resources and performed no migration/deployment writes.
- Cloudflare D1 contents/schema: `INSPECTION_REQUIRED_BEFORE_MIGRATION`.
  - Do not apply `0001_initial.sql` until the existing confirmed D1 is inspected for current tables/schema/data and classified as safe for zero-baseline migration.
- Android BETA signer: `VERIFY_REQUIRED`.
- STABLE: blocked until BETA PASS + explicit Owner approval.

## LAN

Physical regression is paused while Owner is off-site. Existing source/evidence remains preserved in the pre-zero backup branch. When Owner is at the company, LAN and Service may proceed in parallel where independent.
