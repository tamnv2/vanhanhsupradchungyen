# 2026-09-11 — Permission audit recovery authority 0.9.1

- PR #2 merged to `main`: `6c23099b4ff64bbc55699b2611ffa57020ed28b3`.
- Post-merge validation run `34611144635`: SUCCESS.
- Google CI OAuth minimum: `script.projects` + `script.deployments`.
- GAS runtime minimum: `spreadsheets` + `userinfo.email`.
- Cloudflare recovery token minimum: Account Workers Scripts Write + Account D1 Write.
- Recovery deploy fails closed if expected retained D1 is missing.
- BETA projection registry points to current workbook `17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ` and remains `PROVISIONED_NOT_LIVE`.
- Repository default GitHub Actions token remains read-only; LAN release elevates `contents: write` only at workflow level.
- No `beta` or `stable` ref moved. No provider deployment occurred.
