# perfil-da-organizacao Specification

## Purpose

Define o perfil público da organização, seus municípios atendidos, períodos de retirada e o
controle de publicação que determina a visibilidade na consulta pública.

## Requirements

### Requirement: Edição do perfil da organização

O Administrador SHALL consultar e editar nome, tipo, contato público, fuso IANA e limite de
solicitações abertas da própria organização. O fuso MUST ser um identificador IANA válido e o
limite MUST ser positivo. (RF-06; DT-09, DT-12)

#### Scenario: Administrador atualiza o perfil
- **WHEN** o Administrador envia nome, tipo, contato, fuso válido e limite positivo
- **THEN** o sistema persiste a alteração, registra auditoria e devolve o perfil atualizado

#### Scenario: Fuso inválido é rejeitado
- **WHEN** o fuso informado não é um identificador IANA reconhecido
- **THEN** a API responde 400 com erro no campo e não altera o perfil

#### Scenario: Operador ou cidadão não edita o perfil
- **WHEN** um Operador ou Cidadão tenta alterar o perfil
- **THEN** a API responde 403 e não altera dados

### Requirement: Municípios atendidos

O Administrador SHALL definir os municípios atendidos a partir do catálogo do IBGE, sem texto
livre. Remover um município MUST ser bloqueado enquanto existir ponto que o utilize. (RF-06;
DT-15)

#### Scenario: Municípios válidos são aceitos
- **WHEN** o Administrador informa códigos existentes no catálogo
- **THEN** o sistema substitui os vínculos e devolve a lista atualizada

#### Scenario: Código inexistente é rejeitado
- **WHEN** a entrada contém código fora do catálogo
- **THEN** a API responde 400 e não altera vínculos

#### Scenario: Município em uso não é removido
- **WHEN** o Administrador remove município usado por ponto da organização
- **THEN** a API responde 409, orienta ajustar os pontos e mantém o vínculo

### Requirement: Períodos de retirada

O Administrador SHALL configurar os períodos de retirada com hora inicial e final no mesmo dia.
Os intervalos MUST ter fim maior que o início. (RF-06; RN-01)

#### Scenario: Períodos válidos são aceitos
- **WHEN** o Administrador informa períodos com intervalos válidos
- **THEN** o sistema substitui a configuração e devolve os períodos atualizados

#### Scenario: Intervalo inválido é rejeitado
- **WHEN** a hora final não é maior que a inicial
- **THEN** a API responde 400 e não altera a configuração

### Requirement: Publicação da organização

O Administrador SHALL publicar ou despublicar a organização. Publicar MUST exigir ao menos um
município atendido e um tipo habilitado. Despublicar MUST NOT apagar dados nem cancelar
solicitações existentes. (RF-06; RN-09, RN-12; CA-02)

#### Scenario: Publicação com pré-requisitos
- **WHEN** a organização tem município e tipo habilitado e o Administrador publica
- **THEN** o sistema marca a organização como publicada e ela passa a aparecer na consulta pública

#### Scenario: Publicação sem pré-requisitos é rejeitada
- **WHEN** a organização não tem município ou não tem tipo habilitado
- **THEN** a API responde 409 com a pendência e mantém a organização não publicada

#### Scenario: Despublicação preserva dados
- **WHEN** o Administrador despublica a organização
- **THEN** a organização e seus pontos saem da consulta pública, sem exclusão de dados
