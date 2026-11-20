# Spec Delta

## Purpose

Define como o produto sinaliza, na API e na interface, recursos do roadmap que ainda não foram
implementados, mantendo um catálogo previsível, sem dados fictícios e sem expor dados pessoais.

## ADDED Requirements

### Requirement: Catálogo somente leitura dos módulos em breve

O sistema SHALL manter um catálogo imutável em tempo de execução com exatamente quatro módulos:
`preferencias-email` (F7), `extrato-pontos` (F8), `rotas-coletor` (F9) e
`transparencia-impacto` (F10). Cada item MUST conter identificador, título, descrição, fase, rota
da interface e situação `em_breve`, sem dados pessoais ou identificadores de organização.
(RF-24; RNF-10; CA-16; DT-38)

#### Scenario: Catálogo contém os quatro módulos planejados
- **WHEN** o catálogo é consultado por qualquer cliente autorizado a ler o índice
- **THEN** os quatro módulos são devolvidos com fase e rota correspondentes a F7, F8, F9 e F10

#### Scenario: Catálogo não contém dados sensíveis
- **WHEN** um cliente inspeciona todas as propriedades do catálogo
- **THEN** não há nome, e-mail, telefone, endereço, identificador de cidadão ou identificador de organização

### Requirement: Índice público de placeholders

A API SHALL expor `GET /api/v1/em-breve` sem exigir sessão e SHALL responder `200` com o catálogo
em português do Brasil. O contrato OpenAPI MUST documentar a operação e a resposta `200`.
(RF-24; CA-13; DT-36)

#### Scenario: Visitante consulta o índice
- **WHEN** um cliente sem sessão chama `GET /api/v1/em-breve`
- **THEN** a API responde `200` com os quatro módulos e nenhum dado pessoal ou de organização

#### Scenario: Operação aparece no contrato
- **WHEN** um cliente consulta o documento OpenAPI da API
- **THEN** `GET /api/v1/em-breve` aparece com resposta `200` documentada

### Requirement: Operação de módulo responde 501

A API SHALL expor `GET /api/v1/em-breve/{modulo}` para cada item do catálogo e SHALL responder
`501 Not Implemented` com `ProblemDetails` contendo `status=501`, `title`, `detail`,
`code=em_breve`, `modulo`, `fase` e `traceId`. A operação MUST NOT devolver sucesso, dado fictício,
formulário utilizável, persistência ou efeito colateral. (RF-25; RN-13; CA-14; DT-37)

#### Scenario: Cliente chama módulo catalogado
- **WHEN** um cliente autorizado chama `GET /api/v1/em-breve/{modulo}` com um identificador do catálogo
- **THEN** a API responde `501` com `code=em_breve`, `modulo`, `fase`, `detail` e `traceId`

#### Scenario: Nenhum resultado enganoso é produzido
- **WHEN** a operação de um módulo em breve é chamada
- **THEN** não há resposta `2xx`, dado de demonstração, gravação ou efeito colateral

#### Scenario: Módulo desconhecido não é aceito
- **WHEN** um cliente chama a operação com identificador fora do catálogo
- **THEN** a API responde `404` com `code=nao_encontrado`, sem ecoar conteúdo arbitrário

#### Scenario: Contrato documenta o 501
- **WHEN** um cliente consulta o documento OpenAPI da API
- **THEN** as operações de detalhe aparecem com a resposta `501` documentada

### Requirement: Autorização das operações de detalhe

O índice SHALL permanecer público. A operação de `transparencia-impacto` SHALL ser pública; as de
`preferencias-email` e `extrato-pontos` SHALL exigir o papel Cidadão; a de `rotas-coletor` SHALL
exigir Administrador ou Operador. Sem sessão a API MUST responder `401`; com papel incompatível
MUST responder `403`. Mensagens de erro MUST NOT conter dados pessoais. (RF-27; RNF-10; CA-15; DT-39)

#### Scenario: Detalhe público é acessível sem sessão
- **WHEN** um visitante chama a operação de `transparencia-impacto`
- **THEN** a API responde `501` com os metadados do módulo

#### Scenario: Detalhe protegido exige sessão
- **WHEN** um visitante chama a operação de `preferencias-email`, `extrato-pontos` ou `rotas-coletor`
- **THEN** a API responde `401` sem revelar metadados do módulo

