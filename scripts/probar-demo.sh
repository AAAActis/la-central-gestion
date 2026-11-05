#!/usr/bin/env bash
# Levanta la API contra lacentral_demo y el frontend. Ctrl+C corta los dos.
# Uso, desde la raíz del repo: scripts/probar-demo.sh   (ver docs/sprint05/COMO-PROBARLO.md)
set -euo pipefail
cd "$(dirname "$0")/.."

: "${LACENTRAL_DEMO_PG:?Falta LACENTRAL_DEMO_PG, la cadena de conexión a lacentral_demo. Ver docs/sprint05/COMO-PROBARLO.md}"

# La demo se prueba escribiendo: nunca contra lacentral (datos reales) ni lacentral_test.
if ! grep -qiE '(^|;)\s*Database\s*=\s*lacentral_demo\s*(;|$)' <<<"$LACENTRAL_DEMO_PG"; then
  echo "LACENTRAL_DEMO_PG no apunta a Database=lacentral_demo. No arranco." >&2
  exit 1
fi

[ -d frontend/node_modules ] || (cd frontend && npm ci)

export ConnectionStrings__Postgres="$LACENTRAL_DEMO_PG"
export Jwt__Key="$(head -c 48 /dev/urandom | base64 -w0)"
export ASPNETCORE_ENVIRONMENT=Development

dotnet run --project backend/LaCentral.Api --urls http://localhost:5181 &
api=$!
trap 'kill "$api" 2>/dev/null' EXIT

(cd frontend && npx ng serve --port 4200)
