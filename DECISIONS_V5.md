# DECISIONS V5 — RECOVERED EFFECTIVE OWNER RULES

Status: ACTIVE / RECONCILED FROM OWNER-APPROVED MATERIAL 2026-09-13

This file is an additive authority layer over `DECISIONS.md`, `DECISIONS_V3.md` and `DECISIONS_V4.md`.

The five reviewed Owner documents contain both locked Owner rules and implementation proposals. Only rules that were explicitly locked, or older authority that remains compatible with newer Owner instructions, are promoted here. Technical suggestions remain design guidance unless separately adopted.

Where this file conflicts with an older decision, this file wins. V3/V4 continue to win for LAN/Cloud/Google/offline/STABLE topics already superseded there.

## V5-001 — Dynamic permission catalog

Permission UI must not hard-code a fixed list of permissions.

- Active permission definitions are Service/D1 data.
- New product functions declare stable permission codes as part of the Service/domain release.
- Web/APK administration reads the current catalog from Service.
- Retired permissions become inactive/retained; historical grants are not deleted merely because the permission is no longer offered.
- Effective authority remains role baseline + direct grants + cluster/module scope + DENY precedence.

## V5-002 — Employee identity when an MNV is reused

`employee_id` identifies the person; MNV is a reusable business code.

- Two ACTIVE people may never share the same MNV.
- If the same known person returns, the existing `employee_id` may be reactivated after identity verification; the history remains one person's history.
- If a different/new person receives an old MNV, a new `employee_id` is mandatory and the histories remain separate.
- QR continues to contain MNV only; current ACTIVE name + portrait are shown for human verification.

This clarifies D-019, whose earlier wording was too broad when it said every reused MNV always creates a new employee identity.

## V5-003 — ROOT recovery channels retained from the 34/34 Owner review

- The former HH/mm mechanism remains removed; TOTP is the time-based ROOT mechanism.
- ROOT email OTP remains exactly four decimal digits, is one-time, is visible in the subject, and a successful use invalidates it and causes a replacement code to be generated/sent.
- SMS recovery/backup is required from BETA, not merely a future placeholder.
- Recovery destination allowlists and ROOT username are application-policy locked; delivery provider/API credentials may be replaced.
- Actual recovery email/phone values remain outside this PUBLIC repository and belong in reviewed provider/secret configuration.
- OTP/TOTP secrets or readable codes must never be written to source, Sheet, Drive business data, diagnostics or audit logs.

Two implementation/security semantics remain explicitly unresolved and are listed under `OWNER_DECISION_REQUIRED` below; no source should invent them.

## V5-004 — Connected-session convergence

When authoritative services can communicate, the intended account behavior is a single current active login context per account/device-policy, with a newer accepted login able to revoke/supersede an older one according to the reviewed session policy.

A hard partition may temporarily produce independently valid local sessions. Reconnection resolves current authority prospectively; already accepted offline business events retain their original authority evidence and are reconciled rather than silently deleted.

This rule does not impose a duration-only expiry on offline login.

## V5-005 — No legacy business-data migration

The old Pick Pack project is reference/test material only.

- Do not migrate its employee/resource/history rows into the new VHDCHY runtime.
- BETA uses synthetic/controlled data generated under the new schema/contracts.
- STABLE starts with its own new operational data and never receives BETA test/runtime data as the promotion mechanism.
- Legacy source/components may be reused only after review against current contracts.

## V5-006 — Resource behavior retained from the approved Pick Pack design

- PICK requires a PDA; User Pick is optional and multiple User Pick assignments may exist over time/where approved by the current task rule.
- A normally returned PDA becomes reusable immediately in the same business date.
- A PDA returned because of a device fault must be made unavailable for ordinary reassignment until its condition is cleared.
- User Pick, User Pack and Pack Table are same-day locked after release and require explicit Reissue before reuse.
- Reissue retains reason/actor/approval evidence and increments history; there is no silent reset of daily usage.
- PACK Table -> User Pack mapping is 1:n by cluster/effective period/optional shift, but a concrete PACK assignment selects the compatible available User Pack required by the current task; mapping membership is not equivalent to assigning every mapped user.

## V5-007 — Username/account retention rule

Application accounts with security/business history are retained and closed/archived rather than hard-deleted. Historical username identity is not silently reassigned to another person/account.

## V5-008 — UI authority parity

Website does not have a stronger business authority than APK merely because it has a wider management UI. Both use the same Service/domain permission model.

Baseline information architecture retained from the approved master design:
- APK: operational PDA-first navigation with business, people, history, synchronization and settings surfaces as permission allows.
- Website: dashboard, business operations, people, attendance/labor, resources, documents, import/export/history, synchronization, administration and settings surfaces as permission allows.

Exact visual/component layout may evolve during BETA without changing business authority.

## V5-009 — Old pending data survives client/service upgrades

Client/service version upgrades must not discard an old pending business event merely because its source schema/app version is old.

- Attempt reviewed compatibility/migration first.
- If the event cannot be safely interpreted under the new rules, preserve it as explicit version/conflict evidence for authorized review.
- Compatibility adapters are retired only after telemetry proves no relevant pending data/devices remain for a reviewed safe period.

## V5-010 — Free-first is an optimization policy, not a data-safety override

The platform is optimized for free/provider quotas first through indexed reads, batching, idempotency, caching where safe, no polling-heavy designs, archive/readback verification and measured BETA load.

Paid capacity is considered only after reasonable optimization/measurement shows the real workload needs it. No quota optimization may silently drop business history, unreconciled events, required audit evidence or staged media.

## OWNER_DECISION_REQUIRED — ROOT factor semantics

The reviewed sources do not resolve these two points consistently enough to implement them safely:

1. **TOTP requirement:** must ROOT always have TOTP enabled/required, or may ROOT disable TOTP and rely on the approved recovery channels?
2. **Email OTP lifetime:** an older approved master baseline used a 10-minute OTP lifetime, while the later 34/34 review defines a pre-generated one-time code that is replaced after successful use but does not explicitly lock the expiry/rotation behavior when it is not used.

Until Owner answers, implementation must not invent these semantics. All independent work continues.
