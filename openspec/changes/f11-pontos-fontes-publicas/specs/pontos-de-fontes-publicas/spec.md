# pontos-de-fontes-publicas Specification

## Purpose

Define a leitura somente leitura de pontos de coleta em fontes públicas externas
(OpenStreetMap), a mescla com os pontos cadastrados e a degradação quando a fonte não responde.

## Requirements

### Requirement: Leitura de pontos reais do OpenStreetMap

A API SHALL consultar `amenity=recycling` no OpenStreetMap via Overpass quando a busca pública
tiver município selecionado ou latitude/longitude com raio. A consulta MUST NOT ocorrer sem
recorte geográfico, MUST respeitar o limite configurado de pontos e MUST usar cache curto. (RF-23;
DT-35)

#### Scenario: Busca por município traz pontos reais
- **WHEN** o cidadão busca com um município selecionado
- **THEN** a API resolve a caixa do município, consulta o Overpass e devolve os pontos externos
  encontrados junto com os pontos cadastrados

#### Scenario: Busca sem recorte geográfico não varre o país
- **WHEN** a busca não informa município nem coordenadas
- **THEN** a API não consulta a fonte externa e responde apenas com os pontos cadastrados

#### Scenario: Filtro de tipo é aplicado na fonte externa
- **WHEN** a busca informa um tipo de resíduo
- **THEN** só são mesclados pontos externos cujas tags mapeiam para aquele tipo

#### Scenario: Aberto agora considera apenas pontos cadastrados
- **WHEN** a busca pede "aberto agora"
- **THEN** a fonte externa não é consultada, porque o horário dela não é confiável

### Requirement: Mescla, deduplicação e ordenação

Os pontos externos SHALL ser mesclados aos cadastrados antes da ordenação e da paginação, com
`origem = OpenStreetMap`, identificador estável, atribuição ODbL e link para o registro original.
Um ponto externo com o mesmo nome normalizado e até 120 m de um ponto cadastrado MUST ser
descartado. (RF-23; DT-11, DT-35)

#### Scenario: Total e páginas consideram as duas fontes
- **WHEN** existem pontos cadastrados e externos para o mesmo recorte
- **THEN** `total` é a soma após a deduplicação e cada página é um recorte da lista ordenada

#### Scenario: Ponto duplicado não aparece duas vezes
- **WHEN** o mesmo local existe no cadastro e no OpenStreetMap com nome equivalente e até 120 m
- **THEN** apenas o ponto cadastrado é exibido

### Requirement: Degradação e atribuição

A indisponibilidade da fonte pública MUST NOT gerar erro na consulta pública. A resposta SHALL
incluir aviso quando a fonte falhar e atribuição quando houver pontos externos, sem expor dados
pessoais. (RF-23; RNF-02)

#### Scenario: Fonte indisponível mantém a consulta
- **WHEN** o Overpass ou o Nominatim falha, expira ou responde inválido
- **THEN** a API responde 200 com os pontos cadastrados e `avisoDeFonteExterna` preenchido

#### Scenario: Atribuição acompanha os pontos externos
- **WHEN** a resposta contém ao menos um ponto de origem OpenStreetMap
- **THEN** `atribuicaoDeFonteExterna` identifica © OpenStreetMap contributors e a licença ODbL

### Requirement: Pontos externos são somente leitura

Pontos de fonte pública MUST NOT ser persistidos, editados, retirados, usados em retiradas,
registros de volume ou relatórios. (RF-23; RNF-01)

#### Scenario: Fonte desativada mantém o comportamento anterior
- **WHEN** `FontesPublicas:Pontos:Ativa` é `false`
- **THEN** a consulta pública responde como antes da mudança, sem chamadas de rede
