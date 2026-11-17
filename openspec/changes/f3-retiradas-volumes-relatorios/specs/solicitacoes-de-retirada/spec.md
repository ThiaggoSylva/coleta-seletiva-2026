# solicitacoes-de-retirada Specification

## Purpose

Define a solicitação de retirada pelo cidadão, a fila da organização, os estados, o histórico, a
capacidade concorrente e a privacidade dos dados pessoais por estado.

## Requirements

### Requirement: Consultar organizações e janelas disponíveis

O Cidadão autenticado SHALL listar organizações publicadas que atendem o município informado e
possuem ao menos um tipo com retirada domiciliar, com os tipos disponíveis. SHALL listar janelas
com capacidade, ocupação e vagas dentro do horizonte de 30 dias, omitindo datas bloqueadas ou sem
capacidade. (RF-11; RN-01, RN-02, RN-04; CA-06)

#### Scenario: Organização elegível é listada
- **WHEN** o cidadão informa município atendido por organização publicada com tipo domiciliar
- **THEN** a API devolve a organização e os tipos habilitados para retirada

#### Scenario: Município sem atendimento retorna lista vazia
- **WHEN** nenhuma organização publicada atende o município
- **THEN** a API devolve lista vazia

#### Scenario: Janela sem vagas não é oferecida
- **WHEN** a ocupação atinge a capacidade do dia/período ou a data está bloqueada
- **THEN** a janela não aparece como disponível para o cidadão

### Requirement: Criar solicitação de retirada

O Cidadão SHALL criar uma solicitação com organização, endereço de retirada, tipos com quantidade
estimada maior que zero, data e período disponíveis e observações opcionais. A solicitação MUST
nascer em `Solicitada`, copiar nome/telefone/endereço do perfil no momento do pedido e respeitar
antecedência mínima, horizonte, elegibilidade territorial/de tipos, limite de abertas e capacidade.
(RF-11; RN-01–RN-04, RN-10; CA-06, CA-07)

#### Scenario: Solicitação válida é criada
- **WHEN** o cidadão informa dados válidos, tipos domiciliares e janela com vaga
- **THEN** a API responde 201, grava a solicitação em Solicitada, histórico e notificações da organização

#### Scenario: Data hoje ou além do horizonte é rejeitada
- **WHEN** a data é hoje ou ultrapassa 30 dias
- **THEN** a API responde 400 e não cria a solicitação

#### Scenario: Município ou tipo inelegível é rejeitado
- **WHEN** o município não é atendido ou algum tipo não aceita retirada domiciliar
- **THEN** a API responde 400 e não cria a solicitação

#### Scenario: Quantidade estimada não positiva é rejeitada
- **WHEN** algum item tem quantidade zero, negativa ou com mais de três casas
- **THEN** a API responde 400 e não cria a solicitação

#### Scenario: Limite de solicitações abertas é aplicado
- **WHEN** o cidadão já possui o limite configurado em Solicitada/Agendada na organização
- **THEN** a API responde 409 com `limite_de_solicitacoes`

#### Scenario: Capacidade esgotada é rejeitada
- **WHEN** a ocupação da janela é igual ou maior que a capacidade
- **THEN** a API responde 409 com `capacidade_esgotada`

#### Scenario: Concorrência pela última vaga
- **WHEN** três solicitações simultâneas disputam a última vaga
- **THEN** apenas uma é criada e as demais recebem conflito de capacidade

### Requirement: Acompanhar e cancelar solicitação

O Cidadão SHALL listar e detalhar apenas as próprias solicitações, com itens, histórico e dados
pessoais completos em qualquer estado. SHALL cancelar enquanto o estado for Solicitada ou Agendada
e o prazo de 24 horas antes do início do período não tiver passado. (RF-12; RN-03, RN-10; CA-08)

#### Scenario: Cidadão vê apenas as próprias solicitações
- **WHEN** o cidadão X consulta suas solicitações
- **THEN** a API não devolve solicitações do cidadão Y nem de outra organização

