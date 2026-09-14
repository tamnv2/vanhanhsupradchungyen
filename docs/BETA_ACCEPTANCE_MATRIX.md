# BETA ACCEPTANCE MATRIX — PRODUCT V3/V7

Status: ACTIVE TEST CONTRACT
Updated: 2026-09-14
Authority: `docs/TARGET_PRODUCT_ARCHITECTURE_V3.md`, `docs/DELIVERY_PLAN_V5.md`, `DECISIONS.md` + V3..V7.

Online Web, LAN Web, Android/PDA App, Cloud Service, LAN Service, Google output and reconciliation are all inside product acceptance scope.

## Evidence rule

A PASS claim requires reproducible evidence: CI, provider readback, D1/LAN DB query, Google readback, Web/App E2E log, reconciliation evidence or physical test record as applicable.

Compilation alone is never business/runtime PASS. Legacy/prototype evidence is reference only. CI/source PASS is not physical company-network/PDA PASS.

## Gate order

1. Provider/runtime identity
2. Shared domain/API parity
3. Cloud authentication/authorization
4. LAN synchronized authority/offline login
5. Cloud mutation/idempotency
6. LAN local mutation/event/sync
7. Direct LAN Google projection/upload
8. Reconciliation/conflict handling
9. Attendance/presence
10. Work session + PICK/PACK/resources
11. Labor/dropped-goods
12. Documents/media
13. Online Web + LAN Web E2E/V7 UI parity
14. Android/PDA App Cloud/LAN E2E/V7 UI
15. Failure/recovery/>=60-minute offline continuity acceptance
16. Backup/update/restore + physical company-network/capacity gate
17. Owner UAT and exact BETA release acceptance

## A — Provider/runtime identity

| Scenario | Expected result |
|---|---|
| Cloud health | BETA Worker/D1 identity and `business_core_v3` match reviewed resources |
| Cloud Google degraded | Cloud structured authority remains explicit; no false all-green status |
| LAN health | exact BETA LAN build/edge schema/instance/epoch; no secret leakage |
| environment isolation | BETA/STABLE Cloud, Google, LAN state and signing do not cross |

## B — Shared domain/API parity

| Scenario | Expected result |
|---|---|
| same valid business vector on Cloud | expected transition/event/result |
| same vector on LAN | same business meaning/result except runtime/sync status |
| invalid business vector | same stable validation/business error semantics |
| same command retried across paths | same idempotency/device identity retained |
| provider-specific adapter | cannot redefine business rules |
| Web/App compatibility | both consume the reviewed shared contract |

## C — Cloud authentication/authorization

| Scenario | Expected result |
|---|---|
| valid account | Cloud session/auth context established |
| invalid/disabled/locked account | denied without account-information leakage |
| permission ALLOW | operation allowed inside scope |
| explicit DENY | DENY wins |
| cluster/module mismatch | denied |
| SUPERADMIN ordinary business | allowed per current hierarchy |
| SUPERADMIN ROOT-security action | denied |
| ROOT security boundary | current ROOT-only rules preserved |
| V6 one-time login/recovery cases | exact current V6 factor, single-use, cooldown/lifetime and restricted-state semantics are enforced |

## D — LAN synchronized authority / offline login

| Scenario | Expected result |
|---|---|
| LAN has synchronized authority snapshot | user can authenticate locally according to that snapshot and current implemented security capability |
| long Internet outage | login does not fail merely because offline duration is long |
| user permission valid in local snapshot | allowed business operation executes locally |
| account/permission changes remotely during outage | LAN cannot claim knowledge before reconnect; event records authority snapshot/version used |
| reconnect authority refresh | refreshed authority/config applies to subsequent operations |
| prior offline accepted event after refresh | not silently deleted/re-written; reconciles with evidence |
| manual LAN activation while Cloud healthy by ordinary user/admin | denied |
| manual LAN activation by SUPERADMIN/ROOT | allowed with audited scope/reason/time |
| auth/pairing/security epoch unavailable | affected public mutation remains fail-closed |

## E — Cloud mutation/idempotency

| Scenario | Expected result |
|---|---|
| valid command | D1 state + immutable event + required outbox work commit atomically |
| version/state guard fails | conflict; no partial write |
| retry same command identity | original result/event returned; no duplicate mutation |
| same key/different payload | conflict |
| duplicate device sequence/different command | collision/conflict |
| raw history overwrite | rejected by current immutable-history design |

