# SERVICE AUTHORITY

Status: ACTIVE / CLEAN BASELINE 2026-09-12
Baseline: `REPO-RESET-20260912-01`

## GitHub

- Repo: `tamnv2/vanhanhsupradchungyen`
- Visibility: PUBLIC
- Current managing identity: `tamnv2` / `nguyenvantam050595@gmail.com`
- Default branch: `main`
- Pre-zero snapshot: `backup/pre-zero-20260912`
- Old branches/tags are historical only until manually removed.

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

Expected environment names remain:
- BETA Worker: `vhdchy-beta`
- BETA D1: `vhdchy-data-beta`
- STABLE Worker: `vhdchy-stable`
- STABLE D1: `vhdchy-data-stable`

Cloudflare resources must be verified before any active source/deploy workflow is restored. Missing expected resources are recorded as a mismatch; no silent replacement.

Minimum intended deploy-token permissions from retained design:
- Account: `Workers Scripts Write`
- Account: `D1 Write`

## Android signing

Existing signing material is reference until locally re-verified. Do not use retired Pick Pack legacy signer. No keystore content or password may be committed.

## Secrets

Secret values live only in provider secret stores / GitHub Environments when those environments are rebuilt. Repository files may contain only secret names and status.
