# OWNER BUSINESS RULES V1 — VHDCHY / PICK_PACK_1291

Status: ACTIVE CANONICAL HANDBOOK
Updated: 2026-09-14
Authority layers: `DECISIONS.md` -> `DECISIONS_V3.md` -> `DECISIONS_V4.md` -> `DECISIONS_V5.md` -> `DECISIONS_V6.md` -> `DECISIONS_V7.md`.

Purpose: give future AI/Dev one compact business-rules source without requiring the old Word files or chat history. This handbook does not replace the decision files; it consolidates their effective meaning.

## 1. Product and authority

VHDCHY is the DC Hưng Yên platform, not a renamed Pick Pack application. `PICK_PACK_1291` is the first operational cluster/domain proving the Core.

One product has four first-class deliverables:
- Website;
- Android APK for PDA;
- Cloud Service;
- full LAN Service substitute.

Website/APK share one business/domain/API/permission model. Cloud/LAN share the same command validation, event meaning, idempotency, version and error semantics. Google Sheets/Drive are downstream projection/storage, never business authority.

## 2. Current environment boundaries

- BETA online host: `beta.supra.cc.cd`.
- STABLE online host: `supra.cc.cd`.
- BETA LAN canonical host target: `lan-beta.supra.cc.cd`.
- STABLE LAN canonical host target: `lan.supra.cc.cd`.
- BETA/STABLE Worker, D1, Google/GAS/Drive, signing, local LAN state, sessions and operational data stay isolated.
- STABLE is prepared during development but remains fail-closed for business traffic until full BETA PASS + explicit Owner promotion approval.
- Promotion uses the exact accepted BETA release identity, not accidental latest `main`, and never copies BETA runtime/business data into STABLE.

## 3. Account levels and authorization

Account levels: ROOT, SUPERADMIN, ADMIN, USER.

- Role/level is a baseline/ceiling; effective authority comes from permission definitions/grants and cluster/module scope.
- Explicit matching DENY wins over ordinary ALLOW.
- ROOT and SUPERADMIN are equal for ordinary business/all-cluster authority.
- ROOT alone controls ROOT-exclusive security/recovery/policy resources.
- ADMIN/USER see and act only inside granted scope.
- Job title/employee position never automatically creates application permissions.
- Same-level account management is allowed only with a dedicated permission.
- A recipient must not delete/lock the account that granted the relevant authority.
- Accounts with business/security history are closed/archived/disabled, not hard-deleted.
- Historical username identity is not silently reused for another account.
- At most one ACTIVE application account per employee per environment.
- Permission catalog is dynamic Service/D1 data; Web/APK permission administration must not hard-code a fixed permission list.

## 4. ROOT authentication/recovery

Locked through V6:
- ROOT username is fixed at `admin` per environment.
- HH/mm password logic is removed.
- ROOT has no permanent-password login requirement; its primary login is the approved email one-time-password flow.
- The fixed/approved ROOT recovery-email channel cannot be disabled.
- ROOT email OTP is exactly four decimal digits and remains subject to the previously approved email presentation/delivery constraints.
- A ROOT email OTP is single-use, valid for 5 minutes from issuance, and a new OTP cannot be requested until 5 minutes after the previous send whether the previous credential was used or not.
- A later successfully issued OTP supersedes an earlier still-unused credential.
- Successful ROOT email-OTP login does **not** create `MUST_CHANGE_PASSWORD`.
- TOTP is optional for ROOT. If enabled, the ROOT auth state machine must also satisfy TOTP; if disabled, a valid email OTP alone is sufficient.
- Disabling TOTP never disables the fixed ROOT email-OTP channel.
- Web and APK must expose the ROOT-compatible `Lấy lại mật khẩu` / request-one-time-password flow.
- Actual recovery destinations and provider/API credentials remain outside the public repository.
- OTP/TOTP secrets and readable codes never enter source, Sheets, Drive business data, logs or diagnostics.
- Request/use/failure/security events are auditable without logging the credential itself; replay after successful use is rejected and resend before cooldown ends is rejected.
- Cloud and LAN preserve the same auth semantics; neither may fabricate successful email delivery when delivery capability is unavailable.

The prior ROOT-factor Owner-decision gate from V5 is resolved and must not be re-opened from stale documents.

## 5. Normal password/session rules

- Normal password minimum: 8 characters.
- Block overly common passwords.
- Long passphrases are allowed.
- No forced periodic password change absent incident.
- Admin/Super reset creates a temporary password and forces change on next login.
- `Lấy lại mật khẩu` for a normal account with registered recovery email sends a single-use email OTP valid for 5 minutes with 5-minute resend cooldown.
- Successful normal-account login using recovery OTP creates restricted `MUST_CHANGE_PASSWORD`; the account must establish a different new permanent password before ordinary product functions are available.
- Passwords are stored only as reviewed verifiers/hashes; never plaintext.
- When connected authority can coordinate, session policy converges on the current active login context; hard partitions may temporarily produce independent local sessions that reconcile later.
- Security/account changes synchronized after reconnect apply prospectively; previously accepted offline business events retain evidence.

