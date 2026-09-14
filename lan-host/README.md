# VHDCHY LAN HOST — PORTABLE WINDOWS BETA

Status: implementation/runbook for ordinary-user BETA host; physical acceptance pending.

This package is designed for the restricted company Windows-laptop constraint:

- no administrator rights required by the package itself;
- no Windows service installation;
- no certificate-store installation;
- no firewall/router/AP/internal-DNS modification;
- no machine-wide .NET installation when using the self-contained package;
- state and protected certificate material live in the current user's writable profile.

## Package layout

- `service/` — self-contained LAN Service runtime.
- `certificate/` — self-contained ACME/DNS-01 certificate manager.
- `scripts/Start-LanService.ps1` — ordinary-user launcher.
- `scripts/Manage-LanCertificate.ps1` — current-user DPAPI DNS-token bootstrap and certificate issuance launcher.
- `VERSION.txt` — package build identity.

## Default current-user paths

BETA defaults:

- LAN data: `%LOCALAPPDATA%\VHDCHY\LanService\BETA`
- certificate data: `%LOCALAPPDATA%\VHDCHY\LanCertificate\BETA`
- DNS token: `cloudflare-dns-token.current-user.dpapi`
- protected PFX: `lan-tls.pfx.dpapi`

STABLE uses the corresponding `STABLE` directories but must not be business-activated without the mandatory BETA gates and explicit Owner promotion approval.

## BETA certificate sequence

Run PowerShell as the ordinary Windows user that will operate LAN Service.

1. Store the dedicated DNS token locally:

   `./scripts/Manage-LanCertificate.ps1 -Action BootstrapToken -TargetEnvironment BETA`

   The token is entered through a non-echoed SecureString prompt and stored with Windows DPAPI CurrentUser. This step does **not** claim provider permission PASS.

2. Issue a Let’s Encrypt **staging** certificate first:

   `./scripts/Manage-LanCertificate.ps1 -Action IssueStaging -TargetEnvironment BETA -AcmeEmail <approved-email>`

   The certificate manager verifies the exact Cloudflare account/zone before DNS mutation, creates only the ACME challenge TXT, verifies it, and removes it by record ID in `finally`.

3. Start/restart LAN Service:

   `./scripts/Start-LanService.ps1 -TargetEnvironment BETA`

   If a valid protected PFX exists, the LAN Service loads it and starts HTTPS. Without a valid PFX, it remains `HTTP_READ_ONLY` and business mutation stays closed.

4. Only after staging + HTTPS startup validation, request the production certificate explicitly:

   `./scripts/Manage-LanCertificate.ps1 -Action IssueProduction -TargetEnvironment BETA -AcmeEmail <approved-email> -ConfirmProduction`

5. Restart LAN Service and perform company-Windows/browser and NLS-MT90/PDA acceptance.

## Security boundaries

- Do not paste the DNS token into source, GitHub, chat, logs, command-line parameters or screenshots.
- The package does not contain a Cloudflare token or production private key.
- The GitHub-hosted build must not generate the final laptop DPAPI PFX.
- DPAPI protected files are bound to the Windows user context that created them; copying them to another user/machine is not a supported recovery mechanism.
- A staging certificate is not publicly trusted production acceptance.
- A production certificate alone does not solve canonical offline LAN hostname resolution; that remains a separate V4 physical acceptance gate.
- Do not accept browser certificate-warning click-through as a production solution.

## Physical evidence still required

Source/CI package PASS does not replace testing on the intended company laptop/network and real PDA devices. Required later evidence includes canonical hostname reachability/trust, Wi-Fi/LAN reacquisition, restart behavior, >=60-minute Internet-cut Window 2, restoration reconciliation, load/soak and Owner UAT.
