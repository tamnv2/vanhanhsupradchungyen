# DECISIONS

Status: ACTIVE / OWNER RECONCILED 2026-09-13

## D-001 — Public repository
Repository remains PUBLIC. Sensitive credential material never enters source history.

## D-002 — Environment isolation
BETA and STABLE use isolated runtime resources, Drive roots, D1, GAS projects and signing material.

## D-003 — Service authority
Cloudflare Worker is the public service layer. D1 is canonical structured authority. Google Sheets is projection/reconciliation/DR, never a parallel canonical writer. Google Drive stores media/documents/archive; D1 stores identifiers, metadata, hashes and state.

## D-004 — Event integrity
Canonical mutation uses immutable events, idempotency, device sequence/entity version where applicable. Corrections are new events, not raw event rewrites. A successful business mutation transaction validates authorization/effective permissions/business rules/version/availability, updates current state, appends an immutable `domain_event`, and enqueues `projection_outbox` atomically. D1 commits before Google projection; Google failure must not roll back canonical business state.

## D-005 — VHDCHY scope
VHDCHY covers DC Hưng Yên. `PICK_PACK_1291` is the first cluster/module, not the global business-type universe.

## D-006 — Current identity split
Google Drive/Sheets/GAS: `tam95.supra@gmail.com`. Cloudflare/GitHub: `nguyenvantam050595@gmail.com`. `automation@supra.cc.cd` is not current authority.

## D-007 — Drive contract
Project root contains `01_BETA` and `02_STABLE`. Each environment uses `00_SHARED`, `01_CLUSTERS`, `02_MEDIA`, `03_ARCHIVE`, `04_BACKUP`, `05_LOG`, `06_EXPORT`, `07_SYSTEM`.

## D-008 — LAN model
LAN must work without admin/router/firewall/internal-DNS dependency. Existing real-device evidence is exactly two Newland MT90. Synthetic clients prove service headroom only. LAN is a transport/authority fallback using the same command/event contract, not a second canonical backend.

## D-009 — Execution model
Build the dependency graph first. Execute independent lanes in parallel when possible. Current off-site condition pauses physical LAN work only; Service continues.

## D-010 — Repository reset
Pre-reset repository state is historical evidence only. Active source is restored after review. Old branches/tags/deploy claims do not become current authority by inheritance.

## D-011 — Worker/D1 reconciliation gate
Never apply a migration merely because resource identity matches. Inspect the exact BETA D1 first. If provider evidence conflicts with source assumptions, stop mutation and reconcile before continuing.

## D-012 — STABLE gate
STABLE setup/promotion requires BETA PASS and explicit Owner approval.

## D-013 — ROOT / SUPERADMIN boundary
SUPERADMIN is equal to ROOT for ordinary business and all-cluster operations, but ROOT retains exclusive authority over ROOT security and recovery policy.

## D-014 — Same-level account administration
Same-level account management is allowed only through a dedicated permission. A recipient may not delete or lock the account that granted that authority.

## D-015 — Account retention
Accounts with business/security history are never hard-deleted. Close/archive/disable them while retaining history.

## D-016 — Employee/account uniqueness
At most one ACTIVE application account exists per employee per environment.

## D-017 — ROOT authentication and recovery
The former HH/mm password mechanism is removed. ROOT uses TOTP. Email recovery OTP is four digits, one-time, visible in the message subject, and a successful use invalidates it and causes the next code to be generated/sent. Recovery email/phone destinations are restricted to a locked Owner-approved allowlist. Exact recovery contacts stay outside this public repository.

## D-018 — Locked security policy
ROOT username, recovery allowlists and minimum protection rules are locked policy. Provider/API keys may be replaced without changing the business identity model.

## D-019 — Employee code lifecycle
MNV may be reused only after the prior employee is inactive/left. A reused MNV always points to a new `employee_id`; history remains attached to the prior identity. Employee QR contains MNV only; scan UI resolves and displays the current active employee name and portrait.

## D-020 — Shift versioning
Changing a shift name or time creates a new shift version/code. Historical shift definitions remain immutable/referenceable for past records.

## D-021 — Attendance and presence
Multiple IN/OUT events are allowed on one business date, while only one current presence state exists at a time. IN records attendance/presence only; a work session starts only after position/resources are selected.

