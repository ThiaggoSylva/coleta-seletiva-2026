# Design

## Context

Ver `proposal.md` para a motivação. A F1 parte de um repositório sem aplicação e deve respeitar as decisões `DT-03`–`DT-07`, `DT-10`, `DT-12`, `DT-17`–`DT-22` e o escopo `RF-01`–`RF-05`, `RNF-01`–`RNF-03`, `RNF-05` e `RNF-08` do TDD. A fase F0 não tem mudança OpenSpec própria; a verificação e fixação da matriz de versões, a configuração de segredos e o esqueleto de CI entram como pré-requisito desta mudança, sem presumir versões.

As specs `acesso-e-contas`, `equipe-da-organizacao`, `isolamento-de-organizacoes` e `auditoria` definem o comportamento observável. Este documento define como implementá-lo.

## Goals / Non-Goals

**Goals:**

- Criar a solução .NET em Clean Architecture, a SPA Angular, a infraestrutura local, os testes e a CI da F1.
- Entregar registro de organização e cidadão, autenticação, refresh rotativo, logout, troca de senha, perfil do cidadão e gestão de Operadores.
- Garantir isolamento entre organizações e entre cidadãos em leitura e escrita, inclusive com identificadores conhecidos.
- Registrar auditoria transacional das ações críticas da F1, sem segredos ou dados pessoais desnecessários.
- Fazer CA-01 e as partes de CA-02 relativas a contas, papéis, equipe e isolamento passarem; a publicação da organização (`RF-06`) permanece na F2.

**Non-Goals:**

- Publicação, perfil, municípios, tipos habilitados, pontos, retiradas, volumes, painel, CSV, notificações e exclusão de conta.
- TOTP, verificação de organizações, hospedagem, deploy, cofre gerenciado, backup/restore, RPO/RTO e metas de desempenho.
- Contêineres de API ou SPA no desenvolvimento; somente o SQL Server local é conteinerizado (`DT-20`).
- Expor uma API de consulta de auditoria na F1; a auditoria é gravada e protegida, e a leitura operacional fica para uma fase com requisito próprio.
- Declarar cobertura formal de acessibilidade (RNF-04/CA-12); a F1 aplica práticas básicas nas telas, e a validação completa ocorre nas fases de interface.

## Decisions

### 1. Estrutura e dependências

```text
app/
  backend/
    ColetaSeletiva.slnx
    src/ColetaSeletiva.Domain/
    src/ColetaSeletiva.Application/
    src/ColetaSeletiva.Infrastructure/
    src/ColetaSeletiva.Api/
    tests/ColetaSeletiva.Domain.Tests/
    tests/ColetaSeletiva.Application.Tests/
    tests/ColetaSeletiva.IntegrationTests/
    global.json
    Directory.Packages.props
  frontend/
    src/app/core/
    src/app/shared/
    src/app/features/acesso/
    src/app/features/cidadao/
    src/app/features/organizacao/
    package.json
    package-lock.json
  e2e/tests/
  infra/compose.yaml
  scripts/
.github/workflows/ci.yml
```

- `Domain` não referencia EF, Identity, HTTP nem Angular; `Application` referencia `Domain`; `Infrastructure` implementa portas de `Application`; `Api` compõe as camadas e não contém regra de negócio (`DT-03`).
- `Directory.Packages.props` centraliza versões; `global.json` fixa o SDK; `package-lock.json` e o lockfile .NET garantem restauração reproduzível.
- A primeira tarefa verifica as versões estáveis oficiais e registra a matriz; nenhuma versão é presumida neste documento (`DT-01`–`DT-04`, `DT-17`–`DT-22`).

**Alternativa considerada:** um único projeto .NET ou MVC com Razor. Rejeitados por contrariarem `DT-01` e `DT-03` e por reduzir a separação necessária ao isolamento e aos testes.

### 2. Modelo de dados da F1

