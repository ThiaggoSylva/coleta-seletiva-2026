# PRD — Plataforma Web de Coleta Seletiva e Pontos de Descarte

- **Status:** Rascunho. Aguardando validação explícita do solicitante; handoff para o TDD ainda não liberado (ver 12.2).
- **Data:** 2026-11-09.
- **Origem:** tema escolhido pelo solicitante para o TCC da Academia do Programador. Estrutura, rigor e nível de detalhe seguem o modelo `locadora-de-veiculos-2026`.
- **Natureza da entrega:** documentação preparatória, sem implementação.
- **Autoria e limites:** rascunho elaborado por assistente de IA a partir do tema, sem entrevista pela ferramenta `question`. Toda decisão de produto marcada como **Proposta** precisa ser confirmada na validação, por exemplo executando `/generate-prd` sobre este arquivo. Nenhuma pessoa, cooperativa ou prefeitura real foi entrevistada.

## 1. Resumo executivo

Disponibilizar uma plataforma web em que **organizações gestoras de resíduos** (cooperativas de catadores, associações e prefeituras) publicam seus pontos de descarte, recebem solicitações de retirada agendada e registram o volume efetivamente coletado, enquanto **cidadãos** encontram o ponto adequado para cada tipo de resíduo e pedem retirada em casa.

O resultado pretendido é facilitar o descarte correto, dar previsibilidade operacional às organizações e produzir dados confiáveis de volume coletado, preservando isolamento entre organizações e a privacidade dos cidadãos.

As especificações detalhadas deste documento correspondem à v1.

## 2. Contexto e problema

Hipóteses de problema (não resultados de pesquisa observacional): moradores não sabem onde descartar pilhas, eletrônicos, óleo de cozinha e materiais recicláveis; organizações de coleta têm dificuldade de divulgar pontos e horários e de organizar retiradas; o volume coletado costuma ser registrado de forma dispersa, o que dificulta prestar contas e planejar a operação.

O processo atual das organizações, os dados de operação e o comportamento dos moradores não foram levantados. Não houve entrevista com cooperativa, prefeitura, operador ou cidadão reais; a validação de uso em campo permanece necessária.

### 2.1 Decisões de produto propostas

| Tema | Proposta a confirmar |
| --- | --- |
| Tenant | Uma organização gestora é um tenant isolado (RF-01, RF-03). Cidadãos são contas globais, sem organização. |
| Perfis | Administrador da organização, Operador da organização e Cidadão (RF-02, RF-04, RF-05). |
| Catálogo | Tipos de resíduo vêm de um catálogo único da plataforma, com unidade fixa por tipo; cada organização habilita os que atende (RF-07, RN-06). |
| Retirada | Cidadão escolhe data e período entre janelas com capacidade configurada pela organização (RF-10, RF-11, RN-01 a RN-04). |
| Volume | Relatórios usam apenas quantidades reais registradas pelo operador, nunca estimativas do cidadão (RN-05, RN-07). |
| Notificações | Somente internas na v1; sem e-mail, SMS ou mensageria (RF-20). |
| Mapa | Visualização geográfica com lista equivalente; sem geocodificação externa na v1 (RF-09, RNF-04). |

### 2.2 Parâmetros iniciais propostos (a validar)

Valores abaixo são hipóteses de partida, configuráveis quando indicado, e não regras definitivas.

| Parâmetro | Valor inicial proposto | Configurável |
| --- | --- | --- |
| Antecedência mínima para solicitar retirada | Dia seguinte (data local da organização) | Não, na v1 |
| Horizonte máximo de solicitação | 30 dias a partir da data local de hoje | Não, na v1 |
| Solicitações abertas por cidadão, por organização | 3 (estados Solicitada e Agendada) | Sim, por organização |
| Cancelamento pelo cidadão | Até 24 horas antes do início do período | Não, na v1 |
| Períodos de retirada | Manhã 08:00–12:00 e Tarde 13:00–17:00 | Sim, por organização |

