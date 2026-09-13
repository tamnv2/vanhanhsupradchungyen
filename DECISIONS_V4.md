# DECISIONS V4 — OWNER OVERRIDES

Status: ACTIVE / OWNER CLARIFIED 2026-09-13

This file is an additive authority layer over `DECISIONS.md` and `DECISIONS_V3.md`. Where an older decision conflicts with this file, V4 wins. Unaffected older decisions remain active.

## V4-001 — LAN host remains portable/no-admin on the company laptop

The LAN Service host must preserve the company-laptop constraint proven feasible by the legacy pilot: normal-user execution, portable/user-space deployment, user-writable runtime/state storage, no Administrator requirement, and no dependency on changing company firewall, router/AP, route table, internal DNS, certificate store or security policy.

Do not bypass EDR/AppLocker/device-management restrictions. User-level autostart may be used only where company policy permits it.

The legacy Agent implementation is reference only; the new LAN Service may be structurally different but must preserve these deployment constraints unless the Owner explicitly changes them.

## V4-002 — Canonical LAN domains

Canonical LAN web/service names are:
- BETA: `lan-beta.supra.cc.cd`;
- STABLE: `lan.supra.cc.cd`.

The LAN-hosted Website should present substantially the same compatible business UI as the online Website, with runtime/sync indicators where required.

## V4-003 — Offline LAN domain resolution is a required acceptance gate, not an assumed capability

The product target is that the canonical LAN domain remains usable while public Internet is unavailable and resolves to the active LAN host, so ordinary users do not need to type a LAN IP.

Public DNS alone does not prove this requirement. A reviewed user-mode/name-resolution design compatible with the no-admin/company-policy constraints must be implemented and physically tested.

LAN IP/discovery remains a recovery/diagnostic fallback, not the intended normal user-facing URL.

## V4-004 — Prepare STABLE infrastructure during development, but keep it dormant/fail-closed

BETA and STABLE remain strictly isolated environments. STABLE infrastructure/configuration should be prepared during development so an accepted release can be promoted without rebuilding the environment from scratch.

Before Owner approval, STABLE may be provisioned/configured/verified but must remain fail-closed for business traffic and must not receive BETA runtime data.

The Owner gate applies to activating/promoting a release into STABLE, not to safe preparation of isolated STABLE infrastructure.

## V4-005 — BETA to STABLE promotes the exact tested release

A STABLE promotion uses the exact BETA release/build accepted by the Owner and is tied to immutable source/build identity such as a reviewed commit, tag or release hash.

If `main` has advanced after the accepted BETA build, STABLE still receives the accepted release rather than the newest code by accident.

Promoted material includes accepted source, business/domain logic, schema and migration definitions, configuration definitions, API contracts, Web build, APK release, LAN Service release, GAS source and compatible release artifacts.

## V4-006 — Promotion never copies BETA business/runtime data into STABLE

Do not clone or rename BETA into STABLE.

Do not copy BETA D1 rows, Sheets business rows, Drive business files, test users/employees, LAN edge DB, pending queues, event journal, logs, test media, runtime secrets or other BETA operational state as the promotion mechanism.

A new STABLE environment is created or updated from the accepted release definitions. An already-running STABLE environment is upgraded in place with reviewed migrations against its own STABLE data.

## V4-007 — BETA and STABLE identities/state remain independent even when source is identical

BETA and STABLE may run byte-identical accepted application source after promotion, but retain independent Worker/runtime identity, database state, GAS/Google configuration, Sheets/Drive roots, LAN local state/queues/logs, domains, secrets/credentials and release channels.

No environment becomes STABLE by renaming BETA resources.
