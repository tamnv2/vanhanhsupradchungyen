# BETA ACCEPTANCE MATRIX — PRODUCT V2

Status: ACTIVE TEST CONTRACT
Updated: 2026-09-13
Authority: `DECISIONS.md`, `docs/TARGET_PRODUCT_ARCHITECTURE_V2.md`, `docs/SERVICE_API_CONTRACT.md`

Website, APK, Cloud Service and LAN Service are all inside current product acceptance scope. Physical company-network tests remain a later evidence gate, but source/runtime/CI acceptance for APK and LAN is active now.

## Evidence rule

A PASS claim requires reproducible evidence: CI run, provider readback, D1/LAN DB query, Gateway readback, Web/APK E2E log, reconciliation evidence or physical test record as applicable.

Compilation alone is never business/runtime PASS.

Legacy physical evidence and the pre-clarification transport prototype are reference only and cannot be reported as current-product PASS.

## Gate order

1. Provider/runtime identity
2. Shared domain/API parity
3. Authentication/session/security
4. Authorization/account administration
5. Cloud canonical mutation/idempotency
6. LAN runtime state/snapshot/relay/autonomous/reconciliation
7. Attendance/presence vertical slice
8. Work session + PICK/PACK/resources vertical slice
9. Labor/dropped-goods vertical slice
10. Documents/media vertical slice
11. Google projection/reconciliation
12. Website Cloud/LAN E2E
13. APK Cloud/LAN E2E
14. Failure/recovery/split-brain acceptance
15. Backup/update/restore + physical corporate-LAN gate

## A — Provider/runtime identity

| Scenario | Expected result |
|---|---|
| Cloud `/health` | BETA Worker, D1 reachable, `business_core_v3` |
| Cloud `/health/deep` with Google healthy | canonical health PASS, Gateway identity PASS |
| Cloud `/health/deep` with Google unavailable | D1 health remains authoritative; response degraded, not false canonical failure |
| `/api/v1/meta` | BETA environment/build/schema match deployed commit |
| `/api/v1/capabilities` | correct authority/projection/runtime capabilities; anonymous mutation disabled |
| workers.dev state | disabled |
| public custom domain | reviewed BETA domain only |
| binding contract | exact reviewed bindings/secrets; no unexpected binding |
| LAN health | exact BETA LAN runtime identity/build/edge schema/instance/epoch, no credential leakage |
| BETA/STABLE local state | isolated; no accidental cross-environment edge DB/config |

## B — Shared domain/API parity

| Scenario | Expected result |
|---|---|
| same valid business command vector through Cloud adapter | expected transition/event/error contract |
| same vector through LAN edge adapter | same business meaning/machine result except explicit commit-location status |
| invalid command | Cloud and LAN reject with same stable validation/business error semantics |
| same idempotency identity | preserved across Cloud direct, LAN relay, LAN autonomous and retry |
| client actor field forged | ignored/rejected; authenticated context authoritative |
| provider-specific code | cannot redefine business validation outside reviewed adapter boundary |
| API compatibility | Web/APK/LAN use one reviewed contract/build family |

## C — Authentication/session/security

### Cloud baseline

| Scenario | Expected result |
|---|---|
| valid NORMAL username/password | one ACTIVE session; raw token returned once; only token hash persisted |
| wrong password | generic invalid credentials; no account existence leak |
| disabled/locked/closed account | login denied |
| ROOT password without TOTP | `ROOT_MFA_REQUIRED`; no session |
| ROOT valid password + valid TOTP | session only after MFA PASS |
| invalid/expired/revoked bearer | 401 |
| expired/revoked session | 401 |
| device security epoch changed | old linked session rejected |
| temporary password | only auth-self/password-change subset until changed |
| logout | session revoked and unusable afterward |

### LAN/offline security

The exact offline credential/capability mechanism, offline expiry and privileged-offline policy remain unresolved policy and must not be invented. Before LAN autonomous business auth can PASS, the reviewed policy must prove:

- device/Agent pairing/channel binding cannot be replaced by service strings alone;
- no raw Cloud provider credential is distributed to PDA/LAN runtime;
- offline authorization staleness is explicitly bounded/detectable;
- security epoch/revocation reconciliation is deterministic after reconnect;
- unresolved privileged/security operations fail closed.

## D — Authorization/account administration

