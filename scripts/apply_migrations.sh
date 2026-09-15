#!/usr/bin/env bash
set -euo pipefail

# apply_migrations.sh
# Usage: export DATABASE_URL='postgresql://...'
#        ./scripts/apply_migrations.sh
# This script runs the committed EF Core migrations against the database
# configured in DATABASE_URL. It intentionally avoids printing secrets.

if [ -z "${DATABASE_URL:-}" ]; then
  echo "ERROR: DATABASE_URL must be set in the environment (do not commit it)."
  exit 2
fi

echo "Applying EF Core migrations to the database configured in DATABASE_URL..."

dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api

echo "Migrations applied. Review the output above for any errors."

