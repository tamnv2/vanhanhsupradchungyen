# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence reconciled through live main before this state update: `0f1af567cb40fd673d492e4bec76a0a79e38c880`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

The percentage remains unchanged. Android endpoint acquisition and secure session persistence have source/CI evidence, but broader scanner/query/business workflows, reconnect/resync/lifecycle, final Pick Pack UI fidelity and physical PDA/company-network acceptance remain open.

## Accepted milestones

### Projection / Cloudflare Cron / Google Sheet — LIVE BETA PASS

- isolated live projection E2E `34927511443`: SUCCESS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- active Worker exports `fetch` + `scheduled`, Cron exactly `*/2 * * * *`.

### Web Slice-1 employee-create — MERGED PASS

PR #19 merged at `db746a71ed80dafd288218f599ab3e990f87e439`; post-merge clean `34928090928` and product foundations `34928090885`: SUCCESS.

### V6 email OTP — SOURCE/CI PASS / LIVE PROVIDER GATED

PR #30 merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`; PR/post-merge clean and product foundations are SUCCESS. Real provider delivery E2E remains gated; TOTP verifier parameters remain insufficiently locked to invent.

### Android endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`; pre/post-merge clean and Android/product foundations are SUCCESS. Accepted ordering is `cached healthy endpoint -> LAN discovery -> manual recovery`, HTTPS-only + health-probed, no fabricated endpoint and no automatic Cloud/LAN authority switch.

### Android secure session persistence — MERGED SOURCE/CI PASS

PR #34 merged at `ebfb7d238268fb089a34779e2c5d53ae45693608`.

- PR clean `34933869114`: SUCCESS;
- Android foundation `34933869144`: SUCCESS with `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=66 ... sessionRestore=PASS ...`;
- post-merge clean `34933981747`: SUCCESS;
- post-merge product foundations `34933981735`: Cloud/Web/Android/LAN SUCCESS.

Accepted session semantics: bearer+expiry+original runtime only, Android Keystore AES-GCM, app-private encrypted metadata, no raw password/OTP/TOTP/provider credential persistence, exact expiry/corruption/missing key/decrypt failure fail closed, no runtime discovery/fallback during restore.

## Scanner / attendance dependency — VERIFIED

Owner authority V5-002 requires QR to continue containing **MNV only**. Current Android `ScannerPayload` only normalizes raw scan text.

Current Cloud and LAN attendance mutation contracts both require:

- `ATTENDANCE_IN` / `ATTENDANCE_OUT`;
- top-level `entityId` equal to technical `employeeId`;
- payload `employeeId` equal to the same technical identity;
- current presence `entityVersion` as `expectedEntityVersion` when presence already exists;
- actor/permission identity from authenticated Service context, never from scanner/client payload.

Therefore scanned MNV must **not** be treated as `employeeId`.

Read-only source audit verified the underlying parity data already exists:

- Cloud D1 has `employees`, `employee_codes`, `presence_state`; current business store can read employees, active employee codes and presence;
- Cloud operational snapshot already includes employees (including `currentPortraitMediaId`), employee codes and presence;
- LAN `Slice1OperationalStateMaterializer` materializes those three sets into `module_current_state` and rejects duplicate ACTIVE employee codes / multiple ACTIVE codes per employee;
- LAN business adapter already reads current employee-code state from `module_current_state`.

What is missing is an authenticated **client read route** that resolves scanned MNV to the current ACTIVE employee plus current presence/version. No current public Cloud/LAN route for that lookup has been proven.

## Current primary READY

Implement the smallest Cloud + LAN parity scan-context query before Android scanner command planning.

Required semantics:

- input is normalized MNV/employee code only;
- authenticated session required in both runtimes;
- LAN request remains HTTPS + paired-device signed; Cloud uses current authenticated session;
- authorization must be compatible with attendance scan permission, not anonymous employee enumeration;
- resolve only an ACTIVE employee-code assignment whose employee is ACTIVE;
- return technical `employeeId`, display name, current portrait media reference if available, MNV identity and current presence `{currentState, businessDate, entityVersion}` or null;
- not-found/inactive/released/ambiguous state fails closed with stable business errors;
- read route never accepts actor authority fields and never mutates business state;
- Cloud/LAN response meaning must match;
- Android scanner planner remains blocked until this route has source/CI parity evidence.

## LAN continuity / reconciliation — SOURCE/HOSTED CI PASS, PHYSICAL ACCEPTANCE PENDING

Operational snapshot/coverage, signed refresh/rebase/readiness, durable reconciliation queue and hosted integration evidence remain accepted. Physical company-network/PDA/public-trust, target Windows host, live LAN->Cloud linkage and >=60-minute Internet-cut acceptance remain separate gates.

## Cloud schema parity — LIVE BETA PASS

Migration `0014_employee_code_entity_version.sql` remains accepted; do not replay migration 0014.

## Other ready/gated lanes

- After scan-context parity: Android scanner -> `ATTENDANCE_IN/OUT` command plan, then reconnect/resync/lifecycle and HTTPS/trust recovery mechanics.
- Web/business UI may continue only where safe current contracts exist.
- Broader integration failure/recovery/receipt coverage may continue without disturbing the proven projection path.
- Real OTP provider E2E, physical acceptance, final Pick Pack visual fidelity, portrait replacement decision and STABLE promotion remain gated as previously recorded.

## Resume instruction

A fresh chat must live-read `AI_ENTRYPOINT.md` from GitHub `main`, execute its bootstrap, reconcile HEAD against `CHECKPOINT.md`, then continue `NEXT_ACTIONS.md`. Memory/chat summaries are locators only, not authority.
