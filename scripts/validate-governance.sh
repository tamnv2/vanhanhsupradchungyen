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
  docs/GITHUB_ENV_SETUP.md
  docs/runbooks/SETUP_FROM_ZERO_2026-09-12.md
  docs/security/PERMISSION_AUDIT_2026-09-12.md
  docs/reference/pick-pack-1291/INDEX.md
)

for f in "${required[@]}"; do
  test -s "$f" || { echo "Missing/empty governance file: $f" >&2; exit 1; }
done

grep -Fq 'Status: ACTIVE / OWNER-APPROVED 2026-09-12' PROJECT_SCOPE.md
grep -Fq 'VẬN HÀNH DC HƯNG YÊN' PROJECT_SCOPE.md
grep -Fq 'tam95.supra@gmail.com' PROJECT_SCOPE.md
grep -Fq 'nguyenvantam050595@gmail.com' PROJECT_SCOPE.md
grep -Fq 'BACKUP PICK PACK 1291' PROJECT_SCOPE.md

grep -Fq 'tam95.supra@gmail.com' SERVICE_AUTHORITY.md
grep -Fq 'SUSPENDED_RECOVERY_CANDIDATE' SERVICE_AUTHORITY.md
grep -Fq 'tamnv2/vanhanhsupradchungyen' SERVICE_AUTHORITY.md
grep -Fq '1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk' SERVICE_AUTHORITY.md

grep -Fq 'SERVICE_AUTHORITY.md' AI_BOOTSTRAP.md
grep -Fq 'CURRENT_STATE.md' AI_BOOTSTRAP.md
grep -Fq 'NEXT_ACTIONS.md' AI_BOOTSTRAP.md
grep -Fq 'DECISIONS_INDEX.md' AI_BOOTSTRAP.md
grep -Fq 'SETUP-RESET-20260912-01' AI_BOOTSTRAP.md

grep -Fqi 'parallel' AI_OPERATING_CONTRACT.md
grep -Fq 'Checkpoint:' NEXT_ACTIONS.md
grep -Fq 'Checkpoint ID:' SESSION_CHECKPOINT.md
grep -Fq 'D-030' DECISIONS_INDEX.md
grep -Fq 'D-031' DECISIONS_INDEX.md

grep -Fq 'Pick Pack 1291 is evidence, not authority.' docs/reference/pick-pack-1291/INDEX.md

# Current projection registry after Phase 1 must point only to the verified current-account workbook.
python3 - <<'PY'
import json
p='config/projections.beta.json'
d=json.load(open(p, encoding='utf-8'))
assert d['owner_account'] == 'tam95.supra@gmail.com'
assert d['baseline'] == 'SETUP-RESET-20260912-01'
assert len(d['registrations']) == 1
r=d['registrations'][0]
assert r['cluster_id'] == 'PICK_PACK_1291'
assert r['spreadsheet_id'] == '1My2-jG6s8WCOAMox6DfGKKi9M0TBvSrfC2uE9xFNmmk'
assert r['status'] == 'PROVISIONED_NOT_LIVE'
assert r['schema_version'] == 'PP1291_SHEETS_BETA_V1'
assert r['tab_map']['accounts'] == 'DANH SÁCH TÀI KHOẢN'
PY

# Historical workbook IDs must never become current projection config again.
for forbidden in \
  '17lvVEdBno0TelhuZl3gmn7-YmyJ4o6wZxpsoP1YB9XQ' \
  '14gHSgWXP2QvtPmBD3CzFt3vQ6AED2hPSm_LxMGpXAKg'; do
  if grep -Fq "$forbidden" config/projections.beta.json; then
    echo "Historical Sheet ID reintroduced into current BETA projection registry: $forbidden" >&2
    exit 1
  fi
done

# GAS runtime least privilege.
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

echo 'Governance baseline 2026-09-12 Phase 1 + least-privilege validation PASS.'