## 3. Objetivos e métricas de sucesso

| Objetivo | Indicador e medição | Baseline / meta |
| --- | --- | --- |
| Facilitar a descoberta do ponto correto | Cenários de consulta pública por município, tipo, proximidade e "aberto agora" retornam apenas pontos válidos. | Todos os cenários de aceite corretos; uso real a medir após piloto. |
| Evitar excesso de retiradas por janela | Tentativas de ultrapassar a capacidade, inclusive simultâneas. | Nenhuma janela acima da capacidade nos cenários de aceite. |
| Tornar o volume coletado confiável | Comparar painel e CSV com os registros esperados em cenários de aceite, incluindo ajustes. | Concordância em todos os cenários; divergência em produção a medir. |
| Proteger dados entre organizações e cidadãos | Exercitar leitura e escrita com contas de organizações e cidadãos distintos. | Nenhum acesso cruzado nos cenários de aceite. |
| Reduzir esforço operacional | Medir tempo de triagem e registro de uma retirada em teste com operadores. | Baseline e meta a definir após validação com usuários reais. |

Metas quantitativas de adoção, volume, latência, disponibilidade e cobertura de testes não foram definidas nesta etapa.

## 4. Público-alvo e stakeholders

- **Administrador da organização:** registra a organização, configura perfil, tipos, pontos e janelas de retirada, gerencia a equipe e acompanha relatórios.
- **Operador da organização:** atende solicitações, registra execuções e recebimentos. Não gerencia equipe nem configurações da organização.
- **Cidadão:** consulta pontos sem login e, autenticado, solicita e acompanha retiradas.
- **Solicitante:** interlocutor das decisões desta etapa; identidade e papel institucional não presumidos.
- **Organizações reais (cooperativa, associação, prefeitura):** validadoras futuras do modelo operacional; ainda a identificar.
- **Produto, design e engenharia:** consumidores do PRD; responsáveis nominais a definir.

## 5. Escopo da v1

### 5.1 Núcleo da plataforma

Todos os requisitos RF e RNF marcados **P0** são obrigatórios para a v1. Os marcados **P1** pertencem à v1, mas podem ser entregues por último sem invalidar o núcleo. A ordem abaixo indica dependência:

1. Registro de organização com conta administradora, login, logout, papéis e isolamento entre organizações.
2. Perfil da organização, municípios atendidos, fuso, publicação e catálogo de tipos habilitados.
3. Pontos de coleta com localização, horários, tipos aceitos e consulta pública.
4. Conta de cidadão e solicitação de retirada com janelas e capacidade.
5. Triagem, agendamento e execução de retiradas por operadores.
6. Registro de volume (retirada e recebimento em ponto) com correção por ajuste.
7. Painel, exportação e notificações internas.

**Rastreabilidade:** RF-01 a RF-22, RNF-01 a RNF-08, J-01 a J-06 e CA-01 a CA-12.

### 5.2 Evoluções posteriores (fora da v1)

Notificações por e-mail, SMS ou aplicativos de mensagem; gamificação e incentivos por volume entregue; rotas e planejamento de coleta; aplicativo móvel nativo ou modo offline para coletores; fotos de pontos; avaliação de pontos pelos cidadãos; página de impacto ambiental.

### 5.3 Itens fora do escopo da v1

Cálculo de impacto ambiental (por exemplo CO₂ evitado), pois exige fatores de emissão de fonte validada; pagamentos, remuneração e repasses; emissão de manifestos ou certificados de destinação e logística reversa regulamentada; integração com sistemas municipais; geocodificação ou busca de endereço por serviço externo; conversão entre unidades (kg, L, un); administração da própria plataforma (aprovação ou verificação de organizações, moderação, edição do catálogo); recuperação de senha por e-mail; múltiplas organizações por usuário; reabertura de retirada concluída.

## 6. Jornadas e casos de uso — v1

### J-01 — Registrar e preparar a organização

O administrador registra a organização e sua conta, entra, informa municípios atendidos e fuso, habilita tipos de resíduo, cadastra operadores e configura janelas de retirada. Credenciais inválidas não permitem acesso. Enquanto a organização não estiver publicada, ela e seus pontos não aparecem na consulta pública.

### J-02 — Publicar e manter pontos

Cadastra pontos com endereço, localização no mapa, horários e tipos aceitos, publica a organização e mantém status (ativo, inativo, indisponível temporariamente). Formulários inválidos não gravam e indicam os campos a corrigir. Pontos com histórico de recebimentos são retirados logicamente, nunca apagados.

### J-03 — Cidadão encontra um ponto

Sem login, filtra por município, tipo de resíduo e "aberto agora", ou usa a localização do dispositivo para ver os pontos mais próximos, em mapa e em lista equivalente. Sem resultados, vê estado vazio com orientação.

### J-04 — Cidadão solicita retirada

Registra-se, entra, informa o endereço, escolhe a organização que atende seu município, os tipos e quantidades estimadas e uma data e período disponíveis. O sistema valida território, tipos, horizonte, capacidade e limite de solicitações abertas. Acompanha o status, recebe notificações internas e pode cancelar dentro do prazo.

### J-05 — Operador atende a retirada

Vê solicitações da organização por status e data, agenda, reagenda ou recusa com motivo. No dia, registra a execução com quantidades reais por tipo, ou marca como não realizada com motivo. Também registra recebimentos entregues em pontos.

### J-06 — Organização acompanha o volume

Consulta o painel por período, tipo e origem, exporta o relatório em CSV e, opcionalmente, publica indicadores agregados sem dados pessoais. Corrige lançamentos errados por ajuste rastreável.

## 7. Requisitos funcionais — v1

### 7.1 Acesso e organização

- **RF-01 — Registrar organização (P0):** registrar nome da organização, tipo (Cooperativa, Associação, Prefeitura ou Outro) e a conta administradora (usuário, e-mail, senha), criando uma organização isolada. Usuário e e-mail devem ser únicos na plataforma. Organização nasce não publicada.
- **RF-02 — Autenticar, encerrar sessão e alterar senha (P0):** login por usuário ou e-mail e senha para os três perfis; logout; alteração da própria senha. Rejeitar credenciais inválidas com mensagem genérica e impedir operações protegidas sem autenticação.
- **RF-03 — Isolar organizações (P0):** cadastros, configurações, pontos, solicitações, volumes, relatórios e histórico de uma organização não podem ser acessados nem vinculados por outra.
- **RF-04 — Gerir equipe (P0):** o administrador cria, edita e inativa contas de Operador da própria organização (nome, e-mail, usuário, senha inicial). A troca da senha inicial é exigida no primeiro acesso. Conta inativa não autentica e seu nome permanece em registros históricos.
- **RF-05 — Gerir conta de cidadão (P0):** registro de cidadão com nome, e-mail, telefone e senha; edição do próprio perfil. Cidadão não acessa funções de organização.
- **RF-06 — Gerir perfil e publicação da organização (P0):** editar nome, tipo, contato público (e-mail e telefone), fuso da organização (padrão `America/Sao_Paulo`), municípios atendidos (UF e município) e períodos de retirada. O administrador publica ou despublica a organização; a publicação exige ao menos um município e um tipo habilitado (RN-09).

### 7.2 Catálogo e pontos

- **RF-07 — Catálogo e tipos habilitados (P0):** exibir o catálogo da plataforma (código, nome, categoria, unidade fixa, orientações de preparo) e permitir que o administrador habilite os tipos que a organização atende, indicando para cada um se aceita retirada domiciliar. O catálogo é somente leitura na v1.
- **RF-08 — Gerir pontos de coleta (P0):** inserir, editar, listar e retirar logicamente pontos com nome, endereço (UF, município, bairro, rua, número, CEP), coordenadas definidas no mapa, horário de funcionamento por dia da semana (um ou mais intervalos), tipos aceitos (subconjunto dos tipos habilitados), observações e status Ativo, Inativo ou Indisponível temporariamente. Nome único por organização.
- **RF-09 — Consultar pontos publicamente (P0):** consulta sem login, somente leitura, de pontos ativos de organizações publicadas. Filtros: município, tipo de resíduo, "aberto agora" e proximidade a partir da localização informada pelo dispositivo. Mostrar mapa e lista equivalente, horários, tipos aceitos e contato da organização. Nenhum dado pessoal é exposto.

### 7.3 Retiradas

- **RF-10 — Configurar disponibilidade de retirada (P0):** o administrador define, por dia da semana e período, a capacidade (número máximo de retiradas), e pode bloquear datas específicas. Alterar a capacidade não cancela solicitações já agendadas; se a nova capacidade ficar abaixo do ocupado, preserva o ocupado e impede novas solicitações naquela janela.
- **RF-11 — Solicitar retirada (P0):** o cidadão informa organização (que atenda o município do endereço), endereço de retirada com ponto de referência opcional, tipos com quantidade estimada (somente tipos com retirada domiciliar habilitada), data e período com capacidade disponível e observações. Validar RN-01 a RN-04 e RN-10. A solicitação nasce Solicitada.
- **RF-12 — Acompanhar e cancelar (P0):** o cidadão lista suas solicitações com status e histórico de mudanças e pode cancelar conforme RN-03 e RN-10. Não vê solicitações de outros cidadãos.
- **RF-13 — Triar e agendar (P0):** administrador e operador listam solicitações da organização por status, data e município; confirmam (Agendada), reagendam para outra janela com capacidade ou recusam com motivo obrigatório (Recusada). Reagendar mantém o estado Agendada e registra o histórico.
- **RF-14 — Registrar execução (P0):** em solicitação Agendada, o operador marca Coletada, informando as quantidades reais por tipo (maiores que zero, podendo incluir tipos aceitos na retirada que o cidadão não listou), ou marca Não realizada com motivo (Cidadão ausente, Material inadequado, Endereço não localizado, Impossibilidade operacional da organização). Cada quantidade gera um registro de volume (RF-15).

### 7.4 Volumes

- **RF-15 — Registrar volume (P0):** registrar quantidade real por tipo em duas origens: execução de retirada (RF-14) e recebimento em ponto, informado pelo operador com ponto, tipo, quantidade e data e hora. Recebimento em ponto não identifica o cidadão. A unidade é a do tipo.
- **RF-16 — Ajustar volume (P1):** corrigir um registro por lançamento de ajuste positivo ou negativo, com motivo obrigatório, vinculado ao registro original. Registros originais e ajustes não são editados nem apagados (RN-07).

### 7.5 Painel e relatórios

- **RF-17 — Painel da organização (P0):** exibir, para um período e filtros (tipo, origem), o volume por tipo, o total por unidade, a série por dia, semana ou mês, a quantidade de retiradas por status e o volume por origem (retirada ou ponto, com detalhe por ponto). Respeitar RN-05 a RN-07.
- **RF-18 — Exportar CSV (P1):** exportar os registros do período e filtros do painel, com ajustes identificados, em CSV.
- **RF-19 — Página pública de transparência (P1):** se habilitada pelo administrador, página pública da organização com volume agregado por tipo e período, sem dados pessoais.

### 7.6 Notificações e dados

- **RF-20 — Notificações internas (P0):** notificar o cidadão em Agendada, Reagendada, Recusada, Coletada e Não realizada; notificar operadores e administrador de nova solicitação e de cancelamento. Listar, marcar como lida e indicar não lidas. Sem e-mail, SMS ou mensageria.
- **RF-21 — Excluir conta de cidadão (P1):** o cidadão pode solicitar a exclusão da conta; dados pessoais são anonimizados, preservando registros agregados de volume e o histórico necessário às organizações sem identificar a pessoa. Bloquear enquanto houver solicitação Agendada, com explicação.
- **RF-22 — Excluir preservando histórico (P0):** exclusões de pontos, contas de operador e demais cadastros são lógicas, retirando-os de novas operações sem apagar registros de volume e históricos. Ponto com registros de recebimento só pode ser retirado logicamente.

### 7.7 Regras de negócio

- **RN-01 — Janela de retirada:** uma janela é (organização, data local, período). Data e horários seguem o fuso da organização. A data deve respeitar a antecedência mínima e o horizonte máximo da seção 2.2.
- **RN-02 — Capacidade:** a ocupação de uma janela é o número de solicitações em Solicitada ou Agendada nela. Nova solicitação ou reagendamento só é aceito se ocupação < capacidade, inclusive sob concorrência. Concluir, cancelar, recusar ou marcar Não realizada libera a vaga.
- **RN-03 — Estados da solicitação:** Solicitada → Agendada | Recusada | Cancelada; Agendada → Coletada | Não realizada | Cancelada. Coletada, Não realizada, Recusada e Cancelada são finais. O cancelamento pelo cidadão respeita a antecedência de 24 horas antes do início do período; após isso, só a organização encerra a solicitação (Não realizada).
- **RN-04 — Elegibilidade territorial e de tipos:** o município do endereço de retirada deve constar entre os atendidos pela organização, e cada tipo solicitado deve estar habilitado para retirada domiciliar nela.
- **RN-05 — Quantidade real prevalece:** estimativas do cidadão orientam a triagem, mas painel, exportação e transparência usam somente registros de volume reais.
- **RN-06 — Unidades:** cada tipo tem uma unidade fixa (kg, L ou un). Totais são agrupados por tipo; totais gerais são apresentados por unidade, sem somar unidades diferentes e sem conversão.
- **RN-07 — Imutabilidade do volume:** registros de volume não são editados nem apagados. O total de um registro é o original somado aos seus ajustes e nunca pode ficar negativo. Quantidades têm até três casas decimais.
- **RN-08 — Aberto agora:** um ponto está aberto quando o status é Ativo e o instante, no fuso da organização, cai em um intervalo do dia da semana, com início inclusivo e fim exclusivo. Indisponível temporariamente e Inativo nunca estão abertos.
- **RN-09 — Visibilidade pública:** uma organização aparece publicamente se estiver publicada e tiver município e tipo habilitados; um ponto aparece se estiver Ativo e não excluído. Despublicar remove tudo da consulta pública sem apagar dados.
- **RN-10 — Limite de abertas:** um cidadão pode ter até o limite configurado de solicitações em Solicitada ou Agendada por organização (inicial: 3).
- **RN-11 — Visibilidade de dados pessoais:** nome, telefone e endereço do cidadão em uma solicitação são visíveis somente à organização destinatária, enquanto ela for responsável pela solicitação, e ao próprio cidadão. A política de retenção está pendente (seção 12).

## 8. Requisitos não funcionais

- **RNF-01 — Segurança de acesso (P0):** autenticação e autorização por papel e por organização em todas as operações protegidas; senhas nunca armazenadas de forma recuperável nem exibidas. Contas de organização devem poder habilitar autenticação em dois fatores por TOTP (P1). Verificar negação sem sessão, entre papéis e entre organizações.
- **RNF-02 — Privacidade (P0):** restringir dados pessoais conforme RN-11; a localização do dispositivo usada na busca por proximidade não é persistida; consultas públicas não expõem dados pessoais; senhas, tokens e dados pessoais não aparecem em logs ou mensagens de erro. Não se presume conformidade legal certificada nem prazo de retenção.
- **RNF-03 — Consistência (P0):** reserva de vaga e criação ou reagendamento da solicitação são uma única operação; execução de retirada e seus registros de volume são gravados juntos ou não são gravados. Repetições e concorrência não geram excesso de capacidade nem registros duplicados.
- **RNF-04 — Usabilidade e acessibilidade (P0):** interface em português do Brasil, adequada a celular e desktop, rótulos em campos, operação por teclado, foco visível, erros associados aos campos, estados vazios compreensíveis, sem depender só de cor. Toda informação do mapa tem equivalente em lista.
- **RNF-05 — Rastreabilidade (P0):** registrar organização, ator, momento e tipo de alteração crítica (publicação, pontos, capacidade, mudança de estado, registros e ajustes de volume, equipe), sem dados pessoais desnecessários.
- **RNF-06 — Desempenho e disponibilidade (P0):** metas, volume de dados, concorrência esperada e disponibilidade a definir com contexto de uso. O TDD deve explicitar hipóteses e plano de medição, sem números arbitrários como compromisso.
- **RNF-07 — Integridade histórica e de relatórios (P0):** relatórios devem poder ser recompostos a partir dos registros originais e ajustes, e permanecer verificáveis após alterações ou exclusões de cadastros.
- **RNF-08 — Proteção contra abuso (P0):** limitar tentativas em login, cadastros e consultas públicas e proteger exportações contra injeção de fórmulas em planilhas.

## 9. Critérios de aceite

| ID | Requisitos | Cenários verificáveis |
| --- | --- | --- |
| CA-01 | RF-01–03; RNF-01–02 | Registro válido cria organização e administrador; usuário ou e-mail duplicado é rejeitado; senha incorreta não autentica; sem sessão, operações protegidas são negadas; organização A e cidadão X não leem nem alteram dados de B e Y, mesmo conhecendo identificadores. |
| CA-02 | RF-04–06; RF-22 | Administrador cria e inativa operador; operador inativo não entra; troca de senha inicial é exigida; operador não gerencia equipe; publicar sem município ou sem tipo habilitado é rejeitado; despublicar remove a organização da consulta pública sem apagar dados. |
| CA-03 | RF-07–08 | Tipos habilitados limitam os tipos aceitos nos pontos; ponto sem coordenadas, sem horário ou com tipo não habilitado é rejeitado; nome repetido na organização é rejeitado; ponto com recebimentos só é retirado logicamente. |
| CA-04 | RF-09; RN-08–09; RNF-02 | Consulta pública sem login retorna apenas pontos ativos de organizações publicadas; filtros por município, tipo e proximidade funcionam; resposta não contém dado pessoal; lista traz as mesmas informações do mapa. |
| CA-05 | RN-08 | Ponto seg–sex 08:00–17:00 no fuso da organização: às 07:59 fechado; às 08:00 aberto; às 17:00 fechado; no sábado fechado; Indisponível temporariamente fechado mesmo dentro do horário. |
| CA-06 | RF-11; RN-01; RN-04; RN-10 | Com hoje em 2026-11-09: solicitar para 2026-11-09 é rejeitado; 2026-11-10 e 2026-12-09 são aceitos; 2026-12-10 é rejeitado. Município fora dos atendidos e tipo sem retirada domiciliar são rejeitados. A 4ª solicitação aberta do cidadão na mesma organização é rejeitada; cancelar ou concluir uma libera a vaga. |
| CA-07 | RF-10–11; RN-02; RNF-03 | Janela com capacidade 2: duas solicitações são aceitas e a terceira é rejeitada; três solicitações simultâneas para a última vaga resultam em apenas uma aceita; cancelar libera a vaga; reduzir a capacidade abaixo do ocupado preserva as agendadas e bloqueia novas. |
| CA-08 | RF-12–14; RN-03; RF-20 | Transições válidas geram histórico e notificação; transições inválidas (por exemplo Coletada → Agendada) são rejeitadas; cancelamento 24 h antes do início do período é aceito e 23 h 59 min antes é rejeitado; recusa exige motivo; Não realizada exige motivo da lista. |
| CA-09 | RF-14–17; RN-05–07 | Papel: recebimento 10,500 kg + retirada 4,250 kg + ajuste −1,000 kg resultam em 13,750 kg; óleo 8,000 L aparece separado; total geral é exibido como 13,750 kg e 8,000 L, nunca somados; ajuste que deixe o total do registro negativo é rejeitado; registros originais não podem ser editados nem apagados; estimativa do cidadão não entra no painel. |
| CA-10 | RF-17–19; RNF-08 | Painel e CSV concordam em total por tipo e período; filtros por tipo e origem funcionam; ajustes aparecem identificados no CSV; células que começam com `=`, `+`, `-` ou `@` são neutralizadas; página pública de transparência não contém dado pessoal. |
| CA-11 | RF-21–22; RNF-05; RNF-07 | Exclusão de conta de cidadão anonimiza dados pessoais e preserva volumes; é bloqueada com solicitação Agendada; alterações críticas geram registro de auditoria sem segredos; excluir ponto ou operador não altera relatórios nem histórico. |
| CA-12 | RNF-01; RNF-04; RNF-06 | J-01 a J-06 são operáveis por teclado em celular e desktop, com rótulos e erros identificáveis; o mapa tem equivalente em lista; ausência de metas quantitativas aprovadas é registrada no TDD com forma de validação. |

Os cenários são critérios futuros de aceitação, não testes executados nem evidência de produto implementado.

## 10. Dados e integrações

| Entidade conceitual | Informações e relações |
| --- | --- |
| Organização | Nome, tipo, contato público, fuso, municípios atendidos, períodos de retirada, publicada ou não, transparência habilitada. |
| Usuário | Perfil (Administrador, Operador ou Cidadão), nome, e-mail, usuário, credencial protegida; Administrador e Operador pertencem a uma organização. |
| Tipo de resíduo | Catálogo da plataforma: código, nome, categoria, unidade fixa, orientações. |
| Tipo habilitado | Organização, tipo e indicação de retirada domiciliar. |
| Ponto de coleta | Organização, nome, endereço, coordenadas, horários por dia da semana, tipos aceitos, status. |
| Disponibilidade | Organização, dia da semana, período, capacidade; bloqueios de datas. |
| Solicitação de retirada | Cidadão, organização, endereço e contato copiados no momento do pedido, janela, itens e quantidades estimadas, estado, motivo e histórico de estados. |
| Registro de volume | Organização, tipo, quantidade, origem (retirada, recebimento em ponto ou ajuste), referência à solicitação, ao ponto ou ao registro original, operador, instante e motivo (ajustes). |
| Notificação | Destinatário, evento, referência e situação de leitura. |
| Auditoria | Organização, ator, ação, entidade e momento, sem dados pessoais desnecessários. |

Cadastros nascem ativos e saem das novas operações por exclusão lógica. Solicitações transitam conforme RN-03. Dados históricos necessários à leitura de solicitações e volumes sobrevivem a alterações dos cadastros de origem; a representação técnica é decidida no TDD.

**Integrações:** nenhuma integração externa de negócio na v1. Serão necessários, a decidir no TDD: fonte para a lista oficial de municípios e provedor de mapa e tiles. Endereços informados são declaratórios; não há validação por serviço externo.

## 11. Riscos, premissas e dependências

| Tema | Impacto / ação |
| --- | --- |
| Validação limitada ao solicitante | Adequação à rotina real não demonstrada; validar jornadas com uma cooperativa ou setor de limpeza urbana antes de uso real. |
| Organizações sem verificação de identidade | Qualquer pessoa pode registrar uma organização; pontos falsos poderiam ser publicados. Mitigação: publicação explícita e, antes de produção, definir verificação manual (administração de plataforma está fora da v1). |
| Dados pessoais de cidadãos | Endereço e telefone de retirada são sensíveis para a privacidade; validar base legal, retenção e atendimento a solicitações antes de produção, sem presumir conformidade. |
| Resíduos regulados | Pilhas, baterias e eletroeletrônicos têm regras de logística reversa; o catálogo inicial é proposta e deve ser validado com a organização e a regulação local. |
| Qualidade da localização | Coordenadas escolhidas manualmente podem ficar imprecisas; oferecer conferência no mapa e exibir endereço textual. |
| Dependência de mapa e tiles | Uso gratuito de tiles públicos tem políticas de uso; avaliar provedor adequado antes de produção. |
| Concorrência por vagas | Janelas muito disputadas dependem de consistência forte; coberto por RNF-03 e CA-07. |
| Adoção | Sem cidadãos e organizações usando, a plataforma não gera valor; métricas de adoção a definir após piloto. |
| Ausência de e-mail | Cidadão precisa abrir a plataforma para saber do status; avaliar canal externo na evolução. |
| Metas operacionais ausentes | Não prometer capacidade, disponibilidade ou recuperação; definir contexto e metas antes de compromisso de produção. |

Premissas propostas: português do Brasil, fuso por organização com padrão `America/Sao_Paulo`, v1 sem pagamentos e sem integração externa de negócio. Não se escolhem tecnologias neste PRD.

## 12. Questões em aberto e próximos passos

### 12.1 Pendências

| Pendência | Classificação | Validação / responsável sugerido |
| --- | --- | --- |
| Confirmar as decisões propostas (seções 2.1 e 2.2) e as prioridades P0/P1 | Bloqueante para validar o PRD | Solicitante, via `/generate-prd`. |
| Validar o catálogo inicial de tipos e unidades | Bloqueante para o catálogo definitivo; não impede o desenho técnico | Solicitante e representante de organização a identificar. |
| Baseline e metas de desempenho, disponibilidade e adoção | Não bloqueante para o desenho; necessária antes de compromissos operacionais | Produto e representante de organização a identificar. |
| Retenção de dados pessoais, base legal e atendimento a titulares | Não bloqueante para o desenho inicial; resolver antes de produção | Produto, segurança e operação a designar. |
| Verificação de identidade das organizações | Não bloqueante para o TCC; necessária antes de produção | Produto e operação a designar. |
| Fonte da lista oficial de municípios e provedor de mapa | Decisão técnica no TDD | Engenharia. |

### 12.2 Handoff para TDD

- **Entrada:** `docs/prd.md`; escopo limitado à v1.
- **Destino padrão:** `docs/tdd.md`.
- **Estado:** **bloqueado**, pois o PRD está em `Rascunho`. Liberar somente após aprovação explícita pela ferramenta `question`, quando o status passará a `Validado para TDD`.
- **Escopo a validar:** RF-01 a RF-22, RNF-01 a RNF-08, jornadas J-01 a J-06 e critérios CA-01 a CA-12.
- **Regras essenciais:** RN-01 a RN-11; janelas e capacidade por organização, estados da solicitação, imutabilidade do volume com ajustes, unidades fixas sem conversão, visibilidade pública e privacidade de dados pessoais.
- **Limites:** seções 5.2 e 5.3.
- **Dependências e pendências:** seção 12.1 e riscos da seção 11.
- **Entrevista técnica futura:** linguagem, frameworks, bibliotecas, arquitetura, autenticação e sessão, modelo de isolamento, geolocalização e mapa, fonte de municípios, concorrência de capacidade, precisão de quantidades, relatórios e CSV, notificações internas, observabilidade, testes, estrutura em `app/`, execução e entrega. O TDD de rascunho desta pasta contém propostas para cada ponto, ainda não aprovadas.
- **Próximo passo:** após validar, usar `/generate-tdd Use docs/prd.md como base validada`.
