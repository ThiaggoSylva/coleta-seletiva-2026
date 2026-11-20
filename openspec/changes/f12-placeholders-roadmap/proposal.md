# Proposal

## Why

A aplicação publicada cobre F1–F4 e parte de F5, mas quatro recursos já previstos no roadmap —
preferências de e-mail (F7), extrato de pontos (F8), rotas do coletor (F9) e transparência/impacto
(F10) — não existem na API nem na interface. Sem sinalização no próprio produto, a ausência é
confundida com funcionalidade incompleta e um cliente HTTP não tem como descobrir, de forma
previsível, o que está planejado.

Esta mudança entrega a fase F12 do TDD: uma indicação explícita de "em breve" nas duas camadas,
sem criar dados fictícios, sem prometer prazos e sem alterar os fluxos já validados.

## What Changes

- Criar um catálogo estático e imutável com os quatro módulos, contendo identificador, título,
  descrição, fase, rota da SPA e situação `em_breve` (RF-24; RNF-10; DT-38).
- Expor `GET /api/v1/em-breve` publicamente, devolvendo `200` com o catálogo e nenhum dado pessoal
  ou de organização (RF-24; CA-13; DT-36).
- Expor `GET /api/v1/em-breve/{modulo}` com `501 Not Implemented` em `ProblemDetails`, incluindo
  `code=em_breve`, `modulo`, `fase`, `detail` e `traceId`; módulo desconhecido responde `404` (RF-25;
  RN-13; CA-14; DT-36, DT-37).
- Aplicar as policies existentes aos detalhes protegidos: Cidadão para e-mail/extrato, equipe da
  organização para rotas do coletor e acesso público para transparência/impacto (RF-27; DT-39).
- Criar um `EmBrevePage` reutilizável e quatro rotas estáveis em `/em-breve/*`, com título,
  descrição, fase, aviso de indisponibilidade e ação de retorno (RF-26; RNF-09; DT-40).
- Exibir links no cabeçalho conforme papel e um link público no rodapé, sem oferecer acesso a quem
  não pode abrir o módulo (RF-27; DT-41).
- Cobrir contrato, autorização, papéis e viewports desktop (1280×720) e mobile (390×844) com
  testes de integração, Vitest e Playwright, além de inspeção manual local (CA-13–CA-16; DT-42).

## Capabilities

### New Capabilities

- `sinalizacao-de-roadmap`: catálogo somente leitura, índice e operações "em breve" na API, páginas
  e navegação por papel no frontend, com privacidade e acesso restrito conforme o módulo.

### Modified Capabilities

- Nenhuma. As policies, os guardas e a autorização existentes são reutilizados sem alterar os
  requisitos de `acesso-e-contas`, `equipe-da-organizacao` ou `isolamento-de-organizacoes`.

## Out of Scope

- Implementar e-mail/outbox/worker, ledger de pontos, PWA/offline, fotos de pontos, rotas reais,
  impacto calculado ou verificação de organizações (F7–F10).
- Criar entidades, migrations, tabelas, dados de demonstração ou configurações por ambiente.
- Novos pacotes, segredos, variáveis de ambiente, papéis de segurança ou mudanças de tenant.
- Substituir ou antecipar os critérios de aceite das fases reais F7–F10.
- Publicar diretamente em produção: a entrega termina com pacote e instruções de publicação manual.

## Impact

- **Código:** `ColetaSeletiva.Application` (catálogo e consulta), `ColetaSeletiva.Api`
  (controlador, `ProblemDetails` e OpenAPI) e `app/frontend/src` (rota, página, navegação e testes).
- **API:** `GET /api/v1/em-breve` e `GET /api/v1/em-breve/{modulo}`; nenhuma alteração de contratos
  existentes. Não há quebra de compatibilidade.
- **Dados:** nenhuma migration, nenhum segredo e nenhum dado pessoal; catálogo somente em código.
- **Operação:** continua válido o pacote gerado por `app/scripts/publicar-monsterasp.sh`, com
  publicação manual no MonsterASP.NET e rollback pelo pacote anterior.
- **Rastreabilidade:** PRD seção 13, RF-24–RF-27, RN-13, RNF-09–RNF-11, CA-13–CA-16; TDD seção 12,
  DT-36–DT-42 e fase F12.