| Scenario | Expected result |
|---|---|
| matching ALLOW | operation allowed inside effective scope |
| matching explicit DENY | DENY wins over ALLOW |
| cluster/module mismatch | 403 |
| expired/revoked grant | no authority |
| SUPERADMIN normal business | all-cluster business authority |
| SUPERADMIN ROOT-security action | denied |
| same-level admin without dedicated permission | denied |
| same-level admin with permission | allowed subject to grantor/self protection |
| recipient attempts to lock/delete grantor | denied |
| close account with history | retained; never hard-deleted |
| LAN authorization snapshot stale/invalid beyond reviewed policy | autonomous command denied rather than silently widening authority |

## E — Cloud canonical mutation/idempotency

| Scenario | Expected result |
|---|---|
| valid command | state + one immutable event + one projection outbox row commit atomically |
| version/state guard fails | 409; no partial state/event/outbox |
| event insert fails | state rolls back |
| outbox insert fails | state + event roll back |
| retry same idempotency key/same command | original canonical event returned; no second mutation |
| reuse key/different command | 409 `IDEMPOTENCY_KEY_REUSED` |
| concurrent same key | exactly one committed canonical event |
| duplicate device sequence/different command | conflict; no partial writes |
| raw event UPDATE/DELETE | rejected by reviewed immutability mechanism |

## F — LAN runtime / failover / reconciliation

### Edge readiness

| Scenario | Expected result |
|---|---|
| LAN starts with valid synchronized edge snapshot | reports ready scope/version and eligible autonomous modules |
| snapshot missing/incomplete/stale beyond reviewed rule | not falsely reported as fully autonomous-ready |
| Agent/service restart | pending edge events/outbox/staged media survive; new runtime epoch is visible |
| no-admin launch | works as normal user without router/firewall/DNS bypass assumptions |

### Relay mode

| Scenario | Expected result |
|---|---|
| client forced LAN while Cloud reachable | `LAN_RELAY`; same command reaches Cloud with same idempotency/device identity |
| relay Cloud commit succeeds | client sees `LAN_RELAYED_CLOUD_COMMITTED` |
| relay uncertain response then retry | Cloud idempotency prevents duplicate business mutation |
| relay cannot reach Cloud | does not claim Cloud commit; mode transition follows reviewed failover policy |

### Autonomous mode

| Scenario | Expected result |
|---|---|
| Internet/Cloud unavailable + offline-capable valid command | local edge transaction + immutable edge event + sync outbox commit atomically |
| local edge transaction failure | no partial state/event/outbox |
| accepted locally | status is `LAN_ACCEPTED_PENDING_SYNC`, never `CLOUD_COMMITTED` |
| same offline command retried | one edge event only |
| idempotency key reused with different payload | hard conflict |
| `(deviceId, deviceSeq)` reused for different command | hard collision/conflict |
| Google unavailable | no direct Sheets fallback write |

### Recovery/reconciliation

| Scenario | Expected result |
|---|---|
| Cloud returns with non-conflicting edge events | each reconciles to D1 exactly once; original identity retained |
| sync retries after timeout | idempotency prevents duplicate D1 mutation |
| D1 already contains same reconciled command | LAN marks reconciled, no duplicate |
| base/entity version conflicts | `SYNC_CONFLICT`; edge evidence retained |
| conflicting edge event | never silent last-write-wins/drop |
| reconciliation completes | edge snapshot/state refreshed/rebased before normal autonomous-readiness claim |

## G — Attendance/presence vertical slice

The slice is complete only after required Cloud + LAN + Web + APK paths pass.

| Scenario | Expected result |
|---|---|
| employee IN | immutable IN + current presence `IN`; no work session auto-created |
| repeated IN according to business contract | accepted as history where allowed while one current state remains |
| OUT | immutable OUT + current presence `OUT` |
| multiple IN/OUT same business date | history retained in order; one current state |
| MNV active reuse | rejected |
| MNV after prior employee inactive | new `employee_id`; old history preserved |
| QR employee resolution | current active employee identity shown |
| same attendance command offline LAN then recovery | one local edge event -> one reconciled D1 event |

## H — Work session / PICK / PACK / resources vertical slice

| Scenario | Expected result |
|---|---|
| start MAIN while another MAIN OPEN | conflict |
| approved EXTRA | allowed with approval/reason |
| one session contains PICK and PACK | allowed |
| PICK without PDA | rejected |
| PICK with PDA and optional User Pick | allowed |
| change User Pick | old assignment closed; new history/event appended |
| change PDA | does not duplicate User Pick history |
| PACK table selection | valid/available mapped User Pack candidates returned |
| PACK commit | table + one chosen User Pack committed atomically |
| release/reissue rules | enforce same-day locks/reissue contract |
| PDA release/reuse | immediate reuse per contract |
| cross-cluster resource borrow | source ownership + consuming context/approval retained |
| concurrent Cloud claim | exactly one succeeds |
| offline LAN claim later conflicts with Cloud-side claim | explicit `SYNC_CONFLICT`, no silent overwrite |

