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
  docs/security/PERMISSION_AUDIT_2026-09-11.md
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

# Current identity/resource guard.
grep -Fq 'Source authority repo hiện hành: GitHub `tamnv2/vanhanhsupradchungyen`.' PROJECT_SCOPE.md
grep -Fq 'Google owner/runtime account hiện hành: `automation@supra.cc.cd`.' PROJECT_SCOPE.md
grep -Fq 'BACKUP DỰ ÁN CŨ PICK PACK 1291' PROJECT_SCOPE.md

# GAS runtime least-privilege guard: current foundation is Sheets + owner identity only.
grep -Fq 'https://www.googleapis.com/auth/spreadsheets' gateway/appsscript.json
grep -Fq 'https://www.googleapis.com/auth/userinfo.email' gateway/appsscript.json
for forbidden in \
  'https://www.googleapis.com/auth/drive' \
  'https://www.googleapis.com/auth/script.send_mail' \
  'https://www.googleapis.com/auth/script.external_request' \
  'https://www.googleapis.com/auth/script.scriptapp'; do
  if grep -Fq "$forbidden" gateway/appsscript.json; then
    echo "Unauthorized current GAS runtime scope reintroduced: $forbidden" >&2
    exit 1
  fi
done

# Recovery must never silently create a replacement D1.
if grep -Eq 'POST.+/d1/database|Created D1 database' scripts/deploy-cloudflare.sh; then
  echo 'Cloudflare recovery deploy can still create replacement D1.' >&2
  exit 1
fi
grep -Fq 'Refusing to create replacement' scripts/deploy-cloudflare.sh

# Projection registry must never point at the decommissioned-account workbook.
if grep -Fq '14gHSgWXP2QvtPmBD3CzFt3vQ6AED2hPSm_LxMGpXAKg' config/projections.beta.json; then
  echo 'Legacy Google Sheet ID reintroduced into current BETA projection registry.' >&2
  exit 1
fi
grep -Fq '17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ' config/projections.beta.json

echo 'Governance continuity and least-privilege validation PASS.'
