# CURRENT STATE — VHDCHY

Updated: 2026-09-15
Protocol: `AI_AUTHORITY_RESUME_V2`
Current delivery plan: `docs/DELIVERY_PLAN_V5.md`
Progress model: `docs/PROGRESS_TRACKING_V1.md`
Active authority: `DECISIONS.md` + `DECISIONS_V3.md` + `DECISIONS_V4.md` + `DECISIONS_V5.md` + `DECISIONS_V6.md` + `DECISIONS_V7.md`
Evidence reconciled through live main before this state update: `db746a71ed80dafd288218f599ab3e990f87e439`

## Progress

`Overall: 56% displayed | Exact weighted baseline: 56.2% | Primary phase: Phase 6 — LAN continuity/offline/reconcile`

The percentage remains unchanged. The projection live E2E and first authenticated Web employee-create interaction close important sub-slices, but they do not by themselves justify moving a weighted phase threshold. Physical LAN/PDA/outage acceptance, broader provider/business coverage, ROOT recovery E2E and final Web/App coverage remain open.

## Projection / Cloudflare Cron / Google Sheet — LIVE BETA PASS

The earlier scheduled-event incident is closed by direct live evidence; do not continue treating it as an active root-cause lane.

Accepted chain:

- read-only observer run `34926987360` proved the active Worker version exported both `fetch` and `scheduled`, live Cron was `*/2 * * * *`, and a real scheduled execution recorded `PROJECTION_IDLE`;
- isolated projection E2E harness was corrected so dispatch-only commits no longer create a false build-mismatch failure when the deployed Worker source is Git-equivalent;
- isolated live E2E run `34927511443`: `SUCCESS`;
- the E2E inserted an isolated canonical D1 marker/outbox item, received ACK, read the real Google Sheet row back, replayed the same outbox item, retained exactly one logical Sheet row, and completed D1/Sheet cleanup;
- successful ACK rows correctly retain `attempts=0`; attempts increment only on failure and are not a claim counter;
- temporary scheduler-probe code/config was removed in PR #26;
- cleaned Worker was deployed by run `34927869845`: `SUCCESS`;
- public BETA health converged to build `a501e4b7ae551cf1cd86f15786f4ca994032b522`, runtime `BUSINESS_CORE_V3`, authority `D1`, Google not degraded;
- post-deploy live observer run `34927996370`: `SUCCESS`;
- active deployment `ec6e1418-7e78-4db0-a8c8-2f6ff17d6e1b`, version 14 `945436c5-3fdf-477c-baa9-f0c6b87e08ab`, 100% serving, handlers `fetch` + `scheduled`;
- Cron remains exactly `*/2 * * * *`, projection outbox is empty, pending count is 0;
- the remaining `projection_scheduler_probe` row observed in D1 is historical evidence written by the prior diagnostic build at `2026-09-15T04:10:47Z`; the current Worker no longer writes that diagnostic.

PR #28 added `workflow_dispatch` to the read-only Cron observer so this provider evidence can be requested again without mutating runtime/provider state.

## Web Slice-1 — FIRST AUTHENTICATED MUTATION MERGED

PR #19 `Add authenticated Web employee-create interaction` is merged to main at `db746a71ed80dafd288218f599ab3e990f87e439`.

Before merge it was reconciled onto current main with a true two-parent merge and verified to differ from main only in:

- `web/slice1-ui.css`;
- `web/slice1-ui.js`;
- `web/slice1-ui.test.mjs`.

Fresh PR clean-baseline run `34928060646`: `SUCCESS`.
Post-merge clean-baseline run `34928090928`: `SUCCESS`.
Post-merge product foundations run `34928090885`: Cloud Service / Web contract / Android APK / LAN Service all `SUCCESS`.

The personnel screen can now create an employee through the authenticated shared Cloud/LAN business client, while technical employee identity is generated internally and mutation remains gated by authenticated/business-mutation capability. Update/status/MNV/attendance interactions remain fail-closed until safe current-state/version-backed UX exists; do not invent a read/version contract merely to expose buttons.

## LAN continuity / reconciliation — SOURCE/HOSTED CI PASS, PHYSICAL ACCEPTANCE PENDING

Retained accepted evidence:

- signed Cloud operational snapshot/coverage route;
- LAN signed snapshot client, authoritative import, refresh/rebase coordinator and fail-closed readiness while canonical events remain unre-based;
- durable reconciliation queue/retry/restart/conflict mechanics;
- integration run `34851773729`: `SUCCESS`;
- clean baseline `34851772963`: `SUCCESS`;
- Google/Drive integration receipt conflict/recovery source/HOSTED CI runs `34853854932`, `34853938581`, `34879543693` and clean baselines `34853938669`, `34879543810`: `SUCCESS`.

Physical company-network/PDA/public-trust, intended Windows host acceptance, live LAN->Cloud machine/provider linkage and >=60-minute Internet-cut acceptance remain separate gates. Do not infer them from GitHub CI or the Cloud projection E2E.

## Cloud schema parity — LIVE BETA PASS

Migration `0014_employee_code_entity_version.sql` remains accepted via `34844597357`, `34844821406`, `34844932823`. Do not replay migration 0014.

## Current ready lanes

1. **Account/security:** continue provider-independent V6 ROOT recovery/auth source + contract work. ROOT real email-OTP delivery/recovery E2E remains incomplete. Do not repeatedly retry equivalent connector-secret mutations that already hit tool/safety capability limits.
2. **Android/PDA mechanics:** continue endpoint/session/scanner/retry/HTTPS/reconnect behavior independent of unavailable final visual references. Current APK build is green; visual fidelity is not accepted.
3. **Web/business UI:** continue only interactions supported by current state/version contracts; employee-create is merged, broader mutations remain fail-closed where safe read/version UX is absent.
4. **Gateway/integrations:** broader bounded failure/recovery/receipt/provider coverage may continue, but do not disturb the now-proven projection path without evidence.
5. **Repo/governance:** keep Issue #8, this file, `NEXT_ACTIONS.md` and `CHECKPOINT.md` synchronized with live evidence.

## Current blockers / owner gates

- physical company-network/PDA/public-trust and >=60-minute Internet-cut acceptance: physical environment required;
- ROOT OTP real provider delivery/recovery E2E: incomplete;
- final Android/PDA visual fidelity: requires authorized current-product Pick Pack reference assets; do not fabricate visuals;
- portrait replacement behavior: `OWNER_DECISION_REQUIRED`;
- STABLE activation/promotion: explicit Owner approval only after mandatory BETA acceptance.

## Resume instruction

A fresh chat must live-read `AI_ENTRYPOINT.md` from GitHub `main`, execute its bootstrap, reconcile HEAD against `CHECKPOINT.md`, then continue `NEXT_ACTIONS.md`. Memory/chat summaries are locators only, not authority.
