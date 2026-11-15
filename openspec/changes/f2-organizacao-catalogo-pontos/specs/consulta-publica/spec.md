# consulta-publica Specification

## Purpose

Define a consulta anônima e somente leitura de pontos de coleta de organizações publicadas, com
filtros por município, tipo, aberto agora e proximidade, sem qualquer dado pessoal.

## Requirements

### Requirement: Busca pública de pontos

A API SHALL expor busca anônima de pontos com filtros de município, tipo de resíduo e aberto
agora, paginação com teto e ordenação estável. Somente organizações publicadas e pontos ativos e
não retirados MUST aparecer. A resposta MUST NOT conter dados pessoais. (RF-09; RN-08, RN-09;
RNF-02; DT-13; CA-04)

#### Scenario: Consulta sem login
- **WHEN** um visitante consulta sem autenticação
- **THEN** a API devolve apenas pontos ativos de organizações publicadas, com horários, tipos e contato da organização

#### Scenario: Organização despublicada não aparece
- **WHEN** a organização é despublicada
- **THEN** ela e seus pontos deixam de aparecer na consulta pública sem exclusão de dados

#### Scenario: Ponto inativo ou retirado não aparece
- **WHEN** o ponto está Inativo, Indisponível temporariamente ou retirado logicamente
- **THEN** ele não é retornado

#### Scenario: Filtro por município e tipo
- **WHEN** a busca informa município e tipo
- **THEN** a API devolve somente pontos daquele município que aceitam o tipo

#### Scenario: Sem resultados
- **WHEN** nenhum ponto atende aos filtros
- **THEN** a API devolve lista vazia com total zero, sem erro

### Requirement: Aberto agora

A API SHALL calcular `abertoAgora` no fuso da organização, com início inclusivo e fim exclusivo,
considerando apenas status Ativo. (RN-08; CA-05)

#### Scenario: Limites do horário
- **WHEN** o ponto funciona das 08:00 às 17:00 e o instante local é 07:59, 08:00 ou 17:00
- **THEN** o ponto é respectivamente fechado, aberto e fechado

#### Scenario: Dia sem funcionamento
- **WHEN** o dia da semana não possui intervalo configurado
- **THEN** o ponto não está aberto

### Requirement: Busca por proximidade

A API SHALL aceitar coordenadas no corpo da requisição e raio limitado por configuração,
devolvendo pontos ordenados por distância com desempate determinístico. As coordenadas MUST NOT
ser registradas em log ou URL. (RF-09; RNF-02; DT-11)

#### Scenario: Ordenação por distância
- **WHEN** a busca informa coordenadas e raio
- **THEN** a API devolve apenas pontos dentro do raio, em ordem crescente de distância

#### Scenario: Raio acima do máximo é limitado
- **WHEN** o raio informado excede o máximo configurado
- **THEN** a API responde 400 com erro no campo e não executa a busca

#### Scenario: Coordenadas inválidas são rejeitadas
- **WHEN** latitude ou longitude estão fora do intervalo válido
- **THEN** a API responde 400 e não executa a busca

### Requirement: Proteção da superfície pública

As rotas públicas SHALL ter limite de taxa, cache curto e paginação com teto, e MUST usar DTO
próprio sem nome, telefone ou endereço de cidadão. (RF-09; RNF-02, RNF-08; DT-13)

#### Scenario: Excesso de requisições
- **WHEN** a mesma origem excede o limite configurado
- **THEN** a API responde 429

#### Scenario: Contrato sem dados pessoais
- **WHEN** a resposta pública é inspecionada
- **THEN** ela contém apenas dados do ponto, horários, tipos e contato público da organização
