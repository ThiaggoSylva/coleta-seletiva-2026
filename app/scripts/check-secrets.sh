#!/usr/bin/env bash
# Verifica segredos versionados com gitleaks e confere que arquivos .env
# locais não foram adicionados ao Git. Nenhum valor é impresso.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_command docker
require_command git

REPO_ROOT="$(cd -- "$APP_DIR/.." && pwd)"
GITLEAKS_IMAGE="ghcr.io/gitleaks/gitleaks:v8.30.1"

if git -C "$REPO_ROOT" ls-files | grep -qE '(^|/)\.env$'; then
  echo "ERRO: arquivo .env versionado. Remova-o do índice do Git e use variáveis de ambiente." >&2
  exit 1
fi

if [[ -f "$INFRA_DIR/.env" ]]; then
  LOCAL_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' "$INFRA_DIR/.env" | tail -n 1 | cut -d= -f2- || true)"
  if [[ -n "$LOCAL_PASSWORD" ]] && git -C "$REPO_ROOT" grep -q -F -- "$LOCAL_PASSWORD"; then
    echo "ERRO: a senha local do SQL Server aparece em arquivo versionado." >&2
    exit 1
  fi
fi

if command -v dotnet >/dev/null 2>&1; then
  LOCAL_JWT_KEY="$(dotnet user-secrets list --project "$BACKEND_DIR/src/ColetaSeletiva.Api" 2>/dev/null | sed -n 's/^Jwt:SigningKey = //p' || true)"
  if [[ -n "$LOCAL_JWT_KEY" ]] && git -C "$REPO_ROOT" grep -q -F -- "$LOCAL_JWT_KEY"; then
    echo "ERRO: a chave JWT local aparece em arquivo versionado." >&2
    exit 1
  fi
fi

if command -v cygpath >/dev/null 2>&1; then
  HOST_REPO_ROOT="$(cygpath -w "$REPO_ROOT")"
else
  HOST_REPO_ROOT="$REPO_ROOT"
fi

echo "== Segredos versionados (gitleaks) =="
MSYS_NO_PATHCONV=1 docker run --rm \
  -v "${HOST_REPO_ROOT}:/repo" \
  -w /repo \
  "$GITLEAKS_IMAGE" \
  detect --source=/repo --redact --verbose

echo "Nenhum segredo versionado encontrado."
