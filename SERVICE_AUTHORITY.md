# SERVICE AUTHORITY — VHDCHY

Status: ACTIVE / OWNER-APPROVED 2026-09-12
Purpose: authority duy nhất cho identity/provider/resource hiện hành.

## Status vocabulary

- `VERIFIED_CURRENT`: tool/provider evidence xác minh tại baseline hiện hành.
- `OWNER_CONFIRMED_NOT_TOOL_VERIFIED`: Owner chốt nhưng tool hiện tại chưa xác minh provider.
- `VERIFIED_EXISTING_NOT_LIVE`: resource đúng owner và tồn tại, nhưng chưa được chọn/đưa live sau reset.
- `NOT_PROVISIONED`: chưa có resource hiện hành.
- `SETUP_REQUIRED`: phải thực hiện setup/consent/config.
- `SUSPENDED_RECOVERY_CANDIDATE`: không dùng hiện tại; chỉ xem xét migration nếu khôi phục.
- `LEGACY_REFERENCE`: chỉ lịch sử/reference.
- `UNKNOWN`: chưa đủ evidence.

Không tuyên bố DONE/LIVE/PASS cho `NOT_PROVISIONED`, `SETUP_REQUIRED`, `UNKNOWN` hoặc `OWNER_CONFIRMED_NOT_TOOL_VERIFIED`.

## GitHub — VERIFIED_CURRENT

- Account: `tamnv2`.
- Account email: `nguyenvantam050595@gmail.com`.
- Repo: `tamnv2/vanhanhsupradchungyen`.
- Repo visibility: PUBLIC.
- Connection rights verified: admin, maintain, push, pull, triage.
- Default branch: `main`.
- Snapshot trước reset: `archive/pre-setup-reset-20260912`.
- `beta`/`stable` không được move trong setup baseline cho tới gate tương ứng.

GitHub Environment secret values không thể đọc qua connector hiện hành; chúng phải được coi là stale/unknown sau reset cho tới khi owner/provider setup được hoàn tất và CI verify behavior.

## Google identity

### Current

`tam95.supra@gmail.com` — current Google owner cho Drive + Sheets + GAS.

### Suspended

`automation@supra.cc.cd` — `SUSPENDED_RECOVERY_CANDIDATE`.

- Không dùng làm current Drive/Sheets/GAS/OAuth owner.
- Không reuse old IDs/tokens/deployments từ account này.
- Nếu Google mở khóa và Owner muốn chuyển lại, tạo migration decision riêng; không tự chuyển.

### Historical account

`vanhanhdchungyen@gmail.com` — không phải current project authority. Hiện còn writer permission trên reference folder `BACKUP PICK PACK 1291`; quyền này được giữ nguyên trong baseline và phải review riêng trước khi thay đổi.

## Google Drive — VERIFIED_CURRENT / EXISTING

Project root:

- `VẬN HÀNH DC HƯNG YÊN`
- ID `19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`
- Owner `tam95.supra@gmail.com`

Runtime roots:

- BETA `10_RUNTIME_BETA` — `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5` — `VERIFIED_CURRENT` owner `tam95.supra@gmail.com`.
- STABLE `20_RUNTIME_STABLE` — `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI` — discovered under current root; `VERIFIED_EXISTING_NOT_LIVE` until the STABLE setup gate.

BETA skeleton already exists with `00_SHARED`, `01_CLUSTERS`, `02_MEDIA`, `03_ARCHIVE`, `04_BACKUP`, `05_LOG`, `06_EXPORT`, `07_SYSTEM`. Reuse; do not recreate unless a folder is missing or invalid.

Reference:

- `BACKUP PICK PACK 1291`
- ID `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`
- owner `tam95.supra@gmail.com`
- `LEGACY_REFERENCE / REFERENCE ONLY`.

## Google Sheets — NOT_PROVISIONED after reset

No BETA projection workbook is trusted as current at baseline 2026-09-12.

- Old workbook IDs in Git history are historical only.
- `config/projections.beta.json` is intentionally reset to `NOT_PROVISIONED`.
- New/current workbook must be created or explicitly verified under `tam95.supra@gmail.com`, placed in BETA structure, schema checked, then recorded here before use.

## GAS / Google Cloud / OAuth — SETUP_REQUIRED

Current owner: `tam95.supra@gmail.com`.

BETA must be provisioned/authorized again. Minimum current CI OAuth scopes from current source:

```text
https://www.googleapis.com/auth/script.projects
https://www.googleapis.com/auth/script.deployments
```

Current GAS runtime scopes:

```text
https://www.googleapis.com/auth/spreadsheets
https://www.googleapis.com/auth/userinfo.email
```

Do not add Drive/Gmail/Calendar/Contacts/`script.send_mail`/`script.scriptapp`/`script.external_request` unless a current Owner-approved implementation actually requires it.

## Cloudflare — OWNER_CONFIRMED_NOT_TOOL_VERIFIED

- Managing email: `nguyenvantam050595@gmail.com`.
- Zone/domain intent: retain `supra.cc.cd` and existing useful Worker/D1/DNS resources if still present.
- Do not recreate or delete provider resources until account/resource identity is verified in Cloudflare dashboard.
- Current deploy-token minimum based on current deploy source: Account `Workers Scripts Write` + Account `D1 Write`.
- Missing expected D1 must fail closed; never silently create a replacement during recovery/setup.

## Android signing

Existing VHDCHY signing material is historical evidence until locally re-verified. Do not use retired Pick Pack 1291 signer. BETA signer is verified/re-entered before signed release; STABLE signer remains isolated and deferred until needed.

## Permission rule

Every new permission requires all three:

1. Owner-approved current feature exists;
2. current source actually calls the capability;
3. narrowest provider scope/resource boundary is documented.

Nếu thiếu một điều kiện thì không cấp quyền.
