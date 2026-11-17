# painel-e-relatorios Specification

## Purpose

Define as agregações do painel da organização e a exportação CSV, sempre sem dados pessoais e sem
somar unidades diferentes.

## Requirements

### Requirement: Consultar resumo do painel

O Administrador SHALL consultar, por período e filtros de tipo e origem, o volume por tipo, o total
por unidade, a quantidade de retiradas por estado e o volume por origem, com detalhe por ponto para
recebimentos. O cálculo MUST usar apenas `RegistroVolume`, incluindo ajustes, e MUST NOT somar
unidades diferentes. (RF-17; RN-05–RN-07; CA-09, CA-10)

#### Scenario: Totais por unidade são separados
- **WHEN** existem 13,750 kg e 8,000 L no período
- **THEN** o resumo devolve 13,750 kg e 8,000 L separadamente, sem total único somado

#### Scenario: Ajuste entra no total
- **WHEN** um original de 14,750 kg recebe ajuste de −1,000 kg
- **THEN** o total do tipo no painel é 13,750 kg

#### Scenario: Estimativas não entram no painel
- **WHEN** solicitações possuem quantidades estimadas sem execução
- **THEN** o painel não contabiliza essas quantidades

#### Scenario: Operador não consulta painel
- **WHEN** um Operador tenta consultar o painel
- **THEN** a API responde 403

### Requirement: Consultar série temporal

O Administrador SHALL consultar a série por dia, semana ou mês no período, agrupada por tipo e
unidade, com datas estáveis e sem conversão de unidade. (RF-17; RN-06; DT-16)

#### Scenario: Série diária
- **WHEN** o Administrador escolhe agrupamento diário
- **THEN** cada dia com volume aparece uma vez por tipo/unidade

#### Scenario: Agrupamento inválido é rejeitado
- **WHEN** o agrupamento não é dia, semana ou mês
- **THEN** a API responde 400

### Requirement: Exportar CSV sem dados pessoais

O Administrador SHALL exportar os volumes do período e filtros do painel em CSV UTF-8 com BOM,
separador `;`, decimais com vírgula, ajustes identificados e sem nome, telefone ou endereço de
cidadão. Células iniciadas por `=`, `+`, `-` ou `@` MUST ser neutralizadas. (RF-18; RNF-02, RNF-08;
CA-10)

#### Scenario: CSV concorda com o painel
- **WHEN** o Administrador exporta o mesmo período e filtros do painel
- **THEN** os totais por tipo e unidade coincidem

#### Scenario: Ajuste aparece identificado
- **WHEN** o período contém ajuste
- **THEN** a linha do CSV identifica a origem Ajuste e o registro original

#### Scenario: Fórmula é neutralizada
- **WHEN** um valor de texto começa com `=`, `+`, `-` ou `@`
- **THEN** a célula recebe prefixo apóstrofo e não é executada pelo Excel

#### Scenario: CSV não contém PII
- **WHEN** o CSV é gerado para período com retiradas
- **THEN** nenhuma coluna contém nome, telefone ou endereço de cidadão