## I — Labor / dropped-goods vertical slice

| Scenario | Expected result |
|---|---|
| start configured labor type | one OPEN labor item per session |
| second OPEN labor item same session | rejected |
| finish/correct labor | immutable history/correction semantics |
| cross-cluster labor | source/consuming context retained |
| dropped goods manual DO + count | canonical/edge event according to runtime mode |
| dropped goods QR parse | normalized equivalent to manual contract |
| ordinary UI | hides technical idempotency/audit fields |
| offline LAN event recovery | exactly-once reconciliation or explicit conflict |

## J — Documents/media vertical slice

| Scenario | Expected result |
|---|---|
| create DRAFT metadata | allowed |
| Internet unavailable capture | file may be staged durably with hash/metadata; not falsely claimed Drive-durable |
| Drive upload fails | document cannot become FINAL under current rule |
| durable Drive upload/readback PASS | metadata stores Drive identity/hash/state |
| finalize document | FINAL only after durable evidence gate |
| incorrect FINAL | new record/version; prior evidence retained |
| replace employee portrait | previous image deleted per current policy; replacement audit retained |
| LAN-staged file recovery | uploaded once after recovery; hash/readback verified before FINAL |

## K — Google projection/reconciliation

| Scenario | Expected result |
|---|---|
| projection auth absent/disabled | Gateway rejects; no false LIVE state |
| wrong token/protocol/environment/sheet/column/key | rejected |
| valid new item | append/upsert per contract |
| same key replay | no duplicate logical row |
| Google unavailable | D1 command remains committed; outbox retries |
| LAN autonomous event before D1 reconciliation | no direct business Sheet write |
| LAN event reconciled to D1 | normal D1 outbox drives projection |
| repeated projection failure | bounded backoff then DEAD after reviewed limit |
| closed-quarter correction | correction originates in D1 event path |

## L — Website Cloud/LAN E2E

| Scenario | Expected result |
|---|---|
| Cloud login -> authenticated shell | works with permission-aware navigation |
| Cloud business command | Web -> Cloud Service -> D1 |
| user selects/forces LAN while Cloud reachable | Web -> LAN_RELAY -> Cloud with same semantics |
| site Internet unavailable | compatible Web bundle can load locally through reviewed LAN mechanism |
| LAN autonomous command | UI clearly shows pending-sync status |
| reconciliation | UI transitions to Cloud committed or explicit conflict |
| stale Sheet | UI follows Service/runtime state, not Sheets as authority |

## M — APK Cloud/LAN E2E

| Scenario | Expected result |
|---|---|
| authenticated APK | same user/permission/domain semantics as Web |
| normal Cloud business flow | same command/error model as Web |
| forced LAN | uses LAN endpoint without inventing new business meaning |
| Internet/Cloud loss | eligible operation uses LAN autonomous path |
| neither Cloud nor LAN reachable | only eligible client-local command is queued; status explicit |
| QR/scanner input | maps into reviewed domain command rather than direct DB/write shortcut |
| background retry | bounded and preserves command identity |
| APK restart | permitted pending queue survives and resumes deterministically |

## N — Failure/recovery/split-brain acceptance

Required drills:

1. Internet disconnected mid-command.
2. Cloud Worker unavailable while Internet remains available.
3. One client direct-Cloud path fails and is forced to LAN.
4. LAN Service restarts with pending autonomous events.
5. Client retries after uncertain LAN/Cloud response.
6. Cloud and LAN create conflicting resource/version changes during partition.
7. Cloud recovers while LAN has ordered pending events.
8. Google remains unavailable after Cloud recovery.
9. LAN host unavailable while Cloud is healthy.
10. Both Cloud and LAN unavailable -> client-local queue where eligible.

Every case must produce explicit runtime/commit status and preserve evidence.

## O — Pre-STABLE durability / physical gate

Before STABLE promotion can be proposed:

- required V1 vertical slices pass on Cloud;
- required V1 vertical slices pass on LAN relay/autonomous where declared offline-capable;
- Web and APK E2E pass against both runtime paths;
- reconciliation/split-brain tests pass without silent overwrite/drop;
- scheduled D1 snapshots enabled after core business PASS;
- at least one D1 restore test;
- LAN edge pending-event/staged-media recovery test;
- update/rollback tests for LAN Service and APK channel;
- real company laptop/network/PDA no-admin LAN regression;
- quota/retention evidence collected rather than guessed;
- no OPEN/PENDING work included in destructive archive/purge;
- explicit Owner approval for STABLE promotion.
