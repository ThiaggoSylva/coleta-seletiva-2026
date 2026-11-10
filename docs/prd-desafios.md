# PRD — Evoluções da Plataforma de Coleta Seletiva: v1.1 e v2

- **Status:** Rascunho. Aguardando validação explícita; handoff para o TDD não liberado.
- **Data:** 2026-11-09.
- **Origem:** evoluções listadas em `docs/prd.md`, seção 5.2, detalhadas no mesmo padrão do modelo `locadora-de-veiculos-2026`.
- **Base de produto:** `docs/prd.md` (v1).
- **Natureza da entrega:** documentação preparatória, sem implementação nem desenho técnico. Elaborado por assistente de IA sem entrevista pela ferramenta `question`; decisões marcadas **Proposta** precisam de confirmação.

## 1. Resumo executivo

Evoluir a plataforma em duas entregas: **v1.1**, com e-mail transacional, programa de pontos de reconhecimento para cidadãos e fotos de pontos; e **v2**, com perfil Coletor em aplicativo web instalável com modo offline, rotas de coleta, estimativa de impacto ambiental e verificação de organizações pela administração da plataforma.

O resultado pretendido é aumentar o engajamento dos cidadãos, tornar a operação de campo mais eficiente e dar mais confiança ao público, preservando isolamento, histórico e privacidade.

## 2. Contexto e problema

A v1 está especificada em `docs/prd.md`; não há evidência de que tenha sido implementada ou esteja em produção. Suas limitações previstas são: status só visível dentro da plataforma, ausência de incentivo à participação, registro de execução dependente de conexão, ausência de roteirização e ausência de verificação das organizações registradas.

Engajamento, ganho de eficiência e confiança são hipóteses de benefício, não resultados medidos. Não há baseline de participação, tempo de coleta ou taxa de abandono.

### 2.1 Relação com a base

- Este documento complementa, sem editar, `docs/prd.md`.
- IDs locais devem ser citados com o caminho, por exemplo `docs/prd-desafios.md:RF-01`, para evitar confusão com os da v1.
- Regras da v1 continuam valendo salvo alteração explícita aqui, em especial RN-05 a RN-07 (volume real, unidades e imutabilidade) e RN-11 (privacidade).
- Cada alteração está agrupada por versão na seção 5.

## 3. Objetivos e métricas de sucesso

| Objetivo | Indicador e medição | Baseline / meta |
| --- | --- | --- |
| Informar o cidadão fora da plataforma | Cenários de e-mail por evento e por preferência de envio (CA-01) | Todos os cenários corretos; taxa de abertura a medir |
| Incentivar a participação | Pontos creditados corretamente por registro e por ajuste (CA-02, CA-03) | Todos os cenários corretos; meta de engajamento a definir |
| Dar confiança nos pontos | Pontos com foto aprovada pela organização, sem metadados de localização (CA-04) | Cenários corretos; adoção a medir |
| Operar em campo sem conexão | Execuções feitas offline sincronizam uma única vez e sem perda (CA-06) | Nenhuma duplicação ou perda nos cenários |
| Organizar a rota do dia | Rota contém exatamente as retiradas agendadas da janela, ordenadas por proximidade (CA-07) | Cenários corretos; ganho de tempo a medir |
| Dar confiança nas organizações | Apenas organizações verificadas ganham selo e aparecem em destaque (CA-09) | Cenários corretos |

Metas quantitativas de adoção, engajamento, latência e disponibilidade não foram definidas.

## 4. Público-alvo e stakeholders

- **Cidadão:** recebe e-mails, acumula pontos e vê o selo de organizações verificadas.
- **Administrador e operador da organização:** configuram pontos, aprovam fotos, montam rotas e acompanham resultados.
- **Coletor (v2):** profissional de campo vinculado a uma organização; executa retiradas de uma rota, inclusive offline.
- **Administrador da plataforma (v2):** verifica organizações e mantém fatores de impacto; novo papel, sem acesso a dados pessoais de cidadãos.
- **Solicitante, produto, design e engenharia:** como na v1; responsáveis nominais a definir.

