#!/usr/bin/env bash
# Sobe o SQL Server local e garante o banco ColetaSeletiva.
# Seguro para executar várias vezes: o volume e o banco são preservados.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_command docker
load_local_env
require_db_password

cd "$INFRA_DIR"
docker compose config --quiet
docker compose up -d --wait --wait-timeout 240

run_sql "IF DB_ID(N'ColetaSeletiva') IS NULL EXEC(N'CREATE DATABASE [ColetaSeletiva]');"
run_sql "IF DB_ID(N'ColetaSeletiva') IS NULL BEGIN RAISERROR(N'Banco ColetaSeletiva nao foi criado.', 16, 1); END;"

echo "Banco ColetaSeletiva disponível em 127.0.0.1:${MSSQL_PORT}."
