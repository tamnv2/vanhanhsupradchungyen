# DECISIONS V3 — OWNER OVERRIDES

Status: ACTIVE / OWNER CLARIFIED 2026-09-13

This file is an additive authority layer over `DECISIONS.md`. Where an older decision conflicts with this file, this V3 file wins. Unaffected older decisions remain active.

## V3-001 — LAN is a full local Service authority during LAN operation

LAN Service is not only transport/relay. It executes the same approved business model locally using the shared domain/API contract and persists accepted operations in local edge state + immutable event journal + sync work.

D1 remains the central consolidated structured store after synchronization, but a LAN-accepted event is a real local business fact pending Cloud reconciliation.

## V3-002 — LAN synchronizes Cloud whenever Cloud is reachable

Client routing and data synchronization are separate concerns.

Users may continue using LAN while LAN synchronizes accepted events/state to Cloud/D1 in the background. A user does not need to switch back to the Cloud route before synchronization starts.

## V3-003 — LAN may write Google directly when Internet/Google is reachable

While LAN Service is active, if Internet/Google is reachable it may:
- write the controlled Google Sheets projection;
- upload approved Google Drive media/documents.

This is allowed even when Cloud Service is unavailable.

Google remains downstream, not business authority. Stable event/projection/logical-file IDs and receipts must allow later Cloud reconciliation without duplicate rows/files.

If Internet/Google is unavailable, Google work is queued/staged locally.

This supersedes any older rule that required all LAN Google work to wait until after D1 reconciliation.

## V3-004 — Cloud reconciliation comes from LAN events, not from Google

When LAN synchronizes back to Cloud, Cloud/D1 consumes the LAN immutable event journal/sync outbox and stable metadata.

Sheets/Drive content is never scraped or reconstructed as the business source. Existing Google receipts are attached only to prove already-completed projection/upload work and prevent duplicates.

## V3-005 — Offline login has no duration-only expiry

A LAN that is offline may continue authenticating users according to the latest successfully synchronized local authority snapshot. Offline duration alone does not invalidate the ability to log in and perform business operations allowed by that snapshot.

A disconnected LAN cannot know remote account/permission changes that happened after the last synchronization. This is an explicit accepted tradeoff of long-duration offline continuity and must be auditable.

## V3-006 — Online refresh applies new information prospectively

When connectivity returns, LAN refreshes current account/permission/configuration/operational information and applies the refreshed state to subsequent operations.

Previously accepted offline events are not silently deleted or rewritten merely because newer online state exists. They reconcile with their original acceptance evidence and become accepted, conflicted or escalated according to explicit rules.

## V3-007 — SUPERADMIN/ROOT-only forced LAN while Cloud is healthy

Only SUPERADMIN and ROOT may deliberately activate/force LAN routing while Cloud Service is reachable.

The action must be authenticated and audited with reason/scope/time.

Forced LAN routing does not automatically disable Cloud synchronization.

## V3-008 — Human conflict handling starts after automation

Transient provider/network errors, retries, duplicate replay and deterministic non-conflicting reconciliation are handled automatically.

Only unresolved business/data synchronization conflicts are escalated to ADMIN or higher for decision.

ROOT-security/recovery conflicts remain governed by the existing ROOT-exclusive boundary.

## V3-009 — LAN business scope is not emergency-reduced by default

LAN Service is expected to support the same approved warehouse business modules and permission model required for normal operation, subject to technical feature completion. A feature is not removed from LAN merely because LAN is a fallback runtime; any exception requires an explicit product decision.

## V3-010 — Legacy/prototype source remains NON_AUTHORITY

The legacy repository and the earlier transport-only APK/Agent prototype are reference/evidence only. Reuse is selective after review against V3 contracts. They must not redefine current product behavior.