## 5. Escopo por versão

### 5.1 v1.1 — E-mail, pontos e fotos

1. E-mail transacional para eventos já existentes na v1, com preferência de envio por cidadão.
2. Programa de pontos de reconhecimento por organização, creditados por volume real coletado.
3. Fotos de pontos de coleta com aprovação e remoção de metadados.

**Rastreabilidade:** RF-01 a RF-10, RNF-01 a RNF-04, J-01 a J-03, CA-01 a CA-05.

### 5.2 v2 — Coletor, rotas, impacto e verificação

1. Perfil Coletor e aplicativo web instalável (PWA) com modo offline para execução de retiradas.
2. Rotas de coleta por janela, com atribuição a coletor.
3. Estimativa de impacto ambiental com fatores versionados e fonte registrada.
4. Papel Administrador da plataforma e verificação de organizações.

**Rastreabilidade:** RF-11 a RF-22, RNF-05 a RNF-08, J-04 a J-07, CA-06 a CA-12.

### 5.3 Fora de escopo destas entregas

Resgate de pontos por dinheiro, descontos ou prêmios; ranking público com nome real; SMS e aplicativos de mensagem; aplicativo nativo de loja; otimização exata de rotas (problema do caixeiro viajante) ou roteirização com trânsito em tempo real; emissão de certificados ou manifestos de destinação; integrações municipais; marketplace de materiais; pagamentos.

## 6. Jornadas e casos de uso

### J-01 — Receber e-mail e controlar preferências — v1.1

O cidadão confirma o e-mail no cadastro e define quais eventos quer receber. A cada evento elegível (Agendada, Reagendada, Recusada, Coletada, Não realizada), recebe uma mensagem sem dados desnecessários. Falha de envio não altera a solicitação.

### J-02 — Ganhar pontos — v1.1

A organização configura o fator de pontos por tipo. Ao concluir uma retirada, o cidadão recebe pontos proporcionais ao volume real e consulta seu extrato e nível. Ajustes de volume corrigem os pontos.

### J-03 — Publicar fotos de pontos — v1.1

O operador envia fotos de um ponto; o administrador aprova ou rejeita; apenas fotos aprovadas aparecem na consulta pública. Imagens inválidas são recusadas com orientação.

### J-04 — Coletor executa a rota offline — v2

O coletor abre a rota do dia, instala o aplicativo, baixa as retiradas, executa e registra quantidades sem conexão e sincroniza ao voltar a ter rede. Conflitos com mudanças feitas no servidor são mostrados para decisão.

### J-05 — Organização monta a rota — v2

O operador escolhe data e período, vê as retiradas agendadas, atribui um coletor e confere a ordem sugerida por proximidade, ajustando manualmente se necessário.

### J-06 — Acompanhar impacto estimado — v2

A organização e o público (se habilitado) veem a estimativa de impacto por período, com a fonte e a versão dos fatores usados.

### J-07 — Verificar organização — v2

O administrador da plataforma analisa o registro de uma organização, aprova ou rejeita com motivo. Organizações verificadas recebem selo e prioridade na listagem pública.

## 7. Requisitos funcionais

### 7.1 v1.1 — E-mail, pontos e fotos

