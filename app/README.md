# Aplicação — Coleta Seletiva

Este diretório contém a aplicação da plataforma. A matriz abaixo foi verificada em
2026-11-10, antes da criação dos projetos, conforme a tarefa 1.1 da mudança
`f1-fundacao-segura`.

## Pré-requisitos locais

| Ferramenta | Versão verificada | Compatibilidade |
| --- | --- | --- |
| .NET SDK | 10.0.401 | Runtime instalado: 10.0.12; `global.json` fixará a faixa 10.0.4xx |
| Node.js | 24.21.0 | Angular CLI 22.2.6 exige `^22.22.3 \|\| ^24.15.0 \|\| >=26.0.0` |
| npm | 12.2.1 | Angular CLI 22.2.6 exige `^6.11.0 \|\| ^7.5.6 \|\| >=8.0.0` |
| Docker | 29.7.2 | Necessário para SQL Server e Testcontainers |
| Docker Compose | v5.4.0 | Usado apenas para o SQL Server local |

## Matriz de versões

### Backend

| Pacote ou componente | Versão | Justificativa e compatibilidade |
| --- | --- | --- |
| .NET | 10.0.401 (SDK) | Faixa LTS instalada; os pacotes abaixo são `net10.0` |
| ASP.NET Core Identity + EF Core | 10.0.12 | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` e `Microsoft.EntityFrameworkCore` 10.0.12 são `net10.0` |
| EF Core SQL Server | 10.0.12 | Mesma versão do EF Core; migrations e provider alinhados |
| EF Core Design | 10.0.12 | Ferramenta de migrations alinhada ao runtime |
| JWT Bearer | 10.0.12 | Pacote `Microsoft.AspNetCore.Authentication.JwtBearer` para `net10.0` |
| OpenAPI | 10.0.12 | Pacote `Microsoft.AspNetCore.OpenApi` para `net10.0` |
| MVC Testing | 10.0.12 | `Microsoft.AspNetCore.Mvc.Testing` para integração com `WebApplicationFactory` |
| FluentResults | 4.0.0 | Biblioteca de resultados adotada em DT-12 |
| FluentValidation | 12.2.0 | Validação de DTOs; `FluentValidation.DependencyInjectionExtensions` 12.2.0 |
| Serilog.AspNetCore | 10.0.0 | Possui destino `net10.0`; logging estruturado de DT-18 |
| Serilog.Sinks.Console | 6.1.1 | Destino de console para desenvolvimento e CI |
| MSTest | 4.5.2 | Framework de testes de DT-17; compatível via ativo `net9.0` no `net10.0` |
| Microsoft.NET.Test.Sdk | 18.11.1 | Executor de testes; compatível com MSTest 4.5.2 |
| Moq | 4.21.0 | Dublês de teste de aplicação; ativo `net6.0` compatível com `net10.0` |
| Testcontainers.MsSql | 4.16.0 | Possui ativo `net10.0`; SQL Server real nos testes de integração |
| System.Security.Cryptography.Pkcs | 10.0.15 | Pinagem transitiva de segurança; o SqlClient do EF Core fixa 9.0.11, afetado por CVE-2026-84008 e CVE-2026-70162 |
| SQL Server | 2022 Developer | Imagem `mcr.microsoft.com/mssql/server:2022-latest`, com `MSSQL_PID=Developer` |

### Frontend

| Pacote ou componente | Versão | Justificativa e compatibilidade |
| --- | --- | --- |
| Angular | 22.2.5 | Linha estável de DT-01; standalone e zoneless |
| Angular CLI / build | 22.2.6 | Compatível com Angular 22.2.5 |
| TypeScript | 6.0.3 | `@angular/compiler-cli` 22.2.5 exige `>=6.0 <6.1`; a versão 7.0.2 não é compatível |
| RxJS | 7.8.2 | `@angular/core` 22.2.5 aceita `^6.5.3 \|\| ^7.4.0` |
| Tailwind CSS | 4.3.5 | DT-02; `@angular/build` aceita `^4.0.0` |
| @tailwindcss/postcss | 4.3.5 | Plugin PostCSS da mesma versão do Tailwind |
| PostCSS | 8.5.32 | `@angular/build` exige `^8.4.0` |
| Vitest | 5.1.0 | `@angular/build` 22.2.6 aceita `^4.0.8 \|\| ^5.0.0` |
| jsdom | 31.0.0 | Ambiente DOM dos testes de componente |
| @playwright/test | 1.65.0 | Testes E2E de DT-17 |

## Incompatibilidades resolvidas

- **TypeScript 7.0.2:** é a versão `latest` no npm, mas o compilador do Angular 22
  aceita somente TypeScript 6.0.x. A aplicação fixa **6.0.3**.
- **EF Core 11.0.0:** é a versão `latest` no NuGet, mas o pacote exige `net11.0`.
  Como o SDK disponível é o .NET 10, a aplicação fixa os pacotes Microsoft em
  **10.0.12**, incluindo EF Core, Identity, JWT, OpenAPI e MVC Testing.
- **Zone.js:** não será instalado; a SPA é zoneless, conforme DT-01.
- **Pkcs transitivo:** o `Microsoft.Data.SqlClient` 6.1.6, usado pelo EF Core 10,
  fixa `System.Security.Cryptography.Pkcs` 9.0.11, que possui vulnerabilidades
  conhecidas. A aplicação fixa a versão corrigida **10.0.15** por pinagem
  transitiva central e mantém o SqlClient 6.1.6.
- **Versões flutuantes:** `global.json`, `Directory.Packages.props`,
  `package.json`/`package-lock.json` e os lockfiles .NET fixarão as versões
  efetivamente usadas. Atualizações exigirão uma mudança explícita.

## Fontes consultadas

- Versões do Angular: <https://angular.dev/reference/versions>
- Metadados do Angular CLI: <https://www.npmjs.com/package/@angular/cli>
- Metadados do compilador Angular: <https://www.npmjs.com/package/@angular/compiler-cli>
- .NET 10: <https://dotnet.microsoft.com/download/dotnet/10.0>
- EF Core 10: <https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.12>
- Identity com EF Core: <https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore/10.0.12>
- JWT Bearer: <https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.12>
- OpenAPI: <https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.12>
- MVC Testing: <https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.12>
- FluentResults: <https://www.nuget.org/packages/FluentResults/4.0.0>
- FluentValidation: <https://www.nuget.org/packages/FluentValidation/12.2.0>
- Serilog.AspNetCore: <https://www.nuget.org/packages/Serilog.AspNetCore/10.0.0>
- MSTest: <https://www.nuget.org/packages/MSTest/4.5.2>
- Moq: <https://www.nuget.org/packages/Moq/4.21.0>
- Testcontainers para SQL Server: <https://www.nuget.org/packages/Testcontainers.MsSql/4.16.0>
- Testcontainers — módulo MSSQL: <https://dotnet.testcontainers.org/modules/mssql/>
- SQL Server 2022: <https://mcr.microsoft.com/en-us/artifact/mar/mssql/server/tags>
- Tailwind CSS: <https://tailwindcss.com/docs>
- Vitest: <https://vitest.dev/>
- Playwright: <https://playwright.dev/docs/locators>

## Verificação da matriz

Os comandos usados para validar a matriz foram:

```bash
dotnet --info
node --version
npm --version
docker --version
docker compose version
npm view @angular/compiler-cli@22.2.5 peerDependencies --json
npm view @angular/build@22.2.6 peerDependencies --json
npm view @angular/cli@22.2.6 engines --json
npm view typescript@6.0 version --json
npm view @tailwindcss/postcss version
dotnet package search Microsoft.EntityFrameworkCore --take 3 --format json
dotnet package search Microsoft.AspNetCore.Identity.EntityFrameworkCore --take 3 --format json
dotnet package search Testcontainers.MsSql --take 3 --format json
dotnet package search MSTest --take 5 --format json
dotnet package search Moq --take 3 --format json
```

Os metadados de `targetFramework` foram conferidos nos arquivos `.nuspec` dos
pacotes no NuGet. Nenhuma versão foi presumida sem consulta.

## Execução local

Pré-requisitos: .NET SDK 10.0.4xx, Node.js e npm compatíveis, Docker em execução
e Bash (Git Bash/WSL no Windows).

```bash
# 1. Banco local e migrations
cp app/infra/.env.example app/infra/.env
# edite MSSQL_SA_PASSWORD em app/infra/.env
bash app/scripts/apply-migrations.sh

