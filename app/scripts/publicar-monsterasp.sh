#!/usr/bin/env bash
# Gera o pacote de publicação para o MonsterASP.NET: SPA em wwwroot + API em Release.
# Uso: app/scripts/publicar-monsterasp.sh [rid]
#   rid padrão: win-x86 (runtime usado pela hospedagem); use win-x64 se o site for 64 bits.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_command dotnet
require_command npm

RID="${1:-win-x86}"
SAIDA="$BACKEND_DIR/publish"
WWWROOT="$BACKEND_DIR/src/ColetaSeletiva.Api/wwwroot"
PACOTE="$BACKEND_DIR/coleta-seletiva-monsterasp.zip"

echo "== SPA (Angular)"
cd "$FRONTEND_DIR"
npm ci
npm run build

echo "== Copiando a SPA para wwwroot da API"
rm -rf "$WWWROOT"
mkdir -p "$WWWROOT"
cp -R "$FRONTEND_DIR/dist/frontend/browser/." "$WWWROOT/"

echo "== API (.NET, Release, $RID)"
cd "$BACKEND_DIR"
rm -rf "$SAIDA"
dotnet publish src/ColetaSeletiva.Api/ColetaSeletiva.Api.csproj \
  --configuration Release \
  --runtime "$RID" \
  --self-contained false \
  --output "$SAIDA"

echo "== Empacotando $PACOTE"
rm -f "$PACOTE"
if command -v zip >/dev/null 2>&1; then
  ( cd "$SAIDA" && zip -qr "$PACOTE" . )
else
  ( cd "$SAIDA" && python3 -m zipfile -c "$PACOTE" ./* )
fi

echo "Pacote pronto: $PACOTE"
ls -lh "$PACOTE"
