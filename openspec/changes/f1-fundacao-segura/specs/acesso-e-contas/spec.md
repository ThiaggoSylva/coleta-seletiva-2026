# Spec Delta

## Purpose

Define como organizações e cidadãos criam contas, autenticam-se, mantêm a sessão, alteram os próprios dados e têm o acesso negado quando não autorizados.

## ADDED Requirements

### Requirement: Cadastro de organização com administrador

O sistema SHALL permitir registrar uma organização com nome, tipo entre Cooperativa, Associação, Prefeitura ou Outro, e conta administradora (usuário, e-mail e senha), criando organização e Administrador na mesma transação. A organização MUST nascer não publicada. (RF-01; RNF-01; CA-01; DT-05, DT-07)

#### Scenario: Cadastro válido cria organização e administrador
- **WHEN** uma pessoa informa nome da organização, tipo, usuário, e-mail e senha válidos
- **THEN** o sistema cria a organização não publicada e a conta Administrador vinculada, sem autenticar automaticamente

#### Scenario: Usuário ou e-mail duplicado é rejeitado
- **WHEN** o cadastro usa usuário ou e-mail já existente na plataforma
- **THEN** o sistema rejeita a operação com erro de conflito e não cria organização nem conta

#### Scenario: Cadastro inválido não cria registros parciais
- **WHEN** o cadastro contém campo obrigatório ausente, formato inválido ou senha fora da política
- **THEN** o sistema retorna erro de validação por campo e não persiste organização nem administrador

#### Scenario: Cadastro de organização é limitado por taxa
- **WHEN** a origem excede o limite de tentativas de cadastro configurado
- **THEN** o sistema responde com 429 sem revelar dados de contas existentes

### Requirement: Cadastro de cidadão

O sistema SHALL permitir registrar uma conta de cidadão com nome, e-mail, telefone e senha, sem organização e sem autenticação automática. O e-mail MUST ser único na plataforma. (RF-05; RNF-01, RNF-02; CA-02; DT-05, DT-07)

#### Scenario: Cadastro válido cria cidadão
- **WHEN** uma pessoa informa nome, e-mail, telefone e senha válidos
- **THEN** o sistema cria a conta com papel Cidadão e sem organização, sem autenticar automaticamente

#### Scenario: E-mail de cidadão duplicado é rejeitado
- **WHEN** o cadastro usa e-mail já existente na plataforma
- **THEN** o sistema rejeita a operação com erro de conflito e não cria a conta

#### Scenario: Dados pessoais não aparecem em erros ou logs
- **WHEN** o cadastro falha por validação ou conflito
- **THEN** a resposta e o log contêm apenas código, campo e correlação, sem nome, telefone ou senha

### Requirement: Autenticação por perfil

O sistema SHALL autenticar Administrador e Operador por usuário ou e-mail e Cidadão somente por e-mail, usando credenciais protegidas pelo Identity. Falhas MUST retornar mensagem genérica e conta inativa MUST NOT autenticar. (RF-02, RF-04; RNF-01; CA-01, CA-02; DT-04, DT-05)

#### Scenario: Administrador ou operador entra com usuário ou e-mail
- **WHEN** uma conta ativa de organização informa usuário ou e-mail e a senha correta
- **THEN** o sistema inicia a sessão com o papel e a organização da conta

#### Scenario: Cidadão entra somente por e-mail
- **WHEN** uma conta de cidadão informa e-mail e senha corretos
- **THEN** o sistema inicia a sessão com papel Cidadão e sem organização

#### Scenario: Credenciais inválidas usam mensagem genérica
- **WHEN** o login usa senha incorreta, usuário inexistente ou e-mail inexistente
- **THEN** o sistema responde com a mesma mensagem genérica e não revela qual dado está incorreto

#### Scenario: Conta inativa não autentica
- **WHEN** uma conta de Operador inativa tenta entrar
- **THEN** o sistema nega a autenticação sem revelar se a conta existe

### Requirement: Sessão com token curto e refresh rotativo

O sistema SHALL emitir token de acesso de 15 minutos e refresh opaco rotativo em cookie HttpOnly, Secure e SameSite=Strict. Refresh reutilizado MUST revogar a família, e logout MUST revogar a família e limpar o cookie. (RF-02; RNF-01; DT-05)

