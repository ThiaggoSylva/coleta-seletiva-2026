# Scripts locais

Scripts em Bash para preparar o banco, aplicar migrations e executar as
verificações da aplicação. Funcionam no Git Bash/WSL e em runners Linux.

| Script | Função | Idempotência |
| --- | --- | --- |
| `prepare-db.sh` | Sobe o SQL Server, espera o healthcheck e cria o banco `ColetaSeletiva` se ele não existir | Sim; volume e banco são preservados |
| `apply-migrations.sh` | Prepara o banco e aplica as migrations pendentes com `dotnet ef database update` | Sim; o EF registra as migrations aplicadas |
| `check.sh` | Restaura, compila e testa backend e frontend, valida formatação, configuração do Compose e segurança | Sim; apenas leitura, build e testes |
| `check-vulnerabilities.sh` | Consulta pacotes NuGet e npm por vulnerabilidades conhecidas e falha em severidade alta ou crítica | Sim; consulta somente leitura |
| `check-secrets.sh` | Executa gitleaks no histórico, bloqueia `.env` versionado e confere se senha local e chave JWT vazaram para arquivos rastreados | Sim; nenhum valor é impresso |

## Pré-requisitos

- .NET SDK 10.0.4xx
- Node.js e npm compatíveis com o `app/frontend/package.json`
- Docker com daemon em execução
- Bash (Git Bash no Windows ou shell de runner Linux)

## Configuração do banco

O `prepare-db.sh` lê `MSSQL_SA_PASSWORD` e `MSSQL_PORT` do ambiente ou de
`app/infra/.env`. Se o arquivo não existir, crie-o a partir de
`app/infra/.env.example` e defina uma senha local forte. O `.env` é ignorado pelo
Git.

```bash
cp app/infra/.env.example app/infra/.env
# edite a senha e execute:
bash app/scripts/prepare-db.sh
```

## Uso

```bash
# Banco pronto para desenvolvimento
bash app/scripts/prepare-db.sh

# Aplica migrations pendentes (quando existirem)
bash app/scripts/apply-migrations.sh

# Verificações locais completas
bash app/scripts/check.sh

# Verificações isoladas de segurança
bash app/scripts/check-vulnerabilities.sh
bash app/scripts/check-secrets.sh
```

O script de migrations ignora a execução enquanto a pasta
`app/backend/src/ColetaSeletiva.Infrastructure/Migrations` não tiver migrations.
A primeira migration será criada na tarefa 2.3 da mudança `f1-fundacao-segura`;
a partir daí, o script aplica apenas o que estiver pendente.

## Observações

- `check.sh` executa `npm ci` e reinstala as dependências do frontend; por isso,
  pode demorar alguns minutos.
- `check-secrets.sh` usa a imagem fixada
  `ghcr.io/gitleaks/gitleaks:v8.30.1` e não precisa de instalação no host. A
  primeira execução baixa a imagem.
- Nenhum script grava senha, token ou dado pessoal em logs.
- O banco local usa `MSSQL_PID=Developer` e não deve ser usado em produção.
- O workflow `.github/workflows/ci.yml` é validado com actionlint
  (`rhysd/actionlint:1.7.12`) e executa os mesmos comandos dos scripts.
