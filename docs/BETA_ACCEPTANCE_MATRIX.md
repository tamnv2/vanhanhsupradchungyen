# BETA ACCEPTANCE MATRIX — SERVICE / WEB / GOOGLE

Status: ACTIVE TEST CONTRACT
Updated: 2026-09-13
Authority: `DECISIONS.md` D-039 and `docs/SERVICE_API_CONTRACT.md`

Android/PDA build and physical LAN/model acceptance are outside the current execution scope and are intentionally excluded from this matrix until the Owner resumes those lanes.

## Gate order

Acceptance runs in dependency order. A later gate does not redefine an earlier contract.

1. Provider/runtime identity
2. Authentication/session
3. Authorization/account administration
4. Canonical mutation/idempotency
5. Attendance/presence
6. Work session and PICK/PACK resources
7. Labor and dropped goods
8. Documents/Drive
9. Projection/reconciliation/degraded Google
10. Same-origin Web E2E
11. Backup/restore prerequisites for STABLE

## A — Provider/runtime identity

| Scenario | Expected result |
|---|---|
| `/health` | BETA Worker, D1 reachable, `business_core_v3` |
| `/health/deep` with Google healthy | canonical health PASS, Gateway identity PASS |
| `/health/deep` with Google unavailable | D1 health remains authoritative; response is degraded, not a false canonical failure |
| `/api/v1/meta` | BETA environment/build/schema match deployed commit |
| `/api/v1/capabilities` | D1 authority, projection-only Sheets, anonymous mutation disabled |
| workers.dev state | disabled |
| custom domain | only reviewed BETA domain |
| binding contract | exact reviewed bindings/secrets; no unexpected binding |

## B — Authentication/session

| Scenario | Expected result |
|---|---|
| valid NORMAL username/password | one ACTIVE session; raw token returned once; only token hash persisted |
| wrong password | generic invalid credentials; no account existence leak |
| disabled/locked/closed account | login denied |
| ROOT password without TOTP | `ROOT_MFA_REQUIRED`; no session |
| ROOT valid password + valid TOTP | session only after MFA PASS |
| invalid/expired/revoked bearer | 401 |
| expired session | 401 |
| revoked session | 401 |
| device security epoch changed | old linked session rejected |
| temporary password | only auth-self/password-change subset until changed |
| logout | session revoked and unusable afterward |
| change password | old verifier invalid; new verifier accepted; policy enforced |

## C — Authorization/account administration

| Scenario | Expected result |
|---|---|
| matching ALLOW | operation allowed inside effective scope |
| matching explicit DENY | DENY wins over ALLOW |
| cluster mismatch | 403 |
| module mismatch | 403 |
| expired/revoked grant | no authority |
| SUPERADMIN normal business | all-cluster business authority |
| SUPERADMIN ROOT-security action | denied |
| same-level admin without dedicated permission | denied |
| same-level admin with permission | allowed subject to grantor/self protection |
| recipient attempts to lock/delete grantor | denied |
| close account with history | retained; never hard-deleted |

## D — Canonical mutation/idempotency

| Scenario | Expected result |
|---|---|
| valid command | state + one immutable event + one outbox row commit atomically |
| version/state guard fails | 409; no partial state/event/outbox |
| event insert fails | state rolls back |
| outbox insert fails | state + event roll back |
| retry same idempotency key/same command | original canonical event returned; no second mutation |
| reuse key/different command | 409 `IDEMPOTENCY_KEY_REUSED` |
| concurrent same key | exactly one committed canonical event |
| duplicate device sequence/different command | conflict; no partial writes |
| raw event UPDATE/DELETE | database trigger rejects |

## E — Attendance/presence

| Scenario | Expected result |
|---|---|
| employee IN | immutable attendance IN + presence `IN`; no work session auto-created |
| repeated IN according to business contract | accepted as attendance history where allowed while one current presence state remains |
| OUT | immutable OUT + current presence `OUT` |
| multiple IN/OUT same business date | history retained in order; one current state |
| MNV of active employee reused | rejected |
| MNV after former employee inactive | new `employee_id`; old history remains attached to former identity |
| QR employee resolution | QR MNV resolves current active employee and UI identity data |

## F — Work session / PICK / PACK / resources

