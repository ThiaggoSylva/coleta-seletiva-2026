# Proposal

## Why

A F1 entregou contas, sessão, papéis, equipe e isolamento entre organizações, mas a organização
ainda não tem perfil público, municípios atendidos, tipos de resíduo habilitados nem pontos de
coleta. Sem esses cadastros não existe consulta pública nem qualquer operação de retirada nas
fases seguintes. A F2 entrega essa camada de configuração e a primeira superfície anônima do
produto, sempre sem expor dados pessoais.

## What Changes

- Completar o perfil da organização com contato público, fuso IANA, limite de solicitações
  abertas, municípios atendidos, períodos de retirada e publicação explícita.
- Semear o catálogo global de municípios do IBGE e o catálogo global de tipos de resíduo,
  ambos somente leitura na v1.
- Permitir que o Administrador habilite tipos de resíduo, com indicação de retirada domiciliar,
  e gerencie pontos de coleta com endereço, coordenadas, horários por dia e tipos aceitos.
- Publicar a consulta anônima de pontos por município, tipo, aberto agora e proximidade, com
  lista equivalente ao mapa e sem dados pessoais.
- Aplicar exclusão lógica de pontos e bloquear a retirada de município ou tipo ainda usado por
  ponto, preservando histórico e volumes.

## Capabilities

### New Capabilities

- `perfil-da-organizacao`: perfil público, fuso, limite de solicitações abertas, municípios
  atendidos, períodos de retirada e publicação/despublicação.
- `catalogo-e-tipos`: catálogo global de municípios e tipos de resíduo e tipos habilitados por
  organização.
- `pontos-de-coleta`: cadastro, edição, status e retirada lógica de pontos.
- `consulta-publica`: consulta anônima de pontos ativos de organizações publicadas, com filtros,
  abertura agora e proximidade.

### Modified Capabilities

- `isolamento-de-organizacoes`: as novas entidades da organização entram nos filtros globais de
  leitura e na guarda de escrita já existentes.

## Impact

- **Código:** novas entidades e configurações em `app/backend/src`, handlers e validadores em
  `Application`, endpoints em `Api`, telas de configuração e consulta em `app/frontend`.
- **API:** `/api/v1/organizacao`, `/api/v1/organizacao/publicacao`, `/api/v1/organizacao/municipios`,
  `/api/v1/organizacao/periodos`, `/api/v1/organizacao/tipos`, `/api/v1/catalogo/*`,
  `/api/v1/pontos`, `/api/v1/pontos/opcoes` e `/api/v1/publico/*`.
- **Banco:** tabelas `Municipio` (global, semeada por migração), `TipoResiduo` (global, semeada),
  `OrganizacaoMunicipio`, `OrganizacaoTipo`, `ConfiguracaoDePeriodo`, `PontoColeta`,
  `PontoHorario` e `PontoTipo`, com índice espacial em `PontoColeta.Localizacao`.
- **Dependências:** `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` para `geography`.
- **Dados públicos:** catálogo de municípios derivado da base oficial do IBGE; a cópia versionada
  em `Migrations/Dados/municipios-ibge.csv` foi gerada a partir da API de localidades do IBGE por
  espelho público, porque a API oficial não estava acessível no ambiente de implementação.
- **Compatibilidade:** a migração é aditiva e não altera dados da F1.
- **Rastreabilidade:** RF-06–RF-09, RF-22; RN-06, RN-08, RN-09, RN-12; RNF-02, RNF-04;
  DT-02, DT-06, DT-08, DT-11, DT-13, DT-15, DT-19; CA-02–CA-05.
