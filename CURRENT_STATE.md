# CURRENT STATE

Cập nhật: 2026-09-11

## Trạng thái tổng quát

- Dự án chính: `VẬN HÀNH DC HƯNG YÊN`.
- Cluster/module đầu tiên: `PICK_PACK_1291`.
- Pick Pack 1291 cũ là read-only reference/evidence; không phải authority/runtime dependency.
- `RECONCILE-001`: DONE.
- LAN feasibility trước migration account: `FEASIBLE / FINAL V4 PHYSICAL REGRESSION REQUIRED`.
- Priority hiện tại: **ACCOUNT / PROVIDER AUTHORITY RECOVERY** theo mô hình `main -> beta -> stable`.
- Permission authority hiện hành: `docs/security/PERMISSION_AUDIT_2026-09-11.md`.

## Current identity authority

Chi tiết ở `SERVICE_AUTHORITY.md`.

- Google runtime owner: `automation@supra.cc.cd` — VERIFIED_CURRENT qua Google Drive connector.
- `vanhanhdchungyen@gmail.com`: DECOMMISSIONED / DO NOT USE.
- GitHub current account: `tamnv2` — VERIFIED_CURRENT.
- GitHub authority repo: `tamnv2/vanhanhsupradchungyen`.
- Old repo `tamnv2supra/vanhanhdchungyen`: LEGACY_REFERENCE only.

## Provider recovery matrix

### Cloudflare/domain — RETAIN

Owner xác nhận chỉ chuyển login/email; setup provider cũ vẫn còn.

- Zone/domain `supra.cc.cd`: RETAIN / cần re-verify token trước deploy mới.
- BETA Worker `vhdchy-beta`, D1 `vhdchy-data-beta`, host `beta.supra.cc.cd`: RETAIN.
- STABLE resources: RETAIN / chưa restore deploy.
- Recovery deploy phải FAIL nếu expected D1 không tồn tại; không auto-create replacement database.
- Current CI token minimum đã audit: Account `Workers Scripts Write` + Account `D1 Write`.

Historical verified BETA evidence trước migration:

- Worker version `f8ddf638-0829-4249-9ea0-8f2d38b03f05`.
- D1 id `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, APAC.
- D1 schema `business_core_v1` PASS.
- Old deploy run `34495312746` SUCCESS.

### Google Drive — REBUILT / PROVISIONED_NOT_LIVE

Owner: `automation@supra.cc.cd`.

- VHDCHY root `1UbpPnlreVf3SvhUm3LUtVFGx2dLeuNAU`.
- BETA root `1XuIQ6yb4Ey-aKy0MbRxHfEqvyaSWHDog`.
- STABLE root `1MW-XRR3_jEuE3u8Nzw3NIFPYUM5G_zHE`.
- DOCUMENTS `1FhoO_MQrExfI20_HGI-x-mr_Y7QSfkrn`.
- EXPORTS `1YGm23HZbSpoCozgZz76b8LW-hfCajOUI`.
- BACKUP `1FS1re5AGQ1viiv9i9Vlf9P4NchpW-Hvz`.
- BETA/PICK_PACK_1291 `18KZ4FG6AAbSWSEQPE_ta3JUUwQWG93rm`.
- BETA workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`; schema `PP1291_SHEETS_BETA_V1`; `PROVISIONED_NOT_LIVE`.
- `config/projections.beta.json` on the permission-audit branch points to this current workbook ID; old-account workbook ID is rejected by governance validation.

Reference archive:

- `BACKUP DỰ ÁN CŨ PICK PACK 1291` — ID `1Tz2MuCsgIY4tmmFf9NAFLbIbcmc4brb5`.
- LEGACY_REFERENCE / NOT AUTHORITY / NOT RUNTIME.

### GAS / Google Cloud / OAuth — REBUILD_REQUIRED

Old GAS/Cloud/OAuth resources from the decommissioned account are not current resources.

Current audited BETA minimum:

- Cloud API enablement: **Google Apps Script API only** for current CI path.
- Account-level Apps Script API access must also be enabled at Apps Script user settings.
- CI OAuth scopes exactly:
  - `https://www.googleapis.com/auth/script.projects`
  - `https://www.googleapis.com/auth/script.deployments`
- GAS runtime scopes exactly:
  - `https://www.googleapis.com/auth/spreadsheets`
  - `https://www.googleapis.com/auth/userinfo.email`
- Current gateway does not justify Drive, `script.external_request`, `script.scriptapp`, `script.send_mail`, Gmail, Calendar or Contacts scopes.
- Final durable CI refresh token is created after intended External app publishing state is `In production`; do not use a Testing token as the durable CI token.

### GitHub — RECOVERY_IN_PROGRESS

Current repo `tamnv2/vanhanhsupradchungyen`:

- `main`, `beta`, `stable` and LAN pilot tags exist;
- key workflows restored;
- permission hardening is in PR #2 from `audit/least-privilege-20260911`;
- PR #2 validation run `34609361074` completed SUCCESS, including governance, Apps Script manifest, D1 migration, private-material and signing-ignore checks;
- GitHub Environments/secrets are not yet complete;
- `verify-environments.yml` remains intentionally absent until current provider IDs exist;
- historical release objects/assets are not yet fully restored.

GitHub permission policy after audit:

- repository default `GITHUB_TOKEN`: keep read-only;
- deploy workflows: `contents: read`;
- LAN release workflow: workflow-level `contents: write` only;
- no broad PAT and no Actions PR-approval capability required.

Do not move `beta` or `stable` as part of recovery until gates pass.

## Legacy Pick Pack permission audit

Full legacy source backup was expanded/scanned and matching implementations inspected. Historical permission-bearing features include MailApp email, DriveApp file/artifact handling, ScriptApp installable-trigger management in historical bridge code, UrlFetchApp bridges, direct service Google OAuth/Sheets/Drive paths, FCM service-account logic and several DR/provider experiments.

These explain old permissions but **do not carry forward automatically**. A permission is reintroduced only when a current Owner-approved VHDCHY feature exists and current source actually requires it.

## LAN checkpoint preserved

Pre-recovery snapshot: `docs/checkpoints/2026-09-11-LAN-PILOT-20260911-04.md`.

- exactly two real Newland NLS-MT90 Android 11;
- Agent ran as ordinary user without network-policy/Admin changes;
- both devices reached LAN_ACTIVE;
- durable queue/idempotency basic evidence PASS;
- historical candidate `lan-pilot-beta-v0.3.36` passed old-repo build/package gates.

Final V4 physical regression remains NOT DONE.

## Current BETA recovery gate — provider first

1. merge/lock the audited authority/least-privilege source on `main` without moving `beta`/`stable`;
2. Owner creates BETA Google Cloud/Auth/GAS values; Cloudflare token and VHDCHY BETA signing recovery can run in parallel;
3. verify provider outputs: GAS IDs/URL, two-scope CI refresh token, Cloudflare account/token, current BETA signer;
4. only then populate GitHub `beta` Environment with the current consumed values;
5. AI builds current-ID environment verification, runs BETA CI/deploy/health, and records exact run/resource IDs;
6. only after BETA integration PASS may BETA live pointer/state be confirmed;
7. STABLE remains blocked behind BETA PASS + explicit Owner approval.
