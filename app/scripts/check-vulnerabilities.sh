#!/usr/bin/env bash
# Verifica vulnerabilidades conhecidas nos pacotes NuGet e npm.
# Requer que as dependências já tenham sido restauradas/instaladas.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_command dotnet
require_command node
require_command npm

TEMP_FILE="$(mktemp)"
trap 'rm -f "$TEMP_FILE"' EXIT

echo "== Vulnerabilidades NuGet =="
cd "$BACKEND_DIR"
dotnet list ColetaSeletiva.slnx package --vulnerable --include-transitive --format json > "$TEMP_FILE"

node - "$TEMP_FILE" <<'NODE'
const fs = require('node:fs');

const data = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const findings = [];

for (const project of data.projects ?? []) {
  for (const framework of project.frameworks ?? []) {
    for (const group of ['topLevelPackages', 'transitivePackages']) {
      for (const pkg of framework[group] ?? []) {
        for (const vulnerability of pkg.vulnerabilities ?? []) {
          findings.push(
            `${project.path}: ${pkg.id}@${pkg.resolvedVersion} ${JSON.stringify(vulnerability)}`,
          );
        }
      }
    }
  }
}

if (findings.length > 0) {
  console.error('Vulnerabilidades conhecidas encontradas:');
  for (const finding of findings) {
    console.error(`- ${finding}`);
  }
  process.exit(1);
}

console.log(`Nenhuma vulnerabilidade conhecida nos pacotes NuGet (${data.projects?.length ?? 0} projetos).`);
NODE

echo "== Vulnerabilidades npm =="
cd "$FRONTEND_DIR"
npm audit --audit-level=high

echo "Nenhuma vulnerabilidade de severidade alta ou crítica encontrada."
