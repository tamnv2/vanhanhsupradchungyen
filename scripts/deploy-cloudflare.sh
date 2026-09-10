#!/usr/bin/env bash
set -euo pipefail

required=(APP_ENV CF_ACCOUNT_ID CLOUDFLARE_API_TOKEN PUBLIC_HOST GAS_EXEC_URL)
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

config="$PWD/wrangler.generated.json"
trap 'rm -f "$config"' EXIT
jq -n \
  --arg name "$worker_name" \
  --arg account "$CF_ACCOUNT_ID" \
  --arg app_env "$APP_ENV" \
  --arg gas_exec "$GAS_EXEC_URL" \
  --arg build_sha "${GITHUB_SHA:-manual}" \
  --arg host "$PUBLIC_HOST" \
  --arg db_name "$db_name" \
  --arg db_id "$db_id" \
  '{
    name:$name,
    main:"worker/src/index.js",
    compatibility_date:"2026-09-10",
    account_id:$account,
    workers_dev:false,
    vars:{APP_ENV:$app_env,GAS_EXEC_URL:$gas_exec,BUILD_SHA:$build_sha},
    routes:[{pattern:$host,custom_domain:true}],
    d1_databases:[{
      binding:"DB",
      database_name:$db_name,
      database_id:$db_id,
      migrations_dir:"worker/migrations"
    }]
  }' > "$config"

CLOUDFLARE_API_TOKEN="$CLOUDFLARE_API_TOKEN" \
  npx --yes wrangler@4 d1 migrations apply "$db_name" --remote --config "$config"

CLOUDFLARE_API_TOKEN="$CLOUDFLARE_API_TOKEN" \
  npx --yes wrangler@4 deploy --config "$config"

last_payload=""
for i in $(seq 1 24); do
  payload=$(curl -sS --max-time 20 "https://$PUBLIC_HOST/health/deep" 2>/dev/null || true)
  last_payload="$payload"
  if [[ -n "$payload" ]] && jq -e --arg env "$APP_ENV" '.ok == true and .service == "VHDCHY_WORKER" and .environment == $env and .d1.ok == true and .d1.schemaVersion == "business_core_v1"' <<<"$payload" >/dev/null 2>&1; then
    meta=$(curl -sS --max-time 20 "https://$PUBLIC_HOST/api/v1/meta" 2>/dev/null || true)
    if [[ -n "$meta" ]] && jq -e --arg env "$APP_ENV" '.ok == true and .environment == $env and .schemaVersion == "business_core_v1" and .runtimeState == "BUSINESS_CORE_V1"' <<<"$meta" >/dev/null 2>&1; then
      echo "Cloudflare core health PASS: $PUBLIC_HOST"
      echo "D1 business core schema PASS: business_core_v1"
      echo "Worker meta PASS: BUSINESS_CORE_V1"
      if jq -e '.degraded == true' <<<"$payload" >/dev/null 2>&1; then
        echo "Integration advisory: Google Gateway is degraded/non-blocking; projection outbox remains authoritative for retry."
      else
        echo "Integration advisory: Google Gateway probe PASS."
      fi
      echo "D1 database id: $db_id"
      exit 0
    fi
  fi
  sleep 10
done

echo "Core health check failed after deploy: $PUBLIC_HOST" >&2
if [[ -n "$last_payload" ]]; then
  echo "Last core health payload:" >&2
  jq -c . <<<"$last_payload" >&2 2>/dev/null || printf '%s\n' "$last_payload" >&2
else
  echo "Last core health payload: <empty/unreachable>" >&2
fi
exit 4
