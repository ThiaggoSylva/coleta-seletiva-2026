#!/usr/bin/env bash
# Executa as verificações locais do backend, do frontend e da infraestrutura.
# Não sobe o banco; para isso, use prepare-db.sh.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_command dotnet
require_command npm

echo "== Backend =="
cd "$BACKEND_DIR"
dotnet restore ColetaSeletiva.slnx --locked-mode
dotnet build ColetaSeletiva.slnx --no-restore
dotnet test ColetaSeletiva.slnx --no-build --no-restore

echo "== Frontend =="
cd "$FRONTEND_DIR"
npm ci
npm run format:check
npm run build
npm test

echo "== Infraestrutura =="
load_local_env
if [[ -n "${MSSQL_SA_PASSWORD:-}" ]]; then
  cd "$INFRA_DIR"
  docker compose config --quiet
else
  echo "MSSQL_SA_PASSWORD não definida; validação do Compose ignorada." >&2
fi

echo "Verificações locais concluídas."
