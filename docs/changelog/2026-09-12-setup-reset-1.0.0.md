# 2026-09-12 — setup-reset-1.0.0

Change ID: `SETUP-RESET-20260912-01`
Owner approved: 2026-09-12

## Reason

Google runtime owner `automation@supra.cc.cd` became unusable after account lock. Owner selected `tam95.supra@gmail.com` for Drive/Sheets/GAS and retained `nguyenvantam050595@gmail.com` for Cloudflare/GitHub. Owner requested setup restart from a clean authorization baseline while preserving approved project logic.

## Changes

- Created pre-reset snapshot branch `archive/pre-setup-reset-20260912`.
- Replaced current identity authority with new split-account model.
- Reset old provider IDs/credentials/workbooks/live claims from current authority.
- Preserved source, decisions, architecture, migrations, LAN evidence and historical changelog.
- Verified current GitHub repo rights and Drive ownership.
- Reused existing current-account BETA Drive skeleton.
- Reset BETA projection registry to `NOT_PROVISIONED`.
- Added strict setup-from-zero runbook and new permission baseline.
- Retained 5-file indexed bootstrap + dependency-first parallel execution + deterministic checkpoints.

## Provider state after change

- GitHub: VERIFIED_CURRENT.
- Drive root/BETA root: VERIFIED_CURRENT.
- BETA projection Sheet: NOT_PROVISIONED.
- Google Cloud/OAuth/GAS: SETUP_REQUIRED.
- Cloudflare: VERIFY_REQUIRED.
- Android BETA signer: VERIFY_REQUIRED.
- GitHub beta Environment: SETUP_REQUIRED after provider outputs.
- BETA/STABLE: not live-confirmed by this reset.

## Rollback

Use branch `archive/pre-setup-reset-20260912` for historical pre-reset state. Do not roll back provider credentials or account ownership merely by resetting Git refs; provider changes require separate verification.
