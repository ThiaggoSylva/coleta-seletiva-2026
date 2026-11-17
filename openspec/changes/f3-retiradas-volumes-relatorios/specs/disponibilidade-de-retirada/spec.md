# disponibilidade-de-retirada Specification

## Purpose

Define a configuração de capacidade por dia da semana e período e os bloqueios de data que
controlam novas solicitações sem cancelar as existentes.

## Requirements

### Requirement: Configurar capacidade semanal

O Administrador SHALL definir a capacidade de retiradas para cada combinação de dia da semana e
período, aceitando zero para desabilitar a janela. A gravação MUST ser atômica e auditada.
(RF-10; RN-02; DT-16)

#### Scenario: Capacidade válida é gravada
- **WHEN** o Administrador envia as capacidades de segunda a domingo para Manhã e Tarde
- **THEN** a API persiste os valores e devolve a configuração completa

#### Scenario: Capacidade negativa é rejeitada
- **WHEN** a entrada contém capacidade menor que zero
- **THEN** a API responde 400 e não altera nenhuma janela

#### Scenario: Operador não altera capacidade
- **WHEN** um Operador tenta alterar a disponibilidade
- **THEN** a API responde 403

### Requirement: Bloquear e desbloquear datas

O Administrador SHALL bloquear uma data local com motivo opcional e SHALL desbloquear uma data
existente. O bloqueio MUST impedir apenas novas solicitações e MUST preservar as solicitações em
Solicitada ou Agendada já existentes. (RF-10; RN-12)

#### Scenario: Data bloqueada impede nova solicitação
- **WHEN** o cidadão tenta solicitar retirada em uma data bloqueada
- **THEN** a API responde 409 com `data_bloqueada` e não cria a solicitação

#### Scenario: Bloqueio preserva solicitações existentes
- **WHEN** o Administrador bloqueia uma data que já possui solicitações abertas
- **THEN** as solicitações permanecem em seus estados e a organização ainda pode executá-las

#### Scenario: Data duplicada é rejeitada
- **WHEN** o Administrador bloqueia uma data já bloqueada
- **THEN** a API responde 409 e mantém um único bloqueio
