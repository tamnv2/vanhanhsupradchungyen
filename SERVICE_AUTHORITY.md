# SERVICE AUTHORITY — VHDCHY

Status: ACTIVE / OWNER-APPROVED 2026-09-12
Purpose: authority duy nhất cho identity/provider/resource hiện hành.

## Status vocabulary

- `VERIFIED_CURRENT`: tool/provider evidence xác minh tại baseline hiện hành.
- `OWNER_CONFIRMED_NOT_TOOL_VERIFIED`: Owner chốt nhưng tool hiện tại chưa xác minh provider.
- `VERIFIED_EXISTING_NOT_LIVE`: resource đúng owner, đúng vị trí và tồn tại nhưng chưa được đưa live sau reset.
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
- Reset validation run `34666033568`: SUCCESS at commit `fede82f6486e294acf53ea97e60e0780540566c8`.
- `beta`/`stable` không được move trong setup baseline cho tới gate tương ứng.

GitHub Environment secret values không thể đọc qua connector hiện hành; chúng phải được coi là stale/unknown sau reset cho tới khi provider chain mới được verify.

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

Active environment roots:

- BETA `01_BETA` — `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5` — `VERIFIED_CURRENT`, owner `tam95.supra@gmail.com`.
- BETA `01_CLUSTERS` — `1ixxqKs8m0uN10z_S8M7rzSm2tT2GLbVF`.
- BETA cluster `PICK_PACK_1291` — `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN` — `VERIFIED_CURRENT`.
- STABLE `02_STABLE` — `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI` — `VERIFIED_EXISTING_NOT_LIVE` until STABLE gate.

Both environment roots use the same level-1 contract:

- `00_SHARED`
- `01_CLUSTERS`
- `02_MEDIA`
- `03_ARCHIVE`
- `04_BACKUP`
- `05_LOG`
- `06_EXPORT`
- `07_SYSTEM`

Previous setup-era folders were moved out of the project root into sibling folder:

- `VHDCHY_LEGACY_SETUP_20260908`
- ID `1NysNtmsMAxFA5JgsYwEJNgMVvbogvf9R`
- status `LEGACY_REFERENCE / NOT AUTHORITY / NOT RUNTIME`.

Reference:

- `BACKUP PICK PACK 1291`
- ID `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`
- owner `tam95.supra@gmail.com`
- `LEGACY_REFERENCE / REFERENCE ONLY`.

## Google Sheets — VERIFIED_EXISTING_NOT_LIVE

Current BETA projection workbook:

- title: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`;
- ID: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`;
- owner: `tam95.supra@gmail.com`;
- parent cluster folder: `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`;
- schema: `PP1291_SHEETS_BETA_V1`;
- status: `PROVISIONED_NOT_LIVE` until GAS + CI/BETA integration gate passes.

Verification 2026-09-12:

- native Google Sheet, 17 current tabs, header row frozen;
- locale `vi_VN`, timezone `Asia/Saigon`;
- no historical business rows migrated;
- no `Password verifier` or secret field in `DANH SÁCH TÀI KHOẢN`;
- duplicate legacy `User pack`/`User Pack` was ADAPTED to one `User Pack` field;
- legacy LAN/emergency/fallback tabs were not recreated.

Old workbook IDs in Git history remain historical only.

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
