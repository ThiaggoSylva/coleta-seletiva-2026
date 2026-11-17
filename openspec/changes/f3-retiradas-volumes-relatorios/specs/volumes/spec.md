# volumes Specification

## Purpose

Define o registro imutável de volume coletado, em retiradas e recebimentos em ponto, e a correção
por ajuste rastreável.

## Requirements

### Requirement: Registrar volume de retirada

A execução de uma retirada SHALL gerar um registro de volume por tipo, com quantidade real maior
que zero, origem `Retirada`, referência à solicitação, operador e data local calculada na gravação.
A gravação MUST ocorrer na mesma transação da mudança de estado. (RF-14, RF-15; RN-05–RN-07;
RNF-03; CA-09)

#### Scenario: Volumes da execução são gravados juntos
- **WHEN** a execução é concluída
- **THEN** todos os registros de volume e o estado Coletada são persistidos ou nenhum é

#### Scenario: Estimativa do cidadão não vira volume
- **WHEN** a solicitação é criada
- **THEN** nenhum registro de volume é criado a partir das quantidades estimadas

### Requirement: Registrar recebimento em ponto

Administrador e Operador SHALL registrar um recebimento em ponto com ponto ativo da organização,
tipo aceito pelo ponto e habilitado pela organização, quantidade maior que zero e data/hora. O
recebimento MUST NOT identificar o cidadão. (RF-15; RN-06, RN-07; CA-09)

#### Scenario: Recebimento válido é gravado
- **WHEN** o operador informa ponto ativo, tipo aceito e quantidade válida
- **THEN** a API grava o volume de origem RecebimentoEmPonto com a data local da organização

#### Scenario: Ponto de outra organização é rejeitado
- **WHEN** o operador informa ponto de outra organização
- **THEN** a API responde 404 e não grava o volume

#### Scenario: Tipo não aceito pelo ponto é rejeitado
- **WHEN** o tipo não consta entre os tipos do ponto
- **THEN** a API responde 400 e não grava o volume

### Requirement: Ajustar volume sem apagar histórico

Administrador e Operador SHALL corrigir um volume por ajuste positivo ou negativo com motivo
obrigatório, vinculado ao registro original. O ajuste MUST NOT editar nem apagar o original; o total
do original MUST permanecer maior ou igual a zero. (RF-16; RN-07; CA-09)

#### Scenario: Ajuste negativo válido reduz o total
- **WHEN** o operador ajusta −1,000 kg de um original de 14,750 kg
- **THEN** o total do original passa a 13,750 kg e o ajuste fica identificado

#### Scenario: Ajuste que deixa o total negativo é rejeitado
- **WHEN** o ajuste negativo supera o total do original e seus ajustes
- **THEN** a API responde 409 com `volume_negativo` e não grava o ajuste

#### Scenario: Ajuste sem motivo é rejeitado
- **WHEN** o operador envia ajuste sem motivo
- **THEN** a API responde 400 e não grava o ajuste

#### Scenario: Registros originais não são editáveis
- **WHEN** qualquer cliente tenta `PUT` ou `DELETE` em um volume
- **THEN** a API responde 405 e o registro permanece

### Requirement: Listar volumes por organização

Administrador e Operador SHALL listar volumes da própria organização por período, tipo, origem e
ponto, com tipo, unidade, quantidade, origem, operador, motivo e vínculo ao original. (RF-15, RF-16;
RN-06; RNF-07)

#### Scenario: Filtro por origem
- **WHEN** o operador filtra por origem Retirada
- **THEN** a API devolve apenas volumes de retirada do período

#### Scenario: Cidadão não acessa volumes
- **WHEN** um cidadão tenta listar volumes
- **THEN** a API responde 403