#### Scenario: Cancelamento dentro do prazo libera a vaga
- **WHEN** o cidadão cancela mais de 24 horas antes do início do período
- **THEN** o estado passa a Cancelada, o histórico é gravado, a organização é notificada e a vaga é liberada

#### Scenario: Cancelamento após o prazo é rejeitado
- **WHEN** faltam 24 horas ou menos para o início do período
- **THEN** a API responde 409 com `estado_invalido` e não altera a solicitação

### Requirement: Triar e agendar pela organização

Administrador e Operador SHALL listar e detalhar solicitações da própria organização com filtros de
estado, data e município. SHALL agendar uma solicitação Solicitada, reagendar uma Agendada para
outra janela válida com vaga e recusar com motivo obrigatório. (RF-13; RN-01–RN-03; CA-07, CA-08)

#### Scenario: Agendamento gera histórico e notificação
- **WHEN** o operador agenda uma solicitação Solicitada
- **THEN** o estado passa a Agendada, o histórico e a notificação do cidadão são gravados

#### Scenario: Reagendamento para janela lotada é rejeitado
- **WHEN** o operador reagenda para uma janela sem vaga ou bloqueada
- **THEN** a API responde 409 e mantém a janela anterior

#### Scenario: Recusa exige motivo
- **WHEN** o operador recusa sem motivo
- **THEN** a API responde 400 e mantém a solicitação em Solicitada

#### Scenario: Transição inválida é rejeitada
- **WHEN** a organização tenta executar uma solicitação ainda Solicitada ou reagendar uma Coletada
- **THEN** a API responde 409 com `estado_invalido`

### Requirement: Executar ou encerrar retirada

Administrador e Operador SHALL marcar uma solicitação Agendada como Coletada informando quantidades
reais maiores que zero por tipo com retirada domiciliar habilitada, podendo incluir tipos que o
cidadão não listou. Cada quantidade MUST gerar um registro de volume de origem retirada na mesma
transação. SHALL marcar como Não realizada com motivo de lista fechada. (RF-14, RF-15; RN-03–RN-07;
CA-08, CA-09)

#### Scenario: Execução grava estado, volumes e notificação
- **WHEN** o operador executa com quantidades reais válidas
- **THEN** a solicitação passa a Coletada, um registro de volume por item é gravado e o cidadão é notificado

#### Scenario: Execução com quantidade inválida não grava nada
- **WHEN** algum item tem quantidade zero, negativa ou mais de três casas
- **THEN** a API responde 400 e não altera estado nem volumes

#### Scenario: Não realizada exige motivo da lista
- **WHEN** o operador informa motivo fora da lista
- **THEN** a API responde 400 e mantém a solicitação Agendada

### Requirement: Privacidade por estado

As respostas destinadas à organização MUST conter nome, telefone e endereço apenas em Solicitada
ou Agendada. Após Coletada, Não realizada, Recusada ou Cancelada os campos MUST ser nulos e
`dadosPessoaisOcultos` MUST ser verdadeiro, preservando itens, histórico e volumes. O cidadão MUST
continuar vendo os próprios dados em qualquer estado. (RN-11; RNF-02; CA-11)

#### Scenario: Organização deixa de ver PII após estado final
- **WHEN** a organização consulta uma solicitação Coletada
- **THEN** nome, telefone e endereço vêm nulos e os demais dados permanecem

#### Scenario: Cidadão continua vendo os próprios dados
- **WHEN** o cidadão consulta uma solicitação Coletada
- **THEN** a resposta mantém seu nome, telefone e endereço

### Requirement: Isolamento entre organizações e cidadãos

Toda operação MUST derivar organização e ator do token, nunca do corpo ou da query. Acesso com
identificadores de outra organização ou de outro cidadão MUST resultar em 404, sem vazamento de
existência ou conteúdo. (RF-03; RNF-01; CA-01)

#### Scenario: Organização B não acessa solicitação de A
- **WHEN** um usuário de B consulta ou age sobre uma solicitação de A
- **THEN** a API responde 404 e não altera dados

#### Scenario: Cidadão não acessa fila da organização
- **WHEN** um cidadão tenta listar ou executar solicitações da organização
- **THEN** a API responde 403
