# pontos-de-coleta Specification

## Purpose

Define o cadastro, a manutenção e a retirada lógica dos pontos de coleta da organização, com
endereço, localização geográfica, horários de funcionamento e tipos aceitos.

## Requirements

### Requirement: Cadastro de ponto de coleta

O Administrador SHALL criar pontos com nome, endereço com UF e município do catálogo, bairro,
rua, número, complemento opcional, CEP, coordenadas, observações opcionais, status, horários e
tipos aceitos. O município MUST estar entre os atendidos, os tipos MUST ser subconjunto não vazio
dos habilitados, MUST existir ao menos um horário e o nome MUST ser único na organização. (RF-08;
RN-08; CA-03)

#### Scenario: Ponto válido é criado
- **WHEN** o Administrador informa dados válidos, ao menos um horário e tipos habilitados
- **THEN** o sistema cria o ponto com status informado e devolve o detalhe

#### Scenario: Ponto sem coordenadas ou sem horário é rejeitado
- **WHEN** a entrada omite coordenadas válidas ou a lista de horários
- **THEN** a API responde 400 e não cria o ponto

#### Scenario: Tipo não habilitado é rejeitado
- **WHEN** a entrada inclui tipo fora dos habilitados da organização
- **THEN** a API responde 400 e não cria o ponto

#### Scenario: Município não atendido é rejeitado
- **WHEN** o município informado não consta entre os atendidos
- **THEN** a API responde 400 e não cria o ponto

#### Scenario: Horário que atravessa a meia-noite é rejeitado
- **WHEN** a hora final não é maior que a inicial
- **THEN** a API responde 400 e não cria o ponto

#### Scenario: Nome repetido na organização é rejeitado
- **WHEN** já existe ponto, inclusive retirado logicamente, com o mesmo nome na organização
- **THEN** a API responde 409 e não cria o ponto

### Requirement: Edição e status do ponto

O Administrador SHALL editar os dados do próprio ponto e alterar seu status entre Ativo, Inativo
e Indisponível temporariamente. A edição MUST NOT permitir alterar a organização e MUST manter
as validações de criação. (RF-08; DT-06, DT-07)

#### Scenario: Edição válida
- **WHEN** o Administrador altera dados, horários ou tipos para valores válidos
- **THEN** o sistema persiste a alteração em uma única transação e devolve o detalhe

#### Scenario: Ponto de outra organização não é editável
- **WHEN** o Administrador informa identificador de ponto de outra organização
- **THEN** a API responde 404 e não altera dados

#### Scenario: Ponto inativo não está aberto
- **WHEN** o ponto está Inativo ou Indisponível temporariamente dentro do horário
- **THEN** ele não é considerado aberto

### Requirement: Retirada lógica de ponto

O Administrador SHALL retirar logicamente um ponto, que MUST sair das novas operações sem apagar
registros históricos e MUST continuar reservando o nome na organização. (RF-22; CA-03)

#### Scenario: Ponto é retirado logicamente
- **WHEN** o Administrador retira um ponto
- **THEN** o ponto deixa de aparecer nas listas de operação e na consulta pública, mantendo o registro

#### Scenario: Operador não gerencia pontos
- **WHEN** um Operador tenta criar, editar, alterar status ou retirar ponto
- **THEN** a API responde 403

### Requirement: Lista de opções para recebimento

A API SHALL expor ao Administrador e ao Operador uma lista enxuta de pontos ativos da organização
para seleção em recebimentos, sem dados de configuração. (RF-08, RF-15; DT-06)

#### Scenario: Operador consulta opções
- **WHEN** um Operador consulta as opções de ponto
- **THEN** a API devolve apenas pontos ativos e não retirados da própria organização

#### Scenario: Cidadão não consulta opções
- **WHEN** um Cidadão tenta consultar as opções
- **THEN** a API responde 403
