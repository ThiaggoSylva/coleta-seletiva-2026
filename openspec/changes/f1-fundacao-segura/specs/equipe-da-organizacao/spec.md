# Spec Delta

## Purpose

Define como o Administrador gerencia contas de Operador da própria organização, incluindo senha inicial, inativação e restrições de papel.

## ADDED Requirements

### Requirement: Cadastro de operador pelo administrador

O Administrador SHALL criar contas de Operador na própria organização com nome, usuário, e-mail e senha inicial. A conta MUST receber papel Operador, MUST pertencer à organização do administrador e MUST exigir troca de senha no primeiro acesso. (RF-04; RNF-01; CA-02; DT-05, DT-06, DT-07)

#### Scenario: Administrador cria operador válido
- **WHEN** o Administrador informa nome, usuário, e-mail e senha inicial válidos
- **THEN** o sistema cria a conta Operador na organização do administrador, com troca de senha pendente

#### Scenario: Usuário ou e-mail duplicado é rejeitado
- **WHEN** o cadastro usa usuário ou e-mail já existente na plataforma
- **THEN** o sistema rejeita a operação com erro de conflito e não cria a conta

#### Scenario: Não administrador não cria operador
- **WHEN** um Operador ou Cidadão tenta criar uma conta de equipe
- **THEN** a API responde 403 e não cria a conta

### Requirement: Troca obrigatória da senha inicial

O Operador criado com senha inicial SHALL alterar a senha no primeiro acesso antes de usar outras rotas protegidas. Enquanto a troca estiver pendente, o sistema MUST permitir apenas consulta da própria sessão, troca de senha e logout. (RF-02, RF-04; RNF-01; CA-02; DT-05, DT-06)

#### Scenario: Primeiro acesso exige troca de senha
- **WHEN** o Operador autentica com a senha inicial
- **THEN** o sistema informa que a troca é obrigatória e bloqueia as demais operações protegidas até a conclusão

#### Scenario: Troca de senha inicial concluída libera o acesso
- **WHEN** o Operador informa a senha inicial e uma nova senha válida
- **THEN** o sistema atualiza a credencial, remove a pendência e libera as rotas permitidas ao papel

#### Scenario: Nova senha inválida mantém a pendência
- **WHEN** a nova senha não atende à política
- **THEN** o sistema rejeita a troca, mantém a senha inicial e continua bloqueando as demais operações

### Requirement: Edição de operador

O Administrador SHALL editar nome, usuário e e-mail de Operadores da própria organização. A edição MUST NOT alterar papel ou organização, e usuário e e-mail MUST permanecer únicos na plataforma. (RF-04; RNF-01; CA-02; DT-06, DT-07, DT-12)

#### Scenario: Administrador edita dados do operador
- **WHEN** o Administrador altera nome, usuário ou e-mail para valores válidos e únicos
- **THEN** o sistema persiste a alteração e devolve a conta atualizada

#### Scenario: Edição tenta alterar papel ou organização
- **WHEN** a requisição inclui papel diferente de Operador ou outra organização
- **THEN** o sistema ignora ou rejeita esses campos e mantém o vínculo original

#### Scenario: Operador de outra organização não é editável
- **WHEN** o Administrador informa o identificador de um Operador de outra organização
- **THEN** a API responde 404 e não altera dados de outra organização

### Requirement: Inativação de operador

O Administrador SHALL inativar contas de Operador da própria organização. A conta inativa MUST NOT autenticar, MUST ter os refresh tokens revogados e MUST permanecer referenciada em registros históricos. (RF-04, RF-22; RNF-01; CA-02; DT-06, DT-07)

#### Scenario: Inativação impede novo login
- **WHEN** o Administrador inativa um Operador
- **THEN** o sistema revoga as sessões existentes e passa a negar novas autenticações da conta

#### Scenario: Conta inativa permanece no histórico
- **WHEN** uma conta de Operador é inativada após ter realizado operações
- **THEN** os registros históricos e de auditoria continuam identificando o ator sem permitir novo acesso

#### Scenario: Operador de outra organização não é inativado
- **WHEN** o Administrador tenta inativar um Operador de outra organização
- **THEN** a API responde 404 e não altera a conta

### Requirement: Gestão de equipe restrita ao Administrador da organização

Rotas de gestão de equipe SHALL ser exclusivas do Administrador e limitadas à organização do seu token. A API MUST negar acesso de Operador, Cidadão e de Administrador de outra organização. (RF-03, RF-04; RNF-01; CA-01, CA-02; DT-06, DT-07)

#### Scenario: Operador não gerencia equipe
- **WHEN** um Operador autenticado tenta listar, criar, editar ou inativar contas de equipe
- **THEN** a API responde 403 e não executa a operação

#### Scenario: Administrador A não acessa equipe da organização B
- **WHEN** o Administrador da organização A informa identificadores da organização B
- **THEN** a API responde 404 e não retorna nem altera dados da organização B

#### Scenario: Cidadão não acessa gestão de equipe
- **WHEN** um Cidadão autenticado tenta acessar qualquer rota de equipe
- **THEN** a API responde 403 e não executa a operação
