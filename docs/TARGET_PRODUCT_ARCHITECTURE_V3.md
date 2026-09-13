# TARGET PRODUCT ARCHITECTURE V3

Status: ACTIVE — OWNER CLARIFICATION 2026-09-13
Supersedes product-shape assumptions in V2 where they conflict with this document.
Authority: current Owner instruction + existing business decisions.
Legacy repo/prototype: reference only, never product authority.

## 1. Final product

One VHDCHY platform is delivered as four coordinated components:

1. Website — full browser business/administration client.
2. Android APK — PDA-optimized client using the same business/domain contract as Website.
3. Cloud Service — normal online runtime using Cloudflare Worker + D1.
4. LAN Service — full local Service runtime able to replace the Cloud Service for warehouse operation when required.

Cloud Service and LAN Service must share business meaning, permissions, command identity, event semantics and machine error codes. They may use different persistence/runtime adapters.

## 2. LAN is a real Service substitute

LAN Service is not only discovery/relay/transport. It must be able to:

- authenticate users from the latest synchronized local authority snapshot;
- authorize operations from the synchronized permission model;
- execute normal approved warehouse business modules locally;
- maintain local operational state;
- append immutable local business events;
- retain a durable sync/reconciliation outbox;
- project business output to Google when Internet/Google is available;
- stage Google work locally when Internet/Google is unavailable;
- synchronize missing events/state/metadata to Cloud Service/D1 whenever Cloud becomes reachable;
- serve the compatible local Web bundle for browser operation without public Internet.

## 3. Runtime modes

### CLOUD_DIRECT

`Web/APK -> Cloud Service -> D1 -> Google projection/Drive`

This is the normal route.

### LAN_PRIMARY_ONLINE

Client is using LAN Service while Internet is available. LAN accepts the business operation locally under the shared domain contract.

If Cloud Service is reachable, LAN should synchronize accepted events to Cloud/D1 continuously/opportunistically; it must not wait for the client to switch back to Cloud.

If Google is reachable, LAN may also project to Sheets and upload Drive media directly using the controlled Google integration.

This mode covers manually forced-LAN routing and direct-client Cloud path problems.

### LAN_PRIMARY_CLOUD_UNAVAILABLE

Internet is available but Cloud Service is unavailable/degraded.

LAN continues normal local business execution. If Google remains reachable, LAN writes projection/media directly to Google. Cloud/D1 synchronization remains pending until Cloud recovers.

### LAN_OFFLINE

Public Internet is unavailable but local Wi-Fi/LAN remains usable.

LAN continues normal local business execution against the edge database/event journal. Google work is queued/staged locally. Authentication/authorization uses the latest synchronized local authority snapshot.

### LOCAL_QUEUE_ONLY

If neither Cloud nor LAN is reachable from a client, only commands explicitly designed for client-local queuing may be retained locally until a Service becomes available.

## 4. Authority model

D1 is the central consolidated structured store after Cloud synchronization.

During active LAN operation, the LAN edge database + immutable event journal is the local operational authority for events it has accepted. These events are not transport receipts and must not be discarded merely because D1 was temporarily unavailable.

When Cloud returns, LAN synchronizes its event journal/outbox to Cloud Service. Reconciliation is idempotent and exactly-once by stable event/command identity where no conflict exists.

Google Sheets/Drive are never used to reconstruct business truth. They can contain outputs already produced by LAN, but later Cloud synchronization consumes the LAN event journal and attaches Google projection/upload receipts only to avoid duplicate work.

## 5. Direct Google behavior from LAN

When Internet/Google is reachable, an authorized LAN Service may:

- write/update the quarterly Sheets projection through the controlled Gateway contract;
- upload media/documents to the reviewed Drive structure;
- retain projection keys, event IDs, logical file IDs, Drive file IDs, hashes/checksums and upload/projection status locally.

When the same LAN events later reach Cloud/D1, Cloud must recognize already-completed Google work using those stable IDs/receipts rather than duplicate the row/file.

If Google is unavailable, the LAN event remains valid locally and Google work retries later.

Sheets remains projection/reconciliation/DR; Drive remains file/media storage. Neither becomes the business event source.

## 6. Offline authentication and permissions

Owner requirement: offline operation does not expire merely because the site remains offline for a long time.

