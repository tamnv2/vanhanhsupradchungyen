# PROJECTION AUTHENTICATION PLAN — BETA

Status: REVIEWED DESIGN / NOT PROVISIONED
Updated: 2026-09-13
Protocol: `VHDCHY_PROJECTION_V1`
Authority: `DECISIONS.md`, `docs/SERVICE_API_CONTRACT.md`

## Objective

Authenticate the single approved Worker -> Google Gateway projection writer without putting a raw shared secret in GitHub source, dispatch files, Google Sheets, chat, or ordinary logs.

Projection remains fail-closed and `PROVISIONED_NOT_LIVE` until provisioning and end-to-end retry/idempotency acceptance pass.

## Existing foundation retained

The deployed Gateway already implements the intended verifier model:

- request carries a high-entropy `sharedToken` over HTTPS;
- GAS stores only `SHA-256(sharedToken)` as `VHDCHY_PROJECTION_SHARED_TOKEN_SHA256`;
- comparison is length-checked and constant-time style;
- `VHDCHY_PROJECTION_ENABLED` is an independent gate;
- if verifier is missing, Gateway returns `PROJECTION_AUTH_NOT_CONFIGURED`;
- if enable gate is false, Gateway returns `PROJECTION_NOT_LIVE`;
- protocol/environment, allowed sheets, allowed columns, bounded batch size and key-based upsert remain independently enforced.

This design avoids storing the raw token inside GAS while keeping the anonymous web-app transport unusable without possession of the token.

## Secret placement

Raw shared token:

- canonical secret-store location: GitHub Environment `beta` secret named `VHDCHY_PROJECTION_SHARED_TOKEN`;
- Worker runtime: Cloudflare Worker secret binding with the same logical name;
- never commit or print the raw value.

Verifier only:

- GAS Script Properties: `VHDCHY_PROJECTION_SHARED_TOKEN_SHA256`;
- verifier may be computed inside CI from the Environment secret;
- CI must never print the raw token and should not print the verifier unless needed for bounded diagnostic evidence.

Enable state:

- GAS Script Property `VHDCHY_PROJECTION_ENABLED`;
- remains `false` during provisioning and authentication verification;
- becomes `true` only in the explicit projection-live gate after E2E sender/ACK/retry tests are ready.

## Preferred coordinated provisioning path

Use the existing Google OAuth/Apps Script CI identity rather than an anonymous bootstrap endpoint.

1. Extend the Apps Script manifest with an API-executable entry restricted to the deploying account (`MYSELF`), while retaining the existing Web App entry used by the Worker.
2. Add a narrow public Apps Script provisioning function that accepts only:
   - projection token SHA-256 verifier;
   - requested enable state;
   - expected environment.
3. The function must fail closed unless environment/bootstrap identity matches BETA and verifier format is exactly the expected SHA-256 representation.
4. GitHub-hosted CI reads `VHDCHY_PROJECTION_SHARED_TOKEN` from the `beta` Environment secret, computes SHA-256 in memory, and invokes the provisioning function through Apps Script API `scripts.run` using the already-authorized Google OAuth identity.
5. CI reads back Gateway health and requires `authConfigured=true` while `enabled=false` before proceeding.
6. A separate reviewed Cloudflare provisioning/deploy path binds the raw token as a Worker secret without exposing it in source or logs.
7. Only after Worker authentication probes and outbox integration tests pass does a separate fixed operation change the GAS enable state to `true`.

## Why API execution is preferred

The Web App is intentionally `ANYONE_ANONYMOUS` so the Worker can reach it. That public transport must not also be the provisioning authority. Provisioning through Apps Script API uses the authenticated Google account/project path already used to manage the script and keeps the management plane separate from the public projection data plane.

## Required validation before implementation PASS

The coordinated provisioning lane must prove all of the following before it may enable writes:

- Apps Script project can be invoked through `scripts.run` by the existing BETA OAuth identity;
- API-executable access is restricted to the deploying identity;
- Web App URL and existing bootstrap identity remain unchanged;
- raw token does not appear in repository diffs or job output;
- Gateway health reports `authConfigured=true` and `enabled=false` immediately after provisioning;
- Worker receives the same raw token through a secret binding, not plain-text source/config;
- wrong/missing token is rejected;
- correct token is accepted only for the exact BETA protocol/environment;
- replayed projection items remain idempotent through sheet key upsert/event keys;
- Google failure leaves canonical D1 mutations committed and outbox retryable;
- projection activation is a separately evidenced gate.

## Owner interaction boundary

The connected GitHub tools in the current environment do not expose GitHub Environment secret creation/update. If no existing approved secret can be provisioned through a connected high-level provider action, setting `VHDCHY_PROJECTION_SHARED_TOKEN` in the GitHub `beta` Environment becomes an `OWNER_PERMISSION_REQUIRED` step.

When that point is reached, request only the minimum secret-store action. Do not ask the Owner to paste the raw token into chat and do not substitute a committed/plain-text value.

## Current state

- Gateway version 3 code already contains verifier and enable fail-closed checks.
- Workbook remains `PROVISIONED_NOT_LIVE`.
- No projection auth secret is treated as provisioned or LIVE yet.
- Worker sender/ACK integration is not runtime-live.