## 6. Employee identity and MNV

`employee_id` is the durable person identity. MNV is a business code.

- Two ACTIVE people may never share one MNV.
- MNV may be reused only after the former active holder is inactive/left.
- If the same verified person returns, the existing employee identity may be reactivated.
- If a different/new person receives an old MNV, create a new `employee_id`; never merge historical records.
- Employee QR contains MNV only.
- Scan UI resolves the current ACTIVE person and displays name + portrait for human verification.
- A person may belong to multiple clusters.
- Employee profile and application account are separate objects.

Baseline profile concepts: name, phone, vendor/NCC, department, site, warehouse, main position, employment status, start/permanent-leave dates, note, portrait, cluster memberships and audit metadata.

## 7. Employee portrait/media lifecycle

- Replacing the current employee portrait deletes the previous portrait file immediately while preserving replacement/audit metadata.
- For a permanently leaving employee, any separate portrait-removal action requires explicit authorized confirmation; do not silently rewrite employee history.
- Document/evidence images are a different class and remain retained until a later explicit retention policy changes them.
- No arbitrary fixed-MB compression cap is locked; preserve readability/identification and measure BETA.

## 8. Shift/business-date rules

- Shifts belong to clusters.
- Changing shift name or hours creates a new shift version/code; old definitions remain referenceable.
- Existing open work continues against the old shift definition until completion.
- `business_date` follows the reviewed shift/session business day, including cross-midnight cases; do not blindly use wall-clock calendar date.

## 9. Attendance and presence

- Attendance is separate from work-session/business task state.
- Multiple IN/OUT events are allowed on one business date.
- Only one current presence state exists at a time.
- IN records attendance/presence only; it does not automatically start PICK/PACK work.
- OUT without a valid preceding presence is rejected for ordinary users; authorized correction creates new correction evidence rather than rewriting raw history.
- Duplicate/retry of the same logical scan must not create duplicate events.
- Attendance events are immutable history; presence is current state derived/updated from accepted events.

## 10. Work sessions and tasks

- One employee has at most one MAIN open work session across the environment.
- Additional concurrent work requires an explicit EXTRA-session reason/approval record.
- A single work session may contain both PICK and PACK tasks.
- Closing/correcting sessions must not silently auto-finish unrelated open labor; the user/authorized resolver must handle the open state explicitly.

## 11. PICK rules

- PICK requires a PDA.
- User Pick is optional.
- Assignment changes are append/history events, not overwrite-in-place business history.
- Multiple User Pick assignments/history are supported where the task flow requires it.
- Changing PDA must not duplicate or reset User Pick history.

## 12. PACK rules

- Pack Table -> User Pack mapping is configuration, not a fixed 1:1 column.
- One table may map to multiple User Pack identities by cluster/effective range/optional shift.
- When a PACK task selects a table, the Service returns only User Pack candidates that are currently valid for that table/context and available/reissued.
- A concrete assignment selects the required compatible User Pack; mapping membership does not mean assigning every mapped User Pack.
- Table + chosen User Pack assignment must commit atomically under availability/version guards.
- Changing table recalculates the valid User Pack candidates.

## 13. Resource engine

Initial resource types: PDA, USER_PICK, PACK_TABLE, USER_PACK. Core must remain extensible.

- Resource identity is immutable; ownership cluster remains explicit.
- Allocation history is represented by assignments/events, not a single overwritten current-owner text field.
- PDA returned normally may be reused immediately the same business date.
- PDA returned faulty is unavailable until condition is cleared.
- User Pick, User Pack and Pack Table become same-day USED/LOCKED after release.
- Same-day reuse for those locked types requires explicit Reissue.
- Reissue keeps reason, actor/approver where required, count and event history; there is no silent daily reset.
- Cross-cluster resource borrowing is supported from V1.
- Ownership remains with the source cluster; consuming cluster and approval/use context are recorded.
- Permanent ownership transfer is a distinct operation, not implied by borrowing.

## 14. Labor / Công nhật

- Labor is a shared Core domain, not hard-coded only to Pick Pack.
- Initial types: Hỗ trợ Pick, Hỗ trợ Pack, Kéo hàng, Khác.
- Authorized users manage the labor catalog.
- Default staff deduction is false and may be configured by cluster/type.
- At most one OPEN labor item per work session.
- Cross-cluster labor support is required from V1.
- Correction creates new adjustment/history evidence; do not rewrite closed source history silently.

## 15. Dropped goods / Nhận hàng rớt

Current Owner-visible business fields are intentionally minimal:
- business date;
- DO;
- package count;
- actor;
- update time.