#### Scenario: Login estabelece sessão
- **WHEN** a autenticação é concluída
- **THEN** o sistema devolve token de acesso em memória e refresh em cookie HttpOnly, com `Cache-Control: no-store`

#### Scenario: Refresh rotaciona o token
- **WHEN** o cliente apresenta um refresh válido e ainda não consumido
- **THEN** o sistema invalida o refresh apresentado, emite um novo refresh e devolve novo token de acesso

#### Scenario: Reuso de refresh revoga a família
- **WHEN** um refresh já consumido é apresentado novamente
- **THEN** o sistema revoga toda a família de refresh e exige novo login

#### Scenario: Logout encerra a sessão
- **WHEN** o usuário autenticado solicita logout
- **THEN** o sistema revoga a família de refresh, limpa o cookie e impede novas renovações

### Requirement: Alteração da própria senha

O usuário autenticado SHALL alterar a própria senha informando a senha atual e uma nova senha válida. A alteração MUST revogar os refresh tokens existentes e exigir novo login nos demais dispositivos. (RF-02; RNF-01; DT-05)

#### Scenario: Troca de senha válida
- **WHEN** o usuário informa a senha atual correta e uma nova senha que atende à política
- **THEN** o sistema atualiza a credencial e revoga os refresh tokens existentes

#### Scenario: Senha atual incorreta
- **WHEN** o usuário informa senha atual incorreta
- **THEN** o sistema rejeita a alteração com erro genérico e mantém a senha anterior

#### Scenario: Nova senha fora da política
- **WHEN** a nova senha não atende aos requisitos mínimos
- **THEN** o sistema rejeita a alteração e indica os requisitos sem expor a senha

### Requirement: Perfil do cidadão

O cidadão autenticado SHALL consultar e editar o próprio nome, telefone e e-mail. A edição MUST NOT alterar papel, organização ou identificadores, e o e-mail MUST permanecer único na plataforma. (RF-05; RNF-01, RNF-02; CA-02; DT-06, DT-12)

#### Scenario: Cidadão edita o próprio perfil
- **WHEN** o cidadão autenticado altera nome, telefone ou e-mail para valores válidos e únicos
- **THEN** o sistema persiste a alteração e devolve o perfil atualizado

#### Scenario: Tentativa de alterar papel ou organização é rejeitada
- **WHEN** a requisição inclui papel ou organização diferente do contexto autenticado
- **THEN** o sistema ignora ou rejeita esses campos e mantém o vínculo original

#### Scenario: E-mail duplicado é rejeitado
- **WHEN** o cidadão tenta usar e-mail já pertencente a outra conta
- **THEN** o sistema rejeita a alteração com erro de conflito

### Requirement: Autorização de rotas de acesso

Rotas protegidas SHALL exigir sessão válida e policy compatível com o papel. A API MUST responder 401 sem sessão e 403 sem permissão, independentemente do que a interface exibe. (RNF-01; CA-01, CA-02; DT-06)

#### Scenario: Rota protegida sem sessão é negada
- **WHEN** uma requisição sem token válido acessa uma rota protegida
- **THEN** a API responde 401 e não executa a operação

#### Scenario: Cidadão não acessa função de organização
- **WHEN** um cidadão autenticado tenta acessar rota exclusiva de Administrador ou Operador
- **THEN** a API responde 403 e não executa a operação

#### Scenario: Operador não executa ação exclusiva do Administrador
- **WHEN** um operador autenticado tenta acessar gestão de equipe ou configuração da organização
- **THEN** a API responde 403 e não executa a operação

### Requirement: Proteção contra abuso nas rotas de acesso

Login e refresh SHALL aplicar limite de taxa por rota e origem, responder 429 ao excedente e não revelar existência de conta. Cadastro SHALL aplicar o mesmo limite e pode informar conflito de unicidade, sem copiar dados sensíveis. Falhas MUST NOT registrar senha, token ou dado pessoal em log. (RNF-01, RNF-02, RNF-08; CA-01; DT-05, DT-18)

#### Scenario: Excesso de tentativas é limitado
- **WHEN** uma origem excede o limite configurado para login, cadastro ou refresh
- **THEN** o sistema responde 429 sem indicar se a conta existe

#### Scenario: Falha de autenticação não expõe segredos
- **WHEN** uma tentativa de login falha
- **THEN** a resposta e o log não contêm senha, token, e-mail completo ou telefone
