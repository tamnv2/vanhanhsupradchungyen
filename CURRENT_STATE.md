# CURRENT STATE

Updated: 2026-09-12
Baseline: `REPO-RESET-20260912-01`

## GitHub baseline — PASS

- Pre-zero snapshot preserved at `backup/pre-zero-20260912`.
- Active `main` was cleared to a zero marker and rebuilt from the confirmed current scope.
- Current `main` contains only the clean authority/config/docs, reviewed Google Gateway foundation and baseline validation workflow.
- Clean validation run `34688473153`: SUCCESS at commit `d38be4b83aeed4f853c2ded281470a5163b25925`.
- Old branches/tags remain historical only pending Owner manual deletion; connector cannot delete refs.

## Drive / Sheet — VERIFIED

- Project root `VẬN HÀNH DC HƯNG YÊN` remains unchanged.
- Active roots: `01_BETA` and `02_STABLE`.
- BETA cluster `PICK_PACK_1291` remains current.
- BETA projection workbook remains `PROVISIONED_NOT_LIVE`.

## Source restoration

- Google Gateway foundation: restored after least-privilege review.
- Worker/D1 source: not restored yet because pre-reset Worker/deploy checks expected `business_core_v1` while later migrations advanced metadata to `business_core_v2`; reconcile before restoration.
- Deploy scripts: not restored; rebuild after provider verification.
- Android/LAN source and evidence: preserved in snapshot; restore later after task-specific review.

## Provider state

- Google Cloud/OAuth BETA: SETUP_REQUIRED
- GAS BETA: SETUP_REQUIRED
- Cloudflare BETA resources: VERIFY_REQUIRED
- Android BETA signer: VERIFY_REQUIRED
- GitHub beta Environment: REBUILD_REQUIRED after provider outputs are verified
- STABLE: BLOCKED until BETA PASS + Owner approval

## LAN

Physical regression is paused while Owner is off-site. Existing evidence remains reference. When Owner is at the company, LAN and Service may proceed in parallel where independent.
