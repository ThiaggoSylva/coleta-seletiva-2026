#!/usr/bin/env bash
# Aplica as migrations pendentes no banco local.
# Idempotente: o EF Core só executa migrations ainda não registradas.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_command dotnet
load_local_env
require_db_password

bash "$SCRIPT_DIR/prepare-db.sh"

MIGRATIONS_DIR="$BACKEND_DIR/src/ColetaSeletiva.Infrastructure/Migrations"
if ! compgen -G "$MIGRATIONS_DIR/*.cs" >/dev/null; then
  echo "Nenhuma migration encontrada; nada a aplicar."
  exit 0
fi

export ConnectionStrings__ColetaSeletiva="${ConnectionStrings__ColetaSeletiva:-Server=127.0.0.1,${MSSQL_PORT};Database=ColetaSeletiva;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;}"

cd "$BACKEND_DIR"
dotnet tool restore
dotnet ef database update \
  --project src/ColetaSeletiva.Infrastructure/ColetaSeletiva.Infrastructure.csproj \
  --startup-project src/ColetaSeletiva.Api/ColetaSeletiva.Api.csproj
