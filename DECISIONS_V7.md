# VẬN HÀNH DC HƯNG YÊN — OWNER DECISIONS V7

Status: ACTIVE
Decision date: 2026-09-14
Authority: explicit Owner instruction in current session
Supersedes: conflicting UI-direction/language details in older decisions; all non-conflicting decisions remain active.

## 1. Android/PDA app UI direction

The VHDCHY Android/PDA application shall reuse the **UI direction of the Owner's Pick Pack 1291 application**, adapted to the current VHDCHY product, workflows, terminology, permissions, data model and technical architecture.

Rules:
- `BACKUP PICK PACK 1291` is a reference source only. This Owner decision explicitly authorizes using it for **UI/UX reference** for the current app; it does not make old Pick Pack business logic, data, credentials, runtime resources or architecture authoritative for VHDCHY.
- Do not blindly copy old screens or business assumptions. Reuse recognizable interaction/layout patterns only where they fit the current product.
- Current VHDCHY authority for workflow, security, API semantics, offline/LAN behavior and business rules always wins over the reference app.
- Before a screen is treated as visually final, implementation work must inspect the corresponding authoritative Pick Pack 1291 UI source/evidence that is actually accessible; do not invent unavailable reference details.
- Current implementation language is **Vietnamese only**. Multilingual UI is deferred until a later Owner decision explicitly starts that work.

## 2. Online Web + LAN Web UI direction

Both **Online Web** and **LAN Web** shall use the visual direction shown in the two DNSHE screenshots supplied by the Owner on 2026-09-14.

The screenshots define a visual direction, not a license to copy DNSHE branding or proprietary assets verbatim. VHDCHY keeps its own product identity, terminology, icons/assets and information architecture.

### Required visual characteristics

- dark navy primary navigation (top bar and/or left rail depending on screen type);
- light blue/white spacious page background;
- white rounded cards with subtle borders/shadows;
- strong royal-blue primary calls to action;
- restrained secondary accent colors for state/status emphasis;
- compact icon tiles paired with clear labels;
- login layout with a focused central authentication card and contextual/support/security information areas where useful;
- authenticated dashboard layout with navigation rail/top bar, a high-level hero/summary area, KPI/stat cards, search/filter surfaces, recent activity/content cards and account/context panels where relevant;
- clean desktop-first enterprise console appearance while remaining responsive for supported smaller screens.

### Online/LAN parity rules

- Online Web and LAN Web remain **one product and one shared frontend design system/artifact direction**, not two unrelated interfaces.
- A user moving between Online and LAN mode should retain the same navigation language, visual hierarchy and core interaction patterns; only capability/availability indicators and network-dependent actions may differ.
- LAN continuity screens must not require internet-only fonts, icons, scripts, stylesheets or images to remain usable. Core UI assets required during an outage must be packaged/served locally.
- Network state must be visible and unambiguous where it affects behavior (for example Online, LAN active, degraded/reconnecting, queued/local-only), but must not dominate normal workflow.
- External embedded content may be unavailable during an internet outage; that does not invalidate the local shell or LAN continuity UI.

## 3. Current language boundary

For the current delivery stage, Web and Android/PDA App are **Vietnamese only**.

- Do not implement or expose Vietnamese / English / Chinese switching now.
- Do not spend current implementation time on translation catalogs, language selectors, locale persistence or multilingual acceptance.
- User-facing screens, labels, messages and operator help use Vietnamese.
- Architecture should not make future internationalization unnecessarily difficult, but future multilingual support is deferred and must not block current BETA delivery.

This supersedes the previous V5/V7 requirement that the current product expose three languages or default to English.

## 4. UI acceptance boundary

A UI implementation is not accepted merely because it resembles the reference. Acceptance requires all of the following:

1. correct current VHDCHY business workflow and permission behavior;
2. current API/LAN contracts respected;
3. current Vietnamese-only language boundary respected;
4. Online/LAN parity compliant with this decision;
5. no dependency on DNSHE branding/assets;
6. no import of obsolete Pick Pack 1291 business authority;
7. responsive, readable and operable on the intended target devices;
8. LAN-critical UI usable when the internet is unavailable.

## 5. Mandatory execution/reporting rule

The Owner requires factual output from long tool execution, not activity-volume reporting.

Before a tool/session budget interruption or when a long execution block must end, the AI must checkpoint where possible and report:
- what actually reached PASS;
- what FAILED / remains IN_PROGRESS / BLOCKED / not started;
- direct evidence identifiers where available;
- the current evidence-weighted project percentage and justified delta from the start of the block;
- the exact next ready work items.

Issued commands, planned work, source edits without verification and elapsed/tool time are not PASS. If no material product progress was proven, the percentage remains unchanged and that must be stated explicitly.

## 6. Mandatory ready-queue parallel execution

Dependency-aware parallel execution is required in practice, not only in planning.

At each execution point:
1. identify all ready nodes whose prerequisites are satisfied;
2. execute every independent safe ready node in parallel where tools permit;
3. serialize only dependency-bound work or writes to the same file/ref/database/provider resource;
4. after one prerequisite finishes, immediately unlock the next dependent node while unrelated lanes continue;
5. if one node fails/blocks, isolate it and continue all independent ready nodes;
6. keep refilling the ready queue from unfinished work rather than waiting for an unrelated lane to finish.

Example: for `A -> B -> C` plus independent `D`, `E`, `F`, execute `A + D + E + F`, then `B + remaining D/E/F`, then `C + remaining independent work`.

Tool limitations may prevent literal simultaneous API calls, but scheduling must still follow this ready-queue behavior.

## 7. Implementation priority

This decision establishes design, language and execution authority now. It does **not** mean the current repository UI already conforms. Web/App implementation/refinement remains explicit delivery work and must advance in parallel with other independent ready lanes.