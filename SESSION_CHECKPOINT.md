# SESSION CHECKPOINT

Checkpoint ID: `PROVIDER-RECOVERY-20260911-02`
Timestamp: `2026-09-11T20:53+07:00`
Authority branch: `audit/least-privilege-20260911`
PR: `#2 Security: deep least-privilege permission audit and recovery hardening`

## Why this checkpoint supersedes PROVIDER-RECOVERY-20260911-01

The first recovery checkpoint correctly replaced the decommissioned Google/GitHub identities, but the initial authorization guide still requested several permissions/variables that were broader than current source requires and placed some GitHub entry steps before provider outputs existed.

This checkpoint records the deeper permission audit against both current VHDCHY source and the full retired Pick Pack 1291 source backup, and fixes the dependency order to provider-first BETA recovery.

The prior LAN checkpoint remains valid technical evidence at `docs/checkpoints/2026-09-11-LAN-PILOT-20260911-04.md`.

## Current identities

- Google runtime owner: `automation@supra.cc.cd` — VERIFIED_CURRENT through Drive connector.
- `vanhanhdchungyen@gmail.com`: DECOMMISSIONED / DO NOT USE.
- GitHub current account: `tamnv2` — VERIFIED_CURRENT.
- GitHub authority repo: `tamnv2/vanhanhsupradchungyen`.
- Legacy repo `tamnv2supra/vanhanhdchungyen`: LEGACY_REFERENCE only.

## DONE in this tranche

### Deep permission audit

Reviewed current permission-bearing VHDCHY source/config and expanded/scanned the full legacy Pick Pack 1291 main + beta/current source backup, then inspected implementation paths with permission hits.

Verified legacy capabilities included:

- MailApp email for historical reset/OTP flows;
- DriveApp file/artifact/fallback/OTA handling;
- ScriptApp installable-trigger management in historical bridge code;
- UrlFetchApp external bridge calls;
- service-side Google OAuth/Sheets/Drive paths;
- FCM service-account logic and other historical DR/provider integrations.

These are legacy evidence only and are not current VHDCHY permissions by default.

### Current least-privilege result

Google CI OAuth:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

GAS runtime:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

Cloudflare CI token current minimum:

```text
Account -> Workers Scripts -> Write
Account -> D1 -> Write
```

GitHub:

- repository default `GITHUB_TOKEN` remains read-only;
- deploy jobs use `contents: read`;
- LAN release job alone uses workflow-level `contents: write`;
- no broad PAT / Actions PR-approval capability required.

### Source hardening on PR #2

- `gateway/Code.gs`: removed DriveApp/temp Sheet trash/UrlFetch/trigger probes; bootstrap targets the current projection workbook and owner identity only.
- `gateway/appsscript.json`: reduced to `spreadsheets` + `userinfo.email`.
- `scripts/deploy-gas.sh`: uses `GOOGLE_SHEETS_PROJECTION_ID` instead of Drive root.
- deploy BETA/STABLE workflows: remove unused `CF_ZONE_ID` / Drive root inputs and use projection spreadsheet ID.
- `config/projections.beta.json`: corrected to current workbook `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`, status `PROVISIONED_NOT_LIVE`.
- `scripts/deploy-cloudflare.sh`: expected D1 missing now fails closed; no automatic replacement database creation.
- governance validation now rejects stale old workbook ID and reintroduced broad GAS scopes.
- `SERVICE_AUTHORITY.md`, `CURRENT_STATE.md`, `NEXT_ACTIONS.md`, GitHub setup and reauthorization runbook updated to the audited boundaries.

### CI validation

PR #2 head `64563b3b28ad3b268b333cb78e88341889d08b39` validation run `34609361074`: **SUCCESS**.

Successful job included:

- shell validation;
- governance continuity;
- Apps Script manifest validation;
- local D1 migrations;
- forbidden private material check;
- signing-ignore enforcement.

Additional checkpoint commits were added after that validated head, so final branch validation must be re-checked before merge.

## Current Drive/runtime state

- VHDCHY Drive structure under `automation@supra.cc.cd`: VERIFIED_CURRENT.
- BETA projection workbook: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ` — `PROVISIONED_NOT_LIVE`.
- `BACKUP DỰ ÁN CŨ PICK PACK 1291`: reference-only; never current runtime/config.
- New BETA/STABLE GAS/Cloud/OAuth resources do not exist yet.
- Cloudflare setup is retained but current deploy token has not yet been re-verified in the new GitHub environment.
- VHDCHY BETA signing backup still requires Owner-side verification/input to new GitHub secrets.

## Exact Owner actions required next — provider first

Do not start by filling GitHub placeholders.

1. Google BETA: create `VHDCHY-BETA`, enable Google Apps Script API, enable account-level Apps Script API access, configure External Auth Platform, switch intended final CI app to `In production`, create Web OAuth client, mint one final refresh token with exactly the two CI scopes above.
2. GAS BETA: create/link standalone script, use audited current repo source, run `bootstrapAuthorize()` once, deploy Web App, record Script ID / Deployment ID / `/exec` URL.
3. In parallel: verify current Cloudflare account ID + create/verify custom token with Workers Scripts Write + D1 Write.
4. In parallel: verify retained **VHDCHY BETA** signing keystore/alias/passwords; do not use the legacy Pick Pack signer.
5. Only after those outputs exist: populate GitHub Environment `beta` with the exact variables/secrets documented in `docs/GITHUB_ENV_SETUP.md`.

Do not paste secrets into chat.

## Work AI can continue automatically after Owner inputs

- verify environment values indirectly through CI/provider behavior without reading secret values;
- create current-ID `verify-environments.yml` without widening OAuth scopes;
- verify OAuth refresh, Apps Script project/deployment and `/exec` identity;
- verify current projection workbook through GAS runtime;
- verify retained Cloudflare D1/Worker without resource recreation;
- verify VHDCHY BETA signer/build;
- run BETA deploy/health and checkpoint exact commit/run/resource IDs;
- confirm/move BETA only after PASS;
- prepare isolated STABLE recovery only after explicit Owner approval.

## Security gate not yet active

GAS Web App foundation is public-reachable for current health behavior and `doPost()` remains fail-closed with `FOUNDATION_ONLY` 503. Before future business projection writes are enabled, Worker -> GAS privileged calls require an application-level authenticated boundary. Public URL knowledge must never become write authorization. This does not require broader Google OAuth scopes.

## Parallelization

Google BETA setup, Cloudflare token verification and VHDCHY BETA signing recovery are independent and should be done in parallel. GAS setup depends on the selected BETA Cloud project. GitHub beta Environment entry depends on verified provider outputs. STABLE depends on BETA PASS + Owner approval.

## LAN continuity

Physical V4 regression remains pending exactly as before. Do not mark LAN-PILOT PASS until restricted laptop + exactly two MT90 evidence is completed. Provider recovery does not change the LAN result.