#### Scenario: Cidadão acessa apenas os próprios módulos
- **WHEN** um Cidadão autenticado chama `preferencias-email` ou `extrato-pontos`
- **THEN** a API responde `501` com os metadados do módulo

#### Scenario: Cidadão não acessa rotas do coletor
- **WHEN** um Cidadão autenticado chama a operação de `rotas-coletor`
- **THEN** a API responde `403` sem revelar metadados do módulo

#### Scenario: Equipe da organização acessa rotas do coletor
- **WHEN** um Administrador ou Operador autenticado chama a operação de `rotas-coletor`
- **THEN** a API responde `501` com os metadados do módulo

#### Scenario: Equipe da organização não acessa módulos do cidadão
- **WHEN** um Administrador ou Operador autenticado chama `preferencias-email` ou `extrato-pontos`
- **THEN** a API responde `403` sem revelar metadados do módulo

### Requirement: Páginas de roadmap na interface

A interface SHALL disponibilizar rotas estáveis `/em-breve/preferencias-email`,
`/em-breve/extrato-pontos`, `/em-breve/rotas-coletor` e `/em-breve/transparencia-impacto`. Cada rota
MUST apresentar título, descrição, fase, aviso explícito de que o recurso ainda não está
disponível e ação de retorno, em português do Brasil, sem data prometida nem formulário habilitado.
(RF-26; RN-13; CA-15; DT-40)

#### Scenario: Todas as rotas abrem com conteúdo em breve
- **WHEN** um usuário autorizado acessa qualquer rota `/em-breve/{modulo}` do catálogo
- **THEN** a página mostra título, descrição, fase, aviso "em breve" e link de retorno funcional

#### Scenario: Nenhuma ação de recurso pronto é oferecida
- **WHEN** um usuário visualiza uma página de roadmap
- **THEN** não há formulário habilitado, botão de envio, dado de exemplo ou data de lançamento

### Requirement: Navegação por papel

O cabeçalho SHALL exibir links apenas para os módulos que o usuário pode abrir, considerando papel e
troca de senha pendente. O rodapé SHALL manter um link público para transparência/impacto. Nenhum
link MUST ser oferecido a quem não pode acessar o módulo. (RF-27; DT-41)

#### Scenario: Visitante vê apenas o link público
- **WHEN** um visitante sem sessão consulta cabeçalho e rodapé
- **THEN** transparência/impacto aparece no rodapé e nenhum módulo protegido aparece no cabeçalho

#### Scenario: Cidadão vê os módulos do cidadão
- **WHEN** um Cidadão autenticado sem troca de senha pendente consulta a navegação
- **THEN** preferências de e-mail e extrato de pontos aparecem, e rotas do coletor não aparecem

#### Scenario: Equipe vê rotas do coletor
- **WHEN** um Administrador ou Operador autenticado sem troca de senha pendente consulta a navegação
- **THEN** rotas do coletor aparecem, e preferências de e-mail e extrato de pontos não aparecem

#### Scenario: Troca de senha pendente restringe links
- **WHEN** um usuário com troca de senha pendente consulta o cabeçalho
- **THEN** nenhum link de módulo em breve é oferecido

### Requirement: Responsividade e privacidade da interface

As páginas SHALL ser utilizáveis em desktop 1280×720 e celular 390×844, sem rolagem horizontal,
com hierarquia de títulos, foco visível e texto alternativo adequado quando houver ícones. A
interface MUST usar somente o catálogo estático e MUST NOT registrar dados pessoais em log,
screenshot ou mensagem de erro. (RNF-09, RNF-10, RNF-11; CA-15, CA-16; DT-42)

#### Scenario: Layout desktop permanece legível
- **WHEN** uma página em breve é aberta em 1280×720
- **THEN** título, descrição, fase, aviso e retorno são visíveis sem rolagem horizontal

#### Scenario: Layout mobile permanece legível
- **WHEN** uma página em breve é aberta em 390×844
- **THEN** o conteúdo se adapta à largura da tela, sem rolagem horizontal ou corte de texto

#### Scenario: Nenhum dado pessoal é registrado
- **WHEN** testes automatizados ou inspeção manual executam os fluxos de roadmap
- **THEN** logs, falhas e evidências visuais não contêm nome, contato, endereço ou identificador de cidadão
