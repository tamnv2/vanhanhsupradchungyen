# PICK PACK 1291 — REFERENCE FOR VHDCHY

Status: READ-ONLY REFERENCE
Source reviewed: full recovery backup `BACKUP PICK PACK 1291-20260910T153534Z-1-001.zip` and exact Beta128 APK supplied by Owner on 2026-09-10.

## Purpose

This document prevents future AI sessions from misrepresenting the old Pick Pack 1291 architecture. It is reference material only. It must not be used as a runtime dependency and must not cause any fallback/write to the retired Pick Pack 1291 project.

## Critical chronology warning

The backup contains multiple architectural eras. Do **not** read one old file in isolation and call it the final model.

### Historical early model

The `main` snapshot README states:

`Android App ↔ Google Apps Script ↔ Google Sheets`

At that stage GAS was the authoritative API/transaction bridge and Google Sheets was the operational source of truth.

This is a real historical architecture, but it was later superseded on the active Beta branch.

### M1 Service migration

`docs/SERVICE_MIGRATION_M1.md` introduced a staging/shadow Service path:

`Test Client → Cloudflare Worker API → D1 transaction → Durable Object realtime`

with asynchronous replication:

`D1 transaction → sheet_replication_outbox → Google Sheets staging copy`

M1 deliberately left the production GAS/Sheet authority unchanged while the Service path was validated.

### M2 / later canonical model

The later `beta/current` source, `ARCHITECTURE_GUARDRAILS.md`, `config/environment_contracts.json`, `docs/SERVICE_MIGRATION_M2.md`, `docs/INFRA_CAPACITY_DR_POLICY.md`, and final `CURRENT_STATE.md` converge on the Service-first model:

`Android / Web-PWA ↔ Service Core (Cloudflare Worker) ↔ D1`

with:

- Durable Objects/WebSocket for realtime where needed;
- Android local durable projection/outbox/offline state;
- Google Apps Script for discovery, legacy compatibility bridge, controlled Google fallback, recovery/failback fencing;
- Google Sheets as operational replica/projection, emergency ledger/fallback surface and DR surface while Service is primary;
- Firebase only for FCM wake/invalidation, not Auth/DB/Storage authority;
- provider-neutral business core and canonical event/idempotency/fencing semantics.

The final backed-up `CURRENT_STATE.md` says BETA128 LIVE and:

- `authority: SERVICE_PRIMARY / PRODUCTION / epoch 9 / generation m2-prod-reset-20260823-001`;
- current BETA D1: `pick-pack-1291-service-prod`;
- Stable remained `READY_NOT_LIVE`;
- R5 pre-strong optimization rollback did **not** revert the system to the old GAS/Sheets-primary architecture.

Therefore it is incorrect to describe the retired final Pick Pack 1291 model simply as `PDA/App ↔ GAS ↔ Sheets`.

## Final reference topology

```text
Android PDA / Android app --------\
                                  +--> Service Core / Cloudflare Worker --> D1 canonical ledger + projections
Web / PWA ------------------------/                 |
                                                    +--> Durable Objects / WebSocket realtime
                                                    |
                                                    +--> transactional replication outbox
                                                               |
                                                               v
                                                      Google Sheets projections/replica

Android local Room/outbox/offline state
        |
        +--> replay same immutable event_id after timeout/restart

Google Apps Script
        +--> service discovery
        +--> legacy compatibility bridge
        +--> controlled GOOGLE_FALLBACK
        +--> fallback event ledger + recovery/failback fencing
```

## Writer/authority rules from Pick Pack 1291

- Exactly one official writer at a time.
- Timeout/5xx does not prove a mutation was not committed and does not transfer authority.
- Mutations use immutable event identity/idempotency plus fencing/authority metadata.
- D1 canonical transaction writes the replication outbox before ACK.
- Google replication happens outside the mutation critical path; Google failure does not roll back a durable Service mutation.
- No blind dual-write.
- Failover/failback must be fenced so Service and Google fallback cannot both be legitimate writers at the same time.
- Local durable outbox is written before network send; ACK from authority is required before local pending can be finalized.

