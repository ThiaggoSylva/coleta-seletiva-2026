#!/usr/bin/env bash
# Funções compartilhadas pelos scripts locais da aplicação.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
INFRA_DIR="$APP_DIR/infra"
BACKEND_DIR="$APP_DIR/backend"
FRONTEND_DIR="$APP_DIR/frontend"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Comando obrigatório não encontrado: $1" >&2
    exit 1
  fi
}

load_local_env() {
  if [[ -f "$INFRA_DIR/.env" ]]; then
    if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
      MSSQL_SA_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' "$INFRA_DIR/.env" | tail -n 1 | cut -d= -f2-)"
    fi

    if [[ -z "${MSSQL_PORT:-}" ]]; then
      MSSQL_PORT="$(grep -E '^MSSQL_PORT=' "$INFRA_DIR/.env" | tail -n 1 | cut -d= -f2-)"
    fi
  fi

  export MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-}"
  export MSSQL_PORT="${MSSQL_PORT:-1433}"
}

require_db_password() {
  if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    echo "Defina MSSQL_SA_PASSWORD no ambiente ou em app/infra/.env." >&2
    exit 1
  fi
}

run_sql() {
  local query="$1"

  (
    cd "$INFRA_DIR"
    MSYS_NO_PATHCONV=1 docker compose exec -T sqlserver sh -c \
      '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "$1"' \
      _ "$query"
  )
}
