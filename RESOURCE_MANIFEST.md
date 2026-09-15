# VHDCHY PRESERVED RESOURCE MANIFEST

Status: `PRESERVED_FOR_FUTURE_RESTART`
Reset protocol: `VHDCHY_RESET_ZERO_V1`

This file records non-secret resource identities only. It is not evidence that a provider resource is currently live/healthy. Verify provider state read-only before any future mutation.

## GitHub

- Repository: `tamnv2/vanhanhsupradchungyen`
- Authority/default branch after reset: `main`
- Pre-reset archive branch: `archive/pre-reset-20260915`
- Pre-reset exact commit: `1ec5a5e89dac6afc41d778d059d760131adca89f`
- Older reference branch retained by history: `backup/pre-zero-20260912`

## Google ownership

- Drive / Sheets / GAS owner/operator: `tam95.supra@gmail.com`
- Do not automatically transfer ownership or recreate resources on restart.

## Google Drive

Project root:
- `VẬN HÀNH DC HƯNG YÊN` — `19r3s_kTjzncRdzffNntcePW5YZQ5Dxuh`

Environment roots:
- BETA `01_BETA` — `1EpUI49xbFUtgzR3mh3M0EQu7qYYsswB5`
- STABLE `02_STABLE` — `1c6RNTOHOzaX6GrQFOEd64h9rndPoeiFI`

BETA cluster:
- `01_BETA/01_CLUSTERS/PICK_PACK_1291` — `1-Z4D2_ja1uFqJP659B3nU8S9-ObWR0zN`

Reference-only folders retained outside active reset authority:
- `BACKUP PICK PACK 1291` — `1dQ8dYH3zi3MlPsVjNdRd1ckKzoIfYa2h`
- `VHDCHY_LEGACY_SETUP_20260908` — `1NysNtmsMAxFA5JgsYwEJNgMVvbogvf9R`

## Google Sheets

BETA projection workbook:
- Title: `VHDCHY BETA - PICK PACK 1291 - 2026 Q3`
- Spreadsheet ID: `1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk`
- Retained schema shell: `PP1291_SHEETS_BETA_V1`
- Reset state: business rows verified empty; headers + `00_CONTROL` metadata retained.

## Google Cloud / OAuth / Apps Script

- Google Cloud project: `VHDCHY-BETA`
- OAuth app: External / In production
- CI OAuth client label: `VHDCHY BETA CI`
- Apps Script title: `VHDCHY BETA - Google Gateway`
- Apps Script ID: `11jvFS3xBRrl3hmZveMbQP7no_hNw50TmOA0zFrAUJtedal0FrMn2sfnQ`
- Managed deployment ID: `AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ`
- Managed Web App URL: `https://script.google.com/macros/s/AKfycbzxRzxjeFyPpYQ39T3MJRL_sSKrhJhHXLY5LgGy16CnxuPEIFoJo8vr9XijrsxZttRtjQ/exec`

Do not recreate these resources during bootstrap. Their current configuration/liveness must be re-verified when the project is restarted.

## Cloudflare

Managing identity: `nguyenvantam050595@gmail.com`

BETA resources retained:
- Account ID: `1b1695e4f2a3abfe08dc475b352c7f42`
- Worker: `vhdchy-beta`
- Origin: `https://beta.supra.cc.cd`
- D1 database: `vhdchy-data-beta`
- D1 database ID: `37eb7d59-05c0-4ba2-8162-cb6a9fe5d492`
- Last known schema marker before reset: `business_core_v3`

Reserved/preparatory STABLE names retained:
- Worker: `vhdchy-stable`
- D1: `vhdchy-data-stable`

Do not recreate or migrate these resources from name alone. First verify exact provider identity/state on restart.

## Secrets

No provider secret value belongs in this repository. Existing secrets remain only in their provider/GitHub secret stores. A future restart must reuse or deliberately rotate them through the provider secret store; never copy them into chat/source.
