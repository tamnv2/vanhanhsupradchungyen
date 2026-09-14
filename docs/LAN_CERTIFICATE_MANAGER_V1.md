# LAN CERTIFICATE MANAGER V1

Status: IMPLEMENTED_AUTOMATED — source/CI PASS; live CA/DNS/physical acceptance pending
Effective date: 2026-09-14
Scope: user-space LAN TLS certificate lifecycle for canonical VHDCHY LAN hosts
Authority dependencies: `DECISIONS.md`, `DECISIONS_V3.md`, `DECISIONS_V4.md`, `DECISIONS_V5.md`, `DECISIONS_V6.md`, `DECISIONS_V7.md`, `SERVICE_AUTHORITY.md`, `docs/LAN_SECURE_HTTP_V1.md`, `docs/LAN_HOST_DOMAIN_V1.md`

## 1. Purpose

The LAN Service requires a publicly trusted HTTPS certificate for its canonical hostname without relying on company administrator privileges, certificate-store installation, inbound Internet access to the laptop, router/AP changes, or an ad-hoc trust root.

Current canonical names remain:

- BETA: `lan-beta.supra.cc.cd`
- STABLE: `lan.supra.cc.cd`

The reviewed certificate path therefore uses ACME DNS-01 and a separate Windows user-space certificate manager. DNS provider credentials are not part of the LAN Service runtime.

## 2. Windows storage model

The LAN Service supports a DPAPI CurrentUser protected PFX through `VHDCHY_LAN_TLS_PROTECTED_PFX_PATH`.

On Windows, the decrypted PFX is imported with current-user key storage so Schannel can use the server private key. It is not installed into the Windows certificate store and `PersistKeySet` is not used. On non-Windows compatibility/CI paths, ephemeral key loading remains available.

Automated Windows evidence at commit `376cb0976f481acf68e8e21654d3d6593e98a81b`:

- workflow `34817069447` / check `103889883955`: **SUCCESS**;
- `dpapiCurrentUser=PASS`;
- `userSpacePfx=PASS`;
- `canonicalSan=PASS`;
- `wrongHostRejected=PASS`;
- `corruptBlobRejected=PASS`;
- `rawPfxDiskLeak=PASS`.

Secure HTTP regression workflow `34817069438` and baseline workflow `34817069440` on the same commit also passed.

## 3. Certificate manager behavior

Project: `lan-certificate-manager/Vhdchy.LanCertificateManager.csproj`.

Current behavior:

- Windows-only operational path;
- exact environment selection: `BETA` or `STABLE`;
- exact canonical host derived from environment;
- exact Cloudflare account/zone verification before DNS mutation;
- ACME DNS-01 TXT records restricted to `_acme-challenge.<canonical-host>`;
- challenge record is verified after creation and cleaned by exact record ID in `finally`;
- ECDSA P-256 certificate request;
- SAN/validity verification before local activation;
- certificate PFX protected using Windows DPAPI CurrentUser;
- atomic replacement of the protected PFX;
- 30-day renewal threshold;
- separate DPAPI-protected ACME account key;
- production Let’s Encrypt issuance requires explicit `--production` plus `VHDCHY_ACME_ALLOW_PRODUCTION=YES`;
- default ACME directory is staging;
- DNS/API error output is sanitized and does not intentionally log the DNS token, private key, ACME account key, or PFX bytes.

The LAN Service consumes the resulting protected PFX. It does not receive Cloudflare credentials.

## 4. CI evidence

Dedicated workflow `Validate LAN certificate manager` at commit `e11c5730a3e9cd7e212d07ea0fbdfc07aff77655`:

- run `34819836554`;
- check/job `103898656531`;
- result: **SUCCESS**.

Self-test markers:

- `LAN_CERTIFICATE_MANAGER_SELF_TEST_PASS`;
- `dpapiPfxAtomicWrite=PASS`;
- `dpapiAccountSecret=PASS`;
- `renewalMetadata=PASS`;
- `rawPfxDiskLeak=PASS`.

The same HEAD baseline validator run `34819836553`, check `103898626431`: **SUCCESS**.

The workflow is intentionally local-only and asserts that no GitHub `secrets.*` expression is injected into the certificate-manager CI job. It proves source/storage/renewal behavior, not live provider issuance.

## 5. Cloudflare read-only evidence

Read-only trust inspection at commit `3588bbcc29b665be453e3988e8f5898ed35a9fa8`:

- run `34816004518`;
- check/job `103886701628`;
- result: **SUCCESS**;
- token verification: active account token;
- exact configured account matched project authority;
- zone `supra.cc.cd` matched and was `active`;
- at inspection time `lan-beta.supra.cc.cd` had 0 records;
- at inspection time `_acme-challenge.lan-beta.supra.cc.cd` had 0 records.

This is read evidence only. It does not prove DNS Edit permission for the credential that will execute on the actual LAN host.

## 6. Explicit non-proofs

This V1 PASS does **not** prove:

- DNS Edit capability of the final LAN-host credential;
- successful live ACME staging or production issuance;
- publicly trusted BETA certificate installed/loaded on the intended company laptop;
- canonical hostname resolution on the actual company LAN;
- browser trust on the intended ordinary-user Windows environment;
- real NLS-MT90/PDA HTTPS acceptance or Wi-Fi/LAN reacquisition;
- unattended renewal execution on the intended host;
- physical >=60-minute Internet-cut continuity;
- STABLE acceptance.

A GitHub-hosted runner must not be used to produce the final DPAPI PFX for the laptop: DPAPI CurrentUser protection belongs to the Windows user/machine context that will operate the LAN Service.

## 7. Next gate

The next certificate gate is a controlled run on the intended Windows LAN host using a dedicated least-privilege Cloudflare DNS credential or another explicitly approved equivalent:

1. verify exact account `supra.cc.cd` zone identity before mutation;
2. prove DNS TXT create/read/delete capability only in the ACME challenge namespace;
3. run ACME staging issuance first;
4. verify protected PFX SAN/validity and LAN Service HTTPS startup;
5. only then allow production ACME issuance;
6. verify browser/PDA trust and canonical LAN reachability;
7. retain renewal/rotation evidence without moving private material through GitHub.

Until those live/physical gates pass, status remains `IMPLEMENTED_AUTOMATED`, not `ACCEPTED`.
