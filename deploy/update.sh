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

# ng build copies the source public/config.js into dist/ unchanged - that
# file points the dev server at localhost:5555. On the Pi we serve API +
# frontend from the same origin via Caddy, so overwrite with the prod
# default. Edit this file by hand if you ever need a different URL.
echo "→ pinning prod config.js (apiUrl=/api)"
cat > ../frontend/dist/frontend/browser/config.js <<'EOF'
window.TC_CONFIG = { apiUrl: '/api' };
EOF

echo "→ rebuilding api image"
docker compose build api

echo "→ deploying"
docker compose up -d

REV=$(git -C .. rev-parse --short HEAD)
echo "→ done. running revision $REV"
