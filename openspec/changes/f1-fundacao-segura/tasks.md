# Tasks

## 1. Pré-requisitos e estrutura da aplicação

- [x] 1.1 Verificar nas fontes oficiais as versões estáveis de .NET, Angular, Node, TypeScript, Tailwind, EF Core, ASP.NET Core Identity, SQL Server 2022 Developer, FluentResults, FluentValidation, Serilog, MSTest, Moq, Testcontainers, Vitest e Playwright; registrar a matriz e as fontes em `app/README.md` e confirmar compatibilidade entre as escolhas (RF-01–05; RNF-01; DT-01–04, DT-17–22).
- [x] 1.2 Criar `app/backend` com a solução `.slnx`, projetos Domain/Application/Infrastructure/Api e projetos de teste, `global.json` e `Directory.Packages.props`, mantendo as dependências na direção definida no design; verificar com `dotnet build` e com um teste que falhe se Application referenciar Infrastructure ou Api.
- [x] 1.3 Criar `app/frontend` com Angular standalone/zoneless, Tailwind, Vitest e estrutura de features; verificar com `npm ci`, `npm run build` e `npm test -- --run`.
- [x] 1.4 Criar `app/infra/compose.yaml` para SQL Server 2022 Developer, com volume, healthcheck, porta restrita ao host e senha por variável de ambiente; verificar com `docker compose config` e com a subida do serviço até o healthcheck. Se Docker não estiver disponível, registrar o bloqueio sem instalar nada sem autorização (RF-01; RNF-03; DT-19, DT-20).
- [x] 1.5 Criar `app/scripts` para preparar o banco, aplicar migrations e executar as verificações locais; verificar que cada script é idempotente e que o script de banco funciona em um volume limpo (RF-01; RNF-03; DT-19, DT-20).
- [x] 1.6 Configurar user-secrets, placeholders de ambiente e validação de configuração na inicialização; verificar que a API falha cedo sem connection string ou chave JWT e que nenhum segredo real está versionado (RNF-01; DT-22).
- [x] 1.7 Criar o workflow `.github/workflows/ci.yml` com restore por lockfiles, lint/formatação, build .NET e Angular, testes unitários, integração com Testcontainers/SQL real, E2E e verificação de vulnerabilidades e segredos; verificar a sintaxe com `actionlint` ou equivalente e executar os mesmos comandos localmente, deixando a execução real para o PR da mudança, sem job de deploy (DT-17, DT-21).

## 2. Modelo de dados, Identity e migrations

- [x] 2.1 Implementar as entidades `Organizacao`, `Usuario`, `RefreshToken` e `Auditoria`, o `DbContext` derivado de `IdentityDbContext`, os índices únicos e as `rowversion`; verificar com teste de modelo que os tipos, tamanhos, nulabilidade e índices correspondem ao design (RF-01, RF-05; RNF-01; DT-04, DT-07).
- [x] 2.2 Configurar papéis Administrador, Operador e Cidadão, normalização de usuário/e-mail e seed apenas de papéis; verificar com teste de integração que os papéis existem e que usuário/e-mail duplicados são rejeitados (RF-01, RF-04; RNF-01; DT-04, DT-05).
- [ ] 2.3 Criar a migration inicial e aplicá-la em banco limpo; verificar com `dotnet ef database update`, inspeção das tabelas/índices e teste de que um novo banco recebe o schema completo (DT-04, DT-07, DT-19).
- [ ] 2.4 Implementar `ITenantContext` a partir das claims, filtros globais de leitura, guarda de escrita no `SaveChanges` e FKs compostas; verificar com testes de integração A/B para leitura, criação, alteração e exclusão lógica com IDs conhecidos (RF-03; RNF-01, RNF-03; CA-01, CA-02; DT-06, DT-07).

## 3. Registro, autenticação e contas

- [ ] 3.1 Implementar o registro de organização com administrador, incluindo organização não publicada, papel, unicidade e auditoria na mesma transação; verificar com testes de aplicação e integração os cenários de CA-01 e a ausência de registros parciais em falha (RF-01; RNF-01, RNF-03; CA-01; DT-05, DT-07).
- [ ] 3.2 Implementar o registro e o perfil do cidadão, com e-mail único, papel Cidadão, ausência de organização e edição restrita aos próprios campos; verificar com testes de integração os cenários de CA-02 e a rejeição de e-mail duplicado e de alteração de papel/organização (RF-05; RNF-01, RNF-02; CA-02; DT-06, DT-12).
- [ ] 3.3 Implementar login, emissão de JWT, refresh rotativo, detecção de replay, logout e cookie seguro; verificar com testes de integração e contrato a expiração de 15 minutos, a rotação, a revogação da família e a mensagem genérica para credenciais inválidas (RF-02; RNF-01; CA-01; DT-05).
- [ ] 3.4 Implementar troca da própria senha e a pendência de troca no primeiro acesso de Operador; verificar com testes que a senha atual é exigida, que a política é aplicada e que as demais rotas ficam bloqueadas até a troca (RF-02, RF-04; RNF-01; CA-01, CA-02; DT-05).
- [ ] 3.5 Implementar rate limiting e tratamento de erros nas rotas de registro, login e refresh, com ProblemDetails sem PII; verificar com testes de 429, mensagens genéricas e logs sem senha, token, e-mail completo ou telefone (RNF-01, RNF-02, RNF-08; CA-01; DT-05, DT-12, DT-18).

