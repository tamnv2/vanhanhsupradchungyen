# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence reconciled through live main before this state update: `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

The percentage remains unchanged. Android endpoint acquisition has now reached source/CI PASS, but it closes only one Phase-8/Phase-6.2 mechanic. Android business workflows, session persistence, scanner command handoff, lifecycle/reconnect/resync, final UI fidelity, physical PDA/company-network acceptance and the >=60-minute outage gate remain open; no weighted phase threshold is defensibly moved yet.

## Projection / Cloudflare Cron / Google Sheet — LIVE BETA PASS

Accepted evidence remains:

- isolated live projection E2E `34927511443`: `SUCCESS`;
- cleaned Worker deploy `34927869845`: `SUCCESS`;
- post-deploy observer `34927996370`: `SUCCESS`;
- active Worker exports `fetch` + `scheduled`, Cron is exactly `*/2 * * * *`, projection outbox/pending count is 0.

The former Cron incident is closed unless new failing evidence reopens it.

## Web Slice-1 — FIRST AUTHENTICATED MUTATION MERGED PASS

PR #19 is merged at `db746a71ed80dafd288218f599ab3e990f87e439`.

- PR clean baseline `34928060646`: `SUCCESS`;
- post-merge clean baseline `34928090928`: `SUCCESS`;
- product foundations `34928090885`: Cloud/Web/Android/LAN `SUCCESS`.

Employee-create is available through the authenticated shared Cloud/LAN business client. Broader Web mutations remain fail-closed where safe current-state/version-backed UX is absent.

## V6 account/security — EMAIL OTP SOURCE/CI PASS, LIVE PROVIDER GATED

PR #30 is merged at `c45b9fca28b189dc4aa7e0b42ac7db9307c3613a`.

- PR clean baseline `34930120980`: `SUCCESS`;
- post-merge clean baseline `34930171765`: `SUCCESS`;
- post-merge product foundations `34930171683`: Cloud/Web/Android/LAN `SUCCESS`.

Current source has fixed email-OTP request/use paths, registered-email normal recovery -> restricted `MUST_CHANGE_PASSWORD`, runtime-only allowlisted ROOT destination, four-digit/5-minute/5-minute/single-use semantics, fail-closed injected delivery, and a ROOT TOTP gate that cannot be bypassed by client input.

Not live-PASS:

- approved OTP runtime provider/bindings/secrets are not provisioned for real delivery E2E;
- repo has no trusted TOTP verifier and active authority does not lock enough algorithm/digits/time-step/window/`secret_ref` detail to invent one safely.

## Android/PDA endpoint acquisition — MERGED SOURCE/CI PASS

PR #32 `Add ordered Android LAN endpoint acquisition` is merged to main at `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`.

Accepted behavior:

- approved acquisition order is `cached healthy endpoint -> LAN discovery -> manual recovery`;
- every selected candidate is HTTPS-only and health-probed;
- healthy cache short-circuits discovery/manual to avoid flapping;
- stale/invalid cache falls through safely;
- invalid discovery input is discarded as untrusted network input;
- manual recovery is last and rejects insecure/path-bearing endpoints;
- probe exceptions are unhealthy, never success;
- no healthy candidate returns no selection instead of fabricating an endpoint;
- policy has no Cloud fallback input and cannot silently change Cloud/LAN authority.

Evidence:

- PR clean baseline `34931008972`: `SUCCESS`;
- PR Android foundation `34931008973`: `SUCCESS`;
- harness output: `ANDROID_ENDPOINT_ACQUISITION_PASS checks=24`, `ANDROID_RECONNECT_BEHAVIOR_PASS checks=7`, `ANDROID_TRANSPORT_BEHAVIOR_PASS checks=52`;
- PR merge commit `00ac446a6bb3d93f1c69c1bce8ab2571ec5f11a3`;
- post-merge clean baseline `34933290109`: `SUCCESS`;
- post-merge product foundations `34933290105`: Cloud/Web/Android/LAN `SUCCESS`.

The new PR-only Android foundation workflow now requires `assembleDebug`; Android `preBuild` runs transport/reconnect/endpoint harnesses before a changed Android PR can be accepted.

Final Android/PDA visual fidelity is still separate and must not invent Pick Pack 1291 details without authorized source/artifact evidence.

## LAN continuity / reconciliation — SOURCE/HOSTED CI PASS, PHYSICAL ACCEPTANCE PENDING

Retained accepted evidence includes signed Cloud operational snapshot/coverage, LAN signed refresh/rebase/readiness recovery, durable reconciliation queue/retry/restart/conflict mechanics, integration `34851773729` and clean baseline `34851772963`, plus hosted Google/Drive receipt/conflict/recovery evidence `34853854932`, `34853938581`, `34879543693`, `34853938669`, `34879543810`.

Physical company-network/PDA/public-trust, intended Windows host acceptance, live LAN->Cloud provider linkage and >=60-minute Internet-cut acceptance remain separate gates.

## Cloud schema parity — LIVE BETA PASS

Migration `0014_employee_code_entity_version.sql` remains accepted via `34844597357`, `34844821406`, `34844932823`. Do not replay migration 0014.

## Current ready lanes

1. **Android/PDA mechanics — primary READY:** session persistence/expiry is next. Secure local credential storage is an implementation-security decision; raw password/OTP/TOTP must never be persisted. Restore must fail closed on expiry/corruption/key invalidation and retain the originally authenticated runtime mode rather than auto-switch authority.
2. **Android/PDA mechanics — following:** scanner command handoff; reconnect/resync/network lifecycle; HTTPS/trust fail-closed behavior; foreground/background recovery.
3. **Account/security:** real OTP provider E2E is gated; TOTP verifier parameters must not be invented.
4. **Web/business UI:** continue only interactions backed by safe current contracts and V6 auth semantics.
5. **Gateway/integrations:** broader bounded failure/recovery/receipt/provider coverage may continue without disturbing the proven projection path.
6. **Repo/governance:** keep Issue #8, this file, `NEXT_ACTIONS.md` and `CHECKPOINT.md` synchronized with accepted evidence.

## Current blockers / owner gates

- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance: physical environment required;
- real ROOT/normal email-OTP delivery/request/use E2E: reviewed runtime provider/bindings/secrets required;
- ROOT TOTP verifier implementation: operational verifier parameters/secret resolution are not sufficiently locked to invent safely;
- final Android/PDA visual fidelity: authorized Pick Pack reference evidence required;
- portrait replacement behavior: `OWNER_DECISION_REQUIRED`;
- STABLE activation/promotion: explicit Owner approval only after mandatory BETA acceptance.

## Resume instruction

A fresh chat must live-read `AI_ENTRYPOINT.md` from GitHub `main`, execute its bootstrap, reconcile HEAD against `CHECKPOINT.md`, then continue `NEXT_ACTIONS.md`. Memory/chat summaries are locators only, not authority.
