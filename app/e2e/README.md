# Testes E2E

Fluxos Playwright de CA-01 e das partes aplicáveis de CA-02: registro de
organização e de cidadão, login, primeiro acesso do Operador, gestão de equipe,
perfil do cidadão e negações entre organizações e cidadãos.

## Pré-requisitos

- Node.js `^22.22.3 || ^24.15.0 || >=26.0.0`
- .NET SDK 10.0.4xx
- Docker com o SQL Server local em execução e migrations aplicadas
- Connection string e chave JWT disponíveis em user-secrets ou variáveis de
  ambiente (`ConnectionStrings__ColetaSeletiva` e `Jwt__SigningKey`)

```bash
# na raiz do repositório
cp app/infra/.env.example app/infra/.env
# edite a senha local e aplique as migrations
bash app/scripts/apply-migrations.sh

cd app/e2e
npm ci
npx playwright install chromium
```

## Execução

```bash
npm test
```

O Playwright sobe a API em `http://localhost:5012` e a SPA em
`http://localhost:4200`, usando o proxy de `app/frontend/proxy.conf.json`. Não
deixe API ou SPA já em execução nessas portas: a suíte inicia processos próprios
com origem e limites de taxa adequados ao teste.

Os testes usam apenas dados sintéticos e únicos por execução. O banco não é
recriado pelo Playwright; a suíte espera o SQL Server já preparado. Para uma
verificação isolada, use `COMPOSE_PROJECT_NAME` e `MSSQL_PORT` alternativos ao
aplicar as migrations e ao rodar os testes, sem afetar o banco de
desenvolvimento.

Em CI, o workflow sobe o SQL Server, aplica as migrations e define a connection
string e a chave JWT antes de executar `npm test`.
