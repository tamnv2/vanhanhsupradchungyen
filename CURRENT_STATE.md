# CURRENT STATE — VHDCHY

Protocol: `AI_AUTHORITY_RESUME_V2`
Status: EXECUTING V9 BETA PRODUCT DELIVERY
Evidence state date: 2026-09-15

Current delivery plan: `docs/DELIVERY_PLAN_V6.md`
Current execution model: `docs/EXECUTION_MODEL_V1.md`
Progress model: `docs/PROGRESS_TRACKING_V2.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md` + `DECISIONS_V9.md`

## Progress

`Overall: 58% displayed | Exact weighted baseline: 57.7%`

The V2 evidence-credit migration preserves all V1 phase scores except:

- Phase 5 integration/projection: 55% -> 60% from live BETA projection evidence;
- Phase 7 Web: 25% -> 30% from merged employee-create/shared-shell evidence;
- Phase 8 Android: 15% -> 20% from accepted endpoint-acquisition + secure-session foundations.

No progress credit is granted merely for V9 governance/model files.

## Current execution shape

V9 WIP limit is active:

- **Lane A / integrating slice:** Attendance golden path.
- **Lane B / independent client:** Web business UI/parity using stable current contracts.
- **Lane C / next-domain preparation:** work session + PICK/PACK + resources contract/core preparation.

Provider/physical gates are tracked separately and do not consume active WIP.

## Lane A — Attendance golden path

Target end-to-end path:

`PDA scan MNV -> authenticated scan-context -> ACTIVE employee/name/portrait/presence -> ATTENDANCE_IN/OUT -> Cloud or LAN -> immutable event/current presence -> commit/sync state -> projection when applicable -> retry/restart no duplicate`

Current immediate dependency remains Cloud + LAN authenticated scan-context parity. Owner authority V5-002 requires QR content to remain **MNV only**. MNV must never be treated as technical `employeeId`.

Required scan-context result includes current ACTIVE employee-code assignment, technical `employeeId`, full name, current portrait reference and current presence/version or null. Actor identity remains authenticated Service context only. Lookup is read-only and must not create events/outboxes or write Google.

Android attendance command planning remains downstream of accepted scan-context parity.

## Accepted evidence anchors

### Projection / Cloudflare Cron / Google Sheet — LIVE BETA PASS

- isolated live projection E2E `34927511443`: SUCCESS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- accepted evidence is sufficient for the V2 Phase-5 credit increase;
- broader Google/Drive receipt/deduplication/reconciliation work remains incomplete.

### Web employee-create — MERGED PASS

- PR #19 merge commit `db746a71ed80dafd288218f599ab3e990f87e439`;
- post-merge clean baseline `34928090928`: SUCCESS;
- product foundations `34928090885`: SUCCESS;
- broader business mutations/UI remain incomplete and must be implemented against current contracts/version semantics.

### V6 email OTP — SOURCE/CI PASS / LIVE PROVIDER GATED

- PR #30 merge commit `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`;
- source/CI accepted;
- real provider delivery E2E remains gated until reviewed provider configuration/secrets are available;
- ROOT TOTP verifier technical parameters remain insufficiently locked and must not be invented.

### Android endpoint acquisition — MERGED SOURCE/CI PASS

- PR #32 merge commit `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`;
- pre/post-merge CI accepted;
- accepted ordering is `cached healthy endpoint -> LAN discovery -> manual recovery`;
- HTTPS-only + health-probed; no fabricated endpoint and no automatic Cloud/LAN authority switch.

### Android secure session persistence — MERGED SOURCE/CI PASS

- PR #34 merge commit `ebfb7d238268fb089a34779e2c5d53ae45693608`;
- PR clean `34933869114`: SUCCESS;
- Android foundation `34933869144`: SUCCESS, including `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=66 ... sessionRestore=PASS`;
- post-merge clean `34933981747`: SUCCESS;
- post-merge product foundations `34933981735`: SUCCESS.

Accepted semantics include encrypted app-private session persistence, expiry/corruption/key/decrypt failure fail-closed behavior, original runtime preservation and no raw password/OTP/TOTP/provider credential persistence.

### Current main clean baseline

- `Validate clean baseline` run `34934745772`: SUCCESS on the current pre-V9 main lineage.

## Google / provider operational truth

BETA Google projection is operational with the accepted live evidence above. Provider/resource identities remain in `SERVICE_AUTHORITY.md`; this file owns the volatile live/PASS statement.

The previously locked/suspended Google account is not required for current core source delivery. Current authorized Drive/Sheets/GAS ownership remains as recorded in `SERVICE_AUTHORITY.md`.

STABLE remains fail-closed/preparatory and must not be promoted without mandatory BETA acceptance plus explicit Owner approval.

## LAN current truth

Material automated LAN foundations exist for local durable state, snapshots, immutable events/outboxes, signed refresh/rebase/reconciliation, staged media, secure HTTP, paired-device/session security and no-admin packaging.

These foundations are necessary for the Owner-required full-LAN model, but the current priority is no longer deeper generic LAN infrastructure. LAN work in Lane A should close the Attendance slice, then support later business slices.

Still not accepted as physical PASS:

- current corporate Windows host/public trust;
- real NLS-MT90/PDA behavior;
- live target-network reconnect/restart/network-change regression;
- >=60-minute Internet-cut Window 2;
- capacity/soak and final Owner UAT.

## Web current truth

A shared Vietnamese V7 Online/LAN shell and real `EMPLOYEE_CREATE` flow exist with accepted CI. Other employee/attendance/business surfaces are materially partial. Lane B should add usable current-contract actions rather than display-only placeholders.

## Android current truth

Endpoint acquisition and secure session persistence are accepted source/CI foundations. The current final application shell is not a complete warehouse/PDA product workflow. Attendance scanner/user-flow work remains the first product-facing Android slice.

Final Pick Pack visual fidelity remains gated by accessible authorized reference evidence; current business/domain logic must follow VHDCHY authority, not old Pick Pack behavior.

## Current gates / blockers

These are real gates but do not block independent READY source work:

- real ROOT/normal email-OTP provider E2E requires reviewed provider configuration/secrets;
- ROOT TOTP verifier algorithm/digits/timestep/window/secret-ref parameters remain insufficiently specified;
- physical company Windows/network/PDA/public-trust acceptance;
- >=60-minute outage/recovery/capacity/soak/UAT;
- portrait replacement semantic conflict requires Owner decision before actual conflicting behavior is enabled;
- STABLE promotion requires explicit Owner approval after BETA acceptance.

## State ownership rule

This file owns volatile operational/product/provider evidence. Stable identities belong to `SERVICE_AUTHORITY.md`; exact percentage/credits belong to `docs/PROGRESS_TRACKING_V2.md`; READY/BLOCKED execution queue belongs to `NEXT_ACTIONS.md`. If a copied volatile value elsewhere differs, these owning files win.
