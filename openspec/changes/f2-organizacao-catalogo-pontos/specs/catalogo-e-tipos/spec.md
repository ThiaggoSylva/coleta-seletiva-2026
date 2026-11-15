# catalogo-e-tipos Specification

## Purpose

Define o catálogo global e somente leitura de municípios e de tipos de resíduo e a habilitação
de tipos por organização, incluindo a indicação de retirada domiciliar.

## Requirements

### Requirement: Catálogo global de tipos de resíduo

A API SHALL expor o catálogo de tipos de resíduo com código, nome, categoria, unidade fixa e
orientações de preparo. O catálogo MUST ser somente leitura na v1. (RF-07; RN-06; DT-15)

#### Scenario: Catálogo disponível para consulta
- **WHEN** um usuário autenticado consulta o catálogo
- **THEN** a API devolve os tipos ordenados por nome, com unidade fixa e categoria

#### Scenario: Catálogo não é editável
- **WHEN** qualquer requisição tenta criar ou alterar tipo de resíduo
- **THEN** a API não expõe rota de escrita e responde 404/405

### Requirement: Catálogo global de municípios

A API SHALL expor os municípios do IBGE, com filtro por UF e por texto do nome, paginados com
teto. (RF-07; DT-15)

#### Scenario: Filtro por UF e texto
- **WHEN** a consulta informa UF e trecho do nome
- **THEN** a API devolve apenas municípios que atendem aos dois filtros

#### Scenario: UF inválida é rejeitada
- **WHEN** a UF não tem duas letras
- **THEN** a API responde 400 com erro no campo

### Requirement: Tipos habilitados pela organização

O Administrador SHALL habilitar tipos do catálogo para a própria organização, indicando se cada
um aceita retirada domiciliar. A API MUST rejeitar tipos inexistentes e MUST bloquear a
desabilitação de tipo em uso por ponto. (RF-07; CA-03; DT-06)

#### Scenario: Habilitação válida
- **WHEN** o Administrador habilita tipos existentes com a indicação de retirada domiciliar
- **THEN** o sistema substitui os vínculos e devolve os tipos habilitados

#### Scenario: Tipo inexistente é rejeitado
- **WHEN** a entrada contém tipo fora do catálogo
- **THEN** a API responde 400 e não altera a habilitação

#### Scenario: Tipo em uso não é desabilitado
- **WHEN** o Administrador remove tipo usado por ponto ativo da organização
- **THEN** a API responde 409, orienta ajustar os pontos e mantém o vínculo

#### Scenario: Operador não habilita tipos
- **WHEN** um Operador tenta alterar os tipos habilitados
- **THEN** a API responde 403
