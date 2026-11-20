# Tasks

## 1. Revisão do planejamento (gate F12)

- [x] 1.1 Validar proposal, specs, design e tasks com `openspec validate f12-placeholders-roadmap --strict` e obter aprovação explícita pela ferramenta `question`; verificar sucesso do comando e registro da aprovação antes de tocar em código (RF-24–RF-27; DT-36–DT-42).

## 2. Catálogo e API

- [x] 2.1 Criar `ModuloEmBreve`, `CatalogoDePlaceholders` e o serviço de consulta em `ColetaSeletiva.Application` com os quatro módulos, fases F7–F10 e rotas estáveis; verificar com teste unitário que o catálogo contém exatamente os quatro IDs, sem campos de nome, contato, endereço ou organização (RF-24; RNF-10; CA-13, CA-16; DT-38).
- [x] 2.2 Adicionar `CodigosDeErro.EmBreve` e implementar `EmBreveController` com índice `200`, rotas literais por módulo e `404` para id desconhecido; verificar com teste de integração o payload do índice, o `code=nao_encontrado` e a ausência de PII (RF-24; CA-13; DT-36).
- [x] 2.3 Implementar o `ProblemDetails` `501` com `code=em_breve`, `modulo`, `fase`, `detail` e `traceId`, reutilizando a correlação existente, e anotar os códigos no OpenAPI; verificar com teste de integração cada módulo e com asserção sobre o documento OpenAPI (RF-25; RN-13; CA-14; DT-37).
- [x] 2.4 Aplicar `[AllowAnonymous]` ao índice e à transparência, `somente-cidadao` a e-mail/extrato e `somente-equipe-da-organizacao` a rotas, mantendo o limite de taxa público; verificar com testes de integração `401`, `403` e `501` por papel, sem vazamento de metadados nas negações (RF-27; RNF-10; CA-15; DT-36, DT-39).

## 3. Frontend

- [ ] 3.1 Criar em `app/frontend/src` o espelho estável de `id`, `titulo`, `descricao`, `fase` e `rota`, as quatro rotas `/em-breve/*` com `data` e os guardas existentes; verificar com `npm run build` e teste automatizado de paridade de IDs, fases e rotas (RF-26; RNF-11; DT-38, DT-40).
- [ ] 3.2 Implementar `EmBrevePage` reutilizável com título, descrição, fase, aviso "em breve" e ação de retorno, sem formulário, dado fictício ou data prometida; verificar com teste de componente que todos os dados de rota são exibidos (RF-26; RN-13; CA-15; DT-40).
- [ ] 3.3 Adicionar links no cabeçalho filtrados por papel e troca de senha pendente, mais o link público no rodapé; verificar com testes que visitante, Cidadão, Administrador, Operador e usuário com troca pendente veem apenas links permitidos (RF-27; DT-41).

## 4. Verificação em web e celular

- [ ] 4.1 Configurar dois projetos Playwright, desktop `1280×720` e mobile `390×844`, sem nova dependência de runtime; verificar com `npx playwright test --list` que o spec é descoberto nos dois projetos (RNF-09; DT-42).
- [ ] 4.2 Criar spec E2E das quatro rotas com retorno funcional, aviso "em breve" e ausência de rolagem horizontal; verificar execução nos dois viewports com `npx playwright test` (RF-26; RNF-09; CA-15; DT-42).
- [ ] 4.3 Fazer inspeção manual local no servidor de desenvolvimento em desktop e celular, incluindo os estados anônimo e autenticados; verificar ausência de corte de texto, links corretos e nenhum dado pessoal em logs, erros ou screenshots (RNF-09, RNF-10; CA-15, CA-16).
- [ ] 4.4 Executar `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `npm run build`, `npm test` e os testes Playwright; registrar comandos, resultados e eventuais bloqueios no PR (CA-13–CA-16; DT-42).

## 5. Coerência e entrega

- [ ] 5.1 Revisar o diff completo em relação à `main` para confirmar que não há migration, segredo, dado pessoal, dependência nova ou alteração de contrato existente; verificar com inspeção do diff e busca por identificadores sensíveis (RNF-10, RNF-11; CA-16).
- [ ] 5.2 Conferir que a implementação cobre RF-24–RF-27, RN-13, RNF-09–RNF-11 e CA-13–CA-16 e que `docs/prd.md`, `docs/tdd.md` e este change permanecem coerentes; verificar com a matriz de rastreabilidade do PR (F12; DT-36–DT-42).

## Workflow follow-up

- Abrir PR da branch `feat/placeholders-em-breve` para `main` usando `.github/pull_request_template.md`, após as verificações e aprovações exigidas.
- Integrar na `main` somente por meio do PR, respeitando revisões, checks e proteções da branch.
- Após o merge, gerar o pacote com `bash app/scripts/publicar-monsterasp.sh` e publicar manualmente no MonsterASP.NET conforme `docs/deploy-monsterasp.md`.
- Arquivar a mudança OpenSpec após a entrega e registrar o resultado e as pendências.