LAN Service therefore maintains a protected synchronized local authentication/authorization snapshot sufficient for users to log in and perform business operations according to the latest authority known to the LAN Service.

Important consequence: during a true partition, LAN cannot know about Cloud-side account disable, password change or permission revocation that happened after the last successful synchronization. This is an unavoidable consistency/security tradeoff of unlimited offline login and must be visible in audit evidence.

Every locally accepted event must retain the authority/snapshot version under which it was accepted.

When Cloud connectivity returns:

- LAN refreshes user/account/permission/security/configuration data;
- newly synchronized authority applies to subsequent operations immediately after refresh;
- already accepted offline events are not silently deleted or rewritten retroactively;
- they are reconciled with their original acceptance evidence and may become accepted, conflicted or escalated according to the business/security rule.

Exact secure local credential-verification storage/format is an implementation-security decision and must not expose raw credentials in source, logs or diagnostics.

## 7. Manual LAN activation while Cloud is online

Only SUPERADMIN and ROOT may deliberately force/activate LAN routing while Cloud Service is reachable.

The action must be authenticated and audited with scope/reason/time.

Forcing clients through LAN does not mean Cloud synchronization should stop. When Cloud is reachable, LAN should continue synchronizing accepted events in the background unless a future separately approved isolation mode explicitly says otherwise.

## 8. Error and conflict handling

Do not send every technical error to an administrator.

The system automatically handles:

- transient network/provider failures;
- retries/backoff;
- duplicate/idempotent replay;
- already-projected/already-uploaded Google outputs;
- deterministic non-conflicting reconciliation.

If a business/data conflict cannot be safely auto-resolved, it enters an evidence-preserving conflict queue for ADMIN or higher to decide.

Security/recovery conflicts that belong to the ROOT-exclusive security boundary remain ROOT-controlled rather than ordinary ADMIN conflict handling.

## 9. Recovery ordering

When Cloud returns after LAN operation:

1. establish the Cloud sync session/cursor;
2. refresh critical authority/security state for future operations;
3. compare Cloud business versions/deltas with the LAN pending event base versions;
4. reconcile LAN events in stable order where ordering matters;
5. commit non-conflicting events exactly once;
6. retain conflicts with full evidence;
7. attach existing Sheets/Drive receipts instead of re-projecting/re-uploading duplicates;
8. refresh the LAN operational snapshot from the reconciled central state;
9. continue normal synchronization even if users remain routed through LAN.

A route change from LAN back to Cloud is not a prerequisite for data synchronization.

## 10. Split-brain limitation

If LAN is accepting local writes while disconnected from Cloud, other Cloud users may create conflicting changes that the LAN cannot see.

No design can provide both unlimited independent offline writes and perfect global single-writer consistency during that partition.

Therefore the required behavior is:

- stable immutable event identity;
- base/entity version evidence;
- explicit conflict detection;
- no silent last-write-wins;
- no silent event deletion;
- ADMIN+ decision for unresolved business conflicts.

## 11. Client behavior

Website and APK use the same domain/API semantics.

APK is a compact PDA business client, not a diagnostics application. Website is the broader management/administration surface.

Both clients must display meaningful runtime/commit status so users can distinguish, where necessary:

- accepted/committed centrally;
- accepted by LAN and pending Cloud sync;
- Google output completed/pending;
- unresolved sync conflict.

## 12. Legacy reuse boundary

The legacy repository and the earlier transport-only prototype are used only to extract reviewed low-level patterns such as:

- no-admin portable Windows operation;
- discovery/reacquisition;
- anti-flap state handling;
- durable queues/device sequence;
- epoch/sequence realtime recovery;
- diagnostics/load testing/update/rollback ideas.

Do not inherit legacy business schema, UI, cleartext business endpoints, identity strings as authentication, or product assumptions.

## 13. Product completion definition

BETA is not complete until all required business vertical slices work consistently through:

- Website -> Cloud Service;
- APK -> Cloud Service;
- Website -> LAN Service;
- APK -> LAN Service;
- LAN with Internet + Cloud reachable;
- LAN with Internet + Cloud unavailable but Google reachable;
- LAN with no Internet;
- LAN -> Cloud reconciliation;
- direct LAN Google projection/upload deduplication;
- conflict/admin resolution;
- final no-admin company-network/PDA physical regression.
