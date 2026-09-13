# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`
Updated: 2026-09-13

## Google Gateway BETA — FOUNDATION PASS

Google Cloud/OAuth, GitHub Environment `beta`, source synchronization, runtime bootstrap, immutable versioning, managed deployment and `/exec` verification are complete. Gateway remains foundation-only: business projection/outbox handling is not yet live and the BETA projection workbook remains `PROVISIONED_NOT_LIVE`.

## Cloudflare D1 BETA — BUSINESS_CORE_V3 PASS

Evidence:
- guarded migration run `34752340290` — SUCCESS;
- independent post-migration inspection `34752381290` — SUCCESS;
- D1 `vhdchy-data-beta` / `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`;
- schema `business_core_v3`;
- target tables/seeds/integrity PASS;
- business rows remain zero.

## Cloudflare Worker BETA — V3 DEPLOY + HEALTH PASS

Evidence:
- deploy workflow validation after CI correction `34752890965` — SUCCESS;
- guarded deploy run `34752917714` — SUCCESS;
- independent provider verification `34752966242` — SUCCESS;
- Worker `vhdchy-beta` on `https://beta.supra.cc.cd`;
- workers.dev disabled;
- DB binding targets verified BETA D1;
- `/health`, `/health/deep`, `/api/v1/meta`, `/api/v1/capabilities` PASS;
- deployed runtime `BUSINESS_CORE_V3`, schema `business_core_v3`;
- Google Gateway was healthy during deep-health verification.

The first Worker deploy attempt `34752843601` failed before provider upload because of CI Node script-format ambiguity. It is superseded by the successful corrected deployment and must not be retried.

## Immediate priority — authentication/session/effective permissions

1. Implement password verification/reset contract consistent with Owner policy: minimum 8 characters, common-password blocking, long passphrases allowed, no forced periodic rotation absent incident, temporary reset password forces change.
2. Implement authenticated sessions using `auth_sessions`, expiry/revocation, device linkage where available and security-epoch invalidation.
3. Implement effective permissions from role grants plus direct user grants, with ALLOW/DENY handling and cluster/module scope. SUPERADMIN is business/all-cluster equivalent to ROOT but cannot change locked ROOT security/recovery policy.
4. Enforce account status and one-active-account-per-employee invariant; historical accounts remain retained/closed rather than hard-deleted.
5. Keep ROOT MFA structures fail-closed. TOTP/email OTP/SMS recovery secret values/destinations remain outside public source; only hashes/refs/policy metadata belong in D1.
6. Keep `/api/v1/data/*` and `/api/v1/admin/*` closed until authentication and authorization tests PASS. Open each business command only with explicit permission checks and immutable event/outbox transaction behavior.
7. Add CI tests for login/session revocation/expiry, permission scopes, DENY precedence, same-level grant constraints, ROOT protection and anonymous rejection.
8. Deploy only after source/CI PASS using the existing guarded Worker BETA deploy bridge, then rerun public health/runtime verification.

## Parallel priority — projection/outbox foundation

- Define fixed GAS BETA projection request contract and idempotency behavior.
- Worker/Service reads `projection_outbox` asynchronously; Google failure must not roll back D1 business state.
- Writer target is the verified BETA workbook; closed-quarter corrections originate in D1 then re-project.
- Implement retry/checkpoint/dead-letter behavior before marking projection business-live.
- Keep Sheets projection-only, never canonical authority.

## Following gates

After auth + projection foundations PASS: implement attendance/session/resource/labor/dropped-goods/document business commands, Web same-origin flows and automated BETA acceptance scenarios from `DECISIONS.md`. Mark BETA business-live only after end-to-end acceptance passes.

## Autonomous execution model

Routine project execution must not depend on installing tools on a laptop. Execution preference is direct connected action -> GitHub-hosted CI -> minimum Owner permission/UI consent -> local/physical execution only when inherently required or explicitly requested. When a provider permission is missing, request the exact least-privilege grant and resume automation after it is granted.

## Android / LAN

Android signing verification and physical LAN regression remain separate physical/local lanes. Service work continues independently. Keep `backup/pre-zero-20260912` until retained Android/LAN source/evidence is reviewed and restored or deliberately rejected.

## STABLE

Blocked until full BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
