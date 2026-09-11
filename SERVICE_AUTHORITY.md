# SERVICE AUTHORITY — VHDCHY

Status: ACTIVE / OWNER-APPROVED 2026-09-11
Purpose: one compact authority map for provider/account/resource identity so AI does not reuse stale IDs or decommissioned accounts.

## Hard rule

Before any provider write/deploy, verify the active account/resource against this file and `CURRENT_STATE.md`. If a value is absent, marked UNKNOWN, LEGACY, DECOMMISSIONED, or REBUILD_REQUIRED, do not infer it from chat memory, historical backup, old repo, old logs, or old screenshots.

## Google identity

- `vanhanhdchungyen@gmail.com`: **DECOMMISSIONED / DO NOT USE**. Never use for login, OAuth, GAS, Drive, GitHub, recovery, owner checks, CI variables, or new credentials. Any current-looking reference to this address is stale until explicitly superseded.
- `automation@supra.cc.cd`: **CURRENT GOOGLE RUNTIME OWNER** for VHDCHY Google resources. It is a Google Account created with the owned domain email and currently has 5 TB Drive capacity.
- `automation@supra.cc.cd` has no Gmail mailbox. Mail to the address is received through the domain mail/Cloudflare Email Routing forwarding path. Do not add Gmail scopes merely because this address is the Google Account login.

## GitHub

- Current GitHub account: `tamnv2`.
- Current source authority repo: `tamnv2/vanhanhsupradchungyen`.
- Old repo `tamnv2supra/vanhanhdchungyen`: **LEGACY MIGRATION REFERENCE ONLY**. It may be read while reconciliation is incomplete, but must not be treated as the writable authority after the new repo is verified.
- New repo source/history/tags have been partially reconstructed. GitHub Environments, environment variables/secrets, release objects/assets, and full workflow parity must be re-verified/rebuilt before BETA/STABLE are considered restored.
- Branch names remain `main`, `beta`, `stable`; do not move BETA/STABLE live refs until their restoration gates pass.

## Cloudflare and domain

Owner-confirmed state: only account/login email was changed; existing provider setup remains. Therefore **do not recreate** Cloudflare zone, Worker, D1, DNS, domain, or email-routing resources solely because Google/GitHub accounts changed.

- Zone/domain: `supra.cc.cd` — RETAIN.
- Existing BETA/STABLE Cloudflare Worker/D1 resources — RETAIN, then re-verify with new GitHub secrets before next deployment.
- Existing public hostnames — RETAIN unless a later Owner decision changes them.
- Cloudflare token in the old GitHub repository is not recoverable as plaintext; new repo needs a valid scoped token secret.

## Google Drive — new account rebuild

Current Google Drive owner: `automation@supra.cc.cd`.

- VHDCHY root: `1UbpPnlreVf3SvhUm3LUtVFGx2dLeuNAU`.
- BETA root: `1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog`.
- STABLE root: `1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE`.
- DOCUMENTS: `1FhoO_MQrExfI20_HGI-x-mr_Y7QSfkrn`.
- EXPORTS: `1YGm23HZbSpoCozgZz76b8LW-hfCajOUI`.
- BACKUP: `1FS1re5AGQ1viiv9i9Vlf9P4NchpW-Hvz`.
- BETA/PICK_PACK_1291: `18KZ4FG6AAbSWSEQPE_ta3JUUwQWG93rm`.
- New BETA workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ` — **PROVISIONED_NOT_LIVE** until BETA Google/GitHub gates pass.
- `BACKUP DỰ ÁN CŨ PICK PACK 1291`: `1Tz2MuCsgIY4tmmFf9NAFLbIbcmc4brb5` — **REFERENCE ONLY / NOT AUTHORITY / NOT RUNTIME**.

Old Drive IDs from the decommissioned Google account are **INVALID FOR CURRENT RUNTIME** and must not be copied into new GitHub variables.

## GAS / Google Cloud / OAuth

Old GAS projects, deployment IDs, OAuth clients, refresh tokens and Google Cloud resources owned by the decommissioned Google account are **REBUILD_REQUIRED**.

Required model:

- BETA and STABLE remain isolated.
- Each environment gets its own GAS project/deployment and its own GitHub environment variables/secrets.
- Google OAuth/Cloud configuration must use `automation@supra.cc.cd` as the owning/authorizing account.
- OAuth refresh tokens must be generated once per intended client/environment and reused; never mint a new token on each CI run.
- No Gmail/Calendar/Contacts scopes unless a later approved feature demonstrably needs them.
- Apps Script manifest must use only scopes justified by current source. `script.send_mail` is not justified by current gateway source and must be removed.

## Android signing

Existing BETA/STABLE keystore identity is retained if the Owner-controlled backup remains intact. The new GitHub repo must be populated again with environment signing secrets; do not generate replacement signing keys unless the existing keystore is confirmed lost or an explicit rotation decision is made.

## LAN checkpoint

LAN Pilot V4 evidence and release identity remain historical project state, but release objects/assets are not yet present in the new GitHub repo. Do not claim release migration complete until those objects/assets are recreated or an explicit decision supersedes them.
