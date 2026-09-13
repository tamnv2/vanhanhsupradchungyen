# LAN HOST AND DOMAIN V1

Status: ACTIVE
Authority: `DECISIONS_V4.md`, legacy LAN evidence for feasibility only

## Host constraint

The LAN Service runs on a restricted company Windows laptop under normal user rights.

Required deployment shape:
- portable/user-space package;
- normal-user execution;
- user-writable runtime/state location;
- no dependency on changing company firewall, router/AP, route table, internal DNS, certificate store or company security policy;
- no bypass of company controls;
- user-level autostart only where company policy permits it.

Legacy Agent behavior is reference evidence only. The current LAN Service may use different code/structure but must preserve the above host constraints unless the Owner changes them.

## Canonical LAN names

- BETA: `lan-beta.supra.cc.cd`
- STABLE: `lan.supra.cc.cd`

The LAN-hosted Web UI should remain compatible with the online Web UI and use the same business/domain contract.

## Offline-domain target

When public Internet is unavailable but local Wi-Fi/LAN remains available, the product target is that the canonical LAN domain still opens the active local LAN Service rather than requiring ordinary users to type an IP address.

This is not considered solved by public DNS alone. It requires a user-mode resolution mechanism compatible with the restricted-laptop constraints and must pass physical testing on the intended laptop/network/PDA environment.

LAN IP/discovery is retained as fallback for recovery, diagnostics and initial feasibility checks, not as the intended normal end-user URL.

## Acceptance minimum

- domain works while Internet is available;
- domain resolves to the intended active LAN host locally;
- domain remains usable during a real Internet outage while LAN remains up;
- no admin rights or company-network policy changes are required;
- if user-mode resolution cannot satisfy this under actual company policy, report the limitation explicitly rather than claiming PASS;
- Web/API behavior remains compatible between domain access and fallback IP access.
