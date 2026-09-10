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
jq -e '.success == true' <<<"$list" >/dev/null
db_id=$(jq -r --arg n "$db_name" '.result[]? | select(.name==$n) | (.uuid // .id)' <<<"$list" | head -n1)

if [[ -z "$db_id" ]]; then
  create_body=$(jq -nc --arg name "$db_name" '{name:$name,primary_location_hint:"apac"}')
  created=$(curl -fsS -X POST "${auth[@]}" --data "$create_body" "$base/d1/database")
  jq -e '.success == true' <<<"$created" >/dev/null
  db_id=$(jq -r '.result.uuid // .result.id // empty' <<<"$created")
  echo "Created D1 database: $db_name (APAC location hint)"
else
  echo "Using existing D1 database: $db_name"
fi

[[ -n "$db_id" ]] || { echo "Unable to resolve/create D1 database" >&2; exit 3; }

config="${RUNNER_TEMP:-/tmp}/wrangler.generated.json"
jq -n \
  --arg name "$worker_name" \
  --arg account "$CF_ACCOUNT_ID" \
  --arg app_env "$APP_ENV" \
  --arg host "$PUBLIC_HOST" \
  --arg db_name "$db_name" \
  --arg db_id "$db_id" \
  '{
    name:$name,
    main:"worker/src/index.js",
    compatibility_date:"2026-09-10",
    account_id:$account,
    workers_dev:false,
    vars:{APP_ENV:$app_env},
    routes:[{pattern:$host,custom_domain:true}],
    d1_databases:[{binding:"DB",database_name:$db_name,database_id:$db_id}]
  }' > "$config"

CLOUDFLARE_API_TOKEN="$CLOUDFLARE_API_TOKEN" npx --yes wrangler@4 deploy --config "$config"

for i in $(seq 1 24); do
  payload=$(curl -fsS "https://$PUBLIC_HOST/health" 2>/dev/null || true)
  if [[ -n "$payload" ]] && jq -e --arg env "$APP_ENV" '.ok == true and .service == "VHDCHY_WORKER" and .environment == $env and .d1.ok == true' <<<"$payload" >/dev/null 2>&1; then
    echo "Cloudflare health PASS: $PUBLIC_HOST"
    echo "D1 database id: $db_id"
    exit 0
  fi
  sleep 10
done

echo "Health check failed after deploy: $PUBLIC_HOST" >&2
exit 4
