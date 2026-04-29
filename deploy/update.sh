#!/usr/bin/env bash
# One-command deploy on the Pi:
#   1. pull latest source
#   2. rebuild the Angular bundle (Caddy serves the dist/ directly)
#   3. rebuild the API image
#   4. (re)start postgres + api + caddy
#
# Run from the deploy/ directory, or via:
#   ssh pi@host "cd /opt/tennisclub/deploy && ./update.sh"

set -euo pipefail

cd "$(dirname "$0")"

if [[ ! -f .env ]]; then
  echo "→ deploy/.env missing. Copy .env.example, fill it, then re-run." >&2
  exit 1
fi

echo "→ pulling latest from origin/main"
git -C .. pull --ff-only

echo "→ building frontend (npm ci + production build)"
( cd ../frontend && npm ci && npm run build -- --configuration production )

echo "→ rebuilding api image"
docker compose build api

echo "→ deploying"
docker compose up -d

REV=$(git -C .. rev-parse --short HEAD)
echo "→ done. running revision $REV"
