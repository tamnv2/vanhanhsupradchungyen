# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Immediate priority — Google Gateway BETA

Google Cloud/OAuth prerequisites are now verified. Execute this sequence without changing flow:

1. Create GitHub Environment `beta`.
2. Add only the verified Google BETA values:
   - secrets: `GOOGLE_OAUTH_CLIENT_SECRET`, `GOOGLE_OAUTH_REFRESH_TOKEN`;
   - variables: `APP_ENV=BETA`, `OWNER_EMAIL=tam95.supra@gmail.com`, `GOOGLE_OAUTH_CLIENT_ID`, `GAS_SCRIPT_ID`, `GOOGLE_SHEETS_PROJECTION_ID=1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`.
3. Add a manual-only sync workflow. Source authority is `service/google-gateway/`; do not paste source through OAuth Playground or maintain code in the Apps Script editor.
4. Sync GitHub source to Apps Script HEAD through `projects.updateContent`, then read back and verify the complete Apps Script content against the rendered GitHub source.
5. Owner runs `bootstrapAuthorize()` once in the Apps Script editor. This is the runtime authorization gate for Sheets + owner identity.
6. Only after bootstrap PASS, create an immutable Apps Script version and a versioned Web App deployment.
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
