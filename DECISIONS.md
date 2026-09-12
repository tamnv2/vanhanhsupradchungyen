# DECISIONS

Status: ACTIVE / CLEAN BASELINE 2026-09-12

## D-001 — Public repository
Repository remains PUBLIC. Sensitive credential material never enters source history.

## D-002 — Environment isolation
BETA and STABLE use isolated runtime resources, Drive roots, D1, GAS projects and signing material.

## D-003 — Service authority
Cloudflare Worker is the public service layer. D1 is canonical structured authority. Google Sheets is projection/reconciliation/DR, never a parallel canonical writer.

## D-004 — Event integrity
Canonical mutation uses immutable events, idempotency, device sequence/entity version where applicable. Corrections are new events, not raw event rewrites.

## D-005 — VHDCHY scope
VHDCHY covers DC Hưng Yên. `PICK_PACK_1291` is the first cluster/module, not the global business-type universe.

## D-006 — Current identity split
Google Drive/Sheets/GAS: `tam95.supra@gmail.com`. Cloudflare/GitHub: `nguyenvantam050595@gmail.com`. `automation@supra.cc.cd` is not current authority.

## D-007 — Drive contract
Project root contains `01_BETA` and `02_STABLE`. Each environment uses `00_SHARED`, `01_CLUSTERS`, `02_MEDIA`, `03_ARCHIVE`, `04_BACKUP`, `05_LOG`, `06_EXPORT`, `07_SYSTEM`.

## D-008 — LAN model
LAN must work without admin/router/firewall/internal-DNS dependency. Existing real-device evidence is exactly two Newland MT90. Synthetic clients prove service headroom only.

## D-009 — Execution model
Build the dependency graph first. Execute independent lanes in parallel when possible. Current off-site condition pauses physical LAN work only; Service continues.

## D-010 — Repository reset
Pre-reset repository state is historical evidence only. Active source is restored after review. Old branches/tags/deploy claims do not become current authority by inheritance.

## D-011 — Worker/D1 reconciliation gate
Do not restore/deploy the pre-reset Worker/D1 stack until schema-version inconsistency is resolved. Pre-reset Worker/deploy gate expected `business_core_v1`, while later migrations wrote `business_core_v2`.

## D-012 — STABLE gate
STABLE setup/promotion requires BETA PASS and explicit Owner approval.
