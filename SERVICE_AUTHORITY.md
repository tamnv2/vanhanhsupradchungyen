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
- Current verified immutable version: `3`
- Source authority: `service/google-gateway/`
- Runtime bootstrap: `PASS`
- Versioned deployment / `/exec` identity + bootstrap verification: `PASS`
- Projection contract protocol: `VHDCHY_PROJECTION_V1`
- Projection contract deployment run: GitHub Actions `34753872034` — PASS
- Projection write state: `FAIL_CLOSED / NOT_LIVE`; projection auth verifier and explicit enable state are required before writes are accepted.

The managed deployment preserves the same canonical deployment ID and `/exec` URL across immutable versions. Version 3 adds the reviewed projection batch contract while retaining fail-closed behavior. Future releases update the same managed deployment rather than creating uncontrolled deployment identities.

## Cloudflare

Managing email: `nguyenvantam050595@gmail.com`.
Intended zone/domain: `supra.cc.cd`.

Verified BETA account/resource authority:
- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`
- API token form: account-owned API token stored only in GitHub Environment `beta`
- BETA Worker: `vhdchy-beta` — FOUND / V3 DEPLOYED
- BETA Worker public origin: `https://beta.supra.cc.cd`
- BETA Worker `workers.dev`: DISABLED
- BETA Worker bindings: `DB` (D1), `APP_ENV` (plain text), `BUILD_SHA` (plain text), `GAS_EXEC_URL` (plain text)
- BETA D1: `vhdchy-data-beta` — FOUND
- BETA D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`
- Initial identity verification run: GitHub Actions `34699120539` — PASS
- Automated pre-migration D1 inspection run: GitHub Actions `34749417468` — PASS
- Guarded BETA D1 migration run: GitHub Actions `34752340290` — PASS
- Independent post-migration D1 inspection run: GitHub Actions `34752381290` — PASS
- Guarded Worker BETA deploy run: GitHub Actions `34752917714` — PASS
- Independent post-deploy provider inspection run: GitHub Actions `34752966242` — PASS

Current authoritative D1 state:
- Provider schema version: `business_core_v3`
- Target table contract: 54 application tables plus D1 internal `_cf_KV`
- Required seeds verified: `clusters=1`, `modules=1`, `cluster_modules=1`, `module_domain_registry=6`, `resource_type_catalog=4`, `labor_type_catalog=4`, `root_security_policy=1`, `vhdchy_meta=8`
- Business rows remain zero after migration
- Legacy V1 tables `resources`, `session_resource_bindings`, `document_metadata`, `d1_migrations` are absent
- `PRAGMA foreign_key_check`: PASS
- `PRAGMA quick_check`: PASS
- Classification: `BUSINESS_CORE_V3 / MIGRATION_PASS / ZERO_BUSINESS_ROWS`

Current authoritative Worker BETA state:
- Source authority currently deployed: `service/worker/src/index.js`
- Deployed build SHA: `6b23f7134e02c7b53571c0a151f97f27a86bcb2e`
- Runtime state: `BUSINESS_CORE_V3`
- `/health`: PASS with BETA environment and D1 `business_core_v3`
- `/health/deep`: PASS; Google Gateway not degraded at deployment verification
- `/api/v1/meta`: PASS
- `/api/v1/capabilities`: PASS; D1 authority and anonymous mutation disabled
- Custom domain preserved: `beta.supra.cc.cd`
- `workers.dev` remained disabled before and after deployment

Auth/session/permission and outbox source modules are under active development on `main`; they are not treated as deployed Worker runtime until the guarded multi-module deployment path and runtime acceptance tests pass.

The D1 migration was executed only after exact preflight and fixed reviewed migration inputs. The Worker deployment was executed only after exact resource/routing/binding/schema preflight and then independently reverified. Raw Cloudflare credentials remain outside source and chat.

Expected STABLE names remain reserved only:
- STABLE Worker: `vhdchy-stable`
- STABLE D1: `vhdchy-data-stable`

Cloudflare operations are fail-closed: missing/mismatched expected resources or unexpected provider state must stop the affected mutation/deployment; independent safe work continues under `AI_AUTHORITY_RESUME_V2`.

## LAN Service authority model

Current product authority is defined by `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md` and D-042..D-048.

- LAN Service is a first-class substitute runtime, not merely a transport test.
- Normal global canonical structured authority remains D1 after Cloud commit/reconciliation.
- In `LAN_RELAY`, Cloud Service/D1 remains the immediate authoritative commit path.
- In `LAN_AUTONOMOUS`, the LAN Service may durably accept reviewed offline-capable business events into local edge state/event/outbox while Cloud is unreachable; these events are pending global reconciliation rather than disposable receipts.
- Successful reconciliation commits/reconciles them into D1 exactly once where non-conflicting.
- Conflicts remain explicit evidence and must not be silently overwritten/dropped.
- LAN Service may not use Google Sheets as a fallback database.

No production LAN Service implementation is yet authoritative/runtime-PASS. The prior transport-only `android-pilot/` + `lan-agent/` build is a disposable prototype/reference only.

## Website / Android client authority

- Website and APK use one reviewed Service/domain contract.
- Website is the wider/full browser surface.
- APK is the PDA-optimized operational surface.
- Neither client may bypass Service/domain rules with direct D1/Sheets writes.
- Both must understand Cloud-direct, LAN-relay, LAN-autonomous and pending-sync/conflict statuses.

## Android signing

Existing signing material is reference until locally re-verified. Do not use retired Pick Pack legacy signer. No keystore content or password may be committed.

## Unresolved offline security policy

Do not invent or treat as locked:
- exact offline credential/capability mechanism;
- exact offline expiry/TTL;
- privileged/security operations allowed in autonomous LAN mode;
- authorization for explicit emergency autonomous-mode entry/exit.

These remain fail-closed design gates until reviewed.

## Secrets

Secret values live only in provider secret stores / GitHub Environments or reviewed local secure storage when LAN security design is implemented. Repository files may contain only secret names, non-secret resource IDs, verification state and non-sensitive policy.
