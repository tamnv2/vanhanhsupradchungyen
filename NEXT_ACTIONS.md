# NEXT ACTIONS — VHDCHY

Status: ACTIVE READY QUEUE / V9 WIP
Execution authority: `DECISIONS_V9.md`
Procedure: `docs/EXECUTION_MODEL_V1.md`

This file owns the current READY/BLOCKED/WIP queue. It intentionally does not copy the project percentage; read `docs/PROGRESS_TRACKING_V2.md`.

## WIP limit

Normal active WIP is capped at **three lanes**:

1. one integrating vertical slice;
2. one independent client/Web lane;
3. one next-domain preparation lane.

Do not open a fourth active lane unless one existing lane is completed, evidence-blocked/gated, or deliberately replaced.

Provider/physical gates that cannot execute now are listed separately and do not consume WIP.

---

## LANE A — INTEGRATING — Attendance golden path

**State: READY / PRIMARY**

Acceptance target:

`PDA scan MNV -> authenticated scan-context -> ACTIVE employee/name/portrait/current presence -> ATTENDANCE_IN/OUT -> Cloud or LAN -> immutable event/current presence -> visible commit/sync state -> projection where applicable -> retry/restart without duplicate`

### A1 — Cloud + LAN scan-context parity

**State: READY / immediate node**

Implement one authenticated read meaning across Cloud and LAN.

Required contract:

- input is bounded normalized `{employeeCode}` / MNV only;
- current authenticated user required;
- normal password-change restriction applies;
- attendance scan permission/scope required;
- LAN additionally requires secure HTTP, paired-device signed proof and matching session/device/security epoch;
- resolve exactly one ACTIVE employee-code assignment and linked ACTIVE employee;
- return `employeeCodeId`, `employeeCode`, technical `employeeId`, `fullName`, current portrait media reference or null, and current presence `{currentState,businessDate,entityVersion}` or null;
- no actor authority in query payload/response;
- no mutation/event/outbox/Google side effect;
- fail closed on malformed input, no active match, ambiguity, inactive/inconsistent state or runtime dependency failure;
- Cloud/LAN response semantics and tests remain aligned.

Acceptance evidence:

- focused Worker tests;
- focused LAN route/store/security harnesses;
- Cloud/LAN parity vectors;
- clean baseline / affected foundations on exact PR lineage.

### A2 — Android ATTENDANCE_IN/OUT command planning

**State: DEPENDS_ON A1**

After A1 PASS:

- build command from resolved technical `employeeId` and current `presence.entityVersion` where required;
- keep actor identity server/session derived;
- preserve one stable command/idempotency identity across route/retry;
- reject invalid/unsupported scanner values before dispatch;
- no direct D1/SQLite/Sheets/Drive business write from Android.

### A3 — Minimal Vietnamese attendance scanner/result UI

**State: READY in parallel where contract-independent**

Can build shell/state/fixtures while A1 integrates, provided no missing business semantics or inaccessible Pick Pack visual details are invented.

Required visible states include at least:

- ready to scan;
- employee resolved with name/portrait availability/current presence;
- IN/OUT action state;
- committed/accepted;
- LAN accepted pending sync where applicable;
- rejected/permission/session/runtime error;
- conflict/resync-required state where applicable.

### A4 — Dispatch + retry/restart/idempotency acceptance

**State: DEPENDS_ON A1+A2**

Prove same logical attendance command does not duplicate because of Cloud/LAN route choice, retry or app/runtime restart. Keep local acceptance, Cloud synchronization and Google projection as distinct statuses/evidence.

### A5 — Slice reconciliation

**State: DEPENDS_ON A1–A4**

At the meaningful slice boundary only:

- record exact CI/provider/physical evidence levels;
- update `CURRENT_STATE.md` once;
- award justified fixed credits in `docs/PROGRESS_TRACKING_V2.md`;
- update this queue and checkpoint;
- do not create governance-only PRs for every internal mechanic.

---

## LANE B — INDEPENDENT CLIENT — Web current-contract business surface

**State: READY / PARALLEL**

Use stable current contracts and V7 design direction. Priority is usable actions rather than display-only placeholders.

Ready work includes, where current command/version inputs are already sufficient:

- complete employee management actions around existing Slice-1 commands;
- expose current attendance/presence information safely;
- improve Online/LAN shared runtime/network/sync status UX;
- keep Vietnamese-only current product requirement;
- keep LAN-critical assets local/offline-capable;
- use fixtures only where contract is locked and integration is not yet ready.

Do not invent missing server command contracts just to fill UI buttons. A business action that lacks a safe current-state/version contract moves to Lane C/backlog rather than being implemented optimistically.

Lane B can be temporarily narrowed when the same source files conflict with Lane A integration, but it must not be stopped merely by provider/physical gates.

---

## LANE C — NEXT DOMAIN PREPARATION — Work session + PICK/PACK + resources

**State: READY / PARALLEL PREPARATION**

Prepare the next vertical slice from current Owner rules and canonical data model.

Required scope to lock before implementation expansion:

- MAIN active work session per employee; additional session requires current reason/approval authority;
- PICK requires PDA;
- User Pick optional, multi-user history retained;
- Pack Table -> User Pack mapping 1:n and compatible available User Pack chosen atomically;
- PDA/User Pick/User Pack/Pack Table assignment/release/reissue rules;
- faulty PDA unavailable; normal PDA reusable immediately;
- User Pick/User Pack/Pack Table same-day lock/reissue semantics;
- cross-cluster borrowing;
- current permissions/idempotency/event/conflict semantics.

Immediate output should be current command/acceptance contract + focused core tests for the next slice, not speculative generic infrastructure.

Do not let Lane C modify a contract that Lane A currently depends on without explicit compatibility review.

---

## BACKLOG AFTER CURRENT WIP

After Slice 2 is integrating, next order is:

1. labor + dropped goods;
2. documents/media, with portrait replacement conflicting behavior still fail-closed;
3. admin/reporting/conflict-resolution UI;
4. remaining Web/Android product polish;
5. physical/capacity/soak/UAT;
6. exact BETA acceptance and explicit STABLE promotion.

---

## GATED / BLOCKED — does not consume WIP

- Real ROOT/normal email-OTP provider E2E: needs reviewed provider configuration/secrets.
- ROOT TOTP verifier: technical algorithm/digits/timestep/window/secret-ref contract insufficiently specified; do not invent.
- Physical corporate Windows host / target Wi-Fi-LAN / PDA / public-trust regression.
- >=60-minute Internet-cut, reconnect/restart/network-change physical acceptance.
- Capacity/soak and final Owner UAT.
- Final Android Pick Pack visual fidelity: requires accessible authorized reference evidence.
- Actual conflicting portrait replacement behavior: `OWNER_DECISION_REQUIRED`.
- STABLE promotion: explicit Owner approval after mandatory BETA acceptance.

## Admission / anti-overengineering rule

Before starting any new technical task, name the acceptance item it closes or unblocks. If none exists, defer it unless it is a mandatory architecture/security/migration/provider/release-safety prerequisite.

## CI rule

Use the existing focused Worker/LAN/Web/Android workflows and harnesses as Tier 1–2 evidence. Require clean baseline plus affected foundations before exact-head merge, and affected main confirmation afterward. Do not deploy providers or run unrelated full-product suites merely because a docs/helper change occurred.
