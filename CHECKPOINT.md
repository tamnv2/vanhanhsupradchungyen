# CHECKPOINT — VHDCHY

checkpoint_version: 36
protocol: AI_AUTHORITY_RESUME_V2
status: EXECUTING_PRODUCT_V7_BETA
reconciled_through_commit: 55dc697a038c0f6b8ee6d9b606fef69ce301e116
action_mode: AUTONOMOUS_PARALLEL
active_lanes: REPO_GOVERNANCE / SHARED_DOMAIN / CLOUD_SERVICE / LAN_FULL_SERVICE / AUTH / GOOGLE_SYNC / WEB_ONLINE_LAN / ANDROID_PDA / RECONCILIATION / STABLE_PREPARATION
paused_lanes: PHYSICAL_CORPORATE_LAN_REGRESSION

authority_base_ref: DECISIONS.md
authority_v3_ref: DECISIONS_V3.md
authority_v4_ref: DECISIONS_V4.md
authority_v5_ref: DECISIONS_V5.md
authority_v6_ref: DECISIONS_V6.md
authority_v7_ref: DECISIONS_V7.md
service_contract_ref: docs/SERVICE_API_CONTRACT_V3.md
lan_edge_ref: docs/LAN_EDGE_STATE_V2.md
lan_secure_http_ref: docs/LAN_SECURE_HTTP_V1.md
lan_certificate_manager_ref: docs/LAN_CERTIFICATE_MANAGER_V1.md
delivery_plan_ref: docs/DELIVERY_PLAN_V5.md
progress_ref: docs/PROGRESS_TRACKING_V1.md
context_index_ref: CONTEXT_INDEX.md

## Current project progress

- Evidence-weighted total: **55.4% exact / 55% displayed**.
- Phase 6 LAN continuity/offline/reconcile: **60%**.
- Primary active phase: **Phase 6 — LAN continuity, local state, offline operation and reconciliation**.
- No progress increase is taken for the newer certificate-lifecycle source/CI work because live public-CA issuance, target-host DNS credential/write capability, company-network trust and real PDA acceptance remain unproven.
- Parallel lanes: Phase 4 core business Service/API; Phase 5 Gateway/integrations; Phase 7 Online+LAN Web V7 UI; Phase 8 Android/PDA App; Cloud reconciliation machine-auth/network path; account-security routes.

## Authority / UI / execution state

- Current Web and Android/PDA user-facing implementation is **Vietnamese only**; multilingual UI remains deferred.
- Android/PDA visual direction uses actual Pick Pack 1291 UI/UX evidence only; legacy business/data/credentials/runtime remain NON_AUTHORITY.
- Online Web + LAN Web share the V7 VHDCHY design direction inspired by Owner-supplied DNSHE screenshots without copying DNSHE branding/assets.
- Ready-queue parallel execution remains mandatory. A blocked node must not stall independent safe work.
- PASS requires reproducible evidence. CI/source PASS never substitutes for physical company-network/PDA PASS.
- Ordinary-user/no-admin remains a hard LAN-host constraint.

## Reconciled evidence through `55dc697a038c0f6b8ee6d9b606fef69ce301e116`

### Secure LAN HTTPS public route — SOURCE/CI PASS

The reviewed credential transport remains direct Kestrel HTTPS. Paired-client P-256 signatures provide device/request authentication and integrity, not password confidentiality.

Runtime safeguards remain:

- no TLS source => `HTTP_READ_ONLY`, no reviewed credential/business mutation route;
- valid TLS source => secure routes still require synchronized authority/operational state, paired client security, credential authority, session/authz/domain readiness;
- normal-user login issues durable LAN session evidence;
- reviewed business commands require HTTPS + Bearer session + exact signed body;
- ROOT has no permanent password and remains email-OTP authority;
- `mustChangePassword` remains fail-closed until change/recovery routes exist;
- portrait replacement remains fail-closed at the unresolved Owner semantic gate.

Secure HTTP workflow `34811861697`, check `103874646267`, commit `63ceeb6a8db865ced1870209c7cb74d4f65baea1`: **SUCCESS**. Same-HEAD clean baseline `34811861613`: **SUCCESS**.

Detailed boundary: `docs/LAN_SECURE_HTTP_V1.md`.

