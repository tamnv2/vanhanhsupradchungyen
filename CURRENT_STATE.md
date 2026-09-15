# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence reconciled through live main before this state update: `ebfb7d238268fb089a34779e2c5d53ae45693608`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

The percentage remains unchanged. Android endpoint acquisition and secure session persistence now have source/CI evidence, but they remain partial Phase-8 mechanics. Scanner command handoff, reconnect/resync/lifecycle, warehouse workflows, durable client queue/offline recovery, final Pick Pack UI fidelity and physical PDA/company-network acceptance remain open.

## Projection / Cloudflare Cron / Google Sheet — LIVE BETA PASS

- isolated live projection E2E `34927511443`: SUCCESS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- active Worker exports `fetch` + `scheduled`, Cron exactly `*/2 * * * *`, projection pending count 0.

The former Cron incident stays closed unless new failing evidence reopens it.

## Web Slice-1 — EMPLOYEE CREATE MERGED PASS

PR #19 merged at `db746a71ed80dafd288218f599ab3e990f87e439`.

- post-merge clean baseline `34928090928`: SUCCESS;
- product foundations `34928090885`: Cloud/Web/Android/LAN SUCCESS.

Broader Web mutations remain fail-closed where safe current-state/version UX is absent.

## V6 account/security — EMAIL OTP SOURCE/CI PASS, LIVE PROVIDER GATED

PR #30 merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

- PR clean baseline `34930120980`: SUCCESS;
- post-merge clean baseline `34930171765`: SUCCESS;
- product foundations `34930171683`: Cloud/Web/Android/LAN SUCCESS.

Real email-provider delivery E2E remains gated. TOTP-enabled ROOT remains fail-closed because current authority does not define enough verifier implementation parameters to invent one safely.

## Android/PDA endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`.

- pre-merge clean baseline `34931008972`: SUCCESS;
- Android foundation `34931008973`: SUCCESS;
- post-merge clean baseline `34933290109`: SUCCESS;
- post-merge product foundations `34933290105`: Cloud/Web/Android/LAN SUCCESS.

Accepted semantics remain `cached healthy endpoint -> LAN discovery -> manual recovery`, HTTPS-only + health-probed, no fabricated endpoint and no automatic Cloud/LAN authority switch.

## Android/PDA session persistence — MERGED SOURCE/CI PASS

PR #34 `Persist Android PDA session securely` merged at `ebfb7d238268fb089a34779e2c5d53ae45693608`.

Accepted behavior:

- `PdaSession` snapshots only bearer token, expiry and original runtime mode for a currently usable session;
- snapshot string representation redacts bearer material;
- restore clears old in-memory state first, rejects null/invalid/expired snapshots and rejects the exact expiry boundary;
- valid restore preserves the runtime mode under which the session was authenticated and never performs endpoint discovery/fallback;
- Android persistence uses Android Keystore AES-GCM;
- app-private SharedPreferences contain only format version, IV and ciphertext;
- save removes prior persisted session before replacement so failure cannot leave an older bearer blob active;
- restore never generates a replacement key when the original Keystore key is missing;
- missing key, invalid format, corrupt ciphertext, decrypt failure, invalid token, unknown runtime or expiry clears local state and fails closed;
- passwords, email OTPs, TOTP secrets/codes and provider credentials are outside the store contract;
- Android manifest remains `android:allowBackup="false"`.

Evidence:

- PR clean baseline `34933869114`: SUCCESS;
- PR Android foundation `34933869144`: SUCCESS;
- transport harness: `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=66 ... sessionRestore=PASS ...`;
- APK `assembleDebug`: SUCCESS and artifact uploaded;
- post-merge clean baseline `34933981747`: SUCCESS;
- post-merge product foundations `34933981735`: Cloud/Web/Android/LAN SUCCESS.

## LAN continuity / reconciliation — SOURCE/HOSTED CI PASS, PHYSICAL ACCEPTANCE PENDING

Retained accepted evidence includes operational snapshot/coverage, signed refresh/rebase/readiness, durable reconciliation queue/retry/restart/conflict mechanics, integration `34851773729` and clean baseline `34851772963`, plus hosted Google/Drive receipt/conflict/recovery evidence.

Physical company-network/PDA/public-trust, intended Windows host acceptance, live LAN->Cloud provider linkage and >=60-minute Internet-cut acceptance remain separate gates.

## Cloud schema parity — LIVE BETA PASS

Migration `0014_employee_code_entity_version.sql` remains accepted; do not replay migration 0014.

## Current ready lanes

1. **Android/PDA scanner command handoff — primary READY:** current Android only normalizes scanner text; it does not yet turn approved scan intent into a stable domain command. Implement a pure-Java command plan for the currently approved Slice-1 attendance scan path, keeping actor authority out of payload, preserving stable command/idempotency/device evidence and producing no direct database write.
2. **Android/PDA reconnect/resync/lifecycle:** continue same-runtime reconnect/resync/network/foreground/background recovery after scanner handoff where dependencies allow.
3. **Account/security:** real OTP provider E2E remains gated; do not invent TOTP verifier parameters.
4. **Web/business UI:** continue only interactions backed by current safe contracts.
5. **Gateway/integrations:** continue genuinely open bounded failure/recovery/receipt/provider coverage without disturbing the proven projection path.
6. **Repo/governance:** keep Issue #8, this file, `NEXT_ACTIONS.md` and `CHECKPOINT.md` synchronized.

## Current blockers / owner gates

- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance: physical environment required;
- real ROOT/normal email-OTP provider request/delivery/use E2E: reviewed runtime provider/bindings/secrets required;
- ROOT TOTP verifier implementation: operational verifier parameters/secret resolution not sufficiently locked;
- final Android/PDA visual fidelity: authorized Pick Pack reference evidence required;
- portrait replacement behavior: `OWNER_DECISION_REQUIRED`;
- STABLE activation/promotion: explicit Owner approval only after mandatory BETA acceptance.

## Resume instruction

A fresh chat must live-read `AI_ENTRYPOINT.md` from GitHub `main`, execute its bootstrap, reconcile HEAD against `CHECKPOINT.md`, then continue `NEXT_ACTIONS.md`. Memory/chat summaries are locators only, not authority.
