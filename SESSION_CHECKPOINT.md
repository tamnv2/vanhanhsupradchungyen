# SESSION CHECKPOINT

Checkpoint ID: `PROVIDER-RECOVERY-20260911-01`
Timestamp: `2026-09-11T19:41+07:00`
Authority branch: `recovery/identity-authority-20260911`

## Why this checkpoint supersedes the previous current checkpoint

The Google runtime account used by the old environment was decommissioned and the GitHub authority account/repository changed. The prior LAN checkpoint remains valid technical evidence but is archived at `docs/checkpoints/2026-09-11-LAN-PILOT-20260911-04.md` so provider recovery cannot accidentally overwrite or reinterpret it.

## Current identities

- Google runtime owner: `automation@supra.cc.cd` — VERIFIED_CURRENT through Drive connector.
- Old Google account `vanhanhdchungyen@gmail.com`: DECOMMISSIONED / DO NOT USE.
- GitHub current account: `tamnv2` — VERIFIED_CURRENT.
- GitHub current repo: `tamnv2/vanhanhsupradchungyen` — RECOVERY_IN_PROGRESS.
- Old repo `tamnv2supra/vanhanhdchungyen`: LEGACY_REFERENCE / migration source only.

## DONE in this recovery tranche

### Google Drive

- Created VHDCHY root and separated BETA/STABLE/document/export/backup folders under `automation@supra.cc.cd`.
- Created BETA `PICK_PACK_1291` folder and provisioned new Q3 workbook with the approved 17-tab schema.
- New BETA workbook ID: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`.
- Renamed `BACKUP PICK PACK 1291` to `BACKUP DỰ ÁN CŨ PICK PACK 1291`.
- Added a README marker inside the legacy backup stating REFERENCE ONLY / NOT AUTHORITY / NOT RUNTIME.

### GitHub source recovery

- New repo exists and is writable by the current GitHub connection.
- Main/beta/stable branches and LAN pilot tags exist in the new repo.
- Source/history largely imported from old public repo.
- Key workflows restored on new `main`: LAN Pilot build, deploy BETA, deploy STABLE, validate.
- Historical release objects/assets are NOT yet recreated in the new repo.
- `verify-environments.yml` intentionally remains absent because its old copy hard-coded decommissioned Google identity/IDs.

### Authority/security corrections on this recovery branch

- Added `SERVICE_AUTHORITY.md` as the current account/provider/resource identity authority.
- Updated `AI_BOOTSTRAP.md` to read `SERVICE_AUTHORITY.md` every new session and prohibit stale/model-memory IDs.
- Updated `AI_OPERATING_CONTRACT.md` with provider state classifications, provider-write preflight, parallel execution, ~20 minute checkpoint behavior and OAuth discipline.
- Updated `PROJECT_SCOPE.md` for current Google/GitHub identities, exact 2-MT90 physical boundary and renamed legacy backup folder.
- Removed unused Apps Script `script.send_mail` runtime scope.
- Updated `docs/GITHUB_ENV_SETUP.md` with current Drive roots, current owner email, retained Cloudflare IDs, new-GAS placeholders, secret rules and narrow CI OAuth scope set.
- Updated `CURRENT_STATE.md` and `NEXT_ACTIONS.md` to prioritize provider recovery without losing the LAN checkpoint.

## IN PROGRESS / NOT YET LIVE

- New Drive BETA workbook/resources are `PROVISIONED_NOT_LIVE` until BETA integration passes.
- GitHub Environments `beta` and `stable` plus vars/secrets are not yet confirmed configured in the new repo.
- New Google Cloud/OAuth BETA/STABLE resources are not created/verified yet.
- New GAS BETA/STABLE projects/deployments do not exist yet.
- Cloudflare setup is Owner-confirmed retained but not yet re-verified with a current token from the new GitHub Environment.
- Android signing identities are retained by design but new repo secrets still need to be entered and verified.
- New repo release objects/assets are absent.

## Provider classification

- Google Drive account/profile: VERIFIED_CURRENT.
- Drive folder/workbook provisioning: VERIFIED_CURRENT but PROVISIONED_NOT_LIVE.
- Cloudflare/domain resources: OWNER_CONFIRMED_NOT_TOOL_VERIFIED / RETAIN.
- Google Cloud/OAuth/GAS old resources: REBUILD_REQUIRED.
- Android keystore identity: RETAIN / OWNER-CONTROLLED BACKUP, pending new-repo secret verification.
- Old Google account/resources: DECOMMISSIONED.

## Exact Owner actions required next

Use the generated detailed reauthorization guide; do not paste secrets into chat.

1. GitHub: configure Actions write permission and Environments `beta`/`stable`; enter current vars/secrets.
2. Google BETA: create/select BETA Cloud project under `automation@supra.cc.cd`; enable Apps Script API + Drive API; configure Google Auth Platform; set Audience External and Publishing Status `In production`; create the BETA OAuth client and generate one offline refresh token using only approved CI scopes.
3. GAS BETA: create BETA project/deployment, run interactive `bootstrapAuthorize()` once and record Script/Deployment/Exec IDs in GitHub BETA variables.
4. Restore BETA signing secrets from existing keystore backup.
5. Supply/recreate a least-privilege Cloudflare deploy token in GitHub BETA if the prior plaintext token is unavailable.

## Work AI can continue automatically after Owner inputs

- Verify new repo variables indirectly through CI presence checks without reading secret values.
- Build current-ID `verify-environments.yml`.
- Update BETA projection config/registry to the new workbook ID.
- Run CI validation and BETA provider health checks.
- Reconcile branch pointers only after gates pass.
- Then prepare isolated STABLE recovery and wait for Owner STABLE approval.

## Parallelization

Owner GitHub setup, BETA Google Cloud/OAuth setup, Cloudflare token preparation and Android signing secret restoration are independent and can be done in parallel. GAS BETA creation depends on the intended BETA Google Cloud/OAuth project decision. STABLE provider recovery depends on BETA PASS.

## LAN continuity

The physical LAN regression remains pending exactly as before. Do not mark LAN-PILOT PASS until real laptop + two MT90 evidence is completed. Provider recovery is a prerequisite for restoring project automation, not evidence of LAN feasibility completion.
