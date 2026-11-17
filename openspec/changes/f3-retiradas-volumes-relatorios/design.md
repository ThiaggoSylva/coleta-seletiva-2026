# Design

## Contexto

A F1 fixou Clean Architecture, multitenancy por `OrganizacaoId`, filtros globais de leitura,
guarda de escrita e policies por papel. A F2 acrescentou os cadastros e a consulta pública. A F3/F4
reutiliza esses mecanismos e acrescenta o ciclo operacional de retiradas, os volumes e os
relatórios.

## Modelo de dados

| Entidade | Campos e relações |
| --- | --- |
| `DisponibilidadeRetirada` | Organização, dia da semana, período e capacidade; único por `(OrganizacaoId, DiaSemana, Periodo)`. |
| `BloqueioData` | Organização, data local, motivo opcional e autor; único por `(OrganizacaoId, DataLocal)`. |
| `JanelaRetirada` | Organização, data local e período; única e criada sob demanda como âncora lógica da janela. |
| `SolicitacaoRetirada` | Organização, cidadão, snapshot de nome/telefone/endereço, data local, período, estado, motivo, observações, `rowversion`, itens e histórico. |
| `SolicitacaoItem` | Tipo de resíduo e quantidade estimada com até três casas. |
| `SolicitacaoHistorico` | Estado anterior, estado novo, ator, instante e motivo. |
| `RegistroVolume` | Organização, tipo, quantidade com sinal apenas em ajuste, origem, referências à solicitação/ponto/registro original, operador, nome copiado, `OcorridoEmUtc`, `DataLocal` e motivo. |
| `Notificacao` | Destinatário, organização, evento, referência, título, mensagem, leitura e instante. |

Filhos de solicitação não implementam `IEntidadeDaOrganizacao` porque são gravados pelo cidadão;
a autorização é aplicada no serviço sobre a solicitação raiz. Configurações de disponibilidade e
bloqueios implementam a interface e entram nos filtros de tenant. `RegistroVolume` tem filtro por
organização e nunca é exposto ao cidadão.

## Regras e concorrência

- A janela é `(OrganizacaoId, DataLocal, Periodo)`. "Hoje" usa o fuso IANA da organização e
  `TimeProvider`; aceita de `hoje + 1` a `hoje + 30` dias (RN-01).
- Operações que alteram ocupação adquirem `UPDLOCK`/`HOLDLOCK` na linha da organização antes de
  contar solicitações em `Solicitada` ou `Agendada`. Isso serializa criação, reagendamento,
  cancelamento e encerramento por organização e impede excesso de capacidade sob concorrência
  (RN-02, RNF-03). A linha de `JanelaRetirada` é criada sob demanda.
- Capacidade efetiva é zero quando a data está bloqueada ou não há configuração para o dia/período.
  Reduzir a capacidade ou bloquear data não altera solicitações existentes (RN-12).
- Estados: `Solicitada → Agendada | Recusada | Cancelada`; `Agendada → Coletada | NaoRealizada |
  Cancelada`. `Coletada`, `NaoRealizada`, `Recusada` e `Cancelada` são finais (RN-03).
- Cancelamento pelo cidadão exige `agora <= início do período - 24 h`; depois disso, a organização
  encerra como `NaoRealizada`.
- Elegibilidade: município atendido e todos os itens com `AceitaRetiradaDomiciliar` (RN-04).
  Limite de solicitações abertas por cidadão/organização vem de `Organizacao` (RN-10).
- Projeção de PII: organização vê nome, telefone e endereço apenas em `Solicitada`/`Agendada`;
  após estado final os campos são nulos e `DadosPessoaisOcultos = true`. O cidadão sempre vê os
  próprios dados (RN-11).
- Volumes são append-only. Ajuste referencia sempre o registro original, exige motivo, tem sinal
  e o total do original nunca fica negativo; cálculo sob lock do original (RN-07). A `DataLocal`
  é calculada na gravação e não é recalculada ao mudar o fuso.
