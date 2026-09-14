# CHECKPOINT — VHDCHY

checkpoint_version: 38
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: bec379a49d5276c6b8deb6b3d3c04f960355e75f
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB_ONLINE_LAN / ANDROID_PDA / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
lan_secure_http_ref: docs/LAN_SECURE_HTTP_V1.md
lan_certificate_manager_ref: docs/LAN_CERTIFICATE_MANAGER_V1.md
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
context_index_ref: CONTEXT_INDEX.md

## Current project progress

- Evidence-weighted total: **55.4% exact / 55% displayed**.
- Phase 6 LAN continuity/offline/reconcile: **60%**.
- Phase 7 Online/LAN Web UI: **25%**.
- No progress increase is taken from governance cleanup or newly written auth routes until accepted evidence justifies a progress-model delta.

## Full-audit reconciliation — 2026-09-14

The audited current implementation remains aligned with Owner authority: full local LAN Service; common Cloud/LAN domain contract; D1 central consolidation after synchronization; LAN edge authority for locally accepted events; Google downstream only; no-admin host; Vietnamese-only current UI; STABLE fail-closed.

Governance drift found during the audit was reconciled before this checkpoint:

- `PROJECT_SCOPE.md` — V7 Vietnamese-only UI + current 55.4% / Phase 6 60%;
- `CONTEXT_INDEX.md` — current progress authority;
- `docs/OWNER_BUSINESS_RULES_V1.md` — V6 ROOT email OTP / optional TOTP / normal recovery semantics;
- `docs/DATA_MODEL_GUIDE_V2.md` — V6 auth plus 0009/0010/0011 source-foundation vs live-provider distinction;
- `docs/DELIVERY_PLAN_V5.md` — 55.4%, Phase 6 60%, Phase 7 25%, Vietnamese-only current acceptance;
- `docs/LAN_SECURE_HTTP_V1.md` — certificate manager is already source/CI PASS; next gate is target-host live trust;
- `docs/LAN_SOURCE_REVIEW_20260913.md` — current V3 Service contract and correct context-specific LAN/D1 authority model;
- `CURRENT_STATE.md` and `NEXT_ACTIONS.md` — current ready queue and account-security continuation.

The audit also confirmed that `Validate clean baseline` SUCCESS alone is not a semantic cross-document proof: current `validate.yml` enforces important file/invariant/freshness checks but does not compare all canonical statements for semantic equivalence.

## Account-security continuation

Existing source already had normal password/session primitives and the V6 email-OTP state machine. This continuation added the public Cloud Worker route layer that is safe without inventing a mail provider:

- public normal `POST /api/v1/auth/login`;
- ROOT username-only bootstrap returns `ROOT_EMAIL_OTP_REQUIRED` and never creates a permanent-password session;
- public authenticated normal-user `POST /api/v1/auth/change-password`, including `MUST_CHANGE_PASSWORD` sessions;
- bounded JSON body handling;
- route tests for normal login/token persistence semantics, ROOT permanent-password exclusion and permanent-password establishment.

Latest ROOT-bootstrap source/test commits: `2bffb170319f9bf6d6d5f1953655ce960115d35e` and `1f2d328f21af90d09c5e3b260ad6452af05835c6`.

Public ROOT email-OTP E2E remains incomplete by design: the current OTP primitive requires a real delivery callback and stores protected destination hash/hint rather than inventing a readable destination. Migration `0010_auth_v6_email_otp.sql` exists in source but is not claimed live-applied to BETA without provider verification.

This checkpoint is intentionally created so the clean-baseline workflow can proceed beyond checkpoint freshness and execute Worker syntax/unit/schema validation. Treat the new auth route layer as source-IN_PROGRESS until that workflow completes successfully.

## Existing LAN accepted evidence

Portable Windows LAN-host package at commit `e4731983fa681541c317d853276abc9bf206366e` remains source/CI PASS:

- workflow `34821013175`, job/check `103902355956`: **SUCCESS**;
- package version `0.2.2`;
- ZIP SHA256 `87a5d8d9ce14852204ba757a7b1cf7a716af79675c1bf78f36f9f44934286167`;
- artifact ID `10338301582`.

Other retained LAN evidence:

- secure HTTP `34811861697` / `103874646267` — **SUCCESS**;
- Windows DPAPI TLS `34817069447` / `103889883955` — **SUCCESS**;
- certificate manager `34819836554` / `103898656531` — **SUCCESS**;
- Cloudflare read-only inspection `34816004518` / `103886701628` — **SUCCESS**.

## Current ready queue

Primary physical/provider chain: target ordinary-user Windows host -> verified least-privilege DNS credential -> ACME staging -> production public CA -> company Windows/browser trust -> real NLS-MT90/PDA HTTPS/reconnection -> >=60-minute Internet-cut acceptance -> post-restoration reconciliation.

Parallel source lanes: ROOT email-OTP provider/public request+verify and normal recovery; LAN->Cloud reconciliation machine-auth/network E2E; authenticated Web/business surfaces; Android endpoint/session/scanner/retry/HTTPS; Gateway/Google receipt/retry/readback; broader business adapters.

Portrait replacement remains the only current material product-semantic Owner decision gate and stays fail-closed.

STABLE activation remains blocked until mandatory BETA acceptance plus explicit Owner approval.

do_not_repeat:
Do not treat memory as authority. Do not replay uncertain provider mutations. Do not treat package/source CI as physical acceptance. Do not generate target-host protected certificate material on a GitHub runner. Do not invent or fake email-OTP delivery. Do not infer live migration state from a migration file. Do not weaken fail-closed security/readiness. Do not make Google business authority. Do not silently resolve conflicts. Do not invent unavailable Pick Pack UI details. Do not copy DNSHE branding/assets. Do not inflate progress without acceptance evidence. Do not promote STABLE without explicit Owner approval.
