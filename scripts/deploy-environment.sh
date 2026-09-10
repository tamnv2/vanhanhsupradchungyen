#!/usr/bin/env bash
set -euo pipefail

logdir="${RUNNER_TEMP:-/tmp}/vhdchy-deploy-logs"
mkdir -p "$logdir"

./scripts/deploy-gas.sh >"$logdir/gas.log" 2>&1 &
pid_gas=$!
./scripts/deploy-cloudflare.sh >"$logdir/cloudflare.log" 2>&1 &
pid_cf=$!

set +e
wait "$pid_gas"; rc_gas=$?
wait "$pid_cf"; rc_cf=$?
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