- Relatórios consultam apenas `RegistroVolume`, agrupam por tipo e unidade e nunca somam unidades
  diferentes (RN-05, RN-06). O CSV usa UTF-8 com BOM, `;`, vírgula decimal, identifica ajustes e
  neutraliza células iniciadas por `=`, `+`, `-` ou `@` (RNF-08).
- Notificações são gravadas na mesma transação do evento (DT-14): cidadão em agendamento,
  reagendamento, recusa, coleta e não realização; Administrador/Operador em nova solicitação e
  cancelamento.

## Contratos

- `GET /api/v1/retiradas/organizacoes?municipioId=` e `GET /api/v1/retiradas/janelas`
  (Cidadão).
- `POST /api/v1/retiradas`, `GET /api/v1/retiradas/minhas`, `GET /api/v1/retiradas/minhas/{id}`,
  `POST /api/v1/retiradas/{id}/cancelar` (Cidadão).
- `GET /api/v1/retiradas`, `GET /api/v1/retiradas/{id}` e ações `agendar`, `reagendar`, `recusar`,
  `executar`, `nao-realizada` (Administrador/Operador).
- `GET/PUT /api/v1/disponibilidade-retirada` e `GET/POST/DELETE /api/v1/bloqueios-retirada`
  (Administrador).
- `GET /api/v1/volumes`, `POST /api/v1/volumes/recebimentos` e
  `POST /api/v1/volumes/{id}/ajustes` (Administrador/Operador).
- `GET /api/v1/painel/resumo`, `GET /api/v1/painel/serie` e
  `GET /api/v1/relatorios/volumes.csv` (Administrador).
- `GET /api/v1/notificacoes?desde=&apenasNaoLidas=`, `POST /api/v1/notificacoes/{id}/lida` e
  `POST /api/v1/notificacoes/lidas` (próprio usuário).

ProblemDetails segue o padrão existente; conflitos usam códigos estáveis (`capacidade_esgotada`,
`data_bloqueada`, `estado_invalido`, `limite_de_solicitacoes`, `volume_negativo`,
`quantidade_invalida`). Listas usam `PaginaDe…` com `items`, `page`, `pageSize` e `total` quando
aplicável.

## Persistência e transações

- Uma transação grava solicitação, itens, histórico, notificações, auditoria e janela.
- Execução grava mudança de estado e todos os `RegistroVolume` na mesma transação.
- Ajuste trava o registro original, soma os ajustes existentes e rejeita total negativo.
- A migration é aditiva, com índices para fila da organização, limite por cidadão, volumes por
  tipo/data e notificações por destinatário/leitura. FKs sem cascade físico.
- `RegistroVolume` não tem update/delete no código; a API não expõe `PUT`/`DELETE`.

## Frontend

- Cabeçalho com sino de notificações, contagem de não lidas e polling enquanto autenticado.
- Cidadão: lista/minhas retiradas, nova solicitação em etapas (município → organização → janela →
  endereço/itens) e cancelamento.
- Organização: fila com filtros por estado, detalhe com ações e PII mascarada após estado final;
  disponibilidade e bloqueios; volumes com recebimento e ajuste; painel com resumo, série e CSV.
- Formulários com rótulos, foco, mensagens por campo e estados vazios; nenhuma ação exclusiva do
  Administrador é exibida ao Operador.

## Riscos

- A serialização por organização reduz concorrência entre janelas distintas; suficiente para a v1
  e substituível por lock por janela sem mudar o contrato.
- Fuso IANA depende da base do sistema operacional; o padrão `America/Sao_Paulo` é validado.
- Sem `Idempotency-Key` persistida, reenvios manuais de criação/execução podem duplicar; a UI evita
  duplo envio, e a pendência está registrada nas tarefas.
- O CSV não substitui o painel; diferenças de arredondamento são resolvidas com três casas fixas.
