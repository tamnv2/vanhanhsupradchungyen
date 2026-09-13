# TARGET PRODUCT ARCHITECTURE V2

Status: ACTIVE — OWNER-LOCKED PRODUCT DIRECTION 2026-09-13
Authority: `DECISIONS.md`, `PROJECT_SCOPE.md`, current Owner instruction
Legacy reference: `tamnv2supra/vanhanhdchungyen@7b4488a89f585812c1bccba5d07d86049482bf4c` — NON_AUTHORITY

## 1. Owner-locked product target

The finished VHDCHY product is one system with four cooperating deliverables:

1. Website — full browser business/administration client.
2. Android APK — PDA-optimized client derived from the same business surface/contract as Web, with a smaller operational UI where appropriate for handheld use.
3. Cloud Service — Cloudflare Worker + D1, the normal online service runtime.
4. LAN Service — local service runtime able to stand in for the Cloud Service when required, while preserving the same business command/event semantics.

Android and LAN are not side pilots and are not deferred extras. They are built in parallel with Web/Cloud Service toward the same final product.

The legacy repository is used only to extract proven ideas, failure evidence, no-admin constraints, discovery/reconnect patterns and test techniques. It must not be copied wholesale and must not override current requirements.

## 2. Required LAN use cases

The LAN path must support all three Owner-defined situations:

### A. Site Internet unavailable

PDA/Web clients on the same local Wi-Fi/LAN must continue approved operational work through the LAN Service even though Cloudflare, D1, Google Sheets and Google Drive are unreachable.

### B. Cloud Service unavailable/degraded

If the site still has Internet but the Cloudflare Service path is unavailable or unusable, clients must be able to use the LAN Service instead of waiting for Cloud Service recovery.

### C. Individual client forced to LAN

A user/device with a direct Cloud-path problem must be able to select/force the LAN path while other parts of the system remain healthy.

For case C, when Cloud Service is reachable from the LAN host, LAN should normally operate as a local front door/relay to the canonical Cloud Service rather than creating unnecessary independent local commits. This minimizes split-brain while still solving the individual client's path problem.

## 3. Client model

```text
                 +---------------- Website ----------------+
                 |                                          |
User / PDA ------+------------------ APK --------------------+
                                    |
                         one domain/API contract
                                    |
                    +---------------+---------------+
                    |                               |
              Cloud path                         LAN path
                    |                               |
          Cloudflare Worker                 Local LAN Service
                    |                       /               \
                   D1             Cloud reachable       Cloud unavailable
                    |                    |                     |
             canonical state       relay to Cloud      local edge execution
                    |                                      + local edge DB
           outbox / Gateway                                + local event journal
                    |                                      + sync outbox
          Sheets / Drive                                   + staged media
                                                            |
                                                 reconcile when Cloud returns
                                                            |
                                                           D1
```

Web and APK differ in presentation and device ergonomics, not in business meaning. Actor, permissions, command identity, validation, error codes, idempotency and event semantics must remain compatible.

## 4. Runtime modes

### `CLOUD_DIRECT`

Normal mode. Client calls Cloud Service directly. D1 commits canonical state/events.

### `LAN_RELAY`

Client calls LAN Service, but LAN Service can reach Cloud Service. LAN Service relays the same authenticated/idempotent command to Cloud Service and returns the authoritative Cloud result. This is the preferred mode for per-user forced-LAN when Cloud is otherwise healthy.

### `LAN_AUTONOMOUS`

Cloud Service/upstream is unavailable. LAN Service executes the reviewed offline-capable business command locally against its edge state, appends an immutable local event and queues reconciliation. The command must keep the same stable identity when later synchronized.

### `LOCAL_QUEUE_ONLY`

Neither Cloud Service nor LAN Service is usable from the client. The client may retain eligible pending commands durably and retry later according to the domain ordering rules.

Transport/routing changes must never silently change business command identity.

## 5. Authority and offline continuity

D1 remains the global canonical structured authority after connectivity/reconciliation is restored.

However, the requirement to continue working during a hard network partition means the LAN Service must be able to accept durable local business events while D1 is unreachable. These are not disposable transport receipts. They are edge-accepted business facts pending canonical reconciliation.

Required consequences:

- LAN Service needs a local operational state store sufficient for the offline-capable modules.
- LAN Service needs an immutable local event journal.
- LAN Service needs a durable sync/reconciliation outbox.
- every offline event includes stable command/event identity, device identity/sequence, edge instance/epoch and the relevant base/entity version so conflicts are detectable;
- reconnect must be idempotent; replay must not create duplicate D1 mutations;
- conflicts must never be silently overwritten or dropped; they go to an explicit reconciliation/conflict state with evidence retained.

### Hard-partition consistency limit

No architecture can guarantee both uninterrupted local writes and perfect global single-writer consistency while the site is completely disconnected from the global authority. The Owner requirement prioritizes continued local operation, so the design must use explicit conflict detection/reconciliation rather than falsely claiming impossible strict consistency.

The system must minimize this risk by using Cloud commits whenever Cloud is reachable and reserving autonomous local commits for true fallback/emergency operation.

## 6. One business engine, two persistence adapters

Cloud Service and LAN Service must not evolve as two independently invented business backends.

Target design:

- one provider-neutral domain command/event core for validation and business transitions;
- Cloud adapter: D1 transaction/event/outbox implementation;
- LAN adapter: local edge transaction/event/sync-outbox implementation;
- identical command schemas, machine error codes and acceptance vectors across both runtimes;
- provider-specific code remains outside the domain core.