- **RF-01 — Confirmar e-mail (P0):** enviar link de confirmação no cadastro; e-mail não confirmado não recebe mensagens de eventos.
- **RF-02 — Enviar e-mails de evento (P0):** enviar mensagem aos eventos Agendada, Reagendada, Recusada, Coletada e Não realizada, respeitando a preferência. Envio é assíncrono e sua falha nunca altera o estado da solicitação.
- **RF-03 — Preferências de envio (P0):** o cidadão ativa ou desativa e-mails por tipo de evento e pode cancelar o recebimento por link na mensagem.
- **RF-04 — Configurar pontos (P0):** o administrador define, por tipo habilitado, o fator de pontos por unidade, com vigência. Sem fator, o tipo não gera pontos.
- **RF-05 — Creditar pontos (P0):** ao registrar volume de origem retirada, creditar pontos ao cidadão da solicitação conforme RN-01. Recebimentos em ponto só geram pontos quando o operador informar o código do cidadão (RF-06).
- **RF-06 — Código do cidadão (P1):** cada cidadão tem um código pessoal exibível e legível por câmera, usado pelo operador para identificar entrega em ponto. O código não expõe dados pessoais.
- **RF-07 — Extrato e níveis (P0):** o cidadão consulta saldo, extrato (data, origem, tipo, pontos) e nível, definido por faixas de pontos acumulados configuradas pela plataforma.
- **RF-08 — Corrigir pontos por ajuste (P0):** ajustes de volume da v1 geram lançamentos de correção de pontos, sem editar nem apagar lançamentos (RN-02).
- **RF-09 — Fotos de pontos (P1):** operador envia até um número limite de fotos por ponto; o administrador aprova ou rejeita; só fotos aprovadas aparecem publicamente. Formatos, tamanhos e remoção de metadados em RNF-03.
- **RF-10 — Remover foto (P1):** o administrador remove fotos; removidas deixam de aparecer imediatamente.

### 7.2 v2 — Coletor, rotas, impacto e verificação

- **RF-11 — Gerir coletores (P0):** o administrador cria, edita e inativa contas de Coletor, vinculadas a uma organização. Coletor vê somente rotas atribuídas a ele.
- **RF-12 — Montar rota (P0):** o operador cria uma rota por organização, data e período, com as retiradas Agendadas da janela, atribui um coletor e vê a ordem sugerida por proximidade (RN-05). Reordenação manual é permitida.
- **RF-13 — Estados da rota (P0):** rota Planejada, Em andamento, Concluída ou Cancelada (RN-06). Alterar uma retirada (reagendar ou cancelar) atualiza a rota e notifica o coletor.
- **RF-14 — Aplicativo web instalável (P0):** disponibilizar a área do coletor como PWA instalável, com carregamento e navegação sem conexão para a rota baixada.
- **RF-15 — Executar offline (P0):** registrar execuções e quantidades sem conexão, guardadas no dispositivo e identificadas por chave única por operação (RN-07).
- **RF-16 — Sincronizar (P0):** enviar execuções pendentes ao recuperar a conexão, uma única vez por chave, mostrando o resultado por item e os conflitos (RN-08).
- **RF-17 — Fatores de impacto (P0):** o administrador da plataforma mantém fatores de impacto por tipo, com valor, unidade, fonte, versão e vigência. Sem fator, não há estimativa para o tipo.
- **RF-18 — Estimar impacto (P0):** exibir a estimativa por período e tipo a partir de registros de volume reais e do fator vigente na data de cada registro (RN-09), sempre identificada como estimativa, com fonte e versão.
- **RF-19 — Impacto público (P1):** se habilitado pela organização, a página de transparência exibe a estimativa agregada, sem dados pessoais.
- **RF-20 — Papel Administrador da plataforma (P0):** novo papel, sem organização e sem acesso a dados pessoais de cidadãos, com acesso à fila de verificação e aos fatores de impacto.
- **RF-21 — Verificar organizações (P0):** analisar registros, aprovar ou rejeitar com motivo, e revogar a verificação. Estado: Não verificada, Em análise, Verificada, Rejeitada.
- **RF-22 — Selo e prioridade (P0):** organizações Verificadas exibem selo e aparecem antes na consulta pública; as demais continuam consultáveis conforme a v1, com a indicação de não verificadas.

### 7.3 Matriz de acesso — v2

| Ação | Administrador da org. | Operador | Coletor | Cidadão | Admin. da plataforma |
| --- | --- | --- | --- | --- | --- |
| Gerir coletores | Sim | Não | Não | Não | Não |
| Montar rotas | Sim | Sim | Não | Não | Não |
| Ver e executar a rota atribuída | Sim | Sim | Sim, só a sua | Não | Não |
| Configurar pontos e fatores de pontos | Sim | Não | Não | Não | Não |
| Aprovar fotos | Sim | Não | Não | Não | Não |
| Verificar organizações e gerir fatores de impacto | Não | Não | Não | Não | Sim |
| Ver dados pessoais de cidadãos | Só de suas solicitações | Só de suas solicitações | Só das retiradas da sua rota | Só os próprios | Não |

