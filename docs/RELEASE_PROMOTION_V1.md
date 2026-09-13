# RELEASE PROMOTION V1

Status: ACTIVE
Authority: `DECISIONS_V4.md`

STABLE is prepared as an environment during development but remains inactive for business traffic until explicit Owner approval.

Promotion uses the exact BETA release accepted by the Owner. If `main` has moved forward, promotion still uses the accepted release identity.

Promotion transfers release definitions and artifacts: source, business logic, schema/migrations, configuration definitions, API contracts, Web/APK/LAN builds and Google Gateway source.

Promotion does not copy BETA operational data into STABLE. BETA database rows, Sheet rows, Drive business files, LAN edge state/queues/logs and test data remain BETA-only.

An existing STABLE environment is upgraded against its own data using reviewed migrations. It is never replaced by a renamed or cloned BETA environment.

Canonical environment URLs:
- online BETA: `beta.supra.cc.cd`
- online STABLE: `supra.cc.cd`
- LAN BETA: `lan-beta.supra.cc.cd`
- LAN STABLE: `lan.supra.cc.cd`
