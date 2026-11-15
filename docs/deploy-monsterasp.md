# Deploy no MonsterASP.NET

Guia para publicar a aplicação da fase F1 (API ASP.NET Core 10 servindo a SPA Angular) no
MonsterASP.NET com SQL Server. Nenhuma credencial real é registrada no repositório.

## O que é publicado

- `ColetaSeletiva.Api` (.NET 10) em `/wwwroot` do site, com a SPA Angular compilada em
  `wwwroot`.
- Banco MSSQL criado no painel, com o schema aplicado por migration ou pelo script SQL.

## 1. Criar site e banco

1. **Websites → Create website** e anote o nome (`siteXXXX`).
2. **Scripting**: selecione a versão **.NET 10** (a aplicação usa `net10.0`).
3. **Databases**: crie um banco **MSSQL** com um usuário.
4. Em **Databases → seu banco → Users → Connection string**, copie a string de
   **Local access** (a aplicação roda dentro da hospedagem).

## 2. Aplicar o schema

- **Pelo script (recomendado):** execute
  [`app/backend/migrations-monsterasp.sql`](../app/backend/migrations-monsterasp.sql) no
  editor de consultas do banco. O script é idempotente.
- **Na inicialização:** defina `Database__AplicarMigrationsNoInicio=true`. A aplicação executa
  `Migrate()` antes de começar a atender.

O usuário do banco precisa de permissão para criar tabelas e índices apenas quando o schema é
aplicado pela aplicação.

## 3. Variáveis de ambiente

**Websites → seu site → Scripting → Environment variables**:

| Nome | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__ColetaSeletiva` | string de **Local access** |
| `Jwt__SigningKey` | segredo aleatório com pelo menos 32 bytes |
| `Sessao__OrigemPermitida` | `https://SEU-SITE` (sem barra final) |
| `Database__AplicarMigrationsNoInicio` | `true` no primeiro deploy, depois `false` |

Gere a chave localmente, sem compartilhar o valor:

```bash
openssl rand -base64 48
```

`Jwt__Issuer` e `Jwt__Audience` já têm valores públicos em `appsettings.json`. A aplicação
**não inicia** se a connection string ou `Jwt:SigningKey` estiverem ausentes ou inseguras
(falha cedo prevista no TDD). Reinicie a aplicação após alterar variáveis.

## 4. Publicar

### Opção A — MSDeploy (recomendado)

1. No painel, **Websites → seu site → Deploy → Visual Studio access**: habilite **WebDeploy** e
   copie **Site**, **URL**, **Login** e **Password**.
2. Gere o pacote local:

   ```bash
   bash app/scripts/publicar-monsterasp.sh
   ```

   O script compila a SPA, copia o resultado para `wwwroot` da API, publica em Release e gera
   `app/backend/coleta-seletiva-monsterasp.zip`.
3. Envie a pasta `app/backend/publish` por MSDeploy, sem gravar a senha no repositório. Use
   variáveis de ambiente ou um arquivo local ignorado:

   ```bat
   "%ProgramFiles(x86)%\IIS\Microsoft Web Deploy V3\msdeploy.exe" ^
     -verb:sync ^
     -source:contentPath="C:\caminho\para\app\backend\publish" ^
     -dest:contentPath="siteXXXXX",computerName="https://siteXXXXX.siteasp.net:8172/msdeploy.axd?site=siteXXXXX",userName="siteXXXXX",password="SUA-SENHA",authtype="Basic",includeAcls="False" ^
     -allowUntrusted ^
     -disableLink:AppPoolExtension ^
     -disableLink:ContentExtension ^
     -disableLink:CertificateExtension ^
     -verbose
   ```

   O MSDeploy sincroniza apenas os arquivos alterados. Não versione o comando com a senha
   preenchida nem compartilhe o valor.

### Opção B — pacote local + ZIP/FTP

```bash
app/scripts/publicar-monsterasp.sh
# gera app/backend/coleta-seletiva-monsterasp.zip
```

Envie o conteúdo do ZIP para a raiz do site (Files → Upload, ou FTP/SFTP). O `web.config` do
pacote já configura o ASP.NET Core Module.

A CI do GitHub executa apenas verificações e **não publica**; o deploy é uma ação manual,
conforme DT-21/DT-33.

## 5. Verificar

```text
GET https://SEU-SITE/health                     -> {"status":"ok"}
GET https://SEU-SITE/                           -> SPA Angular
GET https://SEU-SITE/api/v1/auth/me             -> 401 sem sessão
```

Depois, pela interface: cadastre uma organização, entre com o usuário administrador e crie um
operador. Erros 500 costumam indicar variável de ambiente ausente ou banco inacessível; os logs
ficam em **Websites → Logs**.

## 6. Reverter

- **Código:** republique o pacote anterior (opção A ou B).
- **Banco:** as migrations desta fase são aditivas (criação de tabelas). A reversão do binário
  não exige alteração de schema.

## Escopo desta entrega

A F1 cobre contas, autenticação, papéis, equipe e isolamento entre organizações. Pontos de
coleta, retiradas, volumes e relatórios pertencem às fases F2 a F5 do roadmap do TDD e ainda
não existem na aplicação.
