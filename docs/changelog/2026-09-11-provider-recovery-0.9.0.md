# CHANGE RECORD — provider-recovery-0.9.0

- Change ID: `CHG-20260911-090`
- Date: 2026-09-11
- Branch: `recovery/identity-authority-20260911`
- Environment: authority/recovery only; BETA/STABLE live refs intentionally not moved
- Modules: governance, provider identity, GitHub, Google Drive, OAuth/GAS security, continuity

## Reason

The previous Google runtime identity was decommissioned and the GitHub authority account/repository changed. Owner required a controlled recovery that preserves provider setups that still exist, rebuilds only lost Google resources, keeps BETA/STABLE isolation, prevents AI from reusing stale IDs, and preserves the LAN V4 checkpoint.

## Changes

1. Defined current account/provider/resource authority in `SERVICE_AUTHORITY.md`.
2. Marked `vanhanhdchungyen@gmail.com` DECOMMISSIONED / DO NOT USE.
3. Set current Google runtime owner to `automation@supra.cc.cd` and current GitHub repo to `tamnv2/vanhanhsupradchungyen`.
4. Retained Cloudflare/domain resources by Owner confirmation; no provider rebuild performed.
5. Recorded newly provisioned Drive roots/workbook as current resources but `PROVISIONED_NOT_LIVE` until BETA integration PASS.
6. Renamed the old backup folder to `BACKUP DỰ ÁN CŨ PICK PACK 1291` and added an explicit reference-only marker inside Drive.
7. Archived the previous LAN checkpoint so provider recovery does not overwrite physical-test evidence.
8. Updated AI bootstrap/operating contract to use a 5-file authority bootstrap, provider state classifications, provider-write preflight, dependency/parallel execution and ~20-minute checkpoint behavior.
9. Updated project scope/current state/next actions for the new identities and recovery gate.
10. Removed unused Apps Script `script.send_mail` runtime scope.
11. Added a provider reauthorization runbook and rebuilt GitHub Environment setup instructions with new Drive roots and new-GAS placeholders.
12. Did not restore historical `verify-environments.yml` because it hard-coded decommissioned Google identity/resource IDs.

## Google/OAuth security decision for recovery

- BETA/STABLE remain separate Google Cloud/OAuth/GAS resources.
- GitHub CI OAuth is limited to `script.projects`, `script.deployments`, `drive.metadata.readonly` based on current CI source.
- Apps Script runtime scopes remain Drive/Sheets/external_request/scriptapp/userinfo.email based on current gateway source; send-mail scope removed.
- Final CI OAuth authorization must not remain in External/Testing state because non-basic Testing authorizations expire after seven days.
- Refresh tokens are long-lived credentials, not permanent credentials; token churn is explicitly discouraged.

## Provider/resource state

- Google Drive account/profile: VERIFIED_CURRENT.
- New Drive resources: VERIFIED_CURRENT / PROVISIONED_NOT_LIVE.
- Cloudflare/domain: OWNER_CONFIRMED_NOT_TOOL_VERIFIED / RETAIN.
- Old Google Cloud/OAuth/GAS: REBUILD_REQUIRED.
- New GitHub Environments/secrets: REBUILD_REQUIRED.
- Android signing identity: RETAIN; new repo secrets pending verification.
- Historical new-repo release objects/assets: NOT RESTORED; tags/source are present.

## Tests / verification

- Drive rename and reference README write: PASS.
- Current Google Drive ownership/profile verified separately during recovery.
- New GitHub repo is writable by the current connection and source/history/key workflows are present.
- Authority branch still requires PR validation before merge.
- No BETA/STABLE provider deploy has been performed in this tranche.

## Rollback

Authority-doc changes can be reverted by reverting this recovery branch/PR. Do not revert the decommissioned-account rule to recover functionality. Provider resources have not been destructively changed by this tranche.

## Next impact

Owner completes GitHub Environments + BETA Google Cloud/OAuth/GAS bootstrap + signing/token secret entry. AI then restores a current-ID environment verification workflow, verifies BETA end-to-end, and only after BETA PASS prepares isolated STABLE recovery. LAN physical V4 regression resumes after BETA infrastructure is restored.
