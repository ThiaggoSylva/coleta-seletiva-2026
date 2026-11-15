## Resumo

Implementa a F2 — Organização, catálogo e pontos: perfil público da organização, catálogo global de municípios e tipos de resíduo, gestão de pontos de coleta e consulta pública anônima com mapa e lista equivalente. A entrega é aditiva sobre a F1 (autenticação, tenancy e auditoria) e não altera contratos existentes fora do novo namespace `/api/v1/publico`.

**Principais mudanças observáveis**

- **Perfil da organização** (`GET/PUT /api/v1/organizacao`): nome, tipo, contato público, fuso IANA validado, limite de solicitações abertas, municípios atendidos, períodos de retirada e tipos habilitados com retirada domiciliar por tipo. Publicação exige ao menos um município e um tipo; despublicar não altera dados existentes.
- **Catálogo global** (somente leitura): 5.571 municípios semeados por migração a partir da lista do IBGE e 10 tipos iniciais de resíduo; consulta por UF e texto, com limite de 50 resultados.
- **Pontos de coleta** (`/api/v1/pontos`): criação, edição, listagem, alteração de status e retirada lógica pelo Administrador, com `geography` SRID 4326 e índice espacial, nome único por organização, município obrigatoriamente atendido, ao menos um tipo habilitado e horários que começam e terminam no mesmo dia.
- **Consulta pública** (`/api/v1/publico/*`, anônima): busca por município, tipo, "aberto agora" e proximidade (raio de 5 a 50 km, coordenadas no corpo via POST), paginação com teto de 100, cache curto e limite de taxa; DTOs próprios sem nome, e-mail ou telefone de cidadãos. Novo endpoint `GET /api/v1/publico/tipos` alimenta os filtros anônimos.
- **Frontend Angular**: telas de perfil (com publicação e catálogo), lista e formulário de pontos com mapa Leaflet/OpenStreetMap para escolher coordenadas, e consulta pública com filtros, geolocalização do dispositivo, marcadores, lista equivalente, estado vazio e paginação.

**Fora de escopo:** F3 em diante (disponibilidade, solicitações, retiradas, notificações, relatórios), geocodificação externa, testes de integração/E2E da F2 e validação do catálogo inicial de tipos com uma organização (pendência 12.1 do PRD). Não há mudança incompatível.

## Rastreabilidade

- PRD (`docs/prd.md`): **RF-06** (perfil e publicação), **RF-07** (catálogo e tipos habilitados), **RF-08** (gestão de pontos), **RF-09** (consulta pública); **RNF-02** (privacidade), **RNF-04** (usabilidade/acessibilidade), **RNF-08** (proteção contra abuso); **RN-08** (`aberto agora` e horários), **RN-09** (pré-requisitos de publicação), **RN-12** (despublicação não cancela dados). Critérios: **CA-02–CA-05** nas partes cobertas pela fase.
- TDD (`docs/tdd.md`): **DT-02** (Tailwind + Leaflet/OpenStreetMap), **DT-06** (policies de papel), **DT-07** (multitenancy por `OrganizacaoId` e FKs compostas), **DT-11** (`geography` + índice espacial + coordenadas fora de URL/logs), **DT-13** (`/publico/*` com DTOs próprios, rate limiting e cache curto), **DT-15** (municípios semeados por migração), **DT-17** (MSTest/Moq e Vitest). Fase **F2 — Organização, catálogo e pontos** do roadmap.
- SDD: `openspec/changes/f2-organizacao-catalogo-pontos/` (`proposal.md`, `design.md`, `tasks.md` e as especificações `perfil-da-organizacao`, `catalogo-e-tipos`, `pontos-de-coleta` e `consulta-publica`). Tarefas 1.1–5.3 e 6.3 concluídas; 6.1 parcial (domínio e aplicação) e 6.2 pendente (integração com SQL real).

## Validação

- [x] Implementação consistente com PRD, TDD e especificações aplicáveis.
- [x] Testes e verificações aplicáveis executados (descrever abaixo).
- [x] Documentação e especificações atualizadas (`tasks.md`, README e `migrations-monsterasp.sql`).

**Testes executados e resultados:**

- `dotnet build ColetaSeletiva.slnx -c Release`: 0 erros.
- `dotnet format ColetaSeletiva.slnx --verify-no-changes --no-restore`: sem alterações pendentes.
- `dotnet test tests/ColetaSeletiva.Domain.Tests -c Release`: **11/11** aprovados (inclui abertura no fuso da organização, fim exclusivo e horário que atravessa a meia-noite).
- `dotnet test tests/ColetaSeletiva.Application.Tests -c Release`: **47/47** aprovados (perfil, publicação, conflitos de vínculo, validação de pontos, busca pública e limites de paginação/raio).
- `npm run build` em `app/frontend`: build de produção concluído; `leaflet` declarado em `allowedCommonJsDependencies`.
- `npm test` em `app/frontend`: **68/68** testes em 12 arquivos, incluindo os novos casos de listagem, alteração de status e retirada de pontos.
- `npm run format:check` em `app/frontend`: estilo Prettier aplicado.
- `dotnet publish -r win-x86 --self-contained false`: pacote de 24 MB com `web.config` e `wwwroot/index.html`.
- `app/backend/migrations-monsterasp.sql`: 19 `CREATE TABLE`, 12 lotes de municípios (5.571 registros) e 1 índice espacial; seed com 10 tipos.

**Não executados nesta máquina:**

- Testes de integração com SQL Server (Testcontainers) e E2E Playwright da F2: Docker indisponível na VM. Consequência: `geography`, índice espacial e isolamento A/B da consulta pública **não foram exercitados em runtime**; o PR não deve ser declarado pronto antes de a CI executá-los.
- Verificação visual das telas: sem SQL Server local a SPA não tem dados; nenhuma tela foi clicada ou inspecionada no navegador.
- `openspec validate`: CLI indisponível no ambiente.

## Riscos e pendências

- **PR existente:** a branch `feat/f1-fundacao-segura` já tem o PR #2 aberto para `main`; os commits da F2 estão nele. Abrir um PR separado a partir desta branch arrastaria toda a F1, então o caminho correto é **atualizar a descrição do #2** com esta seção (e o título abaixo), não criar outro PR.
- **Banco e migração:** aplicar `app/backend/migrations-monsterasp.sql` (idempotente) ou iniciar com `Database__AplicarMigrationsNoInicio=true`; a criação do índice espacial usa `USING GEOGRAPHY_AUTO_GRID` e exige SQL Server compatível. Rollback = reverter a migration aditiva (tabelas novas) antes de qualquer dado de produção.
- **Fonte dos municípios:** a API do IBGE não respondeu no ambiente; o CSV versionado veio de espelho público. Fonte e licença precisam de confirmação (DT-15, pendência registrada em `proposal.md`).
- **Privacidade:** DTOs públicos não expõem dados pessoais e a localização do dispositivo não é persistida; a busca anônima tem limite de taxa e cache curto, mas a topologia com múltiplas instâncias exige armazenamento distribuído para o rate limiting.
- **Dependência nova:** `leaflet` 1.9.4 e `@types/leaflet` no frontend; uso dos tiles do OpenStreetMap sujeito à política do provedor (DT-02).
- **Acessibilidade:** o mapa tem lista equivalente e o seletor de coordenadas aceita digitação, mas RNF-04 não foi validado formalmente (teclado, leitor de tela).
- **Deploy:** continua pendente de `ConnectionStrings__ColetaSeletiva`, `Jwt__SigningKey`, `Sessao__OrigemPermitida` e dos quatro secrets de Web Deploy no GitHub Actions.
