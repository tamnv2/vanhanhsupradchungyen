# DECISIONS V6 — ROOT / PASSWORD-RECOVERY AUTHORITY

Status: ACTIVE / OWNER-LOCKED 2026-09-13

This file is an additive authority layer over `DECISIONS.md`, `DECISIONS_V3.md`, `DECISIONS_V4.md` and `DECISIONS_V5.md`.

Where this file conflicts with older ROOT/password-recovery rules, this file wins. Unaffected older decisions remain active.

## V6-001 — ROOT primary login is email one-time password

ROOT uses the approved email one-time-password flow as its normal login mechanism. ROOT does not depend on a permanent password that must later be changed after this login.

- The fixed/approved ROOT recovery-email channel cannot be disabled.
- A ROOT one-time password is single-use.
- It is valid for 5 minutes from issuance.
- A new one-time password cannot be requested until 5 minutes after the previous send, whether the previous credential was used or not.
- Successful ROOT login with the one-time password does **not** create a `MUST_CHANGE_PASSWORD` requirement.
- Web and APK must expose the ROOT-compatible `Lấy lại mật khẩu` / request-one-time-password flow.
- Actual destination addresses and provider credentials remain outside the public repository.

The previously approved ROOT email-code presentation/delivery constraints remain active unless a later Owner decision changes them; V6 changes the credential lifecycle and makes this email one-time credential an intended ROOT login mechanism rather than only a recovery fallback.

## V6-002 — ROOT TOTP is optional

TOTP may be enabled or disabled for ROOT.

- If enabled, successful ROOT authentication must also satisfy the TOTP factor according to the current ROOT auth state machine.
- If disabled, ROOT can authenticate using the valid email one-time password alone.
- Disabling TOTP never disables the fixed email one-time-password channel.
- TOTP secret material remains protected and must never be emitted to source, Sheet/Drive business data, logs or diagnostics.

## V6-003 — Normal-account forgot-password flow

For a non-ROOT account with a registered recovery email:

- `Lấy lại mật khẩu` sends a single-use one-time password to that registered email.
- The one-time password expires 5 minutes after issuance.
- A new one-time password cannot be requested until 5 minutes after the previous send, regardless of whether the previous credential was used.
- Successful login with that one-time password creates a restricted `MUST_CHANGE_PASSWORD` state.
- Before ordinary product functions are available, the account must set a new permanent password that is different from the one-time password just used.
- The new permanent password remains subject to the normal password policy.
- The one-time credential is never stored/logged in readable form; only reviewed verifier/state metadata may persist.

This flow is distinct from an ADMIN/SUPERADMIN-initiated reset, but both temporary-password paths require a normal account to establish a new permanent password before normal use.

## V6-004 — Common one-time-password lifecycle

Unless a later Owner decision explicitly changes a specific account class, the common lifecycle for email one-time passwords is:

`REQUESTED -> ISSUED (5-minute validity / 5-minute resend cooldown) -> USED | EXPIRED | SUPERSEDED`

Required guarantees:
- atomic single-use consumption;
- bounded validity checked server-side using authoritative service time;
- replay after successful consumption is rejected;
- resend before cooldown ends is rejected;
- a later issued credential supersedes any earlier still-unused credential once issuance is accepted;
- request/use/failure/security events are auditable without logging the credential itself;
- Cloud and LAN implementations must preserve the same semantic contract; offline behavior may use only locally available approved authority/delivery capability and must not fabricate successful email delivery.

## V6-005 — Superseded unresolved gate

The `OWNER_DECISION_REQUIRED` ROOT-factor block in `DECISIONS_V5.md` is resolved by V6:

1. ROOT TOTP **may be disabled**.
2. Email one-time-password validity is **5 minutes** and resend cooldown is **5 minutes**.
3. ROOT uses the email one-time password as an intended login mechanism and does not require a post-login password change.
4. Normal accounts using a one-time recovery password **must** change to a different permanent password before ordinary system use.

No implementation may re-open these points merely because older documents state different ROOT/password-recovery semantics.
