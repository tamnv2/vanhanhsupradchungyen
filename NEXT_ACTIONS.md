# NEXT ACTIONS

Checkpoint: `PROVIDER-RECOVERY-20260911-02`

## Priority objective

Restore the project without rebuilding provider resources that still exist:

`main authority -> provider-first BETA recovery -> GitHub beta registry -> verified BETA gate -> Owner-approved isolated STABLE`.

LAN Pilot V4 physical regression remains preserved and resumes after BETA infrastructure recovery.

## Dependency graph

```text
main authority hardening = DONE

Google BETA Cloud/OAuth/GAS ───────────────┐
Cloudflare retained token verification ────┼─> verified inputs -> GitHub beta Environment -> AI/CI BETA gate
VHDCHY BETA signing recovery ──────────────┘
Current Drive/workbook verification = DONE

BETA PASS -> explicit Owner approval -> isolated STABLE recovery
```

Independent provider lanes should run in parallel; GitHub beta variable/secret entry waits for their outputs.

## Lane A — GitHub authority hardening — DONE

- PR #2 merged to `main`: `6c23099b4ff64bbc55699b2611ffa57020ed28b3`.
- PR-head validation `34610887398`: SUCCESS.
- Post-merge `main` validation `34611144635`: SUCCESS.
- No `beta` or `stable` ref moved.
- Repository default Actions token remains read-only; workflows request only their own minimum permissions.

## Lane B — Google Cloud/OAuth BETA — Owner UI / NEXT

Create `VHDCHY-BETA` under `automation@supra.cc.cd`.

Current exact requirements:

- enable **Google Apps Script API only** for current CI deployment path;
- enable account-level Apps Script API access at `https://script.google.com/home/usersettings`;
- Auth Platform audience: External;
- final CI app publishing state: `In production` before minting durable refresh token;
- CI OAuth scopes exactly:
  - `https://www.googleapis.com/auth/script.projects`
  - `https://www.googleapis.com/auth/script.deployments`
- create one intended BETA refresh token; do not mint a token on every CI run.

Do not add Drive, Sheets, Gmail, Calendar, Contacts, `script.send_mail`, `script.scriptapp` or other scopes to the CI client.

## Lane C — GAS BETA — depends on intended BETA Cloud project

- Create standalone `VHDCHY BETA Google Gateway`.
- Link it to the BETA standard Cloud project.
- Use current audited `gateway/Code.gs` + `gateway/appsscript.json` from `main`.
- Current runtime scopes exactly:
  - `https://www.googleapis.com/auth/spreadsheets`
  - `https://www.googleapis.com/auth/userinfo.email`
- Current projection workbook: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`.
- Run `bootstrapAuthorize()` once as `automation@supra.cc.cd`; if consent asks for Drive/Gmail/mail/external-request/trigger-management/Calendar/Contacts, stop and re-audit.
- Deploy Web App using current foundation design.
- Record new `GAS_SCRIPT_ID`, `GAS_DEPLOYMENT_ID`, `GAS_EXEC_URL`.

Before business `doPost()` writes are activated in a later gate, Worker -> GAS privileged calls need an application-level authenticated boundary; public Web App reachability is not write authorization.

## Lane D — retained Cloudflare — parallel with Google

- Do not recreate zone/Worker/D1/DNS.
- Verify current account ID in the retained Cloudflare account.
- If token must be recreated, custom token minimum is:
  - Account `Workers Scripts Write`;
  - Account `D1 Write`.
- No DNS Write or Workers Routes Write for current Custom Domain path.
- Recovery deploy must fail if expected D1 is missing; do not auto-create a replacement.

## Lane E — VHDCHY BETA Android signing — parallel

- Locate retained **VHDCHY BETA** keystore, not retired Pick Pack 1291 signer in legacy archive.
- Verify alias/passwords locally; recorded alias `vhdchy-beta` is a reference until verified.
- Prepare base64 + store/key passwords privately for GitHub Environment secrets.
- Do not generate a new key.
- STABLE signer is not entered during BETA recovery because no current STABLE Android build workflow consumes it.

## Lane F — Drive/current workbook — DONE / PROVISIONED_NOT_LIVE

- Current BETA projection workbook ID: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`.
- `config/projections.beta.json` on `main` points to this ID and remains `PROVISIONED_NOT_LIVE`.
- Legacy folder `BACKUP DỰ ÁN CŨ PICK PACK 1291` remains reference-only.

## Lane G — GitHub BETA entry — ONLY after B–F outputs exist

Create/configure Environment `beta` and enter only values consumed by current workflows.

Variables:

```text
APP_ENV=beta
OWNER_EMAIL=automation@supra.cc.cd
CF_ACCOUNT_ID=<verified current Cloudflare account ID>
PUBLIC_HOST=beta.supra.cc.cd
GAS_SCRIPT_ID=<new BETA GAS>
GAS_DEPLOYMENT_ID=<new BETA deployment>
GAS_EXEC_URL=<new BETA /exec>
GOOGLE_SHEETS_PROJECTION_ID=17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ
ANDROID_SIGNING_ALIAS=<verified VHDCHY BETA alias>
```

Secrets:

```text
CLOUDFLARE_API_TOKEN
GOOGLE_OAUTH_CLIENT_ID
GOOGLE_OAUTH_CLIENT_SECRET
GOOGLE_OAUTH_REFRESH_TOKEN
ANDROID_SIGNING_KEY_B64
ANDROID_SIGNING_STORE_PASSWORD
ANDROID_SIGNING_KEY_PASSWORD
```

Do not create unused historical variables/scopes merely for parity.

## BETA integration gate — AI after Owner inputs

1. build current-ID environment verifier without broadening OAuth scopes;
2. verify Google refresh + Apps Script content/deployment + `/exec` identity;
3. verify current projection workbook through authorized GAS bootstrap/runtime path;
4. verify Cloudflare token accesses retained D1/Worker and deploy does not recreate resources;
5. verify VHDCHY BETA Android signer/build;
6. run repository validation and BETA deploy/health;
7. checkpoint commit/ref + run/job IDs + provider resources + PASS/FAIL;
8. only then confirm/move BETA pointer according to release gate.

## STABLE recovery

BLOCKED until BETA PASS + explicit Owner approval.

After approval, repeat provider-first with separate STABLE Google Cloud/OAuth/GAS/workbook IDs. Never copy BETA GAS IDs, spreadsheet ID, OAuth token or secrets into STABLE.

## LAN continuity

Pre-recovery checkpoint: `docs/checkpoints/2026-09-11-LAN-PILOT-20260911-04.md`.

After BETA infra is restored, resume physical V4 regression on the restricted corporate laptop + exactly two MT90. No Administrator/router/DNS/firewall changes. Synthetic clients are capacity evidence only.

## Soft-stop/checkpoint rule

For any CI/provider tranche approaching ~20 minutes: stop starting new long work, record commit/ref + run/job ID + provider state, update `SESSION_CHECKPOINT.md`, and continue independent lanes rather than waiting idly.