### 7.4 Regras de negócio

- **RN-01 — Pontos:** pontos de um registro = `piso(total do registro × fator vigente na data do registro)`, em inteiros. O fator é copiado no lançamento. Total do registro = original somado aos ajustes (RN-07 da v1).
- **RN-02 — Imutabilidade dos pontos:** lançamentos de pontos não são editados nem apagados. Correções criam lançamentos com a diferença entre os pontos novos e os já creditados do registro. O saldo não pode ficar negativo por ajuste: se ficar, o saldo é limitado a zero e o excedente é registrado como pendência.
- **RN-03 — Elegibilidade de pontos:** somente registros de origem retirada com solicitação de cidadão identificado, ou recebimentos com código do cidadão, geram pontos. Registros sem identificação nunca geram.
- **RN-04 — Níveis:** o nível é função dos pontos acumulados (créditos menos correções), nunca do saldo gasto; faixas definidas pela plataforma. Pontos não têm valor monetário nem resgate.
- **RN-05 — Ordem sugerida:** a sequência segue vizinho mais próximo a partir do primeiro ponto da rota (heurística simples, sem garantia de ótimo). Retiradas sem coordenadas ficam ao final, destacadas.
- **RN-06 — Estados da rota:** Planejada → Em andamento → Concluída; Planejada ou Em andamento → Cancelada. Rota só inclui retiradas Agendadas da mesma organização, data e período, e cada retirada pertence a no máximo uma rota ativa.
- **RN-07 — Chave de operação offline:** cada execução offline tem uma chave única gerada no dispositivo; repetir a mesma chave não duplica registros de volume nem pontos.
- **RN-08 — Conflito de sincronização:** se a retirada mudou no servidor (cancelada, reagendada ou já concluída), a execução offline não é aplicada, é devolvida como conflito e preservada no dispositivo até decisão do coletor.
- **RN-09 — Impacto estimado:** impacto = `quantidade real × fator vigente na data do registro`, por tipo, somado apenas entre tipos com a mesma unidade de fator. Sempre rotulado como estimativa, com fonte e versão. Ajustes de volume corrigem a estimativa.
- **RN-10 — Verificação:** só o Administrador da plataforma altera o estado de verificação; alterações geram registro de auditoria com motivo.

## 8. Requisitos não funcionais

- **RNF-01 — Entregabilidade e consentimento (v1.1):** e-mails de evento só vão a endereços confirmados e com preferência ativa, com opção de cancelar o recebimento; conteúdo mínimo, sem endereço completo nem telefone.
- **RNF-02 — Privacidade (v1.1 e v2):** código do cidadão sem dados pessoais; extrato visível só ao próprio cidadão; nenhuma localização do coletor é armazenada além do necessário à rota (decisão do TDD).
- **RNF-03 — Fotos (v1.1):** aceitar formatos e tamanhos limitados, remover metadados (incluindo localização) e não executar conteúdo enviado; limites definidos no TDD.
- **RNF-04 — Consistência e idempotência (v1.1):** crédito de pontos e registro de volume na mesma operação; reprocessar um evento não duplica pontos nem e-mails.
- **RNF-05 — Funcionamento offline (v2):** dados locais do coletor protegidos no dispositivo, com expiração e remoção ao encerrar sessão; sem exigir conexão contínua durante a execução.
- **RNF-06 — Segurança de papéis (v2):** isolamento do Coletor às suas rotas e do Administrador da plataforma sem acesso a dados pessoais; testes por papel e por organização.
- **RNF-07 — Transparência de estimativas (v2):** estimativas de impacto sempre acompanhadas de fonte, versão e avisos de limitação; valores reais de fator não são inventados.
- **RNF-08 — Usabilidade e acessibilidade (v1.1 e v2):** português do Brasil, uso por teclado e leitor de tela nas áreas web, alvos de toque adequados no aplicativo do coletor e indicação clara de estado offline e de sincronização pendente.

