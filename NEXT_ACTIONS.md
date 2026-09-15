# NEXT ACTIONS — VHDCHY

Updated: 2026-09-15
Progress: **56% displayed / 56.2% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A blocked lane does not stop unrelated ready work. `AI_TERMINATION_GUARD.md` forbids voluntary finalization while approved `READY` work remains.

## Retained accepted milestones

- Projection live chain: E2E `34927511443`, cleaned deploy `34927869845`, observer `34927996370`: SUCCESS.
- Web employee-create PR #19: merged; post-merge clean `34928090928` + product foundations `34928090885`: SUCCESS.
- V6 email OTP PR #30: source/CI PASS; real provider delivery E2E remains gated.
- Android endpoint PR #32: merged; pre/post-merge clean and Android/product foundations SUCCESS.
- Android secure session PR #34: merged at `ebfb7d238268fb089a34779e2c5d53ae45693608`; PR clean `34933869114`, Android foundation `34933869144`, post-merge clean `34933981747`, product foundations `34933981735`: SUCCESS.

## CURRENT PRIMARY READY — Cloud + LAN attendance scan-context parity

The previous plan to build the Android attendance command immediately is dependency-blocked by an identity boundary that is now proven:

- QR contains **MNV only** under V5-002;
- current `ATTENDANCE_IN/OUT` mutation requires technical `employeeId` as both `entityId` and payload `employeeId`;
- current presence `entityVersion` is required for guarded mutation when presence state already exists;
- actor identity is server-authenticated and must never come from scanner payload.

Underlying data already exists in both runtimes:

- Cloud D1: `employees`, `employee_codes`, `presence_state`;
- Cloud operational snapshot includes employees + current portrait media id, employee codes and presence;
- LAN materializes the same three datasets into `module_current_state` and validates ACTIVE-code uniqueness.

No current public authenticated MNV lookup route has been proven. Implement that read path first.

### Scan-context contract boundary

Use one reviewed route meaning in Cloud and LAN. Exact implementation path may be refined under Service API Contract V3; prefer a body-based authenticated request so MNV is not unnecessarily placed in URL/query logs and LAN can reuse signed-body evidence.

Input:
- employee code / MNV only, bounded and normalized;
- no actor/user/permission fields.

Authorization/security:
- valid current authenticated user session required;
- normal password-change gate still applies;
- require effective attendance `scan` permission/scope;
- LAN additionally requires HTTPS, paired-device signed request proof and matching LAN session/device/security epoch;
- no anonymous enumeration.

Resolution:
1. find exactly one ACTIVE employee-code assignment for the normalized MNV;
2. resolve its employee;
3. employee must be ACTIVE;
4. resolve current presence state for that employee, if any;
5. fail closed on not-found, inactive/released identity, impossible ambiguity or incompatible runtime state.

Response parity must provide only scan-required current context:
- `employeeCodeId`;
- `employeeCode` / MNV;
- `employeeId`;
- `fullName`;
- `currentPortraitMediaId` or null;
- current presence object or null with `currentState`, `businessDate`, `entityVersion`;
- runtime/request metadata as appropriate.

This route is read-only. It must not create/update events, state, outboxes, Sheets or Drive.

### Required implementation/evidence sequence

1. Implement Cloud read store + authenticated scan-context route using existing D1 state and current permission evaluator.
2. Implement LAN read store against materialized `module_current_state` and wire it into secure signed/session-authenticated routes using the same response semantics.
3. Add automated Cloud tests for active resolution, inactive/released/not-found, presence-null/versioned presence, permission/password-change gating, and response shape.
4. Add LAN source/harness coverage for equivalent materialized-state vectors, signed route/session authorization and fail-closed invalid inputs.
5. Add/extend parity validation so Cloud/LAN route contract cannot silently diverge.
6. Do not deploy BETA merely to prove source parity unless current deployment policy/workflow calls for it; source/CI PASS and live provider PASS remain distinct evidence levels.
7. Checkpoint last, open focused PR, require clean baseline and all affected product foundations/harnesses SUCCESS, merge only exact tested head.
8. Verify post-merge main CI and reconcile governance.

## NEXT DEPENDENT — Android scanner -> ATTENDANCE_IN/OUT command planning

Only after scan-context parity is accepted:

1. normalize scanner MNV with `ScannerPayload`;
2. call scan-context route and use returned technical `employeeId` + current presence/version;
3. human-facing workflow may show returned ACTIVE name + current portrait reference, but no final Pick Pack visual details are invented before authorized reference evidence;
4. build stable `requestId`, `idempotencyKey`, command code, `entityId=employeeId`, `expectedEntityVersion=presence?.entityVersion ?? null`, payload with employeeId/businessDate/occurredAt/source and monotonic `deviceSeq` where current device identity provides it;
5. same logical command identity survives Cloud/LAN route choice;
6. no actor fields and no direct database/Google writes;
7. unsupported/invalid state fails closed before dispatch;
8. add pure-Java Android harness vectors and require Android PR foundation before merge.

## Following Android READY queue

- reconnect/resync and network lifecycle while preserving same-runtime authority semantics;
- HTTPS/trust fail-closed behavior;
- foreground/background recovery and stale-session handling;
- client-local durable queue only for commands explicitly approved for `LOCAL_QUEUE_ONLY`.

## Other lanes / gates

- LAN physical provider proof remains pending: target Windows host, company-network/PDA/public trust, live LAN->Cloud linkage, >=60-minute Internet-cut test, reconnect/network-change regression, capacity/soak/UAT.
- Projection path is live-PASS; do not rewrite it absent new failing evidence.
- Web/business work proceeds only where safe current contracts exist.
- Real ROOT/normal email-OTP provider delivery E2E remains gated.
- ROOT TOTP verifier parameters must not be invented.
- Portrait replacement remains `OWNER_DECISION_REQUIRED`.
- STABLE activation/promotion requires mandatory BETA acceptance and explicit Owner approval.

## Repo/governance

Keep Issue #8, `CURRENT_STATE.md`, this file and `CHECKPOINT.md` synchronized. Progress remains **56.2% exact / 56% displayed** until a weighted threshold is defensibly changed.

## Current execution line

`Overall 56% displayed / 56.2% exact | Phase 6 65% | Android endpoint + secure session PASS | Scanner command blocked on trusted MNV→employee scan-context query | Primary READY: Cloud+LAN scan-context parity | Physical Windows/PDA/outage acceptance pending`
