# Proposal

## Why

A consulta pública de pontos de coleta só mostra o que foi cadastrado manualmente pelas
organizações. Em produção isso pode resultar em uma lista vazia ou incompleta, mesmo existindo
pontos reais de entrega voluntária no município. Sem dados reais, o cidadão não encontra onde
descartar e a plataforma perde utilidade prática.

## What Changes

- Ler pontos de coleta reais do OpenStreetMap (`amenity=recycling`) na consulta pública anônima,
  usando o Overpass API, sem chave e com licença ODbL.
- Resolver o município selecionado em caixa geográfica pelo Nominatim, mantendo também a busca
  por proximidade já existente.
- Mesclar os pontos externos aos pontos cadastrados antes da paginação, com identificação visual
  de fonte externa, atribuição e link para o registro original.
- Deduplicar pontos externos que já existem no cadastro da plataforma (mesmo nome e até 120 m).
- Aplicar cache curto, timeout e degradação: se a fonte pública falhar, a consulta continua
  apenas com os pontos cadastrados e exibe um aviso.
- Não persistir, editar nem usar pontos externos em retiradas, volumes ou relatórios.

## Capabilities

### New Capabilities

- `pontos-de-fontes-publicas`: leitura, mapeamento de tags, mescla, deduplicação, atribuição e
  degradação de pontos de coleta vindos de fontes públicas externas.

### Modified Capabilities

- `consulta-publica`: o contrato público passa a incluir `origem`, `atribuicao`, `urlDaFonte`,
  `avisoDeFonteExterna` e `atribuicaoDeFonteExterna`, além de endereço e horário opcionais para
  pontos externos.

## Impact

- **Código:** porta `IFontePublicaDePontos` e mapeador na `Application`; adaptador Overpass/Nominatim
  com cache na `Infrastructure`; mescla em `ConsultaPublicaDePontos`; configuração em
  `StartupConfiguration`; tela de busca em `app/frontend`.
- **API:** `POST /api/v1/publico/pontos/busca` mantém a rota e amplia o JSON de resposta e o de
  `PaginaDePontosPublicos`. Nenhuma rota autenticada muda.
- **Banco:** nenhuma tabela, coluna ou migration nova. Pontos externos não são persistidos.
- **Dependências:** nenhuma biblioteca nova; `HttpClient` do framework e cache em memória próprio.
- **Configuração:** seção `FontesPublicas:Pontos` em `appsettings.json`, com `Ativa`,
  `UrlDoOverpass`, `UrlDoNominatim`, `EmailDeContato`, `TimeoutSegundos`, `MinutosDeCache`,
  `MinutosDeCacheDoMunicipio` e `MaximoDePontos`. Pode ser sobrescrita por variáveis de ambiente
  (`FontesPublicas__Pontos__Ativa`, por exemplo).
- **Dados públicos e licença:** dados © OpenStreetMap contributors, ODbL; a atribuição aparece na
  interface e no JSON. Política de uso dos endpoints públicos é pendência registrada.
- **Compatibilidade:** a mudança é aditiva. Com `Ativa=false`, o comportamento é idêntico ao
  anterior.
- **Rastreabilidade:** `docs/prd.md:RF-23` (revisão de 2026-11-16); RNF-02, RNF-04; RF-09;
  `docs/tdd.md:DT-35`; F11 do roadmap; RNF-02 pelo aviso e ausência de PII.
