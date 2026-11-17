# notificacoes-internas Specification

## Purpose

Define as notificações internas geradas pelos eventos de retirada, restritas ao próprio
destinatário, com leitura e contagem de não lidas.

## Requirements

### Requirement: Gerar notificações na transação do evento

O sistema SHALL notificar o cidadão em Agendada, Reagendada, Recusada, Coletada e Não realizada e
SHALL notificar Administrador e Operador da organização em nova solicitação e cancelamento. As
notificações MUST ser gravadas na mesma transação da mudança de estado e conter apenas referência e
texto genérico, sem PII além do necessário ao destinatário. (RF-20; DT-14; CA-08)

#### Scenario: Nova solicitação notifica a organização
- **WHEN** uma solicitação é criada
- **THEN** os usuários ativos Administrador e Operador da organização recebem uma notificação

#### Scenario: Mudança de estado notifica o cidadão
- **WHEN** a organização agenda, reagenda, recusa, coleta ou encerra como não realizada
- **THEN** o cidadão recebe a notificação correspondente

#### Scenario: Falha na operação não deixa notificação órfã
- **WHEN** a transação da mudança de estado falha
- **THEN** nenhuma notificação é persistida

### Requirement: Consultar e marcar notificações

O usuário autenticado SHALL listar apenas as próprias notificações, filtrar por `desde` e por não
lidas, marcar uma ou todas como lidas e consultar a contagem de não lidas. Marcar notificação de
outro usuário MUST resultar em 404. (RF-20)

#### Scenario: Contagem de não lidas
- **WHEN** o usuário consulta a lista
- **THEN** a resposta inclui a contagem de não lidas do próprio usuário

#### Scenario: Marcar como lida
- **WHEN** o usuário marca uma notificação própria como lida
- **THEN** ela deixa de contar como não lida e registra o instante da leitura

#### Scenario: Notificação de outro usuário não é acessível
- **WHEN** o usuário tenta marcar a notificação de outro
- **THEN** a API responde 404 e não altera o registro

#### Scenario: Cidadão não vê notificações da organização
- **WHEN** o cidadão lista suas notificações
- **THEN** a API devolve apenas as destinadas a ele
