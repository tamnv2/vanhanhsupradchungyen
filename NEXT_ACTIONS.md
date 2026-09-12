# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Immediate priority — Google Gateway BETA

Google Cloud/OAuth prerequisites, GitHub Environment `beta`, and source synchronization are verified. Continue without changing flow:

1. Runtime authorization gate: Owner must execute `bootstrapAuthorize()` once in the Apps Script editor under `tam95.supra@gmail.com` because this step requires interactive Google consent for the script runtime scopes (`spreadsheets` + `userinfo.email`).
2. Require `bootstrapAuthorize()` PASS against the designated BETA projection workbook; stop and diagnose on any owner mismatch, permission error, or workbook access failure.
3. After runtime authorization PASS, AI adds and triggers the guarded autonomous version/deployment workflow from GitHub.
4. Create an immutable Apps Script version from the verified HEAD source.
5. Create/update the versioned BETA Web App deployment.
6. Verify the `/exec` endpoint reports the intended Google Gateway identity, environment `BETA`, and Script ID.
7. Record `GAS_DEPLOYMENT_ID` and `GAS_EXEC_URL` as BETA environment variables only after deployment PASS.

## Autonomous execution model

- After Owner approves a proposed plan/scope, AI executes all actions available through connected tools without delegating automatable clicks or runs back to Owner.
- Owner interaction is requested only for unavoidable UI/consent/secret/physical steps not exposed by available tools.
- Google Gateway sync can be triggered autonomously by updating `.github/dispatch/gas-beta-sync.json`; manual `workflow_dispatch` is fallback only.
- Do not manually paste source into OAuth Playground or Apps Script editor.

## Independent Service lanes

8. Cloudflare under `nguyenvantam050595@gmail.com`:
   - verify account + zone `supra.cc.cd`;
   - verify expected BETA Worker/D1 identity;
   - minimum deploy-token permissions: Workers Scripts Write + D1 Write;
   - missing expected retained resource must fail closed; do not silently create a replacement.

9. Android BETA signer:
   - verify keystore/alias/fingerprint locally;
   - do not expose keystore bytes/passwords in chat or repository.

## After Cloudflare identity PASS

10. Add fresh provider deployment scripts/config for `service/worker/` using the reconciled `business_core_v2` schema contract.
11. Do not create or bind STABLE resources.

## Integration gate

12. Add Cloudflare/signer values to GitHub `beta` only after those outputs are independently verified.
13. Create a fresh `beta` live branch from the approved `main` baseline only when the full provider gate is ready.
14. Run BETA deploy/health checks; D1 core health is critical, Google projection integration is advisory/degraded-capable.

## LAN

No physical LAN work while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending V4 physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until all retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected. Then remove the backup branch.

## STABLE

Blocked until BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