| Entidade | Campos e relações |
| --- | --- |
| `Organizacao` | `Id`, `Nome`, `Tipo`, `Publicada` (sempre `false` na criação), criação e alteração em UTC, `rowversion`; `slug` e demais configurações ficam para a F2 |
| `Usuario` | Deriva de `IdentityUser<Guid>`; `OrganizacaoId` anulável (nulo para Cidadão), `Nome`, `Telefone` (Cidadão), `Ativo`, `TrocaSenhaPendente`, criação e alteração em UTC, `rowversion` |
| `RefreshToken` | `Id`, `UsuarioId`, `FamiliaId`, hash SHA-256, criação, expiração, consumo e revogação; nunca armazena o valor opaco original |
| `Auditoria` | `Id`, `OrganizacaoId` anulável, `AtorUsuarioId` anulável, `Acao`, `Entidade`, `EntidadeId`, `CorrelacaoId`, `OcorridoEmUtc`, nomes dos campos alterados; sem valores pessoais |

- `IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>` abriga identidade e negócio no mesmo `DbContext`, permitindo transação única no registro (`DT-04`, `DT-07`).
- Índices únicos filtrados garantem usuário e e-mail normalizados globalmente e `(OrganizacaoId, Id)` para entidades organizacionais; o filtro é necessário porque `OrganizacaoId` é nulo para Cidadão.
- FKs compostas `(OrganizacaoId, Id)` são usadas nas entidades organizacionais; `RefreshToken` e `Usuario` de Cidadão usam o vínculo por usuário e o escopo de dono. Sem cascade físico.
- `OrganizacaoId` nunca é aceito do cliente. A criação de organização, administrador, papéis e auditoria ocorre em uma única transação.
- A migration inicial cria as tabelas do Identity, `Organizacao`, `RefreshToken` e `Auditoria`, além dos índices e FKs. O seed contém apenas papéis e catálogos globais; nenhuma senha ou dado pessoal real.
- Campos de anonimização e exclusão de conta não são implementados na F1; a estrutura admite adicioná-los na F5 sem reescrever a identidade.

**Alternativa considerada:** banco ou schema por organização. Rejeitado por `DT-07`; o custo operacional e a complexidade de migrations não se justificam na v1.

### 3. Identidade, autenticação e sessão

