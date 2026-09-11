#!/usr/bin/env bash
set -euo pipefail

required=(
  PROJECT_SCOPE.md
  SERVICE_AUTHORITY.md
  AI_BOOTSTRAP.md
  AI_OPERATING_CONTRACT.md
  CURRENT_STATE.md
  NEXT_ACTIONS.md
  SESSION_CHECKPOINT.md
  DECISIONS_INDEX.md
  DECISIONS.md
  CHANGELOG.md
  docs/AI_USAGE_GUIDE.md
  docs/runbooks/REAUTHORIZATION_2026-09-11.md
  docs/reference/pick-pack-1291/INDEX.md
)

for f in "${required[@]}"; do
  test -s "$f" || { echo "Missing/empty governance file: $f" >&2; exit 1; }
done

grep -Fq 'Status: ACTIVE / OWNER-APPROVED' PROJECT_SCOPE.md
grep -Fq 'VẬN HÀNH DC HƯNG YÊN' PROJECT_SCOPE.md
grep -Fq 'Pick Pack 1291 là evidence/reference, không phải authority của VHDCHY.' PROJECT_SCOPE.md
grep -Fq 'automation@supra.cc.cd' SERVICE_AUTHORITY.md
grep -Fq 'vanhanhdchungyen@gmail.com' SERVICE_AUTHORITY.md
grep -Fq 'DECOMMISSIONED' SERVICE_AUTHORITY.md
grep -Fq 'SERVICE_AUTHORITY.md' AI_BOOTSTRAP.md
grep -Fq 'CURRENT_STATE.md' AI_BOOTSTRAP.md
grep -Fq 'NEXT_ACTIONS.md' AI_BOOTSTRAP.md
grep -Fq 'DECISIONS_INDEX.md' AI_BOOTSTRAP.md
grep -Fq 'khoảng 20 phút' AI_OPERATING_CONTRACT.md
grep -Fqi 'chạy song song' AI_OPERATING_CONTRACT.md
grep -Fq 'Checkpoint:' NEXT_ACTIONS.md
grep -Fq 'Owner' NEXT_ACTIONS.md
grep -Fq 'Checkpoint ID:' SESSION_CHECKPOINT.md
grep -Fq 'Exact Owner actions required next' SESSION_CHECKPOINT.md
grep -Fq 'D-014' DECISIONS_INDEX.md
grep -Fq 'docs/changelog/' AI_OPERATING_CONTRACT.md
grep -Fq 'Pick Pack 1291 is evidence, not authority.' docs/reference/pick-pack-1291/INDEX.md

# Current identity/resource guard: stale runtime identity must not remain authoritative.
grep -Fq 'Source authority repo hiện hành: GitHub `tamnv2/vanhanhsupradchungyen`.' PROJECT_SCOPE.md
grep -Fq 'Google owner/runtime account hiện hành: `automation@supra.cc.cd`.' PROJECT_SCOPE.md
grep -Fq 'BACKUP DỰ ÁN CŨ PICK PACK 1291' PROJECT_SCOPE.md

# Current Apps Script manifest must not request send-mail while current gateway has no mail use case.
if grep -Fq 'https://www.googleapis.com/auth/script.send_mail' gateway/appsscript.json; then
  echo 'Unused Apps Script send-mail scope reintroduced.' >&2
  exit 1
fi

echo 'Governance continuity validation PASS.'
