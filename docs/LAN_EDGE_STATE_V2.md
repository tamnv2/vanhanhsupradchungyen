# LAN EDGE STATE V2 — FULL LOCAL SERVICE

Status: ACTIVE DESIGN / IMPLEMENTATION CONTRACT
Updated: 2026-09-13
Authority: V3/V4/V5 decisions and `docs/SERVICE_API_CONTRACT_V3.md`.

This supersedes V1 wording where V1 treated Google output as deferred until after Cloud reconciliation or lacked synchronized local authority state.

## 1. Purpose

LAN Service is a full local runtime for approved warehouse operations. It is not only relay/discovery.

Its local store must survive ordinary restart/update and preserve accepted work while Cloud/Internet is unavailable.

## 2. Logical state groups

### `edge_meta`
Tracks:
- environment/cluster;
- edge instance ID;
- edge epoch/generation;
- local schema/domain compatibility version;
- readiness state;
- last successful Cloud sync cursor/time;
- last verified operational snapshot generation.

### `authority_snapshots`
Protected synchronized account/permission/security/configuration authority for local login/authorization.

Each generation records:
- authority generation/version;
- scope;
- source canonical checkpoint;
- imported/verified time;
- compatibility version;
- status.

Business events accepted locally reference the authority generation used.

### `operational_snapshot_state`
Tracks Cloud-derived operational baseline/delta import for enabled local modules.

Partial/corrupt refresh must not replace the last known verified ready snapshot.

### module current-state records
Only the operational state required by enabled offline modules is copied/maintained locally. Do not mirror every Cloud table just because it exists.

Initial Pick Pack needs current employee/MNV status, shift/config, presence/session/task state, resource/mapping/daily-use state, labor state and other slice-specific state required to validate commands locally.

### `edge_events`
Immutable locally accepted business events.

Required evidence includes stable event/request/idempotency/device identity, edge instance/epoch, command/entity/base version, normalized payload/hash, local acceptance time/order, resulting local version/state evidence, authority snapshot version and reconciliation status.

### `cloud_sync_outbox`
One durable reconciliation work item per LAN accepted event where Cloud synchronization is required.

Logical states include pending, synchronizing, retry-wait, reconciled, conflict and review-required.

### `google_projection_outbox`
Pending controlled Sheets projection work when Google output is required.

### `drive_upload_outbox` / staged media
Pending Drive upload work, local logical-file identity, content hash, type/size, durable local path/state and eventual provider receipt.

### `integration_receipts`
Completed downstream work:
- stable event/logical-file key;
- target kind;
- provider object/file identity where applicable;
- hash/checkpoint/readback evidence;
- producer edge instance/epoch;
- completion time/status.

Receipts are sent with Cloud reconciliation so Cloud avoids duplicate logical output.

### `edge_conflicts`
Preserves unresolved edge-vs-canonical evidence, conflict code/context, resolver state and eventual canonical correction/resolution reference.

Original accepted edge events remain immutable.

## 3. Readiness states

Logical readiness should distinguish at least:
- `EDGE_EMPTY` — no usable operational baseline;
- `EDGE_SYNCING` — refresh in progress;
- `EDGE_READY` — required local state for declared modules is verified/compatible;
- `EDGE_DEGRADED` — a non-critical dependency is unavailable but local business capability/evidence is explicit;
- `EDGE_CONFLICTED` — unresolved condition blocks safe autonomous claims for affected scope/module;
- `EDGE_INCOMPATIBLE` — schema/domain version cannot safely execute current commands.

Offline duration by itself does not create an auth expiry state. Staleness must be described by source checkpoint/time/evidence, not silently turned into a login timeout.

## 4. Snapshot and delta refresh

While Cloud is reachable, LAN keeps reviewed authority/configuration/operational state synchronized.

Rules:
- synchronize through reviewed Service APIs/contracts, not direct Cloud provider administration;
- version every snapshot/delta generation;
- import atomically per reviewed scope or via staging/swap so partial refresh is not reported READY;
- resume/retry deterministically;
- verify domain/schema compatibility;
- after reconciliation, rebase/refresh affected local state before claiming fully current state.

## 5. LAN business transaction

For one accepted local business command, one durable unit contains:
- guarded local current-state transition;
- exactly one immutable edge event for the logical mutation;
- Cloud sync work;
- Google projection/upload work where applicable.

If the unit fails, do not report `LAN_ACCEPTED_PENDING_SYNC`.

## 6. Idempotency

- same idempotency identity + same normalized command returns the original acceptance/result;
- same idempotency identity + different logical command/payload is conflict;
- same device sequence + different command is collision/conflict;
- retries never append a second edge event for the same logical command.

## 7. Cloud synchronization

LAN attempts Cloud synchronization whenever Cloud is reachable, regardless of whether clients remain routed through LAN.

For each pending event:
1. preserve original command/event identity;
2. submit the reconciliation envelope;
3. accept existing canonical linkage for a true replay;
4. otherwise let Cloud validate/commit through canonical rules;
5. attach Google receipts already completed locally;
6. mark reconciled only after authoritative Cloud result;
7. retain explicit conflict evidence if canonical guards fail.

A conflict in one dependency chain must not silently allow later dependent commands to overtake it. Independent work may continue only when domain dependency analysis proves safety.

## 8. Direct Google behavior

If Internet/Google is available while LAN is active, LAN may use the controlled Gateway/Drive contract immediately.

- Sheets projection uses stable event/projection identity.
- Drive upload uses stable logical-file identity + hash.
- completed receipts are durable locally.
- later Cloud sync recognizes the same logical output and does not create a duplicate.

If Google is unavailable, business acceptance remains valid locally and Google work stays queued/staged.

## 9. Offline auth consequence

LAN authenticates/authorizes from its latest verified synchronized authority snapshot. It cannot know remote revocation/password/permission changes during a true partition.

On reconnect, refreshed authority applies to subsequent operations. Previously accepted events keep the original authority generation evidence and reconcile explicitly.

ROOT factor-combination/OTP-lifetime details remain blocked by `DECISIONS_V5.md` and must not be invented in LAN implementation.

## 10. Manual LAN activation

Only SUPERADMIN/ROOT may deliberately force LAN while Cloud is healthy. Record actor, reason, scope, start time and relevant runtime state.

Forced LAN does not disable Cloud synchronization when Cloud is reachable.

## 11. Local Web

LAN Service serves/hosts a compatible reviewed Website build so browser operation can continue without public Internet.

The local bundle must remain version-compatible with the LAN Service/domain contract and expose runtime/sync state clearly.

## 12. Host constraints

LAN host stays portable/no-admin and does not depend on modifying corporate firewall/router/AP/route table/internal DNS/certificate policy. Canonical LAN domain behavior is a required physical acceptance gate; fallback IP/discovery is diagnostic/recovery, not the intended final UX.

## 13. Retention/recovery

Never delete:
- unreconciled edge events;
- pending sync/output work required for accepted events;
- unresolved conflict evidence;
- staged media before durable target receipt/readback passes.

Compaction of reconciled history is future measured policy and must preserve audit/recovery requirements.

## 14. Acceptance minimum

Before a module is declared LAN-capable:
- valid authority + operational snapshot reaches READY;
- partial/corrupt refresh cannot become READY;
- local state+event+sync transaction is atomic;
- restart preserves accepted pending work;
- idempotent retry does not duplicate edge event;
- direct Google retry does not duplicate logical output;
- Cloud sync while clients remain on LAN works;
- non-conflicting event reconciles once;
- conflicting event becomes explicit conflict evidence;
- post-reconciliation rebase completes;
- diagnostics do not expose protected credential material.
