# VHDCHY PERMISSION BASELINE — 2026-09-12

Status: ACTIVE BASELINE
Supersedes the 2026-09-11 audit only for **current account/resource identity and setup state**. The old audit remains historical evidence for source permission analysis.

## Current accounts

- Drive/Sheets/GAS: `tam95.supra@gmail.com`.
- Cloudflare/GitHub managing email: `nguyenvantam050595@gmail.com`.
- `automation@supra.cc.cd`: suspended; no current Google permission should be granted to it.

## Current minimum permissions

| Component | Required now | Not required now |
|---|---|---|
| Google Cloud API | Google Apps Script API | Drive/Sheets/Gmail/Calendar/Contacts APIs for current CI |
| Google CI OAuth | `script.projects`, `script.deployments` | Drive/Sheets/Gmail/Calendar/Contacts scopes |
| GAS runtime | `spreadsheets`, `userinfo.email` | Drive, mail, external request, trigger management |
| Cloudflare token | Account `Workers Scripts Write`, Account `D1 Write` | broad account/DNS/R2/KV permissions unless later required |
| GitHub deploy | `contents: read` | repo-wide write |
| GitHub LAN release | workflow-level `contents: write` | broad PAT/admin token |
| Android BETA | current VHDCHY BETA signer | legacy Pick Pack signer |

## Current source evidence

`gateway/appsscript.json` contains only `spreadsheets` + `userinfo.email`. Current deploy scripts use Apps Script project/deployment APIs and Cloudflare Worker/D1 paths. Any scope expansion requires a new code-based audit.

## Google authorization gates

- Enable Apps Script API in selected standard Google Cloud BETA project.
- Enable account-level Apps Script API access in Apps Script user settings.
- Configure OAuth under `tam95.supra@gmail.com`.
- Testing authorization for an external OAuth app is not a durable CI state for these non-basic scopes; create the intended durable refresh token only after the app is in the intended production publishing state.

## Cloudflare gate

Current connector cannot inspect Cloudflare. Owner/provider UI must verify account ID, zone/resources and token scope. Do not infer retained resource IDs from Git history.

## Approval rule

A new permission is allowed only if:

1. an Owner-approved current feature needs it;
2. current source actually invokes it;
3. the narrowest permission/resource scope is documented.
