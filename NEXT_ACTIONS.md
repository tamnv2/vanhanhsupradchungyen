# NEXT ACTIONS — VHDCHY

Updated: 2026-09-15
Progress: **56% displayed / 56.2% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A blocked lane does not stop unrelated ready work. `AI_TERMINATION_GUARD.md` forbids voluntary finalization while approved `READY` work remains.

## A — Projection Cron / Google Sheet live chain — CLOSED PASS

- isolated live projection E2E `34927511443`: SUCCESS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- active Worker handlers `fetch` + `scheduled`, Cron `*/2 * * * *`.

Do not reopen absent new failing evidence.

## B — Web employee-create — MERGED PASS

PR #19 merged; post-merge clean `34928090928` and product foundations `34928090885`: SUCCESS.

Continue Web only where current read/version/auth contracts support safe UX.

## C — V6 email OTP — SOURCE/CI PASS / PROVIDER GATED

PR #30 merged; clean baselines `34930120980`, `34930171765` and product foundations `34930171683`: SUCCESS.

Do not claim live provider delivery until reviewed provider/binding/secrets exist. Do not invent TOTP verifier parameters or an OTP failure-attempt limit.

## D — Android endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`.

- pre-merge clean `34931008972`: SUCCESS;
- Android foundation `34931008973`: SUCCESS;
- post-merge clean `34933290109`: SUCCESS;
- product foundations `34933290105`: Cloud/Web/Android/LAN SUCCESS.

Accepted ordering remains `cached healthy endpoint -> LAN discovery -> manual recovery`, HTTPS-only + health-probed, no fabricated endpoint, no automatic authority switch.

## E — Android session persistence/expiry — MERGED SOURCE/CI PASS

PR #34 merged at `ebfb7d238268fb089a34779e2c5d53ae45693608`.

- PR clean baseline `34933869114`: SUCCESS;
- PR Android foundation `34933869144`: SUCCESS;
- transport harness `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=66` with `sessionRestore=PASS`;
- Android `assembleDebug`: SUCCESS;
- post-merge clean baseline `34933981747`: SUCCESS;
- post-merge product foundations `34933981735`: Cloud/Web/Android/LAN SUCCESS.

Accepted behavior:

- persist only bearer token + expiry + original runtime mode;
- Android Keystore AES-GCM + app-private encrypted metadata;
- no password/OTP/TOTP/provider credential persistence;
- exact expiry/corruption/missing key/decrypt failure/unknown runtime fail closed and clear local session evidence;
- restore never creates a replacement key and never discovers/fallbacks/switches runtime authority.

## F — CURRENT PRIMARY READY: Android scanner -> domain command handoff

Current Android has `ScannerPayload.normalize(...)` but no current-product command handoff. Implement the smallest Slice-1 attendance command planning layer without UI or direct database writes.

Exact sequence:

1. Re-read live `ScannerPayload`, `PdaRequestPlan`, `PdaSession`, `contracts/commands.slice1.v1.json`, `docs/SERVICE_API_CONTRACT_V3.md`, and current mutation envelope/route source before writing.
2. Restrict this first scanner slice to the approved attendance commands `ATTENDANCE_IN` and `ATTENDANCE_OUT`; do not generalize into Pick/Pack flows whose current command contract is not yet materialized.
3. Treat normalized scanner value as business input only. Actor/user/permission identity must come from authenticated Service context; never add client-authoritative actor fields.
4. Build a stable client command plan containing the command code, normalized scan value and required command identity evidence available to the client (request/idempotency/device sequence/version fields as defined by the current route contract).
5. Same logical command must retain the same identity if later routed through Cloud or LAN; selecting runtime endpoint must not regenerate command identity.
6. Do not write SQLite/D1/Sheet/Drive directly from the Android scanner layer. Output is a Service request plan only.
7. Reject unsupported command codes, blank/control/oversize scan payloads and structurally invalid identity evidence before network dispatch.
8. Add pure-Java harness vectors for IN/OUT planning, normalization, identity stability across Cloud/LAN request planning, unsupported command rejection and absence of client actor authority.
9. Checkpoint last; open focused PR; require clean baseline + Android PR foundation SUCCESS; merge only exact tested head.
10. After merge, verify main clean/product foundations and reconcile governance.

Do not fabricate final Pick Pack UI details.

## G — Following Android READY queue

After scanner handoff:

- reconnect/resync and network lifecycle while retaining same-runtime authority semantics;
- HTTPS/trust fail-closed behavior;
- foreground/background recovery and stale-session handling;
- client-local durable queue only for commands explicitly approved for `LOCAL_QUEUE_ONLY`.

## H — LAN/provider proof — PARTLY GATED

Still pending and not inferable from CI: intended company Windows host acceptance, company-network/PDA/public-trust path, live LAN->Cloud provider linkage, >=60-minute Internet-cut acceptance, reconnect/host restart/network-change physical regression and capacity/soak/UAT.

## I — Gateway / integrations

Projection is live-PASS. Continue only open bounded provider failure/recovery, broader receipt/idempotency/reconciliation, Drive/media provider paths and operator-visible status. Do not rewrite the proven projection path absent new failure evidence.

## J — Repo/governance

Keep Issue #8, `CURRENT_STATE.md`, this file and `CHECKPOINT.md` synchronized. Progress remains **56.2% exact / 56% displayed** until a weighted threshold is defensibly changed.

## Owner decision / release boundaries

- Portrait replacement remains `OWNER_DECISION_REQUIRED`.
- STABLE activation/promotion requires mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall 56% displayed / 56.2% exact | Phase 6 65% | Projection PASS | Web employee-create PASS | V6 email-OTP source PASS/provider gated | Android endpoint + secure session persistence PASS | Next READY: scanner -> attendance domain command handoff | Physical Windows/PDA/outage acceptance pending`