- Identity valida senha, hash, conta ativa, lockout e normalização de usuário/e-mail. Administrador e Operador entram por usuário ou e-mail; Cidadão entra somente por e-mail (`RF-02`, `RF-05`).
- O cadastro de organização cria a organização não publicada, a conta Administrador, o papel e a auditoria na mesma transação. O cadastro de cidadão cria a conta com `OrganizacaoId` nulo, papel Cidadão e auditoria.
- O token de acesso é JWT de 15 minutos, algoritmo fixo, `issuer` e `audience` validados, com claims `sub`, papel, organização quando houver, `jti` e `exp`; nenhum dado pessoal nas claims (`DT-05`).
- A chave de assinatura é simétrica por ambiente no monólito atual; a alternativa RS256 fica para uma futura separação de serviços. Chaves e connection strings vêm de user-secrets ou variáveis de ambiente, nunca do repositório (`DT-22`).
- O refresh é opaco, com entropia criptográfica, armazenado apenas como hash, com família e rotação a cada uso. Reuso de refresh consumido revoga a família. Logout revoga a família e limpa o cookie. Validade inicial do refresh: 7 dias, configurável e sujeita a revisão de segurança antes de produção.
- Cookie de refresh: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`, sem `Domain`. Rotas que aceitam o cookie validam `Origin` e exigem cabeçalho customizado; rotas de negócio aceitam somente bearer. O token de acesso fica em memória no Angular e é renovado pelo cookie após recarregar a página.
- O primeiro acesso de Operador retorna a pendência de troca de senha; somente `GET /auth/me`, `POST /auth/alterar-senha` e `POST /auth/logout` são permitidos até a conclusão. A troca revoga os refresh tokens existentes.
- Login, cadastro e refresh usam rate limiting por rota e origem, com resposta 429 e mensagem genérica; logs não recebem senha, token, e-mail completo, telefone ou endereço (`RNF-08`, `DT-18`).

**Alternativas consideradas:** sessão consultada a cada chamada e cookie de sessão tradicional. A primeira fecha a janela pós-logout ao custo de consulta por requisição; a segunda simplifica o servidor, mas conflita com a SPA e com `DT-05`. O token curto com refresh rotativo foi mantido.

### 4. Autorização e isolamento por organização

- Policies nomeadas: `SomenteAdministrador`, `OrganizacaoAutenticada` (Administrador ou Operador) e `CidadaoAutenticado`. Cada controller/ação declara a policy; a API é a autoridade, mesmo que a UI esconda a ação (`DT-06`).
- `ITenantContext` scoped é preenchido a partir das claims validadas. Para papéis de organização, `OrganizacaoId` é obrigatório; para Cidadão, o escopo é o `UsuarioId` autenticado.
- Filtros globais de leitura aplicam `OrganizacaoId` às entidades organizacionais e o dono às entidades do cidadão. `IgnoreQueryFilters` é proibido em código de negócio, salvo operação interna explicitamente justificada e testada.
- O `SaveChanges` valida entidades adicionadas, modificadas e excluídas: preenche ou confere `OrganizacaoId`, rejeita vínculo cruzado e nunca aceita organização do corpo. FKs compostas reforçam a integridade no banco (`DT-07`).
- Recurso de outra organização ou de outro cidadão responde 404, com o mesmo formato de um recurso inexistente, para não revelar existência. Ausência de sessão responde 401; papel insuficiente responde 403.
- Consultas SQL brutas não são usadas para dados de tenant; quando inevitáveis, são parametrizadas e recebem predicado explícito de organização e dono.
- Testes A/B e X/Y cobrem GET, PUT e POST com identificadores conhecidos, inclusive em equipe, perfil e refresh.

**Alternativa considerada:** confiar apenas em filtros globais. Rejeitada porque filtros não protegem attach, update ou SQL bruto; a guarda de escrita e as FKs compostas são necessárias.

### 5. Auditoria

- Casos de uso críticos gravam `Auditoria` na mesma transação da alteração, por meio de uma porta de aplicação. A implementação adiciona a linha ao `DbContext` antes do `SaveChanges`; falha na alteração desfaz a auditoria junto.
- Ações mínimas da F1: registro de organização e cidadão, login, logout, troca de senha, criação/edição/inativação de Operador e negação de acesso cruzado.
- O registro contém organização (quando houver), ator (quando houver), ação, entidade, identificador, correlação e nomes dos campos alterados. Não contém senha, token, hash, nome, telefone, endereço nem conteúdo de requisição.
- A correlação usa o `traceId` da requisição e aparece no ProblemDetails, sem stack trace ou SQL. A leitura da auditoria não é exposta na F1; o acesso direto ao banco é restrito à operação e à futura administração.
- A alternativa de um interceptor genérico de EF foi descartada nesta fase: ele não conhece o nome semântico da ação e tenderia a registrar ruído ou campos sensíveis.

### 6. Frontend

- Angular standalone, zoneless, Signals e RxJS (`DT-01`). Rotas lazy por área: pública, acesso, cidadão e organização. Guards melhoram a navegação, mas a API continua sendo a autoridade.
- `AuthService` mantém o token de acesso apenas em memória; o interceptor o envia somente para a origem da API. Um `401` dispara renovação única por refresh e repete a requisição uma vez; falha de refresh leva ao login.
- Telas da F1: registro de organização, registro de cidadão, login, troca de senha inicial, alteração de senha, perfil do cidadão e gestão de equipe. A área da organização exibe ao Operador apenas o que o papel permite; gestão de equipe fica oculta e também é negada pela API.
- Formulários reativos tipados, com rótulos, erros por campo, foco preservado e resumo de erros. Listagens têm estados carregando, vazio e erro, e não há dependência apenas de cor. A F1 aplica práticas básicas de acessibilidade sem declarar cobertura de RNF-04/CA-12 (`DT-02`).
- Nenhum dado pessoal é gravado em `localStorage`, cache offline ou log do navegador. O frontend não decide autorização nem monta claims.

### 7. Execução local, segredos e CI

- `app/infra/compose.yaml` sobe SQL Server 2022 Developer com volume persistente e porta vinculada a `127.0.0.1`; a senha vem de variável de ambiente local, nunca do arquivo versionado.
- API e SPA rodam no host (SDK .NET e Node) e são servidas na mesma origem por proxy local. O proxy encaminha `/api` para a API e preserva cookies; a configuração é verificada em F1 (`DT-20`).
- Desenvolvimento usa user-secrets para connection string e chave JWT; CI usa variáveis de ambiente e segredos do GitHub. Exemplos versionados contêm apenas placeholders (`DT-22`).
- CI no GitHub Actions, somente CI (`DT-21`): restore por lockfiles, lint/formatação, build .NET e Angular, testes unitários, integração com Testcontainers e SQL Server real, E2E com Playwright e verificação de vulnerabilidades/segredos. Nenhum deploy.
- Migrations são aplicadas por job/script explícito com credencial de migração; a API roda com credencial sem permissão de migrar. Em desenvolvimento, o script prepara o banco antes da API e dos testes de integração.
- Não há seed de senha fixa nem dados pessoais reais; dados de demonstração, quando necessários, são sintéticos.

### 8. Estratégia de testes

| Camada | Cobertura da F1 |
| --- | --- |
| Domínio | Regras de papel, organização obrigatória para Administrador/Operador, normalização de usuário/e-mail e invariantes de criação |
| Aplicação | Casos de uso de registro, login, refresh, logout, troca de senha, perfil e equipe; auditoria na mesma transação; erros e idempotência de logout |
| Integração | Migrations, Identity, unicidade, refresh e replay, inativação, filtros de tenant, guarda de escrita, FKs, rate limiting e auditoria com SQL Server real |
| Contrato | ProblemDetails, 401/403/404, ausência de PII em erros, cookies e headers, OpenAPI |
| Frontend | Sinais, formulários, interceptor, renovação de sessão, guards e ocultação de ações por papel |
| E2E | Registro, login, primeiro acesso de Operador, gestão de equipe, perfil do cidadão e negações A/B e X/Y |

- Testcontainers com SQL Server real é obrigatório; SQLite e EF InMemory não comprovam constraints, locks ou `rowversion` (`DT-17`).
- Cada tarefa de implementação inclui o teste automatizado correspondente antes de ser marcada como concluída.
- A CI executa o mesmo conjunto em pull requests e pushes para `main`, com relatórios de falha sem dados pessoais.

## Risks / Trade-offs

- **Janela de acesso após logout** → o token de acesso continua válido até 15 minutos; aceito em `DT-05`, com refresh revogado e documentação do risco. Mitigação futura: consulta de sessão por chamada, se necessária.
- **Complexidade de tenancy** → filtros globais, guarda de escrita e FKs compostas exigem disciplina; mitigação: testes A/B e X/Y em todas as rotas, revisão de `IgnoreQueryFilters` e SQL bruto.
- **Rate limiting em memória ou por instância** → pode não proteger múltiplas instâncias; a F1 é local e a calibração/distribuição fica para F5/F6. Mitigação: configuração centralizada e teste de 429.
- **Auditoria explícita pode ser esquecida** → mitigação: porta única, revisão de casos de uso críticos e testes que verificam a linha de auditoria na mesma transação.
- **Testcontainers e SQL Server em CI** → consumo de tempo e dependência de Docker; mitigação: cache de imagens, jobs separados e execução local documentada.
- **Versões não fixadas** → a matriz da F0 será verificada na primeira tarefa; sem ela, a implementação não começa. Mitigação: lockfiles e `global.json`/`Directory.Packages.props` revisados no PR.
- **Cookie `Secure` em desenvolvimento local** → o proxy e o navegador precisam tratar `localhost` como contexto seguro; se houver limitação, a configuração local será documentada sem alterar o contrato de produção.

## Migration Plan

1. Verificar versões oficiais e criar a matriz, `global.json`, `Directory.Packages.props`, lockfiles e a configuração de segredos.
2. Criar a solução, os projetos, a SPA, o compose, os scripts e o workflow de CI.
3. Criar a migration inicial e o seed de papéis; aplicar em banco local limpo e validar o rollback por recriação do banco de desenvolvimento.
4. Implementar identidade, sessão, autorização, tenancy, auditoria e as telas da F1, com testes por camada.
5. Executar build, testes e E2E na CI; revisar o diff e abrir o PR da feature branch.

Rollback: como não há produção nem dados legados, reverter a branch e recriar o banco local. Nenhuma migração é aplicada na `main` fora do PR. O deploy e o rollback de produção ficam para F6 (`DT-21`, `DT-23`).

## Open Questions

- A matriz exata de versões e a compatibilidade de pacotes serão resolvidas na primeira tarefa, com fontes oficiais; não alteram o comportamento especificado.
- A validade do refresh (7 dias iniciais) e os limites de rate limiting serão calibrados antes de produção; a F1 usa valores configuráveis e testáveis.
- A leitura operacional da auditoria e a retenção definitiva ficam para uma fase com requisito próprio; a F1 apenas grava e protege os registros.