## 9. Critérios de aceite

| ID | Requisitos | Cenários verificáveis |
| --- | --- | --- |
| CA-01 | RF-01–03; RNF-01 | E-mail não confirmado não recebe mensagens; preferência desativada impede o envio do evento; link de cancelamento desativa o recebimento; falha de envio não altera a solicitação e permite nova tentativa sem duplicar mensagem. |
| CA-02 | RF-04–05; RN-01 | Fator papel = 2 pontos/kg e óleo = 5 pontos/L: 4,250 kg de papel geram 8 pontos (piso de 8,5); 3,000 L de óleo geram 15; tipo sem fator não gera pontos; mudança posterior do fator não altera pontos já creditados. |
| CA-03 | RF-06–08; RN-02–04 | Ajuste de −1,000 kg no registro de 4,250 kg de papel recalcula para 6 pontos e cria lançamento de −2; lançamento original permanece; recebimento sem código do cidadão não gera pontos; nível depende do acumulado, não do saldo. |
| CA-04 | RF-09–10; RNF-03 | Foto aprovada aparece publicamente e a não aprovada não; arquivo inválido, grande demais ou corrompido é recusado; metadados de localização são removidos; foto removida some imediatamente da consulta pública. |
| CA-05 | RNF-02; RNF-04 | Reprocessar o mesmo evento não duplica pontos nem e-mails; extrato de um cidadão não é visível a outro. |
| CA-06 | RF-14–16; RN-07–08 | Execução offline sincroniza uma única vez mesmo com reenvio; execução de retirada cancelada no servidor volta como conflito e permanece no dispositivo; encerrar sessão remove os dados locais; rota baixada abre sem conexão. |
| CA-07 | RF-11–13; RN-05–06 | Rota inclui apenas retiradas Agendadas da mesma organização, data e período; retirada em duas rotas ativas é rejeitada; ordem por vizinho mais próximo é a esperada em um caso de coordenadas fixas; retirada sem coordenadas vai ao final; reagendar uma retirada atualiza a rota e notifica o coletor; coletor só vê a própria rota. |
| CA-08 | RF-17–19; RN-09; RNF-07 | Com fator de teste fictício de 1,500 kgCO₂e/kg, 10,000 kg resultam em 15,000 kgCO₂e, exibidos como estimativa com fonte e versão; tipo sem fator não gera estimativa; ajuste de volume corrige o valor; alterar o fator não reescreve estimativas passadas. |
| CA-09 | RF-20–22; RN-10 | Administrador da plataforma verifica e revoga com motivo e auditoria; organização verificada aparece primeiro e com selo; não verificada continua consultável e indicada; administrador da plataforma não acessa dados pessoais de cidadãos. |
| CA-10 | RF-11; RF-20; RNF-06 | Matriz de acesso da seção 7.3 aplicada a cada ação, por papel e por organização, mesmo conhecendo identificadores. |
| CA-11 | RNF-05; RNF-08 | Fluxos J-04 e J-05 operáveis em celular, com estado offline e pendências visíveis; áreas web operáveis por teclado. |
| CA-12 | RNF-01–08 | Ausência de metas quantitativas aprovadas registrada no TDD, com plano de medição e sem números arbitrários como compromisso. |

Os cenários são critérios futuros de aceitação, não testes executados. O fator de 1,500 kgCO₂e/kg é fictício e serve só para testar o cálculo; valores reais exigem fonte validada.

## 10. Dados e integrações

| Entidade conceitual | Informações e relações |
| --- | --- |
| Preferência de e-mail | Cidadão, evento, ativo; e-mail confirmado |
| Mensagem de e-mail | Destinatário, evento, referência, estado de envio, tentativas; sem conteúdo pessoal desnecessário |
| Fator de pontos | Organização, tipo, fator, vigência |
| Lançamento de pontos | Cidadão, organização, registro de volume de origem, fator copiado, pontos, tipo (crédito ou correção) |
| Foto de ponto | Ponto, estado (pendente, aprovada, rejeitada), metadados técnicos; sem localização |
| Coletor e rota | Usuário Coletor, organização, data, período, estado, itens ordenados (retiradas) |
| Operação offline | Chave única, retirada, quantidades, instante local, resultado da sincronização |
| Fator de impacto | Tipo, valor, unidade, fonte, versão, vigência |
| Verificação de organização | Estado, motivo, responsável, instante |