### Windows DPAPI CurrentUser TLS — SOURCE/CI PASS

The LAN Service can consume a DPAPI CurrentUser protected PFX without installing a certificate in the Windows certificate store or requiring administrator rights.

Windows Schannel evidence established that `EphemeralKeySet` is unsuitable for the server private key on the target runtime path. Windows therefore uses temporary current-user key storage (`UserKeySet`, no `PersistKeySet`); non-Windows compatibility paths retain ephemeral loading.

Evidence at commit `376cb0976f481acf68e8e21654d3d6593e98a81b`:

- DPAPI TLS workflow `34817069447`, check `103889883955`: **SUCCESS**;
- secure HTTP regression `34817069438`: **SUCCESS**;
- baseline `34817069440`: **SUCCESS**.

Proven markers include DPAPI CurrentUser protection, real Kestrel/Schannel HTTPS startup, canonical SAN acceptance, wrong-host rejection, corrupt-blob rejection and no raw-PFX disk leak.

### ACME DNS-01 certificate manager — SOURCE/CI PASS

A separate Windows user-space certificate manager now owns certificate lifecycle concerns. DNS-provider credentials are intentionally separated from LAN Service.

Implemented safeguards:

- BETA/STABLE canonical host derivation;
- exact Cloudflare account/zone verification before mutation;
- ACME TXT writes restricted to `_acme-challenge.<canonical-host>`;
- exact challenge readback and record-ID cleanup in `finally`;
- ECDSA P-256 certificate request;
- SAN/validity verification before activation;
- atomic DPAPI CurrentUser protected-PFX replacement;
- separate DPAPI-protected ACME account key;
- 30-day renewal threshold;
- Let’s Encrypt staging by default;
- production requires both `--production` and `VHDCHY_ACME_ALLOW_PRODUCTION=YES`;
- no intentional logging of DNS token/private key/account key/PFX bytes.

