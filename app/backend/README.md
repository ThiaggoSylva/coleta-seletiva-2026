# Backend

API ASP.NET Core 10 em Clean Architecture (`Domain`, `Application`,
`Infrastructure` e `Api`). A solução usa `ColetaSeletiva.slnx`, versões NuGet
centralizadas, lockfiles e warnings como erro.

## Pré-requisitos

- .NET SDK 10.0.4xx (`global.json`)
- SQL Server local (`app/infra/compose.yaml`) para migrations e testes de
  integração
- Bash (Git Bash/WSL) para os scripts de `app/scripts`

## Comandos

```bash
# a partir de app/backend
dotnet restore ColetaSeletiva.slnx --locked-mode
dotnet build ColetaSeletiva.slnx --no-restore
dotnet test ColetaSeletiva.slnx --no-build --no-restore
```

## Configuração obrigatória

A API valida a configuração antes de começar a ouvir. A ausência ou invalidade
de qualquer item abaixo encerra o processo com uma mensagem que não expõe
segredos.

| Chave | Variável de ambiente | Regra |
| --- | --- | --- |
| `ConnectionStrings:ColetaSeletiva` | `ConnectionStrings__ColetaSeletiva` | Obrigatória e não vazia |
| `Jwt:SigningKey` | `Jwt__SigningKey` | Obrigatória, com pelo menos 32 bytes |
| `Jwt:Issuer` | `Jwt__Issuer` | Obrigatória; o valor local padrão está em `appsettings.json` |
| `Jwt:Audience` | `Jwt__Audience` | Obrigatória; o valor local padrão está em `appsettings.json` |

`appsettings.json` contém apenas valores públicos e placeholders vazios. Nunca
grave connection strings, senhas ou chaves de assinatura no repositório.

## User-secrets no desenvolvimento

O `UserSecretsId` já está no projeto da API. Os valores ficam no perfil do
desenvolvedor, fora do repositório.

```bash
# a partir de app/backend
dotnet user-secrets set "ConnectionStrings:ColetaSeletiva" \
  "Server=127.0.0.1,1433;Database=ColetaSeletiva;User Id=sa;Password=<senha-local>;Encrypt=True;TrustServerCertificate=True;" \
  --project src/ColetaSeletiva.Api

dotnet user-secrets set "Jwt:SigningKey" \
  "$(openssl rand -base64 48)" \
  --project src/ColetaSeletiva.Api
```

Em ambientes sem `openssl`, gere a chave com Node.js:

```bash
dotnet user-secrets set "Jwt:SigningKey" \
  "$(node -e "console.log(require('crypto').randomBytes(48).toString('base64url'))")" \
  --project src/ColetaSeletiva.Api
```

Use a mesma senha definida em `app/infra/.env` para o SQL Server local. O
comando `dotnet user-secrets list` mostra os valores; evite executá-lo em
terminais compartilhados ou em logs de CI.

## Variáveis de ambiente na CI

Defina os valores como segredos da plataforma de CI:

```text
ConnectionStrings__ColetaSeletiva
Jwt__SigningKey
Jwt__Issuer
Jwt__Audience
```

Nenhum workflow deve imprimir esses valores. Os arquivos versionados de exemplo
contêm apenas placeholders.

## Execução local

```bash
# garante o banco local e aplica migrations pendentes
bash ../scripts/apply-migrations.sh

# inicia a API (perfil http do launchSettings)
dotnet run --project src/ColetaSeletiva.Api
```

A API expõe `GET /health` e a especificação OpenAPI apenas para desenvolvimento.
O healthcheck não lê dados pessoais nem retorna configuração sensível.
