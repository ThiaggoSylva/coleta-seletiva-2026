# Spec Delta

## Purpose

Define como o sistema registra alterações críticas de forma rastreável, com organização, ator, momento e correlação, sem expor segredos ou dados pessoais desnecessários.

## ADDED Requirements

### Requirement: Registro transacional de alterações críticas

Alterações críticas SHALL gerar um registro de auditoria na mesma transação da alteração. O registro MUST conter organização, ator, momento, ação, entidade, identificador e correlação. (RNF-05; CA-02, CA-11; DT-08, DT-18)

#### Scenario: Alteração crítica grava auditoria
- **WHEN** uma operação crítica é concluída com sucesso
- **THEN** o sistema persiste a alteração e seu registro de auditoria na mesma transação

#### Scenario: Falha na alteração não deixa auditoria isolada
- **WHEN** a operação crítica falha antes do commit
- **THEN** nem a alteração nem o registro de auditoria são persistidos

#### Scenario: Ação não crítica não gera registro indevido
- **WHEN** o usuário apenas consulta dados sem alterá-los
- **THEN** o sistema não cria registro de auditoria de alteração

### Requirement: Conteúdo mínimo e privacidade da auditoria

O registro de auditoria SHALL identificar quem alterou o quê, quando e em qual organização. O sistema MUST NOT armazenar senha, token, hash de credencial ou dados pessoais desnecessários. (RNF-02, RNF-05; CA-11; DT-08, DT-18)

#### Scenario: Auditoria não contém segredos
- **WHEN** uma alteração envolve autenticação ou credenciais
- **THEN** o registro contém a ação e os identificadores, mas não a senha, o token ou o hash

#### Scenario: Auditoria não replica dados pessoais
- **WHEN** uma alteração afeta um cadastro com dados pessoais
- **THEN** o registro usa identificadores e campos alterados, sem copiar nome, telefone ou endereço

#### Scenario: Correlação permite rastrear a operação
- **WHEN** a auditoria é consultada para uma operação
- **THEN** o registro permite correlacionar ator, requisição e entidade alterada sem expor conteúdo sensível

### Requirement: Rastreabilidade de eventos de acesso e equipe na F1

A F1 SHALL auditar, no mínimo, registro de organização e cidadão, login e logout, troca de senha, criação, edição e inativação de Operador e tentativas negadas de acesso cruzado. (RNF-01, RNF-05; CA-01, CA-02; DT-08, DT-18)

#### Scenario: Eventos de acesso são rastreáveis
- **WHEN** ocorrem login, logout, troca de senha ou tentativa negada de acesso
- **THEN** o sistema registra o evento com ator ou identificador de origem, momento e correlação

#### Scenario: Eventos de equipe são rastreáveis
- **WHEN** o Administrador cria, edita ou inativa um Operador
- **THEN** o sistema registra a ação com organização, ator, entidade e momento

#### Scenario: Tentativa negada entre organizações é registrada
- **WHEN** uma requisição tenta acessar recurso de outra organização
- **THEN** o sistema registra a negação com correlação, sem copiar dados do recurso alvo
