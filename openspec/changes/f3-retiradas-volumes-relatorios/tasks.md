# Tasks

## 1. Modelo e persistência

- [x] 1.1 Criar `DisponibilidadeRetirada`, `BloqueioData` e `JanelaRetirada` com índices únicos, filtros e guarda de tenant (RF-10; RN-02, RN-12; DT-16).
- [x] 1.2 Criar `SolicitacaoRetirada`, `SolicitacaoItem` e `SolicitacaoHistorico` com snapshot de PII, `rowversion`, índices de fila e limite por cidadão (RF-11–RF-13; RN-03, RN-10, RN-11).
- [x] 1.3 Criar `RegistroVolume` append-only e `Notificacao` com filtros por organização/usuário e checks de origem (RF-15, RF-16, RF-20; RN-07; DT-14, DT-16).
- [x] 1.4 Gerar migration aditiva, atualizar `migrations-monsterasp.sql` e os testes de modelo/migrations.

## 2. Disponibilidade e solicitações

- [x] 2.1 Implementar consulta e gravação da capacidade semanal e bloqueios de data, restritas ao Administrador (RF-10).
- [x] 2.2 Implementar consulta de organizações elegíveis e janelas com vagas para o cidadão (RF-11; RN-01, RN-02, RN-04).
- [x] 2.3 Implementar criação de solicitação com validação de horizonte, território, tipos, limite e capacidade sob lock (RF-11; RN-01–RN-04, RN-10; RNF-03).
- [x] 2.4 Implementar listagem/detalhe do cidadão, cancelamento com prazo de 24 h e projeção de PII (RF-12; RN-03, RN-11).
- [x] 2.5 Implementar fila da organização, agendamento, reagendamento, recusa, execução e não realização com histórico e notificações (RF-13, RF-14; RN-03; CA-08).
- [x] 2.6 Aplicar isolamento A/B e papéis em todas as rotas de retirada (RF-03; RNF-01).

## 3. Volumes

- [x] 3.1 Implementar recebimento em ponto com validação de ponto/tipo e `DataLocal` (RF-15; RN-06).
- [x] 3.2 Implementar ajuste com motivo, lock do original e rejeição de total negativo (RF-16; RN-07).
- [x] 3.3 Implementar listagem de volumes com filtros e vínculo ao original (RF-15, RF-16).

## 4. Painel e notificações

- [x] 4.1 Implementar resumo, série por dia/semana/mês e totais por origem/ponto (RF-17; RN-05, RN-06).
- [x] 4.2 Implementar CSV UTF-8 com BOM, `;`, vírgula decimal, ajustes identificados e neutralização de fórmulas (RF-18; RNF-08).
- [x] 4.3 Implementar notificações transacionais, consulta, leitura e contagem (RF-20; DT-14).
- [x] 4.4 Registrar auditoria das ações críticas na mesma transação (RNF-05).

## 5. Frontend

- [x] 5.1 Criar serviço e sino de notificações com polling e contagem de não lidas (RF-20).
- [x] 5.2 Criar telas do cidadão para listar, criar e cancelar solicitações (RF-11, RF-12).
- [x] 5.3 Criar fila e detalhe da organização com ações por estado e PII mascarada (RF-13, RF-14; RN-11).
- [x] 5.4 Criar telas de disponibilidade/bloqueios, volumes e painel com download CSV (RF-10, RF-15–RF-18).
- [x] 5.5 Atualizar navegação, rotas, estados vazios e restrições por papel (RNF-04).

## 6. Verificação

- [x] 6.1 Cobrir domínio com testes de estados, prazo, horizonte, quantidade, unidade e neutralização de CSV.
- [x] 6.2 Cobrir integração com SQL Server para CA-06–CA-10, concorrência, isolamento A/B, PII e notificações.
- [x] 6.3 Executar `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `npm run build` e `npm test`; registrar bloqueios.

## Pendências registradas

- `Idempotency-Key` persistida em criação/execução (DT-01/DT-17) não foi implementada nesta
  mudança; a UI evita duplo envio e a evolução deve adicionar `ComandoIdempotente`.
- E2E Playwright, metas de desempenho e validação do catálogo permanecem fora desta entrega.
- A concorrência de capacidade é protegida por `UPDLOCK`/`HOLDLOCK` na linha da organização e
  exercitada pelo cenário de esgotamento; não há teste paralelo dedicado nesta entrega.
- O comando de testes do frontend é `npm test` (script `ng test --watch=false`); a variante
  `npm test -- --run` não é aceita pelo Angular CLI e não foi utilizada.
