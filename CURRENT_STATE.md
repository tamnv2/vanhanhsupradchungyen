# CURRENT STATE

Cập nhật: 2026-09-10

## Trạng thái tổng quát

- Dự án chính: `VẬN HÀNH DC HƯNG YÊN` — nền tảng bao quát DC Hưng Yên.
- Cluster/module đầu tiên: Pick Pack 1291.
- Pick Pack 1291 cũ: read-only reference, không phải authority/runtime dependency.
- Governance/continuity contract mới đã được Owner duyệt và áp dụng.

## Foundation đã xác minh

- Google/Gmail/Drive account dự án và GitHub connector đã kết nối.
- Drive runtime BETA/STABLE đã tạo.
- GCP/OAuth/GAS BETA/STABLE đã cấu hình; Apps Script authorize/deployment PASS.
- Cloudflare zone `supra.cc.cd` Active; CI token BETA/STABLE đã cấu hình.
- Android signing BETA/STABLE đã tạo và credential verification PASS.
- GitHub Environments `beta`/`stable` đầy đủ Variables/Secrets và verification PASS.
- BETA D1 `vhdchy-data-beta`: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`, APAC.
- BETA Worker `vhdchy-beta` live tại `beta.supra.cc.cd`.
- BETA Google Gateway foundation PASS.
- BETA `/health` và `/health/deep` foundation PASS.
- R2 OFF; Durable Objects chưa bật; LAN hostnames private/reserved.

## Branch/live refs tại checkpoint governance

- `main` trước governance commit: `1ff5fc46365b06458afb93777674f917e07e9543`.
- `beta` live/source ref: `947a4feb48bc5c99867f1975b56edbb9a7309925`.
- BETA validation run `34495312672`: SUCCESS.
- BETA deploy run `34495312746`: SUCCESS.
- `stable` ref: `5b7132071f032ab46f133d4416f80791505f080d`; chưa promote runtime.

## Business runtime

- Business core work đã bắt đầu trên BETA với D1/event/idempotency/projection concepts.
- Không rollback phần foundation chỉ vì Pick Pack 1291 có lịch sử kiến trúc khác.
- Trước khi mở rộng business schema/auth/client tiếp theo, cần reconcile phần đã làm với `PROJECT_SCOPE.md`, Master Spec hiện hành và các pattern Pick Pack 1291 chỉ khi chúng thật sự liên quan cluster 1291.

## Next checkpoint

Thực hiện tranche `RECONCILE-001`: rà business core hiện có, phân loại phần GENERIC-DC vs CLUSTER-1291 vs REFERENCE-ONLY, xác định phần giữ/adapt/sửa; sau đó mới tiếp tục business API/Sheets/Android theo dependency graph.
