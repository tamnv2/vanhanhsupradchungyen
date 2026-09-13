# CURRENT STATE

Updated: 2026-09-13
Baseline: `REPO-RESET-20260912-01`

## GitHub / authority

- Active repository: `tamnv2/vanhanhsupradchungyen` (PUBLIC), default branch `main`.
- Pre-zero snapshot retained temporarily at `backup/pre-zero-20260912`.
- `AI_AUTHORITY_RESUME_V2` is active.
- Owner-approved 2026-09-13 business/data decisions are persisted in `DECISIONS.md`.
- Owner execution policy is active: direct connected action first; otherwise GitHub-hosted CI; request only exact missing permission/consent; do not require laptop-local tooling for routine cloud/provider operations.
- Secret values remain outside repository source.

## GitHub BETA environment

- Environment `beta` is the provider execution boundary.
- Google CI credentials/variables are configured and proven by successful Apps Script automation.
- Cloudflare credentials are configured as Environment secret/variable; raw token values are not exposed to source or chat.
- Read-only Cloudflare inspection, guarded D1 migration and guarded Worker deployment are available through GitHub-hosted Actions.

## Google / Drive / Sheets / GAS

- Current Google authority: `tam95.supra@gmail.com`.
- Project root: `VẬN HÀNH DC HƯNG YÊN` with `01_BETA` and `02_STABLE` environment roots.
- BETA cluster: `PICK_PACK_1291`.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`.
- Google Cloud/OAuth BETA: PASS.
- GAS BETA foundation: PASS.
- Business `doPost()` remains foundation-only; Google projection is not yet business-live.

## Cloudflare D1 BETA — BUSINESS_CORE_V3 PASS

- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`.
- D1: `vhdchy-data-beta`.
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`.
- Guarded migration run `34752340290` — SUCCESS.
- Independent post-migration verification run `34752381290` — SUCCESS.
- Provider schema: `business_core_v3`.
- Full V3 table contract and baseline seeds verified.
- Business rows remain zero.
- Foreign-key and quick integrity checks passed.

## Cloudflare Worker BETA — V3 DEPLOYED + HEALTH PASS

- Worker: `vhdchy-beta`.
- Public origin: `https://beta.supra.cc.cd`.
- `workers.dev`: disabled and preserved disabled.
- Binding names/types verified before and after deploy: `DB`/D1, `APP_ENV`/plain text, `BUILD_SHA`/plain text, `GAS_EXEC_URL`/plain text.
- Guarded deploy run `34752917714` — SUCCESS.
- Deployed build SHA: `6b23f7134e02c7b53571c0a151f97f27a86bcb2e`.
- `/health`: PASS, environment `BETA`, D1 schema `business_core_v3`.
- `/health/deep`: PASS; Google Gateway was healthy at verification time.
- `/api/v1/meta`: PASS, runtime `BUSINESS_CORE_V3`.
- `/api/v1/capabilities`: PASS, authority `D1`, anonymous mutation disabled.
- Independent post-deploy provider inspection run `34752966242` — SUCCESS; routing, binding names/types and D1 V3 state remained correct.

## Service implementation state

- D1 schema and Worker foundation are now live in BETA.
- Business data/admin API paths intentionally remain closed with `AUTH_REQUIRED` until authenticated session and effective-permission enforcement are implemented.
- Google projection/outbox processing is not business-live yet.
- Web business flows are not business-live yet.
- Next Service implementation gate is authentication/session/permission enforcement, followed by projection/outbox processing and business APIs/acceptance scenarios.

## Target architecture

- D1 is canonical business authority.
- Google Sheets is projection/reconciliation only.
- Google Drive stores media/documents/archive while D1 stores identifiers, metadata, hashes and state.
- Web and later APK use one Service/domain contract.
- LAN remains an independent transport/fallback lane using the same command/event model, not a second backend.

## Android / LAN / STABLE

- Android BETA signer: `VERIFY_REQUIRED` when signing material/machine is available.
- Physical LAN regression remains paused while Owner is off-site; Service work continues independently.
- STABLE remains blocked until BETA PASS and explicit Owner approval.
