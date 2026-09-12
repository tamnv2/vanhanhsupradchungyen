# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Immediate priority — Google Gateway BETA

Google Cloud/OAuth prerequisites, GitHub Environment `beta`, source synchronization, and Owner runtime consent are complete. Continue without changing flow:

1. Treat the Owner-reported `bootstrapAuthorize()` completion as provisional until remote verification proves the stored bootstrap marker/config.
2. Use the guarded GitHub automation to synchronize the verified HEAD, create/reuse an immutable version for the current source revision, and create/update the managed versioned BETA Web App deployment.
3. Require the deployed `/exec` response to prove all of the following: `service=VHDCHY_GOOGLE_GATEWAY`, `environment=BETA`, matching Script ID, `bootstrap.authorized=true`, and `bootstrap.configMatch=true`.
4. Only after that remote verification PASS, mark Google Gateway BETA runtime authorization/deployment as PASS.
5. Record the non-secret `GAS_DEPLOYMENT_ID` and `GAS_EXEC_URL` in repository authority/config; do not store secret values in source.
6. Use the existing managed deployment on future releases by moving it to a new immutable version rather than creating uncontrolled duplicate deployments.

## Autonomous execution model

- After Owner approves a proposed plan/scope, AI executes all actions available through connected tools without delegating automatable clicks or runs back to Owner.
- Owner interaction is requested only for unavoidable UI/consent/secret/physical steps not exposed by available tools.
- Google Gateway synchronization is triggered through `.github/dispatch/gas-beta-sync.json`; manual `workflow_dispatch` remains fallback only.
- Do not manually paste source into OAuth Playground or Apps Script editor.

## Independent Service lanes

7. Cloudflare under `nguyenvantam050595@gmail.com`:
   - verify account + zone `supra.cc.cd`;
   - verify expected BETA Worker/D1 identity;
   - minimum deploy-token permissions: Workers Scripts Write + D1 Write;
   - missing expected retained resource must fail closed; do not silently create a replacement.

8. Android BETA signer:
   - verify keystore/alias/fingerprint locally;
   - do not expose keystore bytes/passwords in chat or repository.

## After Cloudflare identity PASS

9. Add fresh provider deployment scripts/config for `service/worker/` using the reconciled `business_core_v2` schema contract.
10. Do not create or bind STABLE resources.

## Integration gate

11. Add Cloudflare/signer values to GitHub `beta` only after those outputs are independently verified.
12. Create a fresh `beta` live branch from the approved `main` baseline only when the full provider gate is ready.
13. Run BETA deploy/health checks; D1 core health is critical, Google projection integration is advisory/degraded-capable.

## LAN

No physical LAN work while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending V4 physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until all retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected. Then remove the backup branch.

## STABLE

Blocked until BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
