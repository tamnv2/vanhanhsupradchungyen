# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Immediate priority — Google Gateway BETA

Google Cloud/OAuth prerequisites and GitHub Environment `beta` are verified. Execute this sequence without changing flow:

1. Run the manual workflow `Google Gateway BETA sync` from branch `main`.
2. Require workflow PASS. The workflow must:
   - obtain an access token from the stored OAuth refresh token without logging secrets;
   - read current Apps Script content;
   - render `service/google-gateway/Code.gs` with the BETA environment/owner/projection values;
   - submit the complete `Code` + `appsscript` file set through `projects.updateContent`;
   - read Apps Script content back and verify the complete file set/content.
3. If sync FAILS, stop at the failing gate and diagnose. Do not manually paste source into OAuth Playground or Apps Script editor.
4. Only after sync PASS, Owner runs `bootstrapAuthorize()` once in the Apps Script editor. This is the runtime authorization gate for Sheets + owner identity.
5. Require `bootstrapAuthorize()` PASS against the designated BETA projection workbook.
6. Only after bootstrap PASS, add the version/deployment workflow logic, create an immutable Apps Script version and create/update the versioned Web App deployment.
7. Verify the `/exec` endpoint reports the intended Google Gateway identity, environment `BETA`, and Script ID.
8. Record `GAS_DEPLOYMENT_ID` and `GAS_EXEC_URL` as BETA environment variables only after deployment PASS.

## Independent Service lanes

9. Cloudflare under `nguyenvantam050595@gmail.com`:
   - verify account + zone `supra.cc.cd`;
   - verify expected BETA Worker/D1 identity;
   - minimum deploy-token permissions: Workers Scripts Write + D1 Write;
   - missing expected retained resource must fail closed; do not silently create a replacement.

10. Android BETA signer:
   - verify keystore/alias/fingerprint locally;
   - do not expose keystore bytes/passwords in chat or repository.

## After Cloudflare identity PASS

11. Add fresh provider deployment scripts/config for `service/worker/` using the reconciled `business_core_v2` schema contract.
12. Do not create or bind STABLE resources.

## Integration gate

13. Add Cloudflare/signer values to GitHub `beta` only after those outputs are independently verified.
14. Create a fresh `beta` live branch from the approved `main` baseline only when the full provider gate is ready.
15. Run BETA deploy/health checks; D1 core health is critical, Google projection integration is advisory/degraded-capable.

## LAN

No physical LAN work while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending V4 physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until all retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected. Then remove the backup branch.

## STABLE

Blocked until BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