**Integrações necessárias, a decidir no TDD:** provedor de envio de e-mail e armazenamento de fotos. Nenhuma integração de negócio com terceiros além destas.

## 11. Riscos, premissas e dependências

| Tema | Impacto / ação |
| --- | --- |
| Pontos sem resgate | Incentivo pode ter pouca percepção de valor; validar com cidadãos antes de investir; resgates são evolução separada. |
| Fraude em pontos | Operador pode creditar pontos indevidamente; manter rastreabilidade e revisar regras antes de produção. |
| Entregabilidade de e-mail | Mensagens podem cair em spam; configuração de domínio e reputação são dependências operacionais. |
| Conflitos offline | Dados locais podem divergir do servidor; RN-08 preserva a execução, mas a usabilidade do conflito precisa de teste real com coletores. |
| Dados no dispositivo | Perda ou roubo expõe dados da rota; minimizar e expirar dados locais (RNF-05). |
| Fatores de impacto | Valores incorretos geram desinformação; usar apenas fonte validada e rotular como estimativa (RNF-07). |
| Papel de administração da plataforma | Concentra poder; restringir acesso, auditar e definir responsáveis antes de produção. |
| Rotas heurísticas | Ordem sugerida pode ser pior que a experiência do coletor; manter edição manual e medir. |
| Validação limitada ao solicitante | Adequação à rotina real não demonstrada; validar jornadas com coletores e organizações. |

Premissas: v1 implementada e estável antes de iniciar a v1.1; v1.1 antes da v2; português do Brasil e fuso por organização mantidos.

## 12. Questões em aberto e próximos passos

### 12.1 Pendências e validações

| Pendência | Classificação | Validação / responsável sugerido |
| --- | --- | --- |
| Confirmar as decisões propostas e as prioridades P0/P1 | Bloqueante para validar o PRD | Solicitante, via `/generate-prd`. |
| Regras de nível (faixas) e política de uso dos pontos | Bloqueante para RF-07 | Solicitante e organizações parceiras. |
| Fonte validada dos fatores de impacto | Bloqueante para uso real de RF-17 e RF-18; não impede o desenho | Administração da plataforma e especialista de domínio a identificar. |
| Provedor de e-mail e fotos | Decisão técnica no TDD | Engenharia. |
| Política de retenção de dados no dispositivo e no servidor | Não bloqueante para o desenho; resolver antes de produção | Produto, segurança e operação. |
| Metas de engajamento, desempenho e disponibilidade | Não bloqueante para o desenho | Produto e representante de organização. |

### 12.2 Handoff para TDD

- **Entrada:** `docs/prd-desafios.md`, complementando `docs/prd.md`.
- **Destino:** usar `docs/tdd-desafios.md` somente se o solicitante pedir explicitamente um slug para o TDD; caso contrário, atualizar `docs/tdd.md` com confirmação pela ferramenta `question`.
- **Estado:** **bloqueado** até a validação explícita deste PRD.
- **Escopo a validar:** v1.1 (RF-01 a RF-10) e v2 (RF-11 a RF-22), RNF-01 a RNF-08, J-01 a J-07 e CA-01 a CA-12.
- **Regras essenciais:** RN-01 a RN-10 deste documento, mais RN-05 a RN-07 e RN-11 da v1.
- **Entrevista técnica futura:** provedor de e-mail e filas de envio, armazenamento e tratamento de imagens, modelo de pontos, PWA e armazenamento offline, estratégia de sincronização e idempotência, heurística de rota, papel de administração da plataforma, versionamento de fatores, observabilidade e testes.
- **Próximo passo:** depois de validar, usar `/generate-tdd Use docs/prd-desafios.md como base validada`.