Exact packaging/runtime technology for the LAN host is not locked here. Selection must respect the proven no-admin/company-laptop constraint and must be measured for compatibility/footprint. The architecture requirement is behavioral parity and shared/reused domain logic, not a predetermined language.

## 7. LAN edge state

While online, LAN Service must maintain a reviewed local snapshot of the operational state needed for the configured cluster/modules so that a later outage does not begin from an empty database.

Examples of categories that may be required are employee identity/status, active shift definitions, resource configuration/availability, current open work/presence state, module configuration and the minimum authorization state needed for offline operation. The exact dataset is module-specific and must be defined from current business contracts, not guessed from the legacy repository.

Snapshot/delta sync must be versioned and resumable. A stale/incomplete edge snapshot must be detectable and must not be presented as fully ready for autonomous operation.

## 8. Authentication/security continuity

Online authentication/session authority remains Cloud-owned.

Offline autonomous operation requires a bounded offline-auth mechanism or a synchronized verifier/capability model. A network partition necessarily prevents immediate awareness of cloud-side revocations, so the security design must explicitly bound this staleness rather than pretending instant revocation is possible offline.

The following are NOT yet Owner-locked and must not be invented in code:

- exact offline credential/capability mechanism;
- exact offline authorization TTL;
- which security/admin operations, if any, are permitted in autonomous LAN mode;
- who may explicitly enter/exit emergency autonomous mode.

Until these are reviewed, LAN business-auth implementation must remain fail-closed for unresolved privileged cases.

## 9. Google Sheets / Drive during LAN autonomous mode

Google remains downstream of canonical business processing and must not become a second authority just because LAN is active.

When Cloud/Internet is unavailable:

- Sheets projection is deferred; LAN does not directly multi-write business Sheets as an alternate backend;
- projection intent is represented by the business events that will reconcile into D1/outbox later;
- files/images that must be captured may be staged durably with hashes/metadata for later upload;
- current `DRAFT -> FINAL` durable-Drive rule remains in force unless the Owner explicitly changes it; therefore offline capture can be staged without falsely claiming Drive durability.

After D1 reconciliation, normal Cloud outbox/Gateway projection updates Sheets and Drive according to the canonical workflow.

## 10. Web availability under Internet outage

If browser operation is required while the Internet is down, a cloud-hosted page alone is insufficient. The LAN package must therefore be capable of serving the reviewed Web client bundle locally, or the Web client must have an equivalently proven offline-loading mechanism.

The preferred product shape is to let the LAN Service expose the same/current Web build on a local endpoint so a browser on the site can operate against LAN Service without depending on public Internet. Exact hostname/discovery remains constrained by the no-admin/no-internal-DNS rule.

## 11. APK role

The APK is not a LAN diagnostics app and not a separate business implementation.

It is the handheld operational client of the VHDCHY platform:

- same authenticated user/permission model;
- same domain commands and query semantics as Web;
- same Cloud/LAN endpoint-selection state;
- PDA-oriented screens and interaction density;
- scanner/device integrations can be added behind the same business contract;
- durable client queue only where the command contract permits retry/offline behavior.

Which Web functions are included/excluded from the compact APK UI is a module UX decision and must be derived from real PDA workflows, not automatically cloned or removed.

## 12. Failover and split-brain rules

Baseline rules required by the target architecture:

1. Normal path is Cloud Service.
2. User-forced LAN with Cloud reachable uses `LAN_RELAY` by default.
3. `LAN_AUTONOMOUS` exists for real Cloud/upstream loss or an explicitly authorized emergency mode.
4. Returning from autonomous mode does not simply overwrite D1. It runs deterministic idempotent reconciliation.
5. Non-conflicting edge events are committed/reconciled once.
6. Conflicting edge events remain visible and require defined resolver policy/action.
7. Client queue, LAN queue and Cloud idempotency must all preserve the same command identity.
8. No success UI may imply global canonical completion when the event is only accepted locally; status must distinguish local acceptance from cloud reconciliation.

## 13. Status terminology

Use these distinct states in implementation/evidence:

- `CLOUD_COMMITTED` — accepted/reconciled by Cloud Service/D1.
- `LAN_ACCEPTED_PENDING_SYNC` — accepted durably by autonomous LAN, not yet reconciled to D1.
- `LAN_RELAYED_CLOUD_COMMITTED` — client used LAN path but canonical Cloud commit succeeded.
- `QUEUED_CLIENT_LOCAL` — retained only on client, not yet accepted by LAN/Cloud.
- `SYNC_CONFLICT` — LAN event could not be automatically reconciled without violating current canonical/business guards.

Do not collapse these into one generic `SUCCESS` state in diagnostics or audit evidence.

## 14. Legacy reuse boundary

Adopt/adapt only patterns that survive review against this architecture, including:

- no-admin portable Agent/service operation;
- cached endpoint / UDP discovery / manual recovery;
- hysteresis and reconnect measurement;
- durable queue and device sequence;
- epoch/sequence realtime resync;
- diagnostics, load/throughput measurement and safe update concepts.

Do not inherit as authority:

- legacy pilot cleartext business paths;
- legacy pilot service strings as authentication;
- legacy business schema/data;
- legacy UI as current product definition;
- any design that treats LAN only as a transport test when the current requirement is a Cloud-Service substitute runtime.

## 15. Current implementation correction

The transport-only DEV APK/Agent created immediately before this clarification is classified as `DISPOSABLE PROTOTYPE / REFERENCE ONLY`. Its successful build proves only that the current CI can compile/package Android and a no-admin Windows process.

It is not the product architecture and must not be extended as if it were the final APK/LAN Service. Useful low-level mechanics may be selectively reused after they are moved behind the current product contracts.
