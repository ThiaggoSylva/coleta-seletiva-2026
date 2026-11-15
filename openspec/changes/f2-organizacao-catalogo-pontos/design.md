# Design

## Contexto

A F1 fixou Clean Architecture, multitenancy por `OrganizacaoId`, filtros globais de leitura,
guarda de escrita e autorização por policies. A F2 acrescenta cadastros da organização e a
primeira consulta anônima; nenhuma decisão estrutural nova é necessária.

## Modelo de dados

| Entidade | Campos e relações |
| --- | --- |
| `Municipio` | Global: `Id` = código IBGE (inteiro), nome, UF. Semeada por migração. |
| `TipoResiduo` | Global: código estável, nome, categoria, unidade fixa, orientações. Semeada por migração. |
| `Organizacao` | Acrescenta contato público, fuso IANA, limite de solicitações abertas e mantém `Publicada`. |
| `OrganizacaoMunicipio` | `(OrganizacaoId, MunicipioId)` único; municípios atendidos. |
| `OrganizacaoTipo` | `(OrganizacaoId, TipoResiduoId)` único e `AceitaRetiradaDomiciliar`. |
| `ConfiguracaoDePeriodo` | `(OrganizacaoId, Periodo)` único, com hora inicial e final locais. |
| `PontoColeta` | Nome único por organização, município atendido, endereço, `Point` SRID 4326, status e exclusão lógica. |
| `PontoHorario` | Dia da semana com hora inicial e final no mesmo dia. |
| `PontoTipo` | Tipos aceitos, subconjunto não vazio dos tipos habilitados. |

`PontoColeta.OrganizacaoId` e as FKs compostas usam chaves alternativas em
`OrganizacaoMunicipio`, `OrganizacaoTipo` e `PontoColeta`, no mesmo padrão da F1. As entidades
filhas implementam `IEntidadeDaOrganizacao` e entram nos filtros globais e na guarda de escrita.

## Regras e validações

- Publicar exige ao menos um município e um tipo habilitado (RN-09); despublicar não altera
  solicitações nem apaga dados (RN-12).
- Ponto exige coordenadas válidas, ao menos um horário e um tipo, município atendido, tipos
  habilitados, nome único por organização e horários com fim maior que o início no mesmo dia
  (RF-08, RN-08).
- Aberto agora: status `Ativo` e instante atual no fuso da organização dentro de um intervalo,
  início inclusivo e fim exclusivo (RN-08).
- Remover município atendido ou desabilitar tipo é bloqueado enquanto houver ponto usando o
  vínculo; a resposta orienta ajustar os pontos antes.
- Exclusão de ponto é lógica e preserva histórico; o nome permanece reservado.

## Contratos

- `GET/PUT /api/v1/organizacao`, `PUT /api/v1/organizacao/publicacao`,
  `PUT /api/v1/organizacao/municipios`, `PUT /api/v1/organizacao/periodos`.
- `GET /api/v1/catalogo/tipos`, `GET /api/v1/catalogo/municipios?uf=&texto=`.
- `GET/PUT /api/v1/organizacao/tipos`.
- `/api/v1/pontos` (Administrador) e `GET /api/v1/pontos/opcoes` (Administrador e Operador).
- `POST /api/v1/publico/pontos/busca` e `GET /api/v1/publico/municipios`, anônimos, com limite
  de taxa e DTOs sem dados pessoais; coordenadas só no corpo, nunca em URL ou log.

## Persistência e concorrência

- `Municipio` e `TipoResiduo` são somente leitura na aplicação e semeados por migração.
- `PontoColeta` tem `rowversion`; a edição envia `If-Match` e a divergência gera 412.
- A troca de horários e tipos de um ponto acontece na mesma transação da edição.
- A busca por proximidade usa `geography` com índice espacial e limite de resultados.

## Testes

- Domínio: abertura por horário e fuso, horários inválidos, publicação sem pré-requisitos,
  limites de coordenadas e regras de exclusão lógica.
- Aplicação: validação de entradas, autorização por papel, conflitos de nome e de vínculo,
  auditoria na mesma transação.
- Integração: isolamento A/B nas novas rotas, publicação/despublicação, consulta pública sem
  PII e filtros de abertura e proximidade (depende de SQL Server com Docker).

## Riscos

- A base de municípios é uma cópia versionada; atualizações do IBGE exigem nova migração.
- A busca por proximidade depende do provider espacial; o teste automatizado exige SQL Server.
- Alterar o fuso da organização não reescreve registros históricos.
