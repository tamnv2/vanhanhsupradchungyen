# WORKER BETA MULTI-MODULE PACKAGING PLAN

Status: REVIEWED DESIGN / IMPLEMENTATION WRITE BLOCKED BY PLATFORM SAFETY
Updated: 2026-09-13
Authority: `DECISIONS.md`, `AI_OPERATING_CONTRACT.md`, `docs/SERVICE_API_CONTRACT.md`

## Purpose

Remove the current single-module deployment constraint without changing business behavior or opening protected routes early.

The first multi-module deployment is a packaging-only gate. `index.js` remains the entry module and retains the existing fail-closed business/admin route behavior. Additional reviewed modules are uploaded so later integration can import them only after packaging itself is proven.

## Reviewed module manifest

`service/worker/deploy.beta.json` is the authoritative BETA upload manifest. The reviewed set is exactly:

- `index.js`
- `auth.js`
- `auth-service.js`
- `authorization.js`
- `session.js`
- `permission-store.js`
- `projection.js`

No recursive directory scan, wildcard upload or arbitrary dispatch-supplied module path is permitted.

## Required deploy workflow guards

Before upload, the deploy job must fail closed unless all of these hold:

1. branch is `main` and the fixed BETA dispatch contract matches;
2. target Worker, D1 identity, public origin, environment and expected schema/runtime match `deploy.beta.json`;
3. module manifest exactly matches the reviewed set;
4. `index.js` is present and is the configured main module;
5. each module name is a plain filename ending in `.js`, with no directory component or traversal token;
6. each module exists under `service/worker/src/` and passes JavaScript module syntax validation;
7. provider preflight proves the exact BETA Worker/D1/domain/binding state before mutation.

## Multipart upload contract

The Cloudflare Workers upload uses multipart form data:

- metadata `main_module` is `index.js`;
- every manifest item is uploaded as an `application/javascript+module` part using the manifest filename as the part name;
- bindings remain exactly `DB`, `APP_ENV`, `BUILD_SHA`, and `GAS_EXEC_URL` until a separately reviewed change adds another binding;
- raw secret values never enter source, dispatch files or logs.

## Post-deploy acceptance

Packaging is PASS only after all existing checks still pass:

- exact D1 remains `business_core_v3`;
- `workers.dev` remains disabled;
- custom domain remains `beta.supra.cc.cd`;
- binding names/types remain unchanged;
- `/health`, `/health/deep`, `/api/v1/meta`, and `/api/v1/capabilities` pass against the deployed commit SHA;
- anonymous `/api/v1/data/*` and `/api/v1/admin/*` remain fail-closed.

Uploading additional modules alone does not make Auth, Projection or business APIs runtime-live.

## Current evidence

- Manifest change committed on `main` at `45ee0cbadee6e6817e2894a7cfddfa4b995da9a4`.
- Clean-baseline validation run `34754738074`: SUCCESS.
- Fresh Cloudflare read-only verification run `34754968801`: SUCCESS.
- Fresh baseline validation run `34754968807`: SUCCESS.
- Fresh verification confirms BETA Worker/D1 identity, schema `business_core_v3`, expected four bindings, `workers.dev` disabled, and zero rows in business tables.

## Current blocker

The connected GitHub write path currently blocks modification of the provider-mutating GitHub Actions workflow, and also blocked subsequent sensitive runtime source writes. Per project policy this is a platform write-safety constraint, not an Owner permission blocker, and must not be bypassed through lower-level Git/API methods.

Until a reviewed high-level write path can apply the workflow change:

- do not trigger the Worker deploy dispatch;
- keep current runtime fail-closed;
- continue independent design, validation, provider inspection and non-sensitive repository work;
- do not request local tooling merely to bypass this constraint.
