# Tasks

## 1. Modelo e persistência

- [x] 1.1 Criar as entidades globais `Municipio` e `TipoResiduo` com configuração de EF, catálogo semeado por migração e teste de modelo (RF-07; DT-04, DT-15).
- [x] 1.2 Criar `OrganizacaoMunicipio`, `OrganizacaoTipo` e `ConfiguracaoDePeriodo` com índices únicos, chaves alternativas e filtros de tenant (RF-06, RF-07; DT-07).
- [x] 1.3 Criar `PontoColeta`, `PontoHorario` e `PontoTipo` com `geography` SRID 4326, índice espacial, FKs compostas, `rowversion` e exclusão lógica (RF-08, RF-22; DT-07, DT-11).
- [x] 1.4 Gerar a migration aditiva, semear municípios do IBGE e tipos iniciais, e atualizar `migrations-monsterasp.sql` (DT-15).

## 2. Perfil e catálogo

- [x] 2.1 Implementar consulta e edição do perfil da organização, com contato, fuso validado e limite de solicitações abertas (RF-06).
- [x] 2.2 Implementar definição de municípios atendidos e períodos de retirada, rejeitando vínculos em uso (RF-06).
- [x] 2.3 Implementar habilitação de tipos com retirada domiciliar e bloqueio de desabilitação em uso (RF-07).
- [x] 2.4 Implementar publicação e despublicação com validação de município e tipo (RF-06, RN-09, RN-12).
- [x] 2.5 Implementar os catálogos globais de tipos e municípios, este último com filtro por UF e texto (RF-07; DT-15).

## 3. Pontos de coleta

- [x] 3.1 Implementar criação e edição de ponto com endereço, coordenadas, horários e tipos, com validações de CA-03 (RF-08; RN-08).
- [x] 3.2 Implementar listagem, detalhe, alteração de status e retirada lógica, com nome único e preservação de histórico (RF-08, RF-22).
- [x] 3.3 Restringir a gestão ao Administrador e expor `GET /pontos/opcoes` ao Operador (RF-03, RF-04; DT-06).
- [x] 3.4 Implementar `aberto agora` no domínio com fuso IANA, início inclusivo e fim exclusivo (RN-08; CA-05).

## 4. Consulta pública

- [x] 4.1 Implementar busca anônima de pontos com filtros de município, tipo e aberto agora, paginação e lista equivalente ao mapa (RF-09; DT-13).
- [x] 4.2 Implementar busca por proximidade com raio limitado, coordenadas no corpo e ordenação determinística (RF-09; RN-08; DT-11).
- [x] 4.3 Garantir DTOs públicos sem dados pessoais, cache curto e limite de taxa (RF-09; RNF-02, RNF-08; DT-13).

## 5. Frontend

- [ ] 5.1 Criar a tela de perfil da organização com contato, fuso, limite, municípios, períodos, tipos e publicação (RF-06, RF-07).
- [ ] 5.2 Criar as telas de listagem, criação e edição de pontos, com horários por dia e seleção de tipos (RF-08).
- [ ] 5.3 Criar a consulta pública com filtros, lista equivalente, mapa Leaflet/OpenStreetMap e estado vazio (RF-09; RNF-04; DT-02).

## 6. Verificação

- [ ] 6.1 Cobrir domínio e aplicação com testes MSTest/Moq para CA-02–CA-05 e cenários de conflito e autorização (DT-17).
- [ ] 6.2 Cobrir as rotas novas em testes de integração com SQL Server, incluindo isolamento A/B e ausência de PII na consulta pública (RF-03, RF-09; DT-17).
- [ ] 6.3 Executar `dotnet build`, `dotnet format --verify-no-changes`, testes unitários, `npm run build` e `npm test -- --run`; registrar bloqueios (DT-17).

## Workflow follow-up

- Commits pequenos em português na branch `feat/f1-fundacao-segura`; abrir ou atualizar o PR somente ao final, com rastreabilidade RF/DT e evidências reais.