Certificate-manager workflow at commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`:

- run `34819836554`, check/job `103898656531`: **SUCCESS**;
- markers: `LAN_CERTIFICATE_MANAGER_SELF_TEST_PASS`, `dpapiPfxAtomicWrite=PASS`, `dpapiAccountSecret=PASS`, `renewalMetadata=PASS`, `rawPfxDiskLeak=PASS`;
- same-HEAD baseline run `34819836553`, check `103898626431`: **SUCCESS**.

The dedicated CI workflow receives no GitHub `secrets.*` values. It does not perform live DNS mutation or final certificate issuance.

Detailed boundary: `docs/LAN_CERTIFICATE_MANAGER_V1.md`.

### Cloudflare trust prerequisites — READ-ONLY PROVIDER PASS

Read-only inspection at commit `3588bbcc29b665be453e3988e8f5898ed35a9fa8`, run `34816004518`, check/job `103886701628`: **SUCCESS**.

Verified at that time:

- configured account token active;
- exact expected project account matched;
- zone `supra.cc.cd` matched and was `active`;
- `lan-beta.supra.cc.cd` record count = 0;
- `_acme-challenge.lan-beta.supra.cc.cd` record count = 0.

This does **not** prove DNS Edit capability for a dedicated credential on the intended Windows LAN host. No provider mutation PASS is inferred.

### Earlier LAN security/readiness/local foundations — remain PASS

- primary credential/session workflow `34805395079`: **SUCCESS**;
- integrated readiness `34808274815`: **SUCCESS**;
- paired readiness baseline `34808274819`: **SUCCESS**;
- signed route wiring `34808936937`: **SUCCESS**;
- route-wiring baseline `34808937077`: **SUCCESS**;
- durable Cloud-sync queue `34801533266`: **SUCCESS**.

Production fail-closed safeguards remain intact.

### Cloud reconciliation foundation — PASS, network auth pending

Cloud LAN-reconciliation ingestion core remains source/CI PASS, including actor/source/event evidence preservation, collision handling, completed integration receipt ingestion/deduplication and packaged Worker module.

Evidence remains workflows `34803221835` and `34803221873` at `591c4083973141155530357f5bddd4ee58efbdc7`: **SUCCESS**.

A reviewed machine/service authentication mechanism is still required before the LAN -> Cloud network route is exposed. Payload actor evidence is not authentication.

### Web / Android foundations

- Shared V7 Online/LAN Web shell remains PASS in product-foundation workflow `34801611019`.
- Android foundation continues to build; exact final Pick Pack screen/layout evidence remains insufficiently surfaced, so exact visual details must not be invented.

## Current dependency graph / ready queue

### Primary — target-Windows public CA + real-device acceptance

1. use/provision a dedicated least-privilege DNS credential on the intended ordinary-user Windows LAN host;
2. live-verify exact account/zone and TXT create/read/delete capability only in `_acme-challenge.lan-beta.supra.cc.cd`;
3. run ACME **staging** issuance on the target Windows user/machine context;
4. verify DPAPI protected PFX, canonical SAN and LAN Service HTTPS startup/restart;
5. only after staging PASS, explicitly enable production issuance;
6. prove canonical hostname resolution/reachability + browser public-certificate trust on the intended company Windows/network;
7. prove real NLS-MT90/PDA HTTPS login/business/reconnection and Wi-Fi/LAN reacquisition;
8. include the approved secure public subset in later physical continuity acceptance.

A GitHub-hosted runner must not create the final DPAPI PFX for the laptop because DPAPI CurrentUser is bound to the target Windows user/machine context.

Do not create arbitrary `lan-beta` public DNS routing merely to satisfy the hostname label; canonical offline/company-LAN resolution remains a separate V4 acceptance requirement.

### Parallel — account security

1. implement/prove public ROOT email-OTP request/verify flow without permanent ROOT password;
2. prove OTP expiry/cooldown/replay/attempt-limit behavior;
3. implement/prove authenticated `mustChangePassword` change path;
4. implement/prove normal-user forgotten-password recovery ending in mandatory password change;
5. keep credential/OTP material out of diagnostics.

### Parallel — reconciliation network E2E

1. define/prove machine/service authentication for Cloud reconciliation;
2. wire tested Cloud ingestion core behind that boundary;
3. connect LAN sender;
4. prove retry/restart/idempotency/result mapping E2E;
5. prove Google/Drive receipts do not duplicate output;
6. add cursor/delta/rebase/conflict-resolution behavior after transport stabilizes.

### Other independent lanes

- Web authenticated/business surfaces against real Service/LAN routes;
- Android endpoint/scanner/session/durable-queue/certificate-validating HTTPS work not blocked by missing visual evidence;
- broader Cloud/Gateway/Google receipt/retry/readback work;
- broader business adapter coverage.

## Portrait semantic gate

The portrait rule conflict remains unresolved: immediate deletion of the previous remote portrait versus offline staging while Drive is unavailable. Decision-independent media infrastructure may continue. Actual offline portrait-replacement semantics remain fail-closed until explicit Owner authority.

## Physical / STABLE boundary

- Live publicly trusted BETA certificate/DNS/company-device HTTPS evidence is not yet PASS.
- Physical company ordinary-user Windows + target network + real NLS-MT90 regression remains pending.
- Canonical offline continuity acceptance remains **Window 2 >=60 minutes** after Internet cut while valid LAN remains.
- STABLE business activation/promotion remains blocked until mandatory BETA acceptance and explicit Owner approval.

do_not_repeat:
Do not treat memory as authority. Do not replay provider mutations from remembered state. Do not claim provider delivery/projection live without current evidence. Do not bypass action-safety. Do not send reusable passwords over plaintext LAN HTTP. Do not confuse signed requests with encryption. Do not treat CI self-signed certificates as production trust. Do not treat certificate-manager self-test as live CA issuance. Do not generate the target laptop DPAPI PFX on a GitHub runner. Do not assume DNS Edit from read-only token verification. Do not require or silently modify company certificate stores/firewall/router/AP/internal DNS. Do not create arbitrary public LAN routing merely to appear complete. Do not weaken security/readiness assertions merely to make CI pass. Do not mutate raw edge events. Do not make Google business authority. Do not silently last-write-wins conflicts. Do not treat CI as physical company-LAN proof. Do not invent Pick Pack UI details without actual reference evidence. Do not copy DNSHE branding/assets. Do not reintroduce stale duration-only offline TTL. Do not silently resolve the portrait immediate-delete/offline-staging conflict. Do not build/expose multilingual UI in the current stage. Do not inflate progress from tool/commit activity without acceptance evidence. Do not promote STABLE without explicit Owner approval.
