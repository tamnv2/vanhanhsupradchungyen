# SERVICE AUTHORITY — VHDCHY

Status: ACTIVE / OWNER-APPROVED 2026-09-11
Purpose: compact authority map for provider/account/resource identity and current permission boundaries.

## Hard rule

Before any provider write/deploy, verify the active account/resource against this file and `CURRENT_STATE.md`. If a value is absent, UNKNOWN, LEGACY, DECOMMISSIONED or REBUILD_REQUIRED, do not infer it from chat memory, backup, old repo/logs/screenshots.

Permission changes must also satisfy `docs/security/PERMISSION_AUDIT_2026-09-11.md`.

## Google identity

- `vanhanhdchungyen@gmail.com`: **DECOMMISSIONED / DO NOT USE** for login, OAuth, GAS, Drive, GitHub, recovery, CI or new credentials.
- `automation@supra.cc.cd`: **CURRENT GOOGLE RUNTIME OWNER**. Normal Google Account using the owned domain email; current Drive capacity 5 TB.
- It has no Gmail mailbox. Domain mail forwarding is separate from Google runtime. Do not add Gmail scopes merely because this address is the Google Account login.

## GitHub

- Current account: `tamnv2`.
- Current authority repo: `tamnv2/vanhanhsupradchungyen`.
- `tamnv2supra/vanhanhdchungyen`: **LEGACY MIGRATION REFERENCE ONLY**.
- Branch model remains `main`, `beta`, `stable`; do not move BETA/STABLE live refs until restoration gates pass.
- Keep repository default `GITHUB_TOKEN` read-only. Workflows elevate only where required (`contents: write` for LAN release publishing; deploy jobs remain `contents: read`).

## Cloudflare and domain

Only account/login email changed; provider setup remains. Do **not** recreate zone, Worker, D1, DNS, domain or email-routing resources solely because Google/GitHub accounts changed.

- Zone/domain: `supra.cc.cd` — RETAIN.
- Existing BETA/STABLE Worker/D1 resources — RETAIN and re-verify.
- Current deploy-token minimum: Account `Workers Scripts Write` + Account `D1 Write`, scoped to the current account.
- Current custom-domain deployment does not need DNS Write or Workers Routes Write.
- Recovery deploy must fail if expected D1 is missing; never silently create a replacement.

## Google Drive — rebuilt under current account

Owner: `automation@supra.cc.cd`.

- VHDCHY root: `1UbpPnlreVf3SvhUm3LUtVFGx2dLeuNAU`.
- BETA root: `1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog`.
- STABLE root: `1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE`.
- DOCUMENTS: `1FhoO_MQrExfI20_HGI-x-mr_Y7QSfkrn`.
- EXPORTS: `1YGm23HZbSpoCozgZz76b8LW-hfCajOUI`.
- BACKUP: `1FS1re5AGQ1viiv9i9Vlf9P4NchpW-Hvz`.
- BETA/PICK_PACK_1291: `18KZ4FG6AAbSWSEQPE_ta3JUUwQWG93rm`.
- BETA workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ` — **PROVISIONED_NOT_LIVE** until BETA gate passes.
- `BACKUP DỰ ÁN CŨ PICK PACK 1291`: `1Tz2MuCsgIY4tmmFf9NAFLbIbcmc4brb5` — **REFERENCE ONLY / NOT AUTHORITY / NOT RUNTIME**.

Old Drive IDs from the decommissioned account are invalid for current runtime.

## GAS / Google Cloud / OAuth

Old GAS projects/deployments/OAuth clients/tokens/Cloud resources from the decommissioned account are **REBUILD_REQUIRED**.

Current BETA/STABLE model remains isolated.

### Cloud project API enablement

At the current recovery checkpoint, enable **Google Apps Script API only**. Do not enable Drive/Sheets/Gmail/Calendar/Contacts REST APIs merely for Apps Script built-in services.

Also enable account-level Apps Script API access at `https://script.google.com/home/usersettings`.

### GitHub CI OAuth scopes — exact current minimum

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

No Drive/Sheets/Gmail/Calendar/Contacts scopes in the current CI client.

### GAS runtime scopes — exact current minimum

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

Current gateway does not justify Drive, `script.external_request`, `script.scriptapp`, `script.send_mail`, Gmail, Calendar or Contacts scopes. Add a scope only when an Owner-approved implemented feature actually requires it.

OAuth refresh tokens are created once per intended environment/client and reused; never mint a new token on each CI run.

## Android signing

- Retain the existing **VHDCHY** signing identity if Owner-controlled backup is intact.
- During BETA recovery, only BETA signing material is needed by the current build workflow.
- The retired Pick Pack 1291 keystore in the legacy reference archive is not the VHDCHY signer and must not be imported into current environments.
- Do not add STABLE signing secrets until a current STABLE Android build workflow actually consumes them.

## Retired Pick Pack 1291 permission boundary

The full legacy source was audited for permission-bearing features. Historical MailApp, Drive file storage, triggers, UrlFetch bridges, direct Worker Google Sheets/Drive OAuth, Firebase Messaging/service account and DR-provider credentials are **legacy evidence only**. They are not current VHDCHY permissions unless explicitly re-adopted by a later Owner decision and current-source audit.

## LAN checkpoint

LAN Pilot V4 evidence remains historical project state. Release objects/assets are not yet considered fully restored in the new repo. No STABLE promotion.
