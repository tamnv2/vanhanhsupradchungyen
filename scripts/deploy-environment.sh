#!/usr/bin/env bash
set -euo pipefail

DEPLOY_GAS="${DEPLOY_GAS:-true}"
DEPLOY_CLOUDFLARE="${DEPLOY_CLOUDFLARE:-true}"
logdir="${RUNNER_TEMP:-/tmp}/vhdchy-deploy-logs"
mkdir -p "$logdir"

pid_gas=""
pid_cf=""

if [[ "$DEPLOY_GAS" == "true" ]]; then
  ./scripts/deploy-gas.sh >"$logdir/gas.log" 2>&1 &
  pid_gas=$!
else
  echo "GAS deploy skipped: no relevant change" >"$logdir/gas.log"
fi

if [[ "$DEPLOY_CLOUDFLARE" == "true" ]]; then
  ./scripts/deploy-cloudflare.sh >"$logdir/cloudflare.log" 2>&1 &
  pid_cf=$!
else
  echo "Cloudflare deploy skipped: no relevant change" >"$logdir/cloudflare.log"
fi

rc_gas=0
rc_cf=0
set +e
[[ -z "$pid_gas" ]] || wait "$pid_gas"; rc_gas=$?
[[ -z "$pid_cf" ]] || wait "$pid_cf"; rc_cf=$?
set -e

echo '===== GAS ====='
cat "$logdir/gas.log"
echo '===== CLOUDFLARE ====='
cat "$logdir/cloudflare.log"

if (( rc_gas != 0 || rc_cf != 0 )); then
  echo "Deploy failed: GAS=$rc_gas Cloudflare=$rc_cf" >&2
  exit 1
fi

echo "Environment deploy PASS"
