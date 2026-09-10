#!/usr/bin/env bash
set -euo pipefail

required=(
  PROJECT_SCOPE.md
  AI_BOOTSTRAP.md
  AI_OPERATING_CONTRACT.md
  CURRENT_STATE.md
  NEXT_ACTIONS.md
  SESSION_CHECKPOINT.md
  DECISIONS_INDEX.md
  DECISIONS.md
  CHANGELOG.md
  docs/AI_USAGE_GUIDE.md
  docs/reference/pick-pack-1291/INDEX.md
)

for f in "${required[@]}"; do
  test -s "$f" || { echo "Missing/empty governance file: $f" >&2; exit 1; }
done

grep -Fq 'Status: ACTIVE / OWNER-APPROVED' PROJECT_SCOPE.md
grep -Fq 'VẬN HÀNH DC HƯNG YÊN' PROJECT_SCOPE.md
grep -Fq 'Pick Pack 1291 là evidence/reference, không phải authority của VHDCHY.' PROJECT_SCOPE.md
grep -Fq 'PROJECT_SCOPE.md' AI_BOOTSTRAP.md
grep -Fq 'CURRENT_STATE.md' AI_BOOTSTRAP.md
grep -Fq 'NEXT_ACTIONS.md' AI_BOOTSTRAP.md
grep -Fq 'DECISIONS_INDEX.md' AI_BOOTSTRAP.md
grep -Fq 'khoảng 20 phút' AI_OPERATING_CONTRACT.md
grep -Fq 'Chạy song song' AI_OPERATING_CONTRACT.md || grep -Fq 'chạy song song' AI_OPERATING_CONTRACT.md
grep -Fq 'Checkpoint:' NEXT_ACTIONS.md
grep -Fq 'Owner action' NEXT_ACTIONS.md
grep -Fq 'Checkpoint ID:' SESSION_CHECKPOINT.md
grep -Fq 'Exact next action' SESSION_CHECKPOINT.md
grep -Fq 'D-014' DECISIONS_INDEX.md
grep -Fq 'docs/changelog/' AI_OPERATING_CONTRACT.md
grep -Fq 'Pick Pack 1291 is evidence, not authority.' docs/reference/pick-pack-1291/INDEX.md

echo 'Governance continuity validation PASS.'
