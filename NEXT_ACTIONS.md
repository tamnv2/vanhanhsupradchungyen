# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Google Gateway BETA — FOUNDATION PASS

Google Cloud/OAuth, GitHub Environment `beta`, source synchronization, runtime bootstrap, immutable versioning, managed deployment, and `/exec` verification are complete.

Canonical Google Gateway BETA authority:
- Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`
- Managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`
- Managed Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`
- Verification run: `34696139468`

Future Google Gateway changes must continue through GitHub source authority -> guarded sync -> immutable version -> managed deployment update -> `/exec` verification. Do not create new uncontrolled deployment identities.

The Gateway remains foundation-only: `doPost()` does not yet accept business mutations and the BETA projection workbook remains `PROVISIONED_NOT_LIVE`.

## Immediate priority — Cloudflare BETA

1. Verify current Cloudflare account identity under `nguyenvantam050595@gmail.com` and zone `supra.cc.cd`.
2. Verify whether the expected BETA resources already exist:
   - Worker: `vhdchy-beta`
   - D1: `vhdchy-data-beta`
3. Missing expected retained resources must fail closed and be reported; do not silently create replacements during verification.
4. After identity verification, prepare the minimum BETA deployment credentials and configuration required by the current architecture.
5. Add only verified Cloudflare BETA values to GitHub Environment `beta`.
6. Add fresh provider deployment scripts/config for `service/worker/` using the reconciled `business_core_v2` / `BUSINESS_CORE_V2` contract.
7. Apply the clean zero-baseline D1 schema only to the confirmed BETA D1 resource.
8. Deploy Worker BETA and require D1 core health PASS.
9. Bind the canonical Google Gateway URL to Worker BETA and require deep/integration health PASS. Google projection failure remains degradable/advisory; D1 core failure is critical.

## Autonomous execution model

- After Owner approves a proposed plan/scope, AI executes all actions available through connected tools without delegating automatable clicks or runs back to Owner.
- Owner interaction is requested only for unavoidable UI/consent/secret/physical steps not exposed by available tools.
- Google Gateway synchronization/deployment is triggered through `.github/dispatch/gas-beta-sync.json`; manual `workflow_dispatch` is fallback only.
- Do not manually paste source into OAuth Playground or Apps Script editor.

## Android BETA signer

10. Verify keystore/alias/fingerprint locally when the relevant machine/material is available.
11. Do not expose keystore bytes/passwords in chat or repository.

## Integration gate

12. Add Android signer values to GitHub `beta` only after local verification.
13. Create a fresh `beta` live branch from the approved `main` baseline only when the full provider gate is ready.
14. Run BETA deploy/health checks and retain evidence.

## LAN

No physical LAN work while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending V4 physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until all retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected. Then remove the backup branch.

## STABLE

Blocked until BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