## D-022 — Work-session concurrency
An employee has at most one main ACTIVE work session across the environment. An additional concurrent session requires an explicit reason/approval record.

## D-023 — Labor catalog and records
Authorized users manage labor types. Initial types are `Hỗ trợ Pick`, `Hỗ trợ Pack`, `Kéo hàng`, and `Khác`. Staff deduction defaults to false and is configurable by cluster/type. At most one OPEN labor item exists per session. Cross-cluster labor support is required from V1.

## D-024 — Document lifecycle
Documents use `DRAFT -> FINAL`. An incorrect FINAL is corrected/replaced by a new record/version while prior evidence remains retained. Document/evidence images are retained indefinitely until a later explicit policy changes this.

## D-025 — Employee portrait lifecycle
Replacing an employee portrait deletes the previous image immediately while preserving audit metadata about the replacement.

## D-026 — Media processing
Do not enforce an arbitrary fixed-MB compression cap before real-image testing. Prioritize readability/identification and measure BETA storage/performance before setting limits.

## D-027 — Closed-quarter projections
A CLOSED quarterly Google Sheet is never directly edited for business correction. Correct canonical D1 through an adjustment/correction event, then re-project according to projection policy.

## D-028 — PICK task contract
A work session may contain both PICK and PACK tasks. PICK requires a PDA; User Pick is optional. PICK supports multiple User Pick assignments over time/history.

## D-029 — PACK task contract
Pack Table to User Pack mapping is cluster-configured 1:n and may vary by shift/effective period. For each PACK task, the Service returns currently valid/available User Pack candidates mapped to the selected table; exactly one is chosen and table + chosen user assignment are committed atomically.

## D-030 — Resource release/reissue
After release, User Pick, User Pack and Pack Table become used/locked for that business date and require Reissue before same-day reuse. PDA may be reused immediately. Resource changes close the old assignment and append a new assignment/event; changing PDA must not duplicate User Pick history.

## D-031 — Cross-cluster resources
Cross-cluster resource borrowing is supported from V1. Ownership remains with the source cluster; consuming cluster, approval and usage context are recorded.

## D-032 — Dropped-goods business fields
Visible dropped-goods business fields are business date, DO, package count, actor and update time. Input supports manual DO+count or QR parsing. Technical identifiers/idempotency/audit fields remain hidden from ordinary UI.

## D-033 — Normal password policy
Normal accounts use passwords of at least eight characters, block overly common passwords, allow long passphrases, and do not force periodic changes absent incident. Admin reset produces a temporary password and requires change on next login.

## D-034 — D1 retention/archive policy
D1 hot-retention duration is not fixed yet. BETA measures quota/performance/free-tier use. Never archive OPEN/PENDING work. Purge only after Drive archive readback and checksum PASS.

## D-035 — Snapshot/restore policy
Design snapshot capability now; enable it after core business PASS. Before STABLE, scheduled snapshots and a restore test are mandatory.

## D-036 — Legacy compatibility retirement
Remove any old-app compatibility adapter only after telemetry proves no pending data/devices are using the old schema for a reviewed safe period.

## D-037 — Web/APK contract
Web and later APK clients use one Service/domain contract. Client transport differences do not create separate business authorities.

## D-038 — Google quarterly projection
Use one quarterly workbook per environment + cluster + quarter. Service/Gateway is the writer. Sheets are projection/reconciliation only; corrections originate in D1.

## D-039 — BETA business acceptance scenarios
BETA acceptance must cover login, IN/OUT and repeated IN on one business date, MNV reuse, PICK/PACK dispatch, mixed role/task use, resource changes, multiple User Pick assignments, Reissue, cross-cluster borrowing, labor start/finish/correction, dropped-goods manual/QR entry, document FINAL only after durable upload, and Google-degraded/outbox retry behavior.

## D-040 — Target core schema version
The reconciled Owner-approved D1 target introduced on 2026-09-13 is `business_core_v3`. It supersedes the stale source `business_core_v2` and the currently deployed zero-business-row `business_core_v1`. `0001_initial.sql` is the clean V3 baseline for new environments; existing BETA must use a guarded reconciliation path that first re-verifies the exact D1 identity and zero-business-row condition.