## Google Sheets role and schema reference

The VHDCHY read-only schema workbook recovered from Pick Pack 1291 explicitly says it is only for rebuilding/reference and must not be used as old-project runtime/fallback.

Reference tabs include:

- `DANH SÁCH PDA`
- `DANH SÁCH USER PICK`
- `DANH SÁCH BÀN PACK`
- `DANH SÁCH USER PACK`
- `DANH SÁCH NHÂN SỰ`
- `Danh sách Admin`
- `LỊCH SỬ NGHIỆP VỤ`
- `RA - VÀO TRONG CA`
- `THÔNG TIN USER CỦA NLĐ`
- `CÔNG NHẬT`
- `Nhận hàng rớt`
- `LAN AUTHORITY FENCE`
- `EMERGENCY LEDGER YYYYMM`
- `EMERGENCY EVENT INDEX`
- hidden `__PP_M2_FALLBACK_EVENTS`

Examples of important reference semantics:

- personnel/resource sheets are human-readable operational projections;
- business history carries Event ID and revision/authority information;
- attendance and labor sheets are projected from canonical events;
- emergency/fallback ledgers preserve immutable event/idempotency/authority/checksum data;
- LAN authority fence contains epoch/master/backup/lease/generation/checksum state.

## LAN / outage reference

The later Pick Pack 1291 design included a LAN failover model after a sustained Service outage, with one Master + one backup, authority epoch/lease fencing, durable replication before ACK, and controlled replay/failback to Service.

However the final project state also records a later R6 `LAPTOP_SERVICE_FULL_LAN_WEB` requirement as pending/fix-locked. Treat that laptop-service work as an unfinished later scope, not as a proven completed invariant of Beta128 unless exact evidence is separately inspected.

## Release/reference rules

- Beta and Stable used separate package/environment/audience/session/data/OTA mutable state.
- Beta APK exact bytes were distributed through GitHub Release.
- Google Drive APK was recovery backup only, not runtime OTA authority.
- Candidate lock included source SHA, artifact/run, version, APK SHA256/size and signer.
- Stable remained isolated and not live until explicit Owner promotion.

## Exact supplied APK check

The supplied `pick-pack-1291-public-beta-0.4.2-beta.128.apk` SHA256 is:

`04b135c554c6de6aa979b113a3435cec65063c87e79f232d8c8ea28e1d75f4ce`

This matches the backed-up `CURRENT_STATE.md` Beta128 identity. Static APK strings also contain the Beta web origin, GitHub Release API, Google Apps Script endpoint, FCM libraries and WebSocket support, consistent with the final source tree.

## How VHDCHY may use this project as reference

Allowed reference use:

- reuse proven schema ideas and human-readable Google Sheet organization;
- reuse event/idempotency/outbox/fencing/offline/retry patterns;
- reuse Beta/Stable isolation and release-gate patterns;
- reuse QA/receipt/handoff discipline;
- reuse selected business/UI semantics after checking against current VHDCHY Owner decisions.

Forbidden inference:

- do not assume the oldest `main` README is the final Pick Pack 1291 architecture;
- do not copy old account IDs, endpoints, tokens, domains, spreadsheet IDs or retired runtime state;
- do not migrate old business data unless Owner explicitly requests it;
- do not let the retired project become a fallback or authority for VHDCHY;
- do not let Pick Pack 1291 override newer VHDCHY decisions.

## Source precedence when re-checking Pick Pack 1291

For the retired final state, prefer:

1. final `CURRENT_STATE.md` and active `beta/current` contracts;
2. `ARCHITECTURE_GUARDRAILS.md`;
3. `config/environment_contracts.json`;
4. M2 / infra / DR documents and matching source implementation;
5. exact release receipts/APK hashes;
6. historical `main` snapshot only for earlier architecture history.
