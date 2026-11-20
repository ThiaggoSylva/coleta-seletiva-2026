# Design

## Context

F1–F4 entregaram autenticação, multitenancy, catálogo, pontos, retiradas, volumes e painel; F11
acrescentou pontos de fontes públicas. As policies `somente-cidadao` e
`somente-equipe-da-organizacao`, os guardas de rota e o pipeline de publicação já existem. A seção 12
de `docs/tdd.md` fixou as decisões DT-36 a DT-42 para a fase F12; este desenho apenas as opera,
sem introduzir dependências, persistência ou mudança de papéis.

## Goals / Non-Goals

**Goals:**

- Manter o catálogo de módulos em breve em um único registro canônico na camada de aplicação.
- Publicar um contrato HTTP previsível: `200` no índice, `501` estruturado por módulo e `404` para
  identificador desconhecido.
- Reutilizar autenticação, autorização, limites de taxa, `ProblemDetails` e convenções de log.
- Entregar uma página reutilizável e navegação coerente por papel em desktop e celular.
- Permitir verificação automatizada e inspeção manual sem dados pessoais.

**Non-Goals:**

- Implementar qualquer funcionalidade das fases F7–F10, mesmo que de forma parcial.
- Criar tabelas, migrations, segredos, configuração por ambiente ou dados de demonstração.
- Alterar contratos existentes, papéis de segurança ou regras de isolamento entre organizações.
- Publicar em produção sem a etapa manual e sem as credenciais do responsável.

## Decisions

### 1. Superfície da API (DT-36)

Um único `EmBreveController` em `ColetaSeletiva.Api` concentra:

- `GET /api/v1/em-breve` — índice anônimo, `200` com `id`, `titulo`, `descricao`, `fase`, `rota` e
  `situacao="em_breve"`.
- Rotas literais `GET /api/v1/em-breve/{modulo}` por módulo, com `501` para identificadores do
  catálogo e `404` para os demais.
- `[EnableRateLimiting(ConsultaPublica)]` reutilizado, conforme o padrão de consulta pública.

Alternativa descartada: quatro controladores por domínio criariam contratos idênticos espalhados e
aumentariam o trabalho de remoção quando as fases reais chegassem. Rotas literais são preferidas a
um único parâmetro com autorização manual porque permitem aplicar policies declarativamente.

### 2. Catálogo e erro estruturado (DT-37, DT-38)

`ColetaSeletiva.Application` terá um registro somente leitura (`ModuloEmBreve` e
`CatalogoDePlaceholders`) e um serviço/handler de consulta registrado no DI. Os textos ficam
versionados com o código, iguais em todos os ambientes e em português do Brasil.

A resposta `501` usa `ProblemDetails` com `code="em_breve"`, `modulo`, `fase`, `detail` e `traceId`,
seguindo a convenção atual de correlação da API. O `404` de módulo desconhecido usa
`code="nao_encontrado"` e não devolve o valor recebido. Anotações `ProducesResponseType` expõem
`200`, `401`, `403`, `404` e `501` no OpenAPI.

Alternativas descartadas: `appsettings.json` permitiria divergência entre ambientes; `text/plain`
quebraria a convenção de clientes; `200` com status "em breve" sugeriria sucesso de operação.

### 3. Autorização (DT-39)

O índice e a operação de transparência/impacto são `[AllowAnonymous]`. As operações de
`preferencias-email` e `extrato-pontos` usam `somente-cidadao`; `rotas-coletor` usa
`somente-equipe-da-organizacao`. Como o catálogo é estático e não contém dados de nenhum tenant,
não há leitura entre organizações; ainda assim, nenhuma policy é relaxada e nenhuma rota protegida
recebe metadados antes da autorização.

### 4. Frontend e navegação (DT-40, DT-41)

Um componente standalone `EmBrevePage` é carregado por quatro rotas em `/em-breve/*`. Cada rota
fornece por `data` o identificador, título, descrição e fase; o componente só apresenta esse
conteúdo, o aviso de indisponibilidade e uma ação de retorno. O catálogo da Application permanece
a fonte canônica do contrato; a SPA mantém um espelho mínimo e estável desses valores, verificado
por testes de paridade de `id`, `fase` e `rota`.

Guardas existentes protegem as rotas de cidadão e de equipe; transparência/impacto é pública. O
cabeçalho filtra links por papel e por troca de senha pendente usando os membros já expostos pelo
serviço de autenticação. O rodapé acrescenta apenas o link público de roadmap. Nenhum link é
renderizado para um módulo cuja rota ou operação seria negada.

Alternativas descartadas: quatro componentes duplicariam marcação e testes; um único link no
rodapé esconderia os módulos protegidos; um menu por papel exigiria novo padrão visual sem ganho.

### 5. Verificação (DT-42)

- **API:** testes de integração com a infraestrutura existente cobrem índice anônimo, `501` por
  módulo, `404`, `401`/`403` por papel, ausência de PII e presença das operações no OpenAPI.
- **Frontend:** teste do componente verifica título, descrição, fase, aviso e retorno a partir de
  dados de rota; testes de navegação verificam visitante, Cidadão, Administrador, Operador e troca
  de senha pendente.
- **E2E:** um spec Playwright é executado em dois projetos, desktop `1280×720` e mobile
  `390×844`, verificando as quatro rotas e ausência de rolagem horizontal. A inspeção manual local
  complementa a automação antes do PR.

Não há novo pacote de teste: a suíte reutiliza xUnit/WebApplicationFactory no backend e a stack
já configurada no frontend e no Playwright.

## Risks / Trade-offs

- **Texto duplicado entre API e SPA** → teste de paridade de identificadores, fases e rotas;
  qualquer mudança de catálogo deve atualizar as duas camadas no mesmo PR.
- **`501` interpretado como falha de servidor por monitoramento** → `code`, `modulo` e `fase`
  explícitos, contrato documentado e commit que destaca o comportamento como intencional.
- **Link órfão por papel** → testes de navegação para os quatro perfis e para troca de senha
  pendente.
- **Fase real entregue sem remover o placeholder** → o checklist de F7–F10 deve remover o item do
  catálogo, a rota e o link na mesma mudança; produto revisa os rótulos.
- **Regressão de layout no celular** → dois projetos Playwright e inspeção manual antes do PR.
- **Exposição indevida de PII** → catálogo sem campos opcionais de pessoa/organização e asserções
  de ausência de dados sensíveis nos testes.

## Migration Plan

Não há migração de banco nem mudança de contrato existente. Sequência de entrega:

1. Implementar catálogo, API e documentação OpenAPI; executar build e testes de integração.
2. Implementar página, rotas, navegação e testes de componente; executar build e testes do frontend.
3. Executar Playwright nos projetos desktop e mobile e fazer inspeção manual local sem dados reais.
4. Abrir PR para `main` com rastreabilidade PRD/TDD/SDD; integrar somente após verificações e
   aprovações exigidas.
5. Após o merge, gerar o ZIP com `app/scripts/publicar-monsterasp.sh` e publicar manualmente no
   MonsterASP.NET.
6. Validar em produção `/health`, `/api/v1/em-breve` e as quatro rotas. Rollback: republicar o
   pacote anterior; não há rollback de schema.

## Open Questions

- Métricas de uso dos placeholders e responsável pela revisão periódica dos rótulos permanecem
  **A definir** no PRD (13.11). Não alteram o contrato, o desenho nem as tarefas desta mudança.
- A data de publicação das fases F7–F10 não foi prometida e MUST NOT ser exibida na interface.