Input modes:
- manual DO + package count;
- QR parse to DO + package count.

Technical identifiers, device/idempotency data and raw QR may exist internally for audit/retry but are not ordinary business UI fields.

Do not impose DO+package-count as a universal uniqueness rule unless later approved. Network retry deduplication uses technical command/event identity.

## 16. Documents and evidence media

- Documents use `DRAFT -> FINAL`.
- A wrong FINAL is corrected/replaced by a new record/version; previous evidence remains retained.
- Multi-page/multi-image evidence is supported.
- Binary images/files live in Drive or reviewed staged local storage; D1 stores logical/file identity, metadata, hashes/checksums and durable state.
- FINAL must respect the current durable-file/readback gate; do not fabricate a durable state when the target file is not durable.
- Document/evidence images have no fixed deletion deadline today.

## 17. Immutable event and correction rules

Every important mutation keeps stable event/command identity and enough evidence for audit/retry/conflict handling.

Required concepts where applicable:
- event/request/idempotency identity;
- environment/cluster/entity identity;
- actor;
- device + device sequence;
- entity/base version;
- app/schema/business-rule version;
- occurred/device time and accepted/commit time;
- normalized payload/hash;
- causation/correlation information.

Raw events are immutable. Corrections/reversals/tombstones create new evidence. Exact duplicate replay is idempotent; genuine concurrent/business conflicts retain both sides rather than silent last-write-wins.

## 18. Cloud/LAN authority and synchronization

Cloud normal path: Cloud Service commits D1 current-state + immutable event + projection work atomically.

LAN is a full substitute runtime:
- maintains protected synchronized auth/config/operational state;
- commits local current-state + immutable edge event + sync work durably;
- may continue indefinitely offline under the latest synchronized authority snapshot; offline duration alone does not invalidate login;
- may project to controlled Google Sheets and upload Drive media directly when Internet/Google is reachable;
- queues/stages Google work when unreachable;
- synchronizes LAN event/outbox data to Cloud whenever Cloud becomes reachable, even if users stay routed through LAN;
- Cloud reconciliation consumes LAN events, not Google output as source truth;
- Google receipts prevent duplicate rows/files.

Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy; the action is authenticated/audited. Unresolved business conflicts go to ADMIN+ after automatic retry/dedupe/reconciliation; ROOT-security conflicts remain ROOT-only.

## 19. Google Sheets / Drive

- D1 is central structured authority after Cloud synchronization.
- Sheets is human-readable projection/reconciliation/DR only.
- Drive stores media/documents/archive/snapshots.
- One quarterly workbook per environment + cluster + quarter.
- Closed quarter is never directly edited for business correction; create D1 correction event and re-project according to policy.
- Projection uses stable keys/checkpoints and batch operations; application clients never decide business state by reading Sheet.
- Drive/file lookup uses stored IDs/catalogs rather than repeated full scans by name.

## 20. Retention, archive and snapshot

- No fixed D1 hot-retention day count is locked today.
- BETA measures actual quota/performance first.
- Never archive/purge open, pending, unreconciled or otherwise unsafe-to-remove work.
- Purge from hot storage only after durable archive readback + checksum passes.
- Snapshot capability is designed now, enabled after Core business PASS, and scheduled snapshot + restore test are mandatory before STABLE.

## 21. Client/version compatibility

- Website and APK use the same business rules/API/permission semantics.
- Old pending events are never dropped solely because an app/service version changed.
- Attempt compatible migration/adapter processing first; otherwise create explicit version conflict evidence.
- Compatibility adapters are retired only after telemetry proves no relevant old pending data/devices remain for a reviewed safe period.
- BETA/STABLE APK signing/update channels are independent.

## 22. Legacy reuse

Allowed reference reuse: reviewed workflow concepts, scanner-first UX, local durable queue patterns, C01-C10 regression, event/idempotency, resource flows, reports/documents UX, no-admin LAN mechanics, diagnostics/load/update ideas.

Never inherit without review: old provider/account/domain IDs, Sheets-as-authority, old hard-coded shifts, old deletion/retention assumptions, legacy authentication identity, or old runtime state/data.

Old employee/resource/history data is not migrated.

## 23. Mandatory Pick Pack regression C01-C10

- C01 Dispatch PICK.
- C02 Dispatch PACK.
- C03 Picker adds PACK in the same work session.
- C04 Packer adds PICK in the same work session.
- C05 Change User Pick.
- C06 Change User Pack.
- C07 Change Pack Table/bundle and re-evaluate compatible User Pack.
- C08 Change PDA without duplicating User Pick history.
- C09 Multiple User Pick behavior/history.
- C10 Multiple User Pack mapping/candidate behavior under the current one-selected-compatible-assignment rule.

Detailed end-to-end acceptance lives in `docs/BETA_ACCEPTANCE_MATRIX.md` and the current delivery plan.