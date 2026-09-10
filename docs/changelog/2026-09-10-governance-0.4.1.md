# governance-0.4.1 — CI guard for AI continuity

Change ID: `GOV-20260910-02`
Time: 2026-09-10 Asia/Ho_Chi_Minh
Module: governance / CI
Environment: repository validation only

## Reason

Governance must be mechanically protected, not only documented. A future edit must not silently delete or empty the files needed to resume across chats.

## Changes

- Added executable `scripts/validate-governance.sh`.
- Required files: project scope, bootstrap, operating contract, current state, next actions, checkpoint, decision index/full decisions, changelog, Owner usage guide and Pick Pack 1291 reference index.
- Added marker checks for Owner-approved scope, Pick Pack reference boundary, four-file bootstrap, 20-minute soft-stop rule, parallel execution, checkpoint/next-action fields, detailed changelog mechanism and D-014.
- Added `Validate governance continuity` step to `.github/workflows/validate.yml`.

## Files/resources affected

Repository CI/docs only. No provider runtime, BETA Worker/D1/GAS, Android signing, Drive runtime data or STABLE resource mutation.

## Migration

None.

## Verification

Expected CI gate: `Validate public repo` must pass on the governance commit before this change is considered complete.

## Rollback

Do not delete this history. If checks become too strict after an intentional governance revision, update the validator in the same commit as the superseding decision/changelog entry.

## Related decisions

D-011, D-012, D-013, D-014.

## Next impact

Future source changes inherit a mechanical continuity check in addition to the documented operating contract.
