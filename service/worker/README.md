# VHDCHY Worker

Status: `SOURCE_BASELINE / NOT_DEPLOYED`

## Contract

- Cloudflare Worker is the public Service entry point.
- D1 is canonical authority for structured business state.
- Clean schema version: `business_core_v2`.
- Clean runtime marker: `BUSINESS_CORE_V2`.
- Google Sheets is asynchronous projection/reconciliation/DR, not canonical mutation authority.
- Google Gateway failure may degrade integration health but must not invalidate a committed D1 transaction.
- Anonymous business data/mutation remains closed until authenticated session + permission enforcement is implemented and verified.

## Clean reset rule

`migrations/0001_initial.sql` is a new zero-baseline schema. It consolidates the approved model from the pre-reset migration chain without preserving migration-only compatibility artifacts such as the old hard-coded `resources` table.

The schema deliberately seeds only structural configuration for `PICK_PACK_1291` / `PICK_PACK`. It does not seed historical employee/resource/business rows, privileged users, passwords or credentials.

## Deployment gate

Do not create deployment scripts or bind a D1 database until the current Cloudflare account, zone and expected BETA resource identity have been verified. Missing expected resources must fail closed rather than silently creating replacements.