## 4. Gestão de equipe

- [ ] 4.1 Implementar criação e edição de Operadores pelo Administrador, com papel fixo, organização do token e senha inicial com troca pendente; verificar com testes de integração os cenários de CA-02 e a rejeição de duplicidade e de papel/organização divergentes (RF-04; RNF-01; CA-02; DT-05–DT-07, DT-12).
- [ ] 4.2 Implementar inativação lógica de Operador, revogação das sessões e preservação do ator em registros históricos; verificar com testes que a conta inativa não autentica e que as sessões existentes deixam de renovar (RF-04, RF-22; RNF-01; CA-02; DT-06, DT-07).
- [ ] 4.3 Aplicar policies exclusivas do Administrador e escopo de organização nas rotas de equipe; verificar com testes de contrato 403 para Operador e Cidadão e 404 para Administrador de outra organização (RF-03, RF-04; RNF-01; CA-01, CA-02; DT-06, DT-07).

## 5. Auditoria

- [ ] 5.1 Implementar a porta de auditoria e a gravação transacional dos eventos críticos da F1; verificar com teste de integração que a alteração e a auditoria são confirmadas juntas e que uma falha desfaz ambas (RNF-05; CA-02; DT-08, DT-18).
- [ ] 5.2 Auditar registro, login, logout, troca de senha, criação/edição/inativação de Operador e negações de acesso cruzado; verificar com testes que o registro contém organização, ator, ação, entidade, correlação e momento, sem senha, token, hash ou dados pessoais (RNF-02, RNF-05; CA-01, CA-02, CA-11; DT-08, DT-18).

## 6. Frontend da F1

- [ ] 6.1 Implementar `AuthService`, interceptor de token, renovação única por refresh e guards de rota; verificar com Vitest que o token não vai para `localStorage`, que o refresh ocorre uma vez e que a falha leva ao login (RF-02; RNF-01; DT-01, DT-05).
- [ ] 6.2 Criar as telas de registro de organização, registro de cidadão e login, com formulários reativos, rótulos, erros por campo e foco preservado; verificar com Vitest e com os fluxos de CA-01 e CA-02 no E2E (RF-01, RF-02, RF-05; CA-01, CA-02; DT-01, DT-02, DT-12).
- [ ] 6.3 Criar as telas de troca de senha e de primeiro acesso do Operador; verificar com Vitest o bloqueio até a troca, a validação da política e a mensagem de erro acessível (RF-02, RF-04; CA-02; DT-01, DT-12).
- [ ] 6.4 Criar a tela de perfil do cidadão, com edição de nome, telefone e e-mail e tratamento de conflito; verificar com Vitest e E2E que o cidadão não altera papel nem acessa perfil de outro cidadão (RF-05; RNF-01; CA-02; DT-01, DT-06).
- [ ] 6.5 Criar as telas de listagem, criação, edição e inativação de Operadores, ocultando ações não permitidas ao Operador; verificar com Vitest que a UI esconde a gestão e que a API continua negando o acesso (RF-04; RNF-01; CA-02; DT-01, DT-06).

## 7. Integração, E2E e verificação final

- [ ] 7.1 Montar a suíte de integração com `WebApplicationFactory` e Testcontainers para SQL Server, cobrindo migrations, Identity, refresh/replay, tenant A/B, cidadão X/Y, políticas, rate limiting e auditoria; verificar com `dotnet test` e SQL real (RNF-01–03, RNF-05; CA-01, CA-02; DT-17).
- [ ] 7.2 Montar os fluxos Playwright de registro, login, primeiro acesso, equipe, perfil e negações entre organizações e cidadãos; verificar com `npx playwright test` os cenários de CA-01 e das partes aplicáveis de CA-02 (RF-01–05; RNF-01; CA-01, CA-02; DT-17).
- [ ] 7.3 Verificar a execução local com SPA e API na mesma origem, cookie de refresh, proxy e banco em contêiner; registrar os comandos no `app/README.md` e comprovar que os comandos funcionam em um ambiente limpo (RNF-01; DT-19, DT-20, DT-22).
- [ ] 7.4 Executar a verificação final: `dotnet build`, `dotnet test`, `npm run build`, `npm test -- --run`, `npx playwright test` e `openspec validate --strict`; revisar o diff, confirmar ausência de segredos/PII e verificar que todos os jobs da CI passam no PR (RNF-01–03, RNF-05, RNF-08; CA-01, CA-02; DT-17, DT-21).

## Workflow follow-up

- Ao concluir cada tarefa, criar um commit pequeno e atômico em português e executar `git push` na branch `feat/f1-fundacao-segura`, sem alterar a `main`.
- Após todas as tarefas e verificações, revisar o PR com o template de `.github/pull_request_template.md`, citando RF/RNF/DT, as specs e as tarefas SDD atendidas.
- Arquivar a mudança somente depois da implementação e das verificações aprovadas, conforme o fluxo do `AGENTS.md`.