| Scenario | Expected result |
|---|---|
| start MAIN session while another MAIN OPEN exists | conflict |
| approved EXTRA session | allowed with explicit approval/reason |
| one session contains PICK and PACK tasks | allowed |
| PICK without PDA | rejected |
| PICK with PDA and optional User Pick | allowed |
| change User Pick | old assignment closed; new assignment/event appended; history retained |
| change PDA | PDA assignment changes without duplicating User Pick history |
| PACK table selection | Service returns currently valid/available mapped User Pack candidates |
| PACK commit | selected table + exactly one chosen User Pack committed atomically |
| release User Pick/User Pack/Table | locked as used for that business date |
| same-day reuse without Reissue | rejected |
| same-day reuse after Reissue | allowed with history/event |
| PDA release/reuse | PDA may be reused immediately per contract |
| cross-cluster resource borrow | source ownership retained; consuming cluster/approval/context recorded |
| concurrent resource claim | exactly one succeeds; loser gets conflict |

## G — Labor / dropped goods

| Scenario | Expected result |
|---|---|
| start configured labor type | one OPEN labor item per session |
| second OPEN labor item same session | rejected |
| finish labor | record closed with immutable history/event |
| labor correction | correction event/version, not raw history rewrite |
| cross-cluster labor support | accepted with source/consuming context |
| dropped goods manual DO + count | canonical record/event created |
| dropped goods QR parse | normalized DO/count equivalent to manual contract |
| ordinary UI read | exposes business fields only; hides technical idempotency/audit fields |

## H — Documents / Drive

| Scenario | Expected result |
|---|---|
| create DRAFT metadata | allowed without claiming FINAL durability |
| upload media fails | document cannot become FINAL |
| durable Drive upload/readback PASS | metadata stores Drive identity/hash/state |
| finalize document | FINAL only after durable evidence gate |
| incorrect FINAL corrected | new record/version retained; prior FINAL evidence preserved |
| replace employee portrait | previous image deleted immediately; replacement audit metadata retained |
| document/evidence retention | retained indefinitely under current policy |

## I — Google projection / reconciliation

| Scenario | Expected result |
|---|---|
| projection auth absent | Gateway rejects; status remains `PROVISIONED_NOT_LIVE` |
| projection disabled | Gateway rejects writes even with auth configured |
| wrong token | auth failure |
| protocol/environment mismatch | rejected |
| disallowed sheet | rejected |
| unknown column | rejected |
| missing projection key | rejected |
| valid new item | append |
| same key replay | update/upsert, no duplicate logical row |
| Google temporarily unavailable | D1 command remains committed; outbox retries |
| repeated projection failure | bounded backoff then DEAD after reviewed limit |
| ACK | outbox becomes ACKED |
| closed-quarter correction | no direct Sheet edit; D1 correction/adjustment re-projects per policy |
| reconciliation | Sheet data traceable to canonical event IDs/keys |

## J — Same-origin Web E2E

| Scenario | Expected result |
|---|---|
| login -> authenticated shell | same-origin session flow works without enabling broad CORS |
| permission-aware navigation | unavailable functions are not presented as usable authority; Service remains final authority |
| real business command | Web -> Service -> D1 atomic mutation succeeds |
| projection delayed | Web reflects canonical Service state, not stale Sheet state |
| conflict | stable machine code + actionable UI; no silent overwrite |
| retry after client/network uncertainty | idempotency prevents duplicate mutation |
| document upload | Web -> Service -> Drive durable flow + D1 metadata |
| logout | token invalid and UI returns to unauthenticated state |

## K — Pre-STABLE durability

Required before STABLE promotion can even be proposed:

- full BETA acceptance above PASS for implemented V1 scope;
- scheduled snapshot mechanism enabled after core business PASS;
- at least one verified restore test;
- Drive archive/readback/checksum path proven before any purge policy is enabled;
- quota/retention evidence collected rather than guessed;
- no OPEN/PENDING work included in destructive archive/purge operations;
- explicit Owner approval for STABLE promotion.

## Evidence rule

Every PASS claim must point to reproducible evidence such as CI run, provider readback, D1 query result, Gateway health/readback or Web acceptance run. Source presence alone is not runtime PASS.