## F — LAN local mutation/event/sync

| Scenario | Expected result |
|---|---|
| LAN valid command | edge current state + immutable edge event + sync work commit atomically |
| local mutation failure | no partial state/event/sync record |
| local accepted command | explicit LAN-accepted/pending-sync state until Cloud confirms |
| same local command retried | one edge event only |
| LAN restart | edge state, pending events and staged Google work survive |
| Cloud reachable while users stay on LAN | LAN begins/continues background Cloud sync without requiring route switch |
| Cloud unavailable | approved local business operation continues |
| active MNV/code race | one valid winner; loser rolls back without partial state/event/outbox |
| actor evidence | authenticated actor context is captured immutably without becoming client-authoritative payload |

## G — Direct LAN Google projection/upload

| Scenario | Expected result |
|---|---|
| LAN active + Google reachable | authorized LAN path may project/update Sheets through controlled contract |
| same LAN event projected twice | stable projection key prevents duplicate logical row |
| LAN media upload + Drive reachable | file uploaded once; local logical-file ID, Drive file ID and hash receipt retained |
| same media retried | receipt/hash prevents duplicate logical upload |
| Google unavailable | approved Google work queues/stages; no false provider-success claim |
| Google succeeds before Cloud sync | later D1 reconciliation attaches existing receipt and does not duplicate row/file |
| Sheets/Drive queried as business source | prohibited; LAN event journal remains sync source |

## H — Reconciliation/conflict handling

| Scenario | Expected result |
|---|---|
| Cloud returns with non-conflicting LAN events | each reaches D1 exactly once with original stable identity |
| sync timeout/retry | Cloud idempotency prevents duplicate mutation |
| D1 already has same logical event | LAN marks synchronized; no duplicate |
| Google output already exists from LAN | Cloud records/accepts receipt; no duplicate projection/upload |
| base/entity version conflict | explicit conflict state with full evidence |
| business conflict auto-resolvable | deterministic automatic resolution only according to reviewed rule |
| unresolved business/data conflict | ADMIN+ decision required |
| transient provider/network error | automatic retry/backoff, not human escalation |
| ROOT-security conflict | remains under ROOT-exclusive handling |
| reconciliation completes | LAN snapshot/state rebased/refreshed |

## I — Attendance/presence vertical slice

| Scenario | Expected result |
|---|---|
| employee IN | immutable IN + current presence `IN`; no automatic work session |
| repeated IN | follows current business rule while one current presence state remains |
| OUT | immutable OUT + current presence `OUT` |
| MNV lifecycle | active reuse rejected; inactive-history retained on later reuse |
| QR employee resolution | current active identity shown |
| same attendance operation on LAN offline | local event accepted under same business semantics |
| later Cloud sync | one D1 event or explicit conflict |
| Google reachable during LAN | projection can appear before D1 sync using stable event/projection identity |

## J — Work session / PICK / PACK / resources

| Scenario | Expected result |
|---|---|
| MAIN conflict | second main session rejected unless approved extra rule applies |
| PICK without PDA | rejected |
| PICK with PDA/User Pick | follows current contract |
| PACK table/User Pack | candidate and atomic assignment rules preserved |
| change assignment | prior history retained; new event appended |
| release/reissue | current same-day rules enforced |
| cross-cluster borrow | ownership/context/approval retained |
| Cloud/LAN independent conflicting resource claim | explicit conflict; never silent overwrite |
| unresolved resource conflict | ADMIN+ resolver receives evidence |

## K — Labor / dropped goods

| Scenario | Expected result |
|---|---|
| labor open/finish/correction | same semantics on Cloud and LAN |
| cross-cluster labor | context retained |
| dropped goods manual/QR | same normalized business event |
| LAN offline then sync | exactly-once D1 ingestion or explicit conflict |
| Google reachable from LAN | direct projection dedupes correctly |

## L — Documents/media

| Scenario | Expected result |
|---|---|
| create DRAFT | allowed |
| LAN + Drive reachable | direct upload/readback/hash receipt retained locally |
| LAN offline | file staged durably with stable logical ID/hash when that business mutation permits staging |
| Drive unavailable | no false durable-upload claim |
| restart with staged media | identity/path/hash/state remain recoverable |
| duplicate/hash identity | deterministic duplicate behavior; no uncontrolled second logical upload |
| later upload | staged file uploaded once and receipt attached to Cloud/D1 on reconciliation |
| FINAL gate | follows current durable-file rule |
| portrait replacement while Drive unavailable | remains fail-closed until Owner resolves immediate-delete vs offline-staging semantic conflict |

