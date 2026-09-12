# SECURITY

This repository is public.

## Rules

- Do not commit access credentials, refresh tokens, private keys, signing files, signing passwords, local environment files or generated deployment credentials.
- Use provider secret stores and GitHub Environments for runtime secrets after those environments are rebuilt.
- Keep default automation permissions minimal.
- Do not broaden Google or Cloudflare permissions without a current implemented feature that requires them.
- Treat the pre-zero snapshot as historical evidence, not active runtime configuration.

If sensitive material is ever committed, rotate/revoke it first; history cleanup alone is not sufficient remediation.
