# VẬN HÀNH DC HƯNG YÊN — OWNER DECISIONS V7

Status: ACTIVE
Decision date: 2026-09-14
Authority: explicit Owner instruction in current session
Supersedes: only conflicting UI-direction details in older decision/docs; all non-conflicting V2–V6 decisions remain active.

## 1. Android/PDA app UI direction

The VHDCHY Android/PDA application shall reuse the **UI direction of the Owner's Pick Pack 1291 application**, adapted to the current VHDCHY product, workflows, terminology, permissions, data model and technical architecture.

Rules:
- `BACKUP PICK PACK 1291` is a reference source only. This Owner decision explicitly authorizes using it for **UI/UX reference** for the current app; it does not make old Pick Pack business logic, data, credentials, runtime resources or architecture authoritative for VHDCHY.
- Do not blindly copy old screens or business assumptions. Reuse recognizable interaction/layout patterns only where they fit the current product.
- Current VHDCHY authority for workflow, security, API semantics, offline/LAN behavior and business rules always wins over the reference app.
- Before a screen is treated as visually final, implementation work must inspect the corresponding authoritative Pick Pack 1291 UI source/evidence that is actually accessible; do not invent unavailable reference details.
- Existing V5 language rule remains unchanged: Web and App expose Vietnamese / English / Chinese and default to English unless a later Owner decision overrides it.

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

## 3. UI acceptance boundary

A UI implementation is not accepted merely because it resembles the reference. Acceptance requires all of the following:

1. correct current VHDCHY business workflow and permission behavior;
2. current API/LAN contracts respected;
3. language behavior compliant with V5;
4. Online/LAN parity compliant with this V7 decision;
5. no dependency on DNSHE branding/assets;
6. no import of obsolete Pick Pack 1291 business authority;
7. responsive, readable and operable on the intended target devices;
8. LAN-critical UI usable when the internet is unavailable.

## 4. Implementation priority

This decision establishes design authority now. It does **not** mean the current repository UI already conforms. Web and App UI implementation/refinement must be planned and measured as explicit remaining delivery work.