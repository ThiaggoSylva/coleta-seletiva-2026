# PRD — Plataforma Web de Coleta Seletiva e Pontos de Descarte

- **Status:** Validado para TDD em 2026-11-10, por aprovação explícita do solicitante pela ferramenta `question`; handoff liberado (ver 12.2).
- **Data:** 2026-11-10 (rascunho original de 2026-11-09; revisão e entrevista de validação em 2026-11-10).
- **Origem:** tema escolhido pelo solicitante para o TCC da Academia do Programador. Estrutura, rigor e nível de detalhe seguem o modelo `locadora-de-veiculos-2026`. Pedido de revisão e validação de `docs/prd.md` registrado nesta data.
- **Natureza da entrega:** documentação preparatória, sem implementação.
- **Autoria e limites:** rascunho inicial elaborado por assistente de IA a partir do tema. Revisão de produto e entrevista de validação realizadas com o solicitante pela ferramenta `question` em 2026-11-10, em duas rodadas, com aprovação final do conteúdo na mesma data. Nenhuma pessoa, cooperativa ou prefeitura real foi entrevistada; a validação de uso em campo permanece necessária. Identidade e papel institucional do solicitante não são presumidos.

## 1. Resumo executivo

Disponibilizar uma plataforma web em que **organizações gestoras de resíduos** (cooperativas de catadores, associações e prefeituras) publicam seus pontos de descarte, recebem solicitações de retirada agendada e registram o volume efetivamente coletado, enquanto **cidadãos** encontram o ponto adequado para cada tipo de resíduo e pedem retirada em casa.

O resultado pretendido é facilitar o descarte correto, dar previsibilidade operacional às organizações e produzir dados confiáveis de volume coletado, preservando isolamento entre organizações e a privacidade dos cidadãos.

As especificações detalhadas deste documento correspondem à v1.

## 2. Contexto e problema

Hipóteses de problema (não resultados de pesquisa observacional): moradores não sabem onde descartar pilhas, eletrônicos, óleo de cozinha e materiais recicláveis; organizações de coleta têm dificuldade de divulgar pontos e horários e de organizar retiradas; o volume coletado costuma ser registrado de forma dispersa, o que dificulta prestar contas e planejar a operação.

O processo atual das organizações, os dados de operação e o comportamento dos moradores não foram levantados. Não houve entrevista com cooperativa, prefeitura, operador ou cidadão reais; a validação de uso em campo permanece necessária.

### 2.1 Decisões de produto confirmadas

| Tema | Decisão confirmada |
| --- | --- |
| Tenant | Uma organização gestora é um tenant isolado (RF-01, RF-03). Cidadãos são contas globais, sem organização. |
| Perfis | Administrador da organização, Operador da organização e Cidadão (RF-02, RF-04, RF-05). |
| Catálogo | Tipos de resíduo vêm de um catálogo único da plataforma, com unidade fixa por tipo; cada organização habilita os que atende (RF-07, RN-06). O catálogo inicial de tipos e unidades permanece pendente de validação com uma organização (12.1). |
| Retirada | Cidadão escolhe data e período entre janelas com capacidade configurada pela organização (RF-10, RF-11, RN-01 a RN-04). |
| Volume | Relatórios usam apenas quantidades reais registradas pelo operador, nunca estimativas do cidadão (RN-05, RN-07). |
| Notificações | Somente internas na v1; sem e-mail, SMS ou mensageria (RF-20). |
| Mapa | Visualização geográfica com lista equivalente; sem geocodificação externa na v1 (RF-09, RNF-04). |
| Cadastro de organizações | Aberto, sem verificação de identidade na v1; a publicação explícita pelo administrador é o controle de visibilidade (RF-01, RF-06, RN-09). |
| Login | Cidadão entra por e-mail e senha; Administrador e Operador podem usar usuário ou e-mail (RF-02, RF-05). |
| Dados pessoais | Nome, telefone e endereço visíveis à organização apenas enquanto a solicitação estiver em Solicitada ou Agendada; após o estado final, a organização deixa de vê-los, e o cidadão continua vendo os próprios dados (RN-11). |
| Endereço de retirada | UF e município selecionados em lista; bairro, rua, número, complemento, CEP e referência permanecem textuais (RF-11, RN-04). |
| Exclusão de conta | Bloqueada enquanto houver solicitação em Solicitada ou Agendada (RF-21). |
| Ajuste de volume | Administrador e Operador, com motivo obrigatório e registro de auditoria (RF-16). |
| Relatórios | Painel e CSV restritos ao Administrador; o CSV não contém dados pessoais (RF-17, RF-18, RNF-02). |
| Gestão de pontos | Somente o Administrador cadastra, edita e retira pontos; o município do ponto deve estar entre os atendidos e o ponto deve aceitar ao menos um tipo habilitado (RF-08). |
| Bloqueio e despublicação | Bloquear uma data ou despublicar a organização não cancela solicitações existentes; apenas impede novas solicitações e remove a visibilidade pública conforme RN-12. |
| Horário de funcionamento | Intervalos devem começar e terminar no mesmo dia; horários que atravessam a meia-noite não são suportados na v1 (RF-08, RN-08). |

### 2.2 Parâmetros iniciais confirmados

Valores confirmados na entrevista de 2026-11-10, configuráveis quando indicado. Não são metas de desempenho nem compromissos de capacidade operacional.

| Parâmetro | Valor inicial confirmado | Configurável |
| --- | --- | --- |
| Antecedência mínima para solicitar retirada | Dia seguinte (data local da organização) | Não, na v1 |
| Horizonte máximo de solicitação | 30 dias a partir da data local de hoje | Não, na v1 |
| Solicitações abertas por cidadão, por organização | 3 (estados Solicitada e Agendada) | Sim, por organização |
| Cancelamento pelo cidadão | Até 24 horas antes do início do período | Não, na v1 |
| Períodos de retirada | Manhã 08:00–12:00 e Tarde 13:00–17:00 | Sim, por organização |

### 2.3 Registro da entrevista de validação

- **Entrevistado:** solicitante do projeto; identidade e papel institucional não registrados neste documento.
- **Método:** ferramenta `question`, em duas rodadas de entrevista e uma rodada de aprovação, realizadas em 2026-11-10.
- **Confirmado:** atualização e mesclagem de `docs/prd.md`; validação restrita à v1; decisões da seção 2.1; parâmetros da seção 2.2; prioridades P0/P1; cadastro aberto de organizações com publicação explícita; login do cidadão por e-mail e senha; ocultação de dados pessoais da organização após o estado final da solicitação; catálogo inicial pendente de validação; endereço de retirada com UF e município estruturados; exclusão de conta bloqueada com solicitação Solicitada ou Agendada; ajuste de volume por Administrador e Operador; painel e CSV restritos ao Administrador; CSV sem dados pessoais; gestão de pontos pelo Administrador; ponto em município atendido e com ao menos um tipo habilitado; bloqueio de data e despublicação sem cancelamento de solicitações existentes; horários de funcionamento sem atravessar a meia-noite.
- **Aprovação final:** conteúdo aprovado para TDD em 2026-11-10, por escolha explícita do solicitante na ferramenta `question`.
- **Não validado em campo:** rotina de cooperativas, associações ou prefeituras reais, catálogo inicial, metas quantitativas e política de retenção. Ver seções 11 e 12.

## 3. Objetivos e métricas de sucesso

| Objetivo | Indicador e medição | Baseline / meta |
| --- | --- | --- |
| Facilitar a descoberta do ponto correto | Cenários de consulta pública por município, tipo, proximidade e "aberto agora" retornam apenas pontos válidos. | Todos os cenários de aceite corretos; uso real a medir após piloto. |
| Evitar excesso de retiradas por janela | Tentativas de ultrapassar a capacidade, inclusive simultâneas. | Nenhuma janela acima da capacidade nos cenários de aceite. |
| Tornar o volume coletado confiável | Comparar painel e CSV com os registros esperados em cenários de aceite, incluindo ajustes. | Concordância em todos os cenários; divergência em produção a medir. |
| Proteger dados entre organizações e cidadãos | Exercitar leitura e escrita com contas de organizações e cidadãos distintos. | Nenhum acesso cruzado nos cenários de aceite. |
| Reduzir esforço operacional | Medir tempo de triagem e registro de uma retirada em teste com operadores. | Baseline e meta a definir após validação com usuários reais. |

Metas quantitativas de adoção, volume, latência, disponibilidade e cobertura de testes não foram definidas nesta etapa. A ausência de baseline e a forma de validação estão registradas na seção 12.1.

## 4. Público-alvo e stakeholders

- **Administrador da organização:** registra a organização, configura perfil, tipos, pontos e janelas de retirada, gerencia a equipe, consulta relatórios e exporta CSV.
- **Operador da organização:** atende solicitações, registra execuções, recebimentos e ajustes de volume. Não gerencia equipe, pontos, configurações nem relatórios da organização.
- **Cidadão:** consulta pontos sem login e, autenticado, solicita e acompanha retiradas.
- **Solicitante:** interlocutor das decisões desta etapa e entrevistado na validação de 2026-11-10; identidade e papel institucional não presumidos.
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

Notificações por e-mail, SMS ou aplicativos de mensagem; gamificação e incentivos por volume entregue; rotas e planejamento de coleta; aplicativo móvel nativo ou modo offline para coletores; fotos de pontos; avaliação de pontos pelos cidadãos; página de impacto ambiental; horários de funcionamento que atravessam a meia-noite.

### 5.3 Itens fora do escopo da v1

Cálculo de impacto ambiental (por exemplo CO₂ evitado), pois exige fatores de emissão de fonte validada; pagamentos, remuneração e repasses; emissão de manifestos ou certificados de destinação e logística reversa regulamentada; integração com sistemas municipais; geocodificação ou busca de endereço por serviço externo; conversão entre unidades (kg, L, un); administração da própria plataforma (aprovação ou verificação de organizações, moderação, edição do catálogo); recuperação de senha por e-mail; múltiplas organizações por usuário; reabertura de retirada concluída.

## 6. Jornadas e casos de uso — v1

### J-01 — Registrar e preparar a organização

O administrador registra a organização e sua conta em cadastro aberto, sem verificação de identidade na v1, entra, informa municípios atendidos e fuso, habilita tipos de resíduo, cadastra operadores e configura janelas de retirada. Credenciais inválidas não permitem acesso. Enquanto a organização não estiver publicada, ela e seus pontos não aparecem na consulta pública.

### J-02 — Publicar e manter pontos

O administrador cadastra pontos com endereço (UF e município selecionados em lista; os demais campos são textuais), localização no mapa, horários e tipos aceitos, publica a organização e mantém status (ativo, inativo, indisponível temporariamente). O ponto deve estar em município atendido pela organização e aceitar ao menos um tipo habilitado. Formulários inválidos não gravam e indicam os campos a corrigir. Pontos com histórico de recebimentos são retirados logicamente, nunca apagados.

### J-03 — Cidadão encontra um ponto

Sem login, filtra por município, tipo de resíduo e "aberto agora", ou usa a localização do dispositivo para ver os pontos mais próximos, em mapa e em lista equivalente. Sem resultados, vê estado vazio com orientação.

### J-04 — Cidadão solicita retirada

Registra-se, entra, informa o endereço de retirada com UF e município selecionados em lista e os demais campos textuais, escolhe a organização que atende seu município, os tipos e quantidades estimadas e uma data e período disponíveis. O sistema valida território, tipos, horizonte, capacidade e limite de solicitações abertas. Acompanha o status, recebe notificações internas e pode cancelar dentro do prazo.

### J-05 — Operador atende a retirada

Vê solicitações da organização por status e data, agenda, reagenda ou recusa com motivo. No dia, registra a execução com quantidades reais por tipo, ou marca como não realizada com motivo. Também registra recebimentos entregues em pontos e ajustes de volume.

### J-06 — Organização acompanha o volume

O administrador consulta o painel por período, tipo e origem, exporta o relatório em CSV sem dados pessoais e, opcionalmente, publica indicadores agregados sem dados pessoais. Administrador e operador corrigem lançamentos errados por ajuste rastreável.

## 7. Requisitos funcionais — v1

### 7.1 Acesso e organização

- **RF-01 — Registrar organização (P0):** registrar nome da organização, tipo (Cooperativa, Associação, Prefeitura ou Outro) e a conta administradora (usuário, e-mail, senha), criando uma organização isolada. O cadastro é aberto e não exige verificação de identidade na v1. Usuário e e-mail devem ser únicos na plataforma. A organização nasce não publicada; a publicação é o controle de visibilidade (RF-06, RN-09).
- **RF-02 — Autenticar, encerrar sessão e alterar senha (P0):** login por usuário ou e-mail e senha para Administrador e Operador; login por e-mail e senha para Cidadão; logout; alteração da própria senha. Rejeitar credenciais inválidas com mensagem genérica e impedir operações protegidas sem autenticação.
- **RF-03 — Isolar organizações (P0):** cadastros, configurações, pontos, solicitações, volumes, relatórios e histórico de uma organização não podem ser acessados nem vinculados por outra.
- **RF-04 — Gerir equipe (P0):** o administrador cria, edita e inativa contas de Operador da própria organização (nome, e-mail, usuário, senha inicial). A troca da senha inicial é exigida no primeiro acesso. Conta inativa não autentica e seu nome permanece em registros históricos. O Operador não gerencia equipe, pontos, configurações nem relatórios.
- **RF-05 — Gerir conta de cidadão (P0):** registro de cidadão com nome, e-mail, telefone e senha; login por e-mail e senha; edição do próprio perfil. O e-mail é único na plataforma. Cidadão não acessa funções de organização.
- **RF-06 — Gerir perfil e publicação da organização (P0):** editar nome, tipo, contato público (e-mail e telefone), fuso da organização (padrão `America/Sao_Paulo`), municípios atendidos (UF e município), períodos de retirada e limite de solicitações abertas por cidadão (padrão 3). O administrador publica ou despublica a organização; a publicação exige ao menos um município e um tipo habilitado (RN-09). Despublicar não cancela solicitações existentes (RN-12).

### 7.2 Catálogo e pontos

- **RF-07 — Catálogo e tipos habilitados (P0):** exibir o catálogo da plataforma (código, nome, categoria, unidade fixa, orientações de preparo) e permitir que o administrador habilite os tipos que a organização atende, indicando para cada um se aceita retirada domiciliar. O catálogo é somente leitura na v1; o catálogo inicial de tipos e unidades será validado com uma organização (12.1).
- **RF-08 — Gerir pontos de coleta (P0):** o administrador insere, edita, lista e retira logicamente pontos com nome, endereço (UF, município, bairro, rua, número, complemento, CEP), coordenadas definidas no mapa, horário de funcionamento por dia da semana (um ou mais intervalos no mesmo dia), tipos aceitos (subconjunto não vazio dos tipos habilitados), observações e status Ativo, Inativo ou Indisponível temporariamente. O município do ponto deve constar entre os municípios atendidos pela organização. Nome único por organização. Horários que atravessam a meia-noite não são aceitos na v1 (RN-08).
- **RF-09 — Consultar pontos publicamente (P0):** consulta sem login, somente leitura, de pontos ativos de organizações publicadas. Filtros: município, tipo de resíduo, "aberto agora" e proximidade a partir da localização informada pelo dispositivo. Mostrar mapa e lista equivalente, horários, tipos aceitos e contato da organização. Nenhum dado pessoal é exposto.

### 7.3 Retiradas

- **RF-10 — Configurar disponibilidade de retirada (P0):** o administrador define, por dia da semana e período, a capacidade (número máximo de retiradas), e pode bloquear datas específicas. Alterar a capacidade não cancela solicitações já agendadas; se a nova capacidade ficar abaixo do ocupado, preserva o ocupado e impede novas solicitações naquela janela. Bloquear uma data com solicitações existentes preserva essas solicitações e impede apenas novas (RN-12).
- **RF-11 — Solicitar retirada (P0):** o cidadão informa organização (que atenda o município do endereço), endereço de retirada com UF e município selecionados em lista, bairro, rua, número, complemento, CEP e ponto de referência opcional, tipos com quantidade estimada maior que zero (somente tipos com retirada domiciliar habilitada), data e período com capacidade disponível e observações. Validar RN-01 a RN-04 e RN-10. A solicitação nasce Solicitada.
- **RF-12 — Acompanhar e cancelar (P0):** o cidadão lista suas solicitações com status e histórico de mudanças e pode cancelar conforme RN-03 e RN-10. Não vê solicitações de outros cidadãos.
- **RF-13 — Triar e agendar (P0):** administrador e operador listam solicitações da organização por status, data e município; confirmam (Agendada), reagendam para outra janela válida conforme RN-01 e RN-02 ou recusam com motivo obrigatório (Recusada). Reagendar mantém o estado Agendada e registra o histórico.
- **RF-14 — Registrar execução (P0):** em solicitação Agendada, o operador marca Coletada, informando as quantidades reais por tipo (maiores que zero, podendo incluir tipos habilitados para retirada domiciliar que o cidadão não listou), ou marca Não realizada com motivo (Cidadão ausente, Material inadequado, Endereço não localizado, Impossibilidade operacional da organização). Cada quantidade gera um registro de volume (RF-15).

### 7.4 Volumes

- **RF-15 — Registrar volume (P0):** registrar quantidade real por tipo em duas origens: execução de retirada (RF-14) e recebimento em ponto, informado pelo operador com ponto, tipo, quantidade e data e hora. Recebimento em ponto não identifica o cidadão. A unidade é a do tipo.
- **RF-16 — Ajustar volume (P1):** administrador e operador podem corrigir um registro por lançamento de ajuste positivo ou negativo, com motivo obrigatório, vinculado ao registro original. Registros originais e ajustes não são editados nem apagados (RN-07).

### 7.5 Painel e relatórios

- **RF-17 — Painel da organização (P0):** o administrador consulta, para um período e filtros (tipo, origem), o volume por tipo, o total por unidade, a série por dia, semana ou mês, a quantidade de retiradas por status e o volume por origem (retirada ou ponto, com detalhe por ponto). Respeitar RN-05 a RN-07.
- **RF-18 — Exportar CSV (P1):** o administrador exporta os registros do período e filtros do painel, com ajustes identificados, em CSV, sem nome, telefone ou endereço de cidadão.
- **RF-19 — Página pública de transparência (P1):** se habilitada pelo administrador e a organização estiver publicada, página pública com volume agregado por tipo e período, sem dados pessoais.

### 7.6 Notificações e dados

- **RF-20 — Notificações internas (P0):** notificar o cidadão em Agendada, Reagendada, Recusada, Coletada e Não realizada; notificar operadores e administrador de nova solicitação e de cancelamento. Listar, marcar como lida e indicar não lidas. Sem e-mail, SMS ou mensageria.
- **RF-21 — Excluir conta de cidadão (P1):** o cidadão pode solicitar a exclusão da conta; dados pessoais são anonimizados, preservando registros agregados de volume e o histórico necessário às organizações sem identificar a pessoa. Bloquear enquanto houver solicitação em Solicitada ou Agendada, com explicação.
- **RF-22 — Excluir preservando histórico (P0):** exclusões de pontos, contas de operador e demais cadastros são lógicas, retirando-os de novas operações sem apagar registros de volume e históricos. Ponto com registros de recebimento só pode ser retirado logicamente.

### 7.7 Regras de negócio

- **RN-01 — Janela de retirada:** uma janela é (organização, data local, período). Data e horários seguem o fuso da organização. A data deve respeitar a antecedência mínima e o horizonte máximo da seção 2.2.
- **RN-02 — Capacidade:** a ocupação de uma janela é o número de solicitações em Solicitada ou Agendada nela. Nova solicitação ou reagendamento só é aceito se ocupação < capacidade, inclusive sob concorrência. Concluir, cancelar, recusar ou marcar Não realizada libera a vaga.
- **RN-03 — Estados da solicitação:** Solicitada → Agendada | Recusada | Cancelada; Agendada → Coletada | Não realizada | Cancelada. Coletada, Não realizada, Recusada e Cancelada são finais. O cancelamento pelo cidadão respeita a antecedência de 24 horas antes do início do período, no fuso da organização; após isso, só a organização encerra a solicitação (Não realizada).
- **RN-04 — Elegibilidade territorial e de tipos:** o município do endereço de retirada deve constar entre os atendidos pela organização, e cada tipo solicitado deve estar habilitado para retirada domiciliar nela.
- **RN-05 — Quantidade real prevalece:** estimativas do cidadão orientam a triagem, mas painel, exportação e transparência usam somente registros de volume reais.
- **RN-06 — Unidades:** cada tipo tem uma unidade fixa (kg, L ou un). Totais são agrupados por tipo; totais gerais são apresentados por unidade, sem somar unidades diferentes e sem conversão.
- **RN-07 — Imutabilidade do volume:** registros de volume não são editados nem apagados. O total de um registro é o original somado aos seus ajustes e nunca pode ficar negativo. Quantidades têm até três casas decimais.
- **RN-08 — Aberto agora:** um ponto está aberto quando o status é Ativo e o instante, no fuso da organização, cai em um intervalo do dia da semana, com início inclusivo e fim exclusivo. Indisponível temporariamente e Inativo nunca estão abertos. Os intervalos devem começar e terminar no mesmo dia; horários que atravessam a meia-noite não são suportados na v1.
- **RN-09 — Visibilidade pública:** uma organização aparece publicamente se estiver publicada e tiver município e tipo habilitados; um ponto aparece se estiver Ativo e não excluído. Despublicar remove tudo da consulta pública sem apagar dados e sem cancelar solicitações existentes (RN-12).
- **RN-10 — Limite de abertas:** um cidadão pode ter até o limite configurado de solicitações em Solicitada ou Agendada por organização (inicial: 3).
- **RN-11 — Visibilidade de dados pessoais:** nome, telefone e endereço do cidadão em uma solicitação são visíveis à organização destinatária somente enquanto a solicitação estiver em Solicitada ou Agendada, e ao próprio cidadão em qualquer estado. Após Coletada, Não realizada, Recusada ou Cancelada, a organização deixa de visualizar esses dados pessoais; o histórico operacional e os registros de volume permanecem. A retenção e a anonimização definitiva estão pendentes (seção 12).
- **RN-12 — Bloqueio de data e despublicação:** bloquear uma data não cancela solicitações em Solicitada ou Agendada nela existentes e impede apenas novas solicitações; despublicar a organização não cancela solicitações existentes, apenas remove a visibilidade pública. Em ambos os casos, o histórico e os compromissos assumidos permanecem.

## 8. Requisitos não funcionais

- **RNF-01 — Segurança de acesso (P0):** autenticação e autorização por papel e por organização em todas as operações protegidas; senhas nunca armazenadas de forma recuperável nem exibidas. Contas de organização devem poder habilitar autenticação em dois fatores por TOTP (P1). Verificar negação sem sessão, entre papéis e entre organizações.
- **RNF-02 — Privacidade (P0):** restringir dados pessoais conforme RN-11; a localização do dispositivo usada na busca por proximidade não é persistida; consultas públicas e o CSV do painel não expõem dados pessoais; senhas, tokens e dados pessoais não aparecem em logs ou mensagens de erro. Não se presume conformidade legal certificada nem prazo de retenção.
- **RNF-03 — Consistência (P0):** reserva de vaga e criação ou reagendamento da solicitação são uma única operação; execução de retirada e seus registros de volume são gravados juntos ou não são gravados. Repetições e concorrência não geram excesso de capacidade nem registros duplicados.
- **RNF-04 — Usabilidade e acessibilidade (P0):** interface em português do Brasil, adequada a celular e desktop, rótulos em campos, operação por teclado, foco visível, erros associados aos campos, estados vazios compreensíveis, sem depender só de cor. Toda informação do mapa tem equivalente em lista.
- **RNF-05 — Rastreabilidade (P0):** registrar organização, ator, momento e tipo de alteração crítica (publicação, pontos, capacidade, mudança de estado, registros e ajustes de volume, equipe), sem dados pessoais desnecessários.
- **RNF-06 — Desempenho e disponibilidade (P0):** metas, volume de dados, concorrência esperada e disponibilidade a definir com contexto de uso. O TDD deve explicitar hipóteses e plano de medição, sem números arbitrários como compromisso.
- **RNF-07 — Integridade histórica e de relatórios (P0):** relatórios devem poder ser recompostos a partir dos registros originais e ajustes, e permanecer verificáveis após alterações ou exclusões de cadastros.
- **RNF-08 — Proteção contra abuso (P0):** limitar tentativas em login, cadastros e consultas públicas e proteger exportações contra injeção de fórmulas em planilhas.

## 9. Critérios de aceite

| ID | Requisitos | Cenários verificáveis |
| --- | --- | --- |
| CA-01 | RF-01–03; RNF-01–02 | Registro válido cria organização e administrador; a organização nasce não publicada e só aparece após publicação; usuário ou e-mail duplicado é rejeitado; senha incorreta não autentica; sem sessão, operações protegidas são negadas; organização A e cidadão X não leem nem alteram dados de B e Y, mesmo conhecendo identificadores. |
| CA-02 | RF-04–06; RF-22; RN-12 | Administrador cria e inativa operador; operador inativo não entra; troca de senha inicial é exigida; operador não gerencia equipe, pontos, configurações nem relatórios; cidadão não acessa funções de organização; e-mail de cidadão duplicado é rejeitado; publicar sem município ou sem tipo habilitado é rejeitado; despublicar remove a organização da consulta pública sem apagar dados e sem cancelar solicitações existentes. |
| CA-03 | RF-07–08; RN-08 | Tipos habilitados limitam os tipos aceitos nos pontos; ponto sem coordenadas, sem horário, sem tipo aceito, com tipo não habilitado, em município não atendido ou com horário que atravessa a meia-noite é rejeitado; nome repetido na organização é rejeitado; ponto com recebimentos só é retirado logicamente. |
| CA-04 | RF-09; RN-08–09; RNF-02 | Consulta pública sem login retorna apenas pontos ativos de organizações publicadas; filtros por município, tipo e proximidade funcionam; resposta não contém dado pessoal; lista traz as mesmas informações do mapa. |
| CA-05 | RN-08 | Ponto seg–sex 08:00–17:00 no fuso da organização: às 07:59 fechado; às 08:00 aberto; às 17:00 fechado; no sábado fechado; Indisponível temporariamente fechado mesmo dentro do horário. |
| CA-06 | RF-11; RN-01; RN-04; RN-10 | Com hoje em 2026-11-10: solicitar para 2026-11-10 é rejeitado; 2026-11-11 e 2026-12-10 são aceitos; 2026-12-11 é rejeitado. Município fora dos atendidos, tipo sem retirada domiciliar e quantidade estimada não positiva são rejeitados. A 4ª solicitação aberta do cidadão na mesma organização é rejeitada; cancelar ou concluir uma libera a vaga. |
| CA-07 | RF-10–11; RN-02; RN-12; RNF-03 | Janela com capacidade 2: duas solicitações são aceitas e a terceira é rejeitada; três solicitações simultâneas para a última vaga resultam em apenas uma aceita; cancelar libera a vaga; reduzir a capacidade abaixo do ocupado preserva as agendadas e bloqueia novas; bloquear uma data com solicitações existentes preserva essas solicitações e impede apenas novas. |
| CA-08 | RF-12–14; RN-03; RF-20 | Transições válidas geram histórico e notificação; transições inválidas (por exemplo Coletada → Agendada) são rejeitadas; cancelamento 24 h antes do início do período é aceito e 23 h 59 min antes é rejeitado; recusa exige motivo; Não realizada exige motivo da lista. |
| CA-09 | RF-14–17; RN-05–07 | Papel: recebimento 10,500 kg + retirada 4,250 kg + ajuste −1,000 kg resultam em 13,750 kg; óleo 8,000 L aparece separado; total geral é exibido como 13,750 kg e 8,000 L, nunca somados; ajuste que deixe o total do registro negativo é rejeitado; registros originais não podem ser editados nem apagados; estimativa do cidadão não entra no painel. |
| CA-10 | RF-17–19; RNF-02; RNF-08 | Painel e CSV concordam em total por tipo e período; filtros por tipo e origem funcionam; ajustes aparecem identificados no CSV; células que começam com `=`, `+`, `-` ou `@` são neutralizadas; CSV e página pública de transparência não contêm dado pessoal. |
| CA-11 | RF-21–22; RN-11; RNF-05; RNF-07 | Exclusão de conta de cidadão anonimiza dados pessoais e preserva volumes; é bloqueada com solicitação em Solicitada ou Agendada; após estado final, a organização não vê nome, telefone nem endereço do cidadão, e o cidadão continua vendo os próprios dados; alterações críticas geram registro de auditoria sem segredos; excluir ponto ou operador não altera relatórios nem histórico. |
| CA-12 | RNF-01; RNF-04; RNF-06 | J-01 a J-06 são operáveis por teclado em celular e desktop, com rótulos e erros identificáveis; o mapa tem equivalente em lista; ausência de metas quantitativas aprovadas é registrada no TDD com forma de validação. |

Os cenários são critérios futuros de aceitação, não testes executados nem evidência de produto implementado.

## 10. Dados e integrações

| Entidade conceitual | Informações e relações |
| --- | --- |
| Organização | Nome, tipo, contato público, fuso, municípios atendidos, períodos de retirada, publicada ou não, transparência habilitada, limite de solicitações abertas. |
| Usuário | Perfil (Administrador, Operador ou Cidadão), nome, e-mail, usuário (Administrador e Operador), credencial protegida; Administrador e Operador pertencem a uma organização; Cidadão acessa por e-mail. |
| Tipo de resíduo | Catálogo da plataforma: código, nome, categoria, unidade fixa, orientações. |
| Tipo habilitado | Organização, tipo e indicação de retirada domiciliar. |
| Ponto de coleta | Organização, nome, endereço (UF e município entre os atendidos), coordenadas, horários por dia da semana no mesmo dia, tipos aceitos, status. |
| Disponibilidade | Organização, dia da semana, período, capacidade; bloqueios de datas. |
| Solicitação de retirada | Cidadão, organização, endereço e contato copiados no momento do pedido (UF e município selecionados em lista), janela, itens e quantidades estimadas, estado, motivo e histórico de estados. |
| Registro de volume | Organização, tipo, quantidade, origem (retirada, recebimento em ponto ou ajuste), referência à solicitação, ao ponto ou ao registro original, operador, instante e motivo (ajustes). |
| Notificação | Destinatário, evento, referência e situação de leitura. |
| Auditoria | Organização, ator, ação, entidade e momento, sem dados pessoais desnecessários. |

Cadastros nascem ativos e saem das novas operações por exclusão lógica. Solicitações transitam conforme RN-03. Dados históricos necessários à leitura de solicitações e volumes sobrevivem a alterações dos cadastros de origem; a representação técnica é decidida no TDD. A visibilidade dos dados pessoais segue RN-11.

**Integrações:** nenhuma integração externa de negócio na v1. Serão necessários, a decidir no TDD: fonte para a lista oficial de municípios e provedor de mapa e tiles. Endereços informados são declaratórios; não há validação por serviço externo.

## 11. Riscos, premissas e dependências

| Tema | Impacto / ação |
| --- | --- |
| Validação limitada ao solicitante | Adequação à rotina real não demonstrada; a entrevista de 2026-11-10 validou o produto apenas com o solicitante. Validar jornadas com uma cooperativa ou setor de limpeza urbana antes de uso real. |
| Organizações sem verificação de identidade | O cadastro é aberto por decisão da v1; qualquer pessoa pode registrar uma organização e pontos falsos poderiam ser publicados. Mitigação: publicação explícita e, antes de produção, definir verificação manual (administração de plataforma está fora da v1). |
| Dados pessoais de cidadãos | Endereço e telefone de retirada são sensíveis para a privacidade; a v1 oculta esses dados da organização após o estado final, mas base legal, retenção e atendimento a solicitações seguem pendentes. Validar antes de produção, sem presumir conformidade. |
| Resíduos regulados | Pilhas, baterias e eletroeletrônicos têm regras de logística reversa; o catálogo inicial permanece pendente de validação com a organização e a regulação local. |
| Qualidade da localização | Coordenadas escolhidas manualmente podem ficar imprecisas; oferecer conferência no mapa e exibir endereço textual. |
| Dependência de mapa e tiles | Uso gratuito de tiles públicos tem políticas de uso; avaliar provedor adequado antes de produção. |
| Concorrência por vagas | Janelas muito disputadas dependem de consistência forte; coberto por RNF-03 e CA-07. |
| Adoção | Sem cidadãos e organizações usando, a plataforma não gera valor; métricas de adoção a definir após piloto. |
| Ausência de e-mail | Cidadão precisa abrir a plataforma para saber do status; avaliar canal externo na evolução. |
| Metas operacionais ausentes | Não prometer capacidade, disponibilidade ou recuperação; definir contexto e metas antes de compromisso de produção. |

Premissas confirmadas: português do Brasil, fuso por organização com padrão `America/Sao_Paulo`, v1 sem pagamentos e sem integração externa de negócio. Não se escolhem tecnologias neste PRD.

## 12. Questões em aberto e próximos passos

### 12.1 Pendências

O conteúdo foi aprovado para TDD em 2026-11-10. As pendências abaixo não bloqueiam o desenho técnico; as que exigem validação antes de produção permanecem registradas.

| Pendência | Classificação | Validação / responsável sugerido |
| --- | --- | --- |
| Validar o catálogo inicial de tipos e unidades | Bloqueante para o catálogo definitivo; não impede o desenho técnico | Solicitante e representante de organização a identificar. |
| Baseline e metas de desempenho, disponibilidade e adoção | Não bloqueante para o desenho; necessária antes de compromissos operacionais | Produto e representante de organização a identificar. |
| Retenção de dados pessoais, base legal e atendimento a titulares | Não bloqueante para o desenho inicial; resolver antes de produção | Produto, segurança e operação a designar. |
| Verificação de identidade das organizações | Não bloqueante para o TCC; necessária antes de produção, pois a v1 usa cadastro aberto | Produto e operação a designar. |
| Fonte da lista oficial de municípios e provedor de mapa | Decisão técnica no TDD | Engenharia. |

### 12.2 Handoff para TDD

- **Entrada:** `docs/prd.md`; escopo limitado à v1.
- **Destino padrão:** `docs/tdd.md`.
- **Estado:** **validado** em 2026-11-10 por aprovação explícita do solicitante pela ferramenta `question`; handoff liberado.
- **Validação realizada:** entrevista de produto com o solicitante pela ferramenta `question` em 2026-11-10, em duas rodadas, com aprovação final do conteúdo; as decisões e os parâmetros confirmados estão nas seções 2.1 a 2.3.
- **Escopo validado para o TDD:** RF-01 a RF-22, RNF-01 a RNF-08, jornadas J-01 a J-06 e critérios CA-01 a CA-12.
- **Regras essenciais:** RN-01 a RN-12; janelas e capacidade por organização, estados da solicitação, imutabilidade do volume com ajustes, unidades fixas sem conversão, visibilidade pública, bloqueio e despublicação sem cancelamento e visibilidade de dados pessoais somente enquanto a solicitação estiver em Solicitada ou Agendada (RN-11).
- **Limites:** seções 5.2 e 5.3.
- **Dependências e pendências:** seção 12.1 e riscos da seção 11.
- **Entrevista técnica futura:** linguagem, frameworks, bibliotecas, arquitetura, autenticação e sessão, modelo de isolamento, geolocalização e mapa, fonte de municípios, concorrência de capacidade, precisão de quantidades, relatórios e CSV, notificações internas, observabilidade, testes, estrutura em `app/`, execução e entrega. O TDD de rascunho desta pasta pode conter propostas para cada ponto; elas serão confirmadas ou substituídas na etapa técnica.
- **Próximo passo:** usar `/generate-tdd Use docs/prd.md como base validada`.
