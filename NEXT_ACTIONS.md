# NEXT ACTIONS

Baseline: `REPO-RESET-20260912-01`

## Immediate priority — Service/BETA

Run these independent lanes in parallel where tools permit:

1. Google Cloud/OAuth BETA under `tam95.supra@gmail.com`.
   - verify/create intended BETA GCP project;
   - enable required Apps Script API;
   - create current OAuth client;
   - CI OAuth scopes only: `script.projects` + `script.deployments`.

2. Cloudflare under `nguyenvantam050595@gmail.com`.
   - verify account + zone `supra.cc.cd`;
   - verify expected BETA Worker/D1 identity;
   - minimum deploy-token permissions: Workers Scripts Write + D1 Write;
   - missing expected retained resource must fail closed; do not silently create a replacement.

3. Android BETA signer.
   - verify keystore/alias/fingerprint locally;
   - do not expose keystore bytes/passwords in chat or repository.

## After Google OAuth PASS

4. Create/link GAS BETA from `service/google-gateway/` and bind the current BETA projection workbook.
5. Run bootstrap authorization and deploy the BETA Web App.
6. Record verified script/deployment/exec outputs in authority files only; no secret values in source.

## After Cloudflare identity PASS

7. Add fresh provider deployment scripts/config for `service/worker/` using the reconciled `business_core_v2` schema contract.
8. Do not create or bind STABLE resources.

## Integration gate

9. Recreate GitHub `beta` Environment only from verified provider outputs.
10. Add only the minimum required BETA secrets/variables.
11. Create a fresh `beta` live branch from the approved `main` baseline only when provider gate is ready.
12. Run BETA deploy/health checks; D1 core health is critical, Google projection integration is advisory/degraded-capable.

## LAN

No physical LAN work while Owner is off-site. When Owner returns to the company, restore reviewed LAN/Android source from `backup/pre-zero-20260912`, resume pending V4 physical regression, and run independent LAN/Service items in parallel.

## Backup branch

Keep `backup/pre-zero-20260912` until all retained Android/LAN source/evidence has been reviewed and restored or deliberately rejected. Then remove the backup branch.

## STABLE

Blocked until BETA PASS and explicit Owner approval. Do not copy BETA credentials/IDs into STABLE.
