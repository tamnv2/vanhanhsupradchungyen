# CURRENT STATE

Cập nhật: 2026-09-11

## Trạng thái tổng quát

- Dự án chính: `VẬN HÀNH DC HƯNG YÊN`.
- Cluster/module đầu tiên: `PICK_PACK_1291`.
- Pick Pack 1291 cũ là read-only reference/evidence; không phải authority/runtime dependency.
- Governance/continuity contract đã được Owner duyệt.
- `RECONCILE-001`: DONE.
- LAN feasibility trước migration account: `FEASIBLE / FINAL V4 PHYSICAL REGRESSION REQUIRED`.
- Priority tạm thời: **ACCOUNT / PROVIDER AUTHORITY RECOVERY** để khôi phục đúng mô hình `main -> beta -> stable` sau khi Google/GitHub identity thay đổi. Sau khi BETA infra gate PASS, quay lại LAN physical regression.

## Current identity authority

Chi tiết đầy đủ ở `SERVICE_AUTHORITY.md`.

- Google runtime owner: `automation@supra.cc.cd` — VERIFIED_CURRENT qua Google Drive connector.
- `vanhanhdchungyen@gmail.com`: DECOMMISSIONED / DO NOT USE.
- GitHub current account: `tamnv2` — VERIFIED_CURRENT.
- GitHub current authority repo: `tamnv2/vanhanhsupradchungyen` — source/history/tags đã phục hồi phần lớn; Environments/secrets/releases/provider integration chưa hoàn tất.
- Old repo `tamnv2supra/vanhanhdchungyen`: LEGACY_REFERENCE trong thời gian đối soát, không phải writable authority.

## Provider recovery matrix

### Cloudflare/domain — RETAIN

Owner xác nhận chỉ chuyển login/email; setup provider cũ vẫn còn.

- Zone/domain `supra.cc.cd`: OWNER_CONFIRMED_NOT_TOOL_VERIFIED.
- BETA Worker `vhdchy-beta`, D1 `vhdchy-data-beta`, public host `beta.supra.cc.cd`: RETAIN / cần re-verify bằng token mới trong GitHub Environment trước deploy tiếp theo.
- STABLE Worker/D1/domain resources: RETAIN / chưa deploy lại.
- Không recreate Cloudflare/D1/DNS chỉ vì account Google/GitHub thay đổi.

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
- New BETA workbook `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`: `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ`; schema/tabs provisioned, **not yet live projection**.
- Old-account Drive IDs are invalid for new runtime and must not be reused.

Reference archive:

- Folder renamed to `BACKUP DỰ ÁN CŨ PICK PACK 1291`.
- ID `1Tz2MuCsgIY4tmmFf9NAFLbIbcmc4brb5`.
- Status `LEGACY_REFERENCE`; a README marker inside explicitly states NOT AUTHORITY / NOT RUNTIME.

### GAS / Google Cloud / OAuth — REBUILD_REQUIRED

- Old GAS Script IDs, deployment IDs, OAuth clients and refresh tokens belonged to the decommissioned Google account and are not current resources.
- BETA and STABLE must each receive separate new Google Cloud/OAuth/GAS resources owned/authorized by `automation@supra.cc.cd`.
- GitHub CI OAuth and Apps Script runtime scopes are audited separately.
- Current gateway source does not send email. `script.send_mail` has been removed from the recovery-branch manifest.
- Durable CI token requires OAuth publishing status appropriate for production use; do not leave the final CI OAuth client in Testing.

### GitHub — RECOVERY_IN_PROGRESS

Current repo `tamnv2/vanhanhsupradchungyen`:

- source/history imported;
- `main`, `beta`, `stable` branches present;
- LAN pilot tags imported;
- key workflows restored on `main`: build LAN pilot, deploy BETA, deploy STABLE, validate;
- GitHub Environment `beta` / `stable`, variables and secrets require Owner UI setup;
- `verify-environments.yml` intentionally NOT restored yet because the historical file hardcodes old Google owner/Drive/GAS IDs;
- new repo release objects/assets are currently absent; old public repo still contains historical release evidence/assets.

Do not move `beta` or `stable` as part of recovery until each environment passes provider gates.

## LAN checkpoint preserved

Full pre-recovery snapshot is archived at `docs/checkpoints/2026-09-11-LAN-PILOT-20260911-04.md`.

Physical evidence remains valid:

- exactly two real Newland NLS-MT90 Android 11;
- Agent ran as ordinary user without network-policy/Admin changes;
- both devices reached LAN_ACTIVE;
- durable queue/idempotency basic evidence PASS;
- candidate `lan-pilot-beta-v0.3.36` passed historical CI/build/sign/package gates in the old repo.

Final V4 physical regression is still NOT DONE. Account/provider recovery does not change that result.

## Current gate

BETA restoration must complete in this order:

1. current authority docs merged to new repo main;
2. GitHub beta Environment + variables/secrets configured;
3. new BETA Google Cloud OAuth client published for durable authorization;
4. new BETA GAS project + Web App deployed and authorized with minimum justified scopes;
5. BETA Drive/GAS/Cloudflare/Android signing verification PASS;
6. restore current-ID `verify-environments.yml` and run CI;
7. only then move/confirm BETA live pointer.

STABLE remains blocked behind BETA PASS + Owner approval.
