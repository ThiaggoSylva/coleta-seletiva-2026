# Infraestrutura local

O único serviço conteinerizado no desenvolvimento da F1 é o SQL Server 2022
Developer. A API e a SPA rodam no host.

## Pré-requisitos

- Docker com o daemon em execução
- Docker Compose v2 ou superior (verificado com v5.4.0)

## Configuração

1. Copie `.env.example` para `.env`:

   ```bash
   cp .env.example .env
   ```

2. Defina `MSSQL_SA_PASSWORD` com uma senha forte e exclusiva do ambiente local.
   A senha não deve ser versionada nem reutilizada em produção.
3. Opcionalmente, altere `MSSQL_PORT`. A porta é sempre publicada em
   `127.0.0.1`, nunca em todas as interfaces da rede.

## Subir o banco

```bash
docker compose up -d --wait
docker compose ps
```

O comando aguarda o healthcheck. O serviço `sqlserver` deve aparecer como
`healthy`. O healthcheck usa `sqlcmd` e executa `SELECT 1` a cada 10 segundos.

## Parar e remover

```bash
# Para o contêiner e preserva os dados
docker compose down

# Remove também o volume local; use apenas quando quiser recriar o banco
docker compose down -v
```

## Conexão local

```text
Server=127.0.0.1,${MSSQL_PORT:-1433};
Database=ColetaSeletiva;
User Id=sa;
Password=<MSSQL_SA_PASSWORD>;
TrustServerCertificate=True;
Encrypt=True;
```

A connection string completa pertence ao user-secrets ou às variáveis de
ambiente da API, nunca a este arquivo. O banco `ColetaSeletiva` e as migrations
serão criados pelas tarefas de banco e pelos scripts de preparação.

## Segurança

- O SQL Server usa `MSSQL_PID=Developer`, restrito ao desenvolvimento local.
- O volume `sqlserver_data` persiste os dados entre reinícios.
- `.env` é ignorado pelo Git; apenas `.env.example` com placeholders é versionado.
- Não use a senha do `sa` em produção. O deploy e as credenciais de produção
  ficam fora do escopo da F1.
