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

## Segredos e dados

- Nenhuma senha, chave, token ou dado pessoal real pertence a este diretório.
- Connection strings e chaves JWT virão de user-secrets no desenvolvimento e de
  variáveis de ambiente/segredos na CI.
- Exemplos versionados conterão apenas placeholders.