# 2. Configuração da API (uma vez por ambiente de desenvolvimento)
cd app/backend
dotnet user-secrets set "ConnectionStrings:ColetaSeletiva" \
  "Server=127.0.0.1,1433;Database=ColetaSeletiva;User Id=sa;Password=<senha-local>;Encrypt=True;TrustServerCertificate=True;" \
  --project src/ColetaSeletiva.Api
dotnet user-secrets set "Jwt:SigningKey" \
  "$(node -e "console.log(require('crypto').randomBytes(48).toString('base64url'))")" \
  --project src/ColetaSeletiva.Api

# 3. API em um terminal
cd app/backend
dotnet run --project src/ColetaSeletiva.Api

# 4. SPA em outro terminal
cd app/frontend
npm ci
npm start
```

Acesse `http://localhost:4200`. O proxy de `app/frontend/proxy.conf.json`
encaminha `/api` para `http://localhost:5012`; para o navegador, SPA e API usam
a mesma origem. O cookie de refresh permanece `HttpOnly`, `Secure`, com
`SameSite=Strict` e `Path=/api/v1/auth`, e a renovação ocorre sem expor o token
ao JavaScript. O perfil `http` da API usa a origem permitida
`http://localhost:4200` em `appsettings.Development.json`.

### Testes E2E

