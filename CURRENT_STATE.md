# CURRENT STATE

Updated: 2026-09-12
Baseline: `REPO-RESET-20260912-01`

## PASS

- GitHub active tree was cleared to a zero marker and is being rebuilt from the clean baseline.
- Pre-zero snapshot preserved at `backup/pre-zero-20260912`.
- Drive project root and environment roots are already verified and remain unchanged.
- BETA projection workbook exists and remains `PROVISIONED_NOT_LIVE`.

## ACTIVE REBUILD

- Governance/authority files: being rebuilt cleanly.
- Google Gateway foundation: retained because current scopes match least-privilege design.
- Worker/D1 source: not restored yet because the pre-reset Worker expected `business_core_v1` while later migrations advanced D1 metadata to `business_core_v2`; this inconsistency must be resolved before restoration.
- Deploy scripts/workflows: not restored; they will be recreated only after provider identities/resources are verified.
- Android/LAN source and evidence: preserved in the pre-zero snapshot, not yet restored to active tree.

## PROVIDER STATE

- Google Cloud/OAuth BETA: SETUP_REQUIRED
- GAS BETA: SETUP_REQUIRED
- Cloudflare BETA resources: VERIFY_REQUIRED
- Android BETA signer: VERIFY_REQUIRED
- GitHub beta Environment: REBUILD_REQUIRED after provider outputs are verified
- STABLE: BLOCKED until BETA PASS + Owner approval

## LAN

Physical regression is paused because Owner is off-site. Existing evidence remains valid reference. When Owner is at the company, LAN and Service work may proceed in parallel where dependencies allow.
