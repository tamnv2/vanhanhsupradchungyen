# SERVICE AUTHORITY

Status: ACTIVE / OWNER RECONCILED 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub

- Repo: `tamnv2/vanhanhsupradchungyen`
- Visibility: PUBLIC
- Current managing identity: `tamnv2` / `nguyenvantam050595@gmail.com`
- Default branch: `main`
- Pre-zero snapshot: `backup/pre-zero-20260912`

## Google

Current owner for Drive / Sheets / GAS: `tam95.supra@gmail.com`.

Current BETA projection workbook:
- Title: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`
- ID: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`
- Schema: `PP1291_SHEETS_BETA_V1`
- Status: `PROVISIONED_NOT_LIVE`

Current Google runtime scopes:
- `https://www.googleapis.com/auth/spreadsheets`
- `https://www.googleapis.com/auth/userinfo.email`

Current CI OAuth scopes:
- `https://www.googleapis.com/auth/script.projects`
- `https://www.googleapis.com/auth/script.deployments`

Google Cloud / OAuth BETA:
- Standard Cloud project: `VHDCHY-BETA`
- OAuth app: External / In production
- OAuth owner/operator: `tam95.supra@gmail.com`
- CI client: `VHDCHY BETA CI`
- Status: `PASS`

Google Apps Script BETA authority:
- Script title: `VHDCHY BETA - Google Gateway`
- Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`
- Canonical managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`
- Canonical Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`
- Verified immutable version: `2`
- Source authority: `service/google-gateway/`
- Runtime bootstrap: `PASS`
- Versioned deployment / `/exec` identity + bootstrap verification: `PASS`
- Verification run: GitHub Actions `34696139468`

The manually-created deployment URL supplied during bootstrap is non-authoritative and superseded by the managed deployment above. Future releases update the managed deployment to a new immutable version instead of creating uncontrolled deployment identities.

## Cloudflare

Managing email: `nguyenvantam050595@gmail.com`.
Intended zone/domain: `supra.cc.cd`.

Verified BETA account/resource authority:
- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`
- API token form: account-owned API token stored only in GitHub Environment `beta`
- BETA Worker: `vhdchy-beta` — FOUND
- BETA D1: `vhdchy-data-beta` — FOUND
- BETA D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`
- Identity verification run: GitHub Actions `34699120539` — PASS
- Automated identity + D1 read-only inspection run: GitHub Actions `34748247818` — PASS

Automated D1 inspection evidence:
- Provider schema version: `business_core_v1`
- Business row state: ZERO BUSINESS ROWS across all observed business tables
- Provider bookkeeping rows only: `d1_migrations=1`, `vhdchy_meta=3`
- Classification: `BUSINESS_CORE_V1 / ZERO_BUSINESS_ROWS / SCHEMA_MISMATCH`
- The inspection reads only schema metadata, schema version and table counts; it does not read business row contents or issue DML/DDL writes.

Expected STABLE names remain reserved only:
- STABLE Worker: `vhdchy-stable`
- STABLE D1: `vhdchy-data-stable`

Cloudflare operations are fail-closed: missing/mismatched expected resources or unexpected business rows must stop migration/deployment; never silently recreate provider resources.

Current token is already proven capable of the automated read-only D1 inspection through the GitHub Actions Environment bridge. Provider permission does not expose the raw token to ChatGPT; GitHub Actions receives the secret at runtime.

The earlier `business_core_v2` source migration is now stale relative to Owner-approved 2026-09-13 target decisions. Do not apply it. Reconcile and validate the new target schema first, then rerun automated read-only inspection immediately before any BETA D1 write.

## Android signing

Existing signing material is reference until locally re-verified. Do not use retired Pick Pack legacy signer. No keystore content or password may be committed.

## Secrets

Secret values live only in provider secret stores / GitHub Environments. Repository files may contain only secret names, non-secret resource IDs, verification state and non-sensitive policy.