## D-041 — Strict non-stop autonomous execution and automation-enablement escalation
After Owner scope/instruction is established, AI continues execution automatically and may stop only for one of three proven conditions: `OWNER_DECISION_REQUIRED`, `OWNER_PERMISSION_REQUIRED`, or `TOOL_CAPABILITY_LIMIT` after safe alternate connected tools/APIs/hosted CI/indirect AI-executable paths have been exhausted. Ordinary implementation failures, CI failures, transient provider errors, incomplete evidence, uncertainty, unavailable preferred tooling, elapsed execution time, checkpoint creation, or a blocked single lane do not justify stopping. AI isolates/fixes/verifies the affected lane and continues every independent safe lane; a whole-project/session halt is allowed only when one of the three stop conditions applies and no independent safe ready work remains.

When permission/access is missing, AI pauses only the affected mutation and first analyzes how Owner can grant the exact least-privilege capability to AI durably for future automation. Prefer an existing connected integration/API/GitHub App/provider service account or a reviewed fail-closed GitHub-hosted CI bridge with credentials kept in approved secret stores; never ask Owner to paste raw secrets into chat. If direct AI access cannot be granted, AI must independently seek a safe indirect AI-executable path. Only when neither a safe direct grant nor a safe indirect path exists may the task become `OWNER_MANUAL_LAST_RESORT`, with only the minimum unavoidable manual action delegated to Owner. Existing explicit lane gates such as STABLE promotion remain lane-specific and do not halt unrelated work.

## D-042 — Final product topology: Web + APK + Cloud Service + LAN Service
Owner clarified 2026-09-13. The finished product is one VHDCHY platform with Website, Android APK, normal Cloud Service runtime and a LAN Service substitute runtime. Android/LAN are first-class deliverables built in parallel with Cloud/Web; they are not diagnostic side projects or deferred extras.

## D-043 — APK is the PDA-optimized business client
Expands D-037. APK follows the same authenticated domain/API contract and business semantics as Web. It may expose a smaller PDA-appropriate operational UI, but it must not become an independently invented business system or merely a LAN diagnostics application. Which functions are compacted/omitted is decided from current PDA workflows/module requirements, not inherited automatically from the legacy app.

## D-044 — LAN Service must substitute for Cloud Service when required
Expands D-008. LAN must support three Owner-defined cases: (1) site Internet unavailable while local Wi-Fi/LAN remains usable; (2) Cloudflare Service unavailable/degraded; (3) an individual client is forced from direct Cloud access to the LAN path. When Cloud is reachable from LAN, forced-LAN normally uses LAN as a local relay/front door to the canonical Cloud Service. When Cloud/upstream is genuinely unavailable, LAN must support reviewed autonomous local execution for offline-capable business commands, not merely accept transport packets.

## D-045 — Cloud and LAN share one business command/event model
Cloud Service and LAN Service must not develop divergent business rules. The target is a provider-neutral domain core/shared command-event contract with Cloud D1 and LAN edge persistence adapters, plus identical acceptance vectors/error semantics. Provider/runtime packaging may differ; business meaning may not.

## D-046 — Offline LAN events reconcile; conflicts are explicit
D1 remains global canonical authority after recovery, but uninterrupted operation during a hard partition requires durable edge-accepted business events. LAN autonomous acceptance is recorded with stable idempotency/device/event identity and later reconciled to D1 exactly once when possible. Split-brain conflicts are retained and surfaced for deterministic/manual resolution; silent last-write-wins, silent drop or pretending strict global single-writer consistency during a hard partition is prohibited.

## D-047 — Google remains downstream during LAN autonomous operation
LAN Service does not make Google Sheets a fallback database. Sheets/Drive are deferred while Internet/Cloud dependencies are unavailable. Structured LAN events reconcile to D1 first, then normal outbox/Gateway projection runs. Files/images may be staged locally with hashes/metadata for later upload. Current `DRAFT -> FINAL` durable-Drive rule remains unchanged unless Owner explicitly changes it.

## D-048 — Legacy and pre-clarification transport prototypes are NON_AUTHORITY
The legacy repo and the transport-only APK/Agent prototype created immediately before the 2026-09-13 clarification are reference/evidence only. Reuse is selective after review against D-042..D-047. Do not continue implementation merely to reproduce the old pilot or the temporary transport prototype.
