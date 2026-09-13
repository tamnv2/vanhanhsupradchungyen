# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13

## Execution rule

The active operating mode is autonomous and parallel. The default state is `CONTINUE`.

Overall progress requests Owner interaction only for:
- `OWNER_PERMISSION_REQUIRED`: an Owner-controlled permission/access/consent/secret-store action is required to continue the affected operation; or
- `OWNER_DECISION_REQUIRED`: a material business/authority contradiction has multiple valid outcomes and cannot be resolved from current Owner instruction, provider evidence or GitHub authority.

A technical error, failed CI, tool limitation, unavailable local tooling, physical-lane pause or blocked single lane does not stop independent safe work.

## Provider baseline — PASS

Cloudflare BETA:
- D1 `vhdchy-data-beta` is `business_core_v3`; migration/integrity/independent verification PASS.
- Worker `vhdchy-beta` is deployed at `https://beta.supra.cc.cd`; public health and independent provider verification PASS.
- Existing deployed business/admin APIs remain fail-closed, which is intentional until auth/runtime enforcement passes.

Google BETA:
- Projection workbook remains `PP1291_SHEETS_BETA_V1` / `PROVISIONED_NOT_LIVE`.
- Managed Apps Script deployment is immutable version `3`; run `34753872034` PASS.
- Version 3 contains fail-closed `VHDCHY_PROJECTION_V1` batch/upsert handling. Projection writes remain disabled until secure cross-service authentication + enable gate + end-to-end tests pass.

## Parallel Lane A — Auth / session / permission

Completed foundation:
- password policy/hash, bearer utilities and TOTP verifier;
- session token resolution, expiry/revocation/device-security-epoch checks;
- scoped role/direct permission loading;
- ROOT/SUPERADMIN boundary and explicit DENY precedence;
- password login/session issuance foundation for non-ROOT accounts;
- ROOT password stage fails closed to `ROOT_MFA_REQUIRED` rather than issuing a password-only session;
- automated Worker unit/contract validation PASS through run `34754126291`.

Next executable work:
1. implement remaining ROOT MFA/recovery state machine only to the extent already resolved by authority; do not invent unresolved recovery/login semantics;
2. integrate authenticated context into protected Worker routes once deploy packaging supports the reviewed module set;
3. add login/logout/me/change-password route acceptance tests;
4. add account-administration grantor/self-protection enforcement and tests;
5. provision BETA bootstrap identities only after exact locked identity inputs are authoritative.

## Parallel Lane B — Projection / outbox

Completed foundation:
- `projection.js` outbox envelope, bounded reads, PROCESSING/ACK/PENDING/DEAD transitions and retry backoff;
- GAS version 3 fixed allowed-sheet mapping, key-based idempotent upsert, unknown-column rejection, bounded batch and ScriptLock serialization;
- live workbook `00_CONTROL` records projection protocol/version/deploy evidence while keeping `projection_status=PROVISIONED_NOT_LIVE`.

Next executable work:
1. establish a coordinated secret/authentication design for Worker -> GAS that keeps raw secret values outside source/chat;
2. prefer an automated coordinated rotation/provisioning bridge using existing GitHub Environment/provider credentials where safe, rather than requiring laptop-local tooling;
3. implement Worker outbox send/ACK/failure behavior against the Gateway;
4. test Google unavailable/retry/dead-letter and prove no rollback of canonical D1 state;
5. only then change projection status from `PROVISIONED_NOT_LIVE`.

## Parallel Lane C — Shared Service API / mutation model

Completed foundation:
- `docs/SERVICE_API_CONTRACT.md` defines bearer/session rules, effective permissions, idempotency, same-origin boundary, errors, and required canonical state + immutable event + outbox transaction behavior.

Next executable work:
1. implement/test the reusable D1 mutation transaction helper;
2. implement business commands in dependency order after Auth enforcement is executable;
3. cover BETA scenarios from `DECISIONS.md`: IN/OUT/repeated IN, MNV reuse, PICK/PACK mixed tasks, resource changes/reissue/borrow, labor, dropped goods, documents and degraded projection.

## Parallel Lane D — LAN / Android review

- Physical regression remains paused until actual company network/PDA access is available.
- Source review is not paused: retained pilot transport concepts were reviewed in `docs/LAN_SOURCE_REVIEW_20260913.md`.
- Reusable queue/device-sequence/discovery/hysteresis/diagnostic concepts may be restored behind BETA transport boundaries.
- Cleartext `VHDCHY_LAN_PILOT_V1` remains test-only and must not carry business credentials/PII/canonical mutations.
- Business LAN requires reviewed pairing/authentication and the same Service command/event/idempotency semantics before activation.

## Known packaging constraint

The current Cloudflare deploy bridge uploads the single foundation `index.js`. Auth/session/permission/projection modules on `main` are source/CI PASS but are not yet deployed runtime. Do not import them into deployed `index.js` until the packaging path is reconciled and independently verifiable. Preserve current fail-closed business routes meanwhile.

If platform write-safety blocks one implementation path, do not bypass it through lower-level Git/API tricks. Continue independent lanes and use another reviewed high-level execution path when available. Such a platform block is not itself an Owner permission blocker.

## Android signing

Signing material verification remains device/key-material dependent. Never expose keystore bytes/passwords in chat or repository. This does not block Service/Projection work.

## STABLE

STABLE remains a lane-specific gate: no promotion until full BETA PASS plus explicit Owner approval. Reaching that gate does not stop unrelated BETA/LAN/Android work that is still actionable.
