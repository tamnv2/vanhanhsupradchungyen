#!/usr/bin/env bash
set -euo pipefail

required=(APP_ENV CF_ACCOUNT_ID CLOUDFLARE_API_TOKEN PUBLIC_HOST)
for v in "${required[@]}"; do
  [[ -n "${!v:-}" ]] || { echo "Missing $v" >&2; exit 2; }
done

case "$APP_ENV" in
  beta) worker_name="vhdchy-beta"; db_name="vhdchy-data-beta" ;;
  stable) worker_name="vhdchy-stable"; db_name="vhdchy-data-stable" ;;
  *) echo "Unsupported APP_ENV=$APP_ENV" >&2; exit 2 ;;
esac

auth=(-H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" -H 'Content-Type: application/json')
base="https://api.cloudflare.com/client/v4/accounts/$CF_ACCOUNT_ID"

list=$(curl -fsS "${auth[@]}" "$base/d1/database?name=$db_name")
db_id=$(jq -r --arg n "$db_name" '.result[]? | select(.name==$n) | (.uuid // .id)' <<<"$list" | head -n1)
if [[ -z "$db_id" ]]; then
  created=$(curl -fsS -X POST "${auth[@]}" -d "{\"name\":\"$db_name\"}" "$base/d1/database")
  db_id=$(jq -r '.result.uuid // .result.id // empty' <<<"$created")
fi
[[ -n "$db_id" ]] || { echo "Unable to resolve/create D1 database" >&2; exit 3; }

config="${RUNNER_TEMP:-/tmp}/wrangler.generated.toml"
cat > "$config" <<EOF
name = "$worker_name"
main = "worker/src/index.js"
compatibility_date = "2026-09-10"
account_id = "$CF_ACCOUNT_ID"
workers_dev = false

[vars]
APP_ENV = "$APP_ENV"

[[d1_databases]]
binding = "DB"
database_name = "$db_name"
database_id = "$db_id"

routes = [
  { pattern = "$PUBLIC_HOST", custom_domain = true }
]
EOF

CLOUDFLARE_API_TOKEN="$CLOUDFLARE_API_TOKEN" npx --yes wrangler@4 deploy --config "$config"

for i in 1 2 3 4 5 6; do
  if curl -fsS "https://$PUBLIC_HOST/health"; then
    echo
    echo "Cloudflare health PASS: $PUBLIC_HOST"
    exit 0
  fi
  sleep 10
done

echo "Health check failed after deploy: $PUBLIC_HOST" >&2
exit 4
