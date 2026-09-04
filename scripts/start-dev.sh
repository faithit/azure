#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export PATH="$HOME/.dotnet:$PATH"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5432;Database=cloudops;Username=cloudops;Password=cloudops}"
export Jwt__Issuer="${Jwt__Issuer:-CloudOps.Api}"
export Jwt__Audience="${Jwt__Audience:-CloudOps.Web}"
export Jwt__Secret="${Jwt__Secret:-12345678901234567890123456789012}"
export VITE_API_URL="${VITE_API_URL:-http://localhost:5080/api}"
export CLOUDOPS_DEFAULT_ADMIN_EMAIL="${CLOUDOPS_DEFAULT_ADMIN_EMAIL:-admin@cloudops.local}"
export CLOUDOPS_DEFAULT_ADMIN_PASSWORD="${CLOUDOPS_DEFAULT_ADMIN_PASSWORD:-AdminPass123!}"

if command -v pg_ctlcluster >/dev/null 2>&1; then
  pg_ctlcluster 16 main start >/dev/null 2>&1 || true
fi

if command -v sudo >/dev/null 2>&1; then
  if ! sudo -u postgres psql -d postgres -tc "SELECT 1 FROM pg_roles WHERE rolname='cloudops';" | grep -q 1; then
    sudo -u postgres psql -d postgres -c "CREATE ROLE cloudops WITH LOGIN PASSWORD 'cloudops';"
  fi

  if ! sudo -u postgres psql -d postgres -tc "SELECT 1 FROM pg_database WHERE datname='cloudops';" | grep -q 1; then
    sudo -u postgres createdb -O cloudops cloudops
  fi
fi

if ! pgrep -f "dotnet run --project src/CloudOps.Api" >/dev/null 2>&1; then
  nohup bash -lc "cd '$ROOT_DIR' && export ASPNETCORE_ENVIRONMENT='${ASPNETCORE_ENVIRONMENT}' && export ConnectionStrings__DefaultConnection='${ConnectionStrings__DefaultConnection}' && export Jwt__Issuer='${Jwt__Issuer}' && export Jwt__Audience='${Jwt__Audience}' && export Jwt__Secret='${Jwt__Secret}' && export CLOUDOPS_DEFAULT_ADMIN_EMAIL='${CLOUDOPS_DEFAULT_ADMIN_EMAIL}' && export CLOUDOPS_DEFAULT_ADMIN_PASSWORD='${CLOUDOPS_DEFAULT_ADMIN_PASSWORD}' && dotnet run --project src/CloudOps.Api --urls http://localhost:5080" > /tmp/cloudops-api.log 2>&1 &
fi

if [ ! -d "$ROOT_DIR/frontend/node_modules" ]; then
  (cd "$ROOT_DIR/frontend" && npm install)
fi

if ! pgrep -f "vite.*--port 5173" >/dev/null 2>&1; then
  nohup bash -lc "cd '$ROOT_DIR/frontend' && export VITE_API_URL='${VITE_API_URL}' && npm run dev -- --host 0.0.0.0 --port 5173" > /tmp/cloudops-web.log 2>&1 &
fi

echo "CloudOps local services started."
echo "API: http://localhost:5080"
echo "Web: http://localhost:5173"
echo "Admin login: ${CLOUDOPS_DEFAULT_ADMIN_EMAIL} / ${CLOUDOPS_DEFAULT_ADMIN_PASSWORD}"
echo "Logs: /tmp/cloudops-api.log and /tmp/cloudops-web.log"
