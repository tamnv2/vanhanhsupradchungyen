# NEXT ACTIONS — VHDCHY

Updated: 2026-09-15
Progress: **56% displayed / 56.2% exact**
Primary phase: **Phase 6 — LAN continuity/offline/reconcile**

## Execution rule

Default is `CONTINUE` with dependency-aware parallel execution. Evidence is required before PASS. A blocked lane does not stop unrelated ready work. `AI_TERMINATION_GUARD.md` forbids voluntary finalization while approved `READY` work remains.

## A — Projection Cron / Google Sheet live chain — CLOSED PASS

Do not continue the former Cron root-cause investigation unless new evidence reopens it.

- isolated live projection E2E `34927511443`: SUCCESS;
- cleaned Worker deploy `34927869845`: SUCCESS;
- post-deploy observer `34927996370`: SUCCESS;
- active Worker exports `fetch` + `scheduled`, Cron exactly `*/2 * * * *`, projection outbox/pending count 0.

## B — Web Slice-1 employee-create — MERGED PASS

PR #19 merged at `db746a71ed80dafd288218f599ab3e990f87e439`.

- PR clean baseline `34928060646`: SUCCESS;
- post-merge clean baseline `34928090928`: SUCCESS;
- product foundations `34928090885`: Cloud/Web/Android/LAN SUCCESS.

Continue Web only where safe read/version contracts support the interaction. Do not invent a version model merely to expose mutations.

## C — Account/security V6 email OTP — MERGED SOURCE/CI PASS / PROVIDER LIVE GATED

PR #30 merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

- PR clean baseline `34930120980`: SUCCESS;
- post-merge clean baseline `34930171765`: SUCCESS;
- post-merge product foundations `34930171683`: Cloud/Web/Android/LAN SUCCESS.

Still gated:

1. Do not deploy/claim live OTP delivery until reviewed runtime delivery provider/binding, OTP pepper and ROOT destination are provisioned.
2. Real ROOT + normal-account request/delivery/use E2E remains required.
3. Do not invent TOTP algorithm/digits/time-step/window/`secret_ref` resolution; TOTP-enabled ROOT stays fail-closed until authoritative verifier semantics exist.
4. Do not invent an OTP failure-attempt limit; V6 does not define one.

## D — Android/PDA endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 merged at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`.

Accepted evidence:

- PR clean baseline `34931008972`: SUCCESS;
- PR Android foundation `34931008973`: SUCCESS;
- `ANDROID_ENDPOINT_ACQUISITION_PASS checks=24`;
- `ANDROID_RECONNECT_BEHAVIOR_PASS checks=7`;
- `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=52`;
- post-merge clean baseline `34933290109`: SUCCESS;
- post-merge product foundations `34933290105`: Cloud/Web/Android/LAN SUCCESS.

Accepted behavior is `cached healthy endpoint -> LAN discovery -> manual recovery`, HTTPS-only + health-probed, cache-first anti-flap, fail-closed invalid/manual handling, no fabricated endpoint and no automatic Cloud/LAN authority switch.

PR changes to Android are now guarded by `.github/workflows/android-pr-foundation.yml`, whose APK build executes all transport harnesses through `preBuild` before merge.

## E — CURRENT PRIMARY READY: Android/PDA session persistence + expiry

Current `PdaSession` is in-memory only. Implement the smallest source-testable persistence slice without final UI work.

Exact next sequence:

1. Re-read current `PdaSession`, Android build config and transport harness on live main.
2. Add a persistence-safe session snapshot/restore contract that preserves only the authenticated bearer token, expiry and selected runtime mode needed to resume the same accepted session context.
3. Persist the session encrypted at rest using Android Keystore-backed AES-GCM; store only ciphertext/IV/version metadata in app-private storage.
4. Never persist passwords, email OTPs, TOTP secrets/codes or provider credentials.
5. Restore only when the snapshot is structurally valid and `expiresAt > now`; expiry, corruption, key invalidation, decrypt failure or unknown runtime mode must clear local persisted session state and fail closed.
6. Restoring a session must preserve its original runtime mode; it must not discover/fallback/switch Cloud/LAN authority as a side effect.
7. Add pure-Java harness coverage for snapshot/restore validity, exact expiry boundary and clear behavior; Android PR foundation must compile the Keystore adapter and build the APK.
8. Write checkpoint last after all source changes, open a focused PR, require clean baseline + Android PR foundation SUCCESS, and merge only the exact tested head.
9. After merge, run post-merge clean baseline/product foundations, reconcile governance, then continue scanner command handoff or reconnect/resync/lifecycle mechanics.

Current implementation language remains Vietnamese only. Do not fabricate Pick Pack 1291 visual details.

## F — Following Android READY queue

After session persistence closes, continue independent/source-testable mechanics:

- scanner input -> current domain command handoff without direct database writes;
- reconnect/resync and network lifecycle while retaining same-runtime authority semantics;
- HTTPS/trust fail-closed behavior;
- foreground/background recovery and stale-session handling.

Final business-screen/UI fidelity remains separately gated by authorized Pick Pack reference evidence.

## G — LAN/provider proof — PARTLY GATED

Retained source/HOSTED CI PASS includes machine-authenticated operational snapshot/coverage, signed LAN refresh/rebase/readiness, atomic authoritative import, durable reconciliation and integration `34851773729` / clean baseline `34851772963`.

Still pending and not inferable from CI:

- intended company Windows host acceptance;
- company-network/PDA/public-trust path;
- live LAN->Cloud machine/provider linkage where Owner-controlled setup is required;
- >=60-minute Internet-cut acceptance;
- reconnect/host restart/network-change physical regression;
- capacity/soak/UAT.

## H — Gateway / integrations

Projection is live-PASS. Continue genuinely open coverage only: bounded provider failure/recovery, broader receipts/idempotency/reconciliation, Drive/media provider paths, operator-visible status and safe retry behavior. Do not rewrite the proven projection path absent new failure evidence.

## I — Repo/governance

Keep synchronized with evidence:

- Issue #8;
- `CURRENT_STATE.md`;
- `NEXT_ACTIONS.md`;
- `CHECKPOINT.md`.

Progress remains **56.2% exact / 56% displayed** until a weighted phase threshold is defensibly changed under `docs/PROGRESS_TRACKING_V1.md`.

## Retained complete/current nodes

- Cloud migration `0014_employee_code_entity_version.sql`: LIVE BETA PASS; do not replay.
- LAN integration receipt/conflict/recovery source/HOSTED CI evidence remains PASS.
- Projection cleanup is clean; do not re-clean absent E2E markers blindly.

## Owner decision / release boundaries

- Portrait replacement behavior remains `OWNER_DECISION_REQUIRED`.
- STABLE activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

## Current execution line

`Overall 56% displayed / 56.2% exact | Phase 6 65% | Projection live PASS | Web employee-create PASS | V6 email-OTP source PASS/provider gated | Android endpoint acquisition merged + post-merge CI PASS | Next READY: Android session persistence/expiry | Physical Windows/PDA/outage acceptance pending`
