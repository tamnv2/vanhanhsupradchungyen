# VẬN HÀNH DC HƯNG YÊN — RESET ZERO

Project status: **DORMANT / 0%** as of 2026-09-15.

The previous implementation was reset because it did not meet the required delivery pace. Current `main` treats all pre-reset source, workflows, delivery plans, progress ledgers and decisions as **historical/inactive**, even where the files remain physically present for non-destructive reference.

External provider containers/resources were intentionally retained to avoid repeatedly creating Google Drive/Sheets/Google Cloud/GAS/OAuth/CI and Cloudflare resources. See:

- `AI_ENTRYPOINT.md` — mandatory future-chat bootstrap;
- `RESET_STATE.md` — exact reset boundary/evidence;
- `RESOURCE_MANIFEST.md` — preserved non-secret resource identities.

The exact repository state immediately before this reset is preserved at `archive/pre-reset-20260915` and is historical reference only. A future restart begins from 0% and may reuse preserved infrastructure only after live verification and new Owner-approved scope.
