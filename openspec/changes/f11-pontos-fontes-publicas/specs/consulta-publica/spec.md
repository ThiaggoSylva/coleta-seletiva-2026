# consulta-publica Specification (delta)

## Purpose

Registra a ampliação do contrato público causada pelos pontos de fontes externas.

## Requirements

### Requirement: Contrato público com origem e atribuição

`POST /api/v1/publico/pontos/busca` SHALL devolver, para cada item, `origem`
(`Organizacao` ou `OpenStreetMap`), `atribuicao` e `urlDaFonte` opcionais, e SHALL aceitar
`municipio`/`bairro`/`rua`/`numero`/`complemento`/`cep` e `abertoAgora` nulos quando o ponto vier
de fonte externa. A página SHALL incluir `avisoDeFonteExterna` e `atribuicaoDeFonteExterna`. (RF-09,
RF-23; DT-13, DT-35)

#### Scenario: Ponto cadastrado mantém os campos preenchidos
- **WHEN** o item vem de uma organização publicada
- **THEN** `origem` é `Organizacao`, o endereço e `abertoAgora` são booleanos/strings preenchidos
  como antes e `atribuicao` e `urlDaFonte` são nulos

#### Scenario: Ponto externo não quebra o consumidor existente
- **WHEN** o item vem do OpenStreetMap
- **THEN** `origem` é `OpenStreetMap`, `abertoAgora` é nulo, endereço pode ser nulo e o consumidor
  exibe "horário não informado" e o link para o registro original

#### Scenario: Consulta pública continua sem dados pessoais
- **WHEN** qualquer combinação de filtros retorna pontos externos
- **THEN** nenhum nome, telefone ou endereço de cidadão aparece na resposta