Com o banco preparado e as credenciais da API configuradas:

```bash
cd app/e2e
npm ci
npx playwright install chromium
npm test
```

O Playwright inicia a API e a SPA em processos próprios, em
`http://localhost:5012` e `http://localhost:4200`. Não mantenha esses processos
em execução durante os testes. A suíte usa dados sintéticos únicos e eleva os
limites de taxa apenas no ambiente de teste; os limites reais continuam
cobertos pelos testes de integração.

Para comprovar a execução em um banco limpo sem afetar o ambiente de
desenvolvimento, use um projeto Compose e uma porta isolados:

```bash
export COMPOSE_PROJECT_NAME=coleta-e2e
export MSSQL_PORT=14330
bash app/scripts/apply-migrations.sh

export ConnectionStrings__ColetaSeletiva="Server=127.0.0.1,14330;Database=ColetaSeletiva;User Id=sa;Password=<senha-local>;Encrypt=True;TrustServerCertificate=True;"
export Jwt__SigningKey="<chave-efemera-com-32-bytes-ou-mais>"
cd app/e2e
npm test

# limpeza do ambiente isolado
cd ../..
COMPOSE_PROJECT_NAME=coleta-e2e docker compose -f app/infra/compose.yaml down -v
```

## Integração contínua

O workflow `.github/workflows/ci.yml` roda em pull requests para a `main` e em
pushes na `main`, sem job de deploy:

- **backend:** restore por lockfiles, `dotnet format`, build Release e testes
  unitários;
- **integração:** testes com SQL real e Testcontainers;
- **frontend:** `npm ci`, formatação, build e testes Vitest;
- **segurança:** vulnerabilidades NuGet/npm e varredura de segredos com
  gitleaks;
- **E2E:** Playwright sobe API e SPA por proxy, com SQL Server e migrations
  preparados no job.

O comando local equivalente para backend, frontend e segurança é
`bash app/scripts/check.sh`; os fluxos E2E são executados à parte com
`npm test` em `app/e2e`. A execução real do workflow fica para o PR da mudança.

## Segredos e dados

- Nenhuma senha, chave, token ou dado pessoal real pertence a este diretório.
- Connection strings e chaves JWT virão de user-secrets no desenvolvimento e de
  variáveis de ambiente/segredos na CI.
- Exemplos versionados conterão apenas placeholders.
- A API valida connection string, `Jwt:Issuer`, `Jwt:Audience` e
  `Jwt:SigningKey` na inicialização e falha antes de ouvir se algum valor
  obrigatório estiver ausente ou inseguro. O guia está em
  `app/backend/README.md`.
