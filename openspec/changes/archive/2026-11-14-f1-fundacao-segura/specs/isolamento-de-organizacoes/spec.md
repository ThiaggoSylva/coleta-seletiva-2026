# Spec Delta

## Purpose

Define como o sistema garante que cada organização e cada cidadão acesse somente os próprios dados, mesmo quando identificadores de outro tenant ou de outro cidadão são conhecidos.

## ADDED Requirements

### Requirement: Contexto de organização derivado da identidade

A organização ativa SHALL ser derivada exclusivamente da identidade autenticada, nunca de cabeçalho, corpo, query string ou identificador de recurso. Requisições de organização sem claim válida MUST ser negadas. (RF-03; RNF-01; CA-01; DT-06, DT-07)

#### Scenario: Organização vem do token
- **WHEN** um Administrador ou Operador autenticado acessa uma rota da organização
- **THEN** o sistema usa a organização da claim do token para filtrar e gravar os dados

#### Scenario: Organização informada no corpo é ignorada
- **WHEN** a requisição tenta informar outra organização no corpo, cabeçalho ou query string
- **THEN** o sistema mantém a organização do token e não executa operação sobre a organização informada

#### Scenario: Claim de organização ausente ou inválida é negada
- **WHEN** uma conta de organização apresenta token sem claim de organização válida
- **THEN** a API responde 401 ou 403 e não executa a operação

### Requirement: Isolamento de leitura entre organizações

Consultas de Administrador e Operador SHALL retornar somente registros da organização do token. Um identificador de outra organização MUST ser tratado como inexistente, sem revelar sua existência ou conteúdo. (RF-03; RNF-01; CA-01, CA-02; DT-06, DT-07)

#### Scenario: Listagem mostra apenas a própria organização
- **WHEN** o Administrador da organização A lista recursos da organização
- **THEN** a resposta contém apenas registros vinculados à organização A

#### Scenario: Recurso da organização B não é lido
- **WHEN** o Administrador da organização A solicita um recurso cujo identificador pertence à organização B
- **THEN** a API responde 404 e não retorna dados da organização B

#### Scenario: Filtros não ampliam o escopo
- **WHEN** a requisição usa filtros, ordenação ou paginação para tentar alcançar outra organização
- **THEN** o sistema aplica o filtro de organização antes de qualquer outro critério

### Requirement: Isolamento de escrita entre organizações

Criações e alterações SHALL gravar somente na organização do token. O sistema MUST rejeitar vínculos, referências ou identificadores de outra organização antes do commit, inclusive em operações de atualização e exclusão lógica. (RF-03; RNF-01, RNF-03; CA-01, CA-02; DT-07)

#### Scenario: Criação com vínculo de outra organização é rejeitada
- **WHEN** o Administrador da organização A tenta criar um registro vinculado à organização B
- **THEN** o sistema rejeita a operação e não persiste o registro

#### Scenario: Alteração de recurso de outra organização é negada
- **WHEN** o Administrador da organização A tenta alterar ou excluir logicamente um recurso da organização B
- **THEN** a API responde 404 e o registro da organização B permanece inalterado

#### Scenario: Inconsistência de organização não chega ao banco
- **WHEN** uma entidade chega à persistência com organização diferente do contexto autenticado
- **THEN** a guarda de escrita rejeita a operação e a transação não é confirmada

### Requirement: Isolamento entre cidadãos

Rotas do cidadão SHALL filtrar pelo próprio identificador autenticado. O cidadão MUST NOT ler nem alterar perfil, solicitações ou qualquer dado de outro cidadão, mesmo conhecendo o identificador. (RF-03, RF-05; RNF-01; CA-01, CA-02; DT-06, DT-07)

#### Scenario: Cidadão acessa apenas o próprio perfil
- **WHEN** o cidadão X consulta ou edita o próprio perfil
- **THEN** o sistema retorna ou altera somente os dados de X

#### Scenario: Cidadão X não acessa dados do cidadão Y
- **WHEN** o cidadão X informa o identificador do cidadão Y em uma rota autenticada
- **THEN** a API responde 404 ou 403 e não retorna nem altera dados de Y

#### Scenario: Identificador conhecido não concede acesso
- **WHEN** um cidadão usa identificadores conhecidos de outro cidadão em GET, PUT ou POST
- **THEN** o sistema nega a operação em todas as tentativas, sem revelar a existência do recurso

### Requirement: Identificadores opacos não substituem autorização

Identificadores SHALL ser opacos e gerados pelo servidor, sem função de autorização. O sistema MUST aplicar a policy e o escopo de organização ou de dono antes de localizar o recurso. (RF-03; RNF-01; CA-01, CA-02; DT-06, DT-07)

#### Scenario: GUID válido de outro escopo é negado
- **WHEN** uma requisição apresenta um GUID válido pertencente a outro tenant ou outro cidadão
- **THEN** a API responde como recurso inexistente e não executa a operação

#### Scenario: Recurso inexistente e recurso de outro escopo têm a mesma resposta
- **WHEN** o identificador não existe ou pertence a outro escopo
- **THEN** a API mantém o mesmo status e formato de erro, sem permitir inferir a existência do recurso