## M — Online Web + LAN Web E2E / V7 UI

| Scenario | Expected result |
|---|---|
| Cloud path | normal authenticated business flow works |
| forced LAN by SUPERADMIN/ROOT | client uses LAN while LAN may continue background Cloud sync |
| Cloud unavailable, Internet available | Web uses LAN; LAN continues approved business + Google where available |
| full Internet loss | compatible LAN Web bundle loads locally and uses LAN Service |
| LAN accepted pending Cloud | UI shows local/sync state clearly |
| conflict | ADMIN+ receives actionable conflict UI |
| V7 shared design | Online/LAN use the same VHDCHY design system/navigation hierarchy in the approved DNSHE-inspired direction |
| branding | no DNSHE brand/proprietary asset copying |
| LAN critical assets | login/shell/business continuity UI does not require Internet-only font/icon/script/style/image assets |
| language | current UI is Vietnamese only; multilingual acceptance is deferred to a later feature phase |
| responsive target | supported desktop/smaller-screen layouts remain readable/operable |

## N — Android/PDA App E2E / V7 UI

| Scenario | Expected result |
|---|---|
| Cloud path | same domain/permission semantics as Web |
| LAN path | same business command semantics through local Service |
| offline LAN | normal approved PDA workflow continues under local authority snapshot |
| scanner/QR | produces domain command, not direct DB shortcut |
| app restart | permitted local pending work survives deterministically |
| sync/Google state | user can distinguish pending/confirmed/conflict states where relevant |
| V7 visual/interaction direction | uses actual Pick Pack 1291 UI/UX reference where reviewed, adapted to VHDCHY rather than copying legacy business logic |
| language | current App UI is Vietnamese only; multilingual acceptance is deferred |
| real PDA | NLS-MT90 layout/scanner/performance/background behavior accepted physically |

## O — Failure/recovery / canonical >=60-minute continuity

Required drills:

1. Warm up the current system in connected state.
2. Cut public Internet while preserving valid LAN connectivity.
3. Keep approved App + LAN Web + LAN Service business operation usable for **Window 2 >=60 minutes**.
4. External Internet embeds/provider content may fail during the timed window without failing local continuity.
5. Restore Internet after the timed window and separately verify Cloud/Google reconciliation/retry without duplicate logical output.
6. Separately test Cloud Service unavailable while Internet/Google remains usable.
7. Separately test SUPERADMIN/ROOT forced LAN while Cloud remains healthy.
8. Separately test users staying on LAN after Cloud recovers while background synchronization resumes.
9. Separately test remote account/permission/config changes during outage and prospective refresh behavior after reconnect.
10. Separately test LAN Service restart with pending events/media/Google work.
11. Separately test duplicate/uncertain response retries and partition conflicts.
12. Separately test both Cloud and LAN unavailable -> client-local queue only where explicitly permitted.

Host restart/power loss is not part of the 60-minute continuity timer; it is a separate recovery gate. Every case must preserve immutable evidence and explicit runtime/sync status.

## P — Pre-STABLE durability / physical / capacity gate

Before STABLE promotion can be proposed:

- required business slices pass through Cloud and LAN;
- Online Web/LAN Web/App E2E pass against both runtime paths;
- V7 UI current Vietnamese-only behavior passes Owner UAT;
- >=60-minute LAN continuity acceptance passes on current target environment;
- LAN -> Cloud reconciliation and Google receipt deduplication pass;
- conflict/admin-resolution acceptance passes;
- D1 snapshot/restore tested;
- LAN edge pending-event/staged-media recovery tested;
- LAN host/App update/rollback channels tested;
- real company ordinary-user laptop/network/NLS-MT90 no-admin regression passes;
- synthetic 10/25/50/100 capacity plus soak evidence collected;
- quota/retention evidence collected;
- exact accepted BETA release/artifacts are identified;
- explicit Owner approval for STABLE promotion is recorded.

STABLE must promote the exact accepted BETA release and must not inherit BETA business/runtime data by default.