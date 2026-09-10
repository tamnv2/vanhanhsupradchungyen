#!/usr/bin/env bash
set -euo pipefail

required=(APP_ENV OWNER_EMAIL GOOGLE_DRIVE_ENV_ROOT_ID GAS_SCRIPT_ID GAS_DEPLOYMENT_ID GAS_EXEC_URL GOOGLE_OAUTH_CLIENT_ID GOOGLE_OAUTH_CLIENT_SECRET GOOGLE_OAUTH_REFRESH_TOKEN)
for v in "${required[@]}"; do
  [[ -n "${!v:-}" ]] || { echo "Missing $v" >&2; exit 2; }
done

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
cp gateway/Code.gs "$work/Code.gs"
cp gateway/appsscript.json "$work/appsscript.json"

python3 - "$work/Code.gs" <<'PY'
import os, sys
p=sys.argv[1]
s=open(p, encoding='utf-8').read()
s=s.replace('__ENVIRONMENT__', os.environ['APP_ENV'].upper())
s=s.replace('__OWNER_EMAIL__', os.environ['OWNER_EMAIL'])
s=s.replace('__ENV_ROOT_FOLDER_ID__', os.environ['GOOGLE_DRIVE_ENV_ROOT_ID'])
open(p,'w',encoding='utf-8').write(s)
PY

token_json=$(curl -fsS https://oauth2.googleapis.com/token \
  -d client_id="$GOOGLE_OAUTH_CLIENT_ID" \
  -d client_secret="$GOOGLE_OAUTH_CLIENT_SECRET" \
  -d refresh_token="$GOOGLE_OAUTH_REFRESH_TOKEN" \
  -d grant_type=refresh_token)
access_token=$(jq -r '.access_token // empty' <<<"$token_json")
[[ -n "$access_token" ]] || { echo "OAuth refresh failed" >&2; exit 3; }

jq -n \
  --rawfile code "$work/Code.gs" \
  --rawfile manifest "$work/appsscript.json" \
  '{files:[{name:"Code",type:"SERVER_JS",source:$code},{name:"appsscript",type:"JSON",source:$manifest}]}' \
  > "$work/content.json"

content_response=$(curl -fsS -X PUT \
  -H "Authorization: Bearer $access_token" \
  -H 'Content-Type: application/json' \
  --data-binary @"$work/content.json" \
  "https://script.googleapis.com/v1/projects/$GAS_SCRIPT_ID/content")
[[ -n "$content_response" ]] || { echo "GAS content update returned empty response" >&2; exit 4; }

version_json=$(curl -fsS -X POST \
  -H "Authorization: Bearer $access_token" \
  -H 'Content-Type: application/json' \
  -d "{\"description\":\"CI ${APP_ENV} ${GITHUB_SHA:-manual}\"}" \
  "https://script.googleapis.com/v1/projects/$GAS_SCRIPT_ID/versions")
version_number=$(jq -r '.versionNumber // empty' <<<"$version_json")
[[ -n "$version_number" ]] || { echo "Create GAS version failed" >&2; exit 5; }

jq -n \
  --arg scriptId "$GAS_SCRIPT_ID" \
  --arg desc "CI ${APP_ENV} ${GITHUB_SHA:-manual}" \
  --argjson versionNumber "$version_number" \
  '{deploymentConfig:{scriptId:$scriptId,versionNumber:$versionNumber,manifestFileName:"appsscript",description:$desc}}' \
  > "$work/deployment.json"

deploy_response=$(curl -fsS -X PUT \
  -H "Authorization: Bearer $access_token" \
  -H 'Content-Type: application/json' \
  --data-binary @"$work/deployment.json" \
  "https://script.googleapis.com/v1/projects/$GAS_SCRIPT_ID/deployments/$GAS_DEPLOYMENT_ID")
jq -e --arg id "$GAS_DEPLOYMENT_ID" '.deploymentId == $id' <<<"$deploy_response" >/dev/null

echo "GAS deployment updated: env=$APP_ENV version=$version_number"

expected_env="${APP_ENV^^}"
for i in $(seq 1 18); do
  payload=$(curl -fsSL "$GAS_EXEC_URL" 2>/dev/null || true)
  if [[ -n "$payload" ]] && jq -e --arg env "$expected_env" --arg sid "$GAS_SCRIPT_ID" '.ok == true and .environment == $env and .scriptId == $sid' <<<"$payload" >/dev/null 2>&1; then
    echo "GAS endpoint PASS: $APP_ENV"
    exit 0
  fi
  sleep 5
done

echo "GAS endpoint validation failed after deployment" >&2
exit 6
