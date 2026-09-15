# AI ENTRYPOINT — VHDCHY

Protocol: `VHDCHY_RESET_ZERO_V1`
Status: `DORMANT_RESET_ZERO`
Owner reset decision: 2026-09-15
Repository: `tamnv2/vanhanhsupradchungyen`
Authority branch: `main`

## Current authority

This `main` branch is the current persistent authority after the 2026-09-15 reset.

Project state is intentionally reset to **0%**:
- no active implementation scope;
- no active WIP or delivery queue;
- no pre-reset source/decision baseline is active;
- no BETA/STABLE promotion state;
- all pre-reset source, docs, workflows, decisions and checkpoints that remain physically in the repository are **HISTORICAL / INACTIVE** and must not be resumed automatically.

The exact pre-reset repository state is also preserved at branch `archive/pre-reset-20260915`, commit `1ec5a5e89dac6afc41d778d059d760131adca89f`. It is NON_AUTHORITY unless the Owner explicitly requests review/reuse of a specific item.

## Bootstrap

For any future VHDCHY resume/restart in a fresh chat:
1. live-fetch this file from GitHub `main`;
2. fetch `RESET_STATE.md`;
3. fetch `RESOURCE_MANIFEST.md`;
4. obtain current `main` HEAD and confirm these reset files are still current;
5. treat project progress as 0% and do not resume pre-reset WIP from memory/history or from still-present historical source;
6. do not create or recreate Google Drive folders, Google Sheets, Google Cloud projects, OAuth/CI clients, Apps Script projects/deployments, Cloudflare Worker/D1 resources, or replacement CI merely because the project is restarting;
7. first verify and reuse the preserved resource identities in `RESOURCE_MANIFEST.md` where still valid;
8. inspect existing GitHub Actions/workflow definitions only as historical material; do not run/re-enable/recreate CI until new scope requires it and its triggers/provider writes are reviewed;
9. only after a new explicit Owner instruction establishes new scope/architecture may implementation begin again from zero.

## Anti-spam / resource preservation rule

The reset deliberately preserves provider containers/resources so a future restart does not repeatedly create Google/Drive/Sheets/Cloud/CI resources. No automated provisioning or provider mutation is part of bootstrap.

## Memory/history rule

Memory, previous chats, old branches, old PRs, old releases, old source on `main`, and `archive/pre-reset-20260915` are NON_AUTHORITY. They may be consulted only as reference when the Owner explicitly asks to reuse or compare something.
