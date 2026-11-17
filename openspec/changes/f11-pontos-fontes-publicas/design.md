# Design

## Contexto

A consulta pública (`POST /api/v1/publico/pontos/busca`) hoje percorre
`ConsultaPublicaDePontos`, filtra pontos ativos de organizações publicadas, projeta em
`PontoPublico`, ordena e pagina em memória após carregar até 500 candidatos. A mudança precisa
inserir uma segunda fonte sem quebrar esse fluxo, sem persistir nada e sem transformar
indisponibilidade externa em erro para o cidadão.

## Decisão

Seguir `docs/tdd.md:DT-35`.

- **Porta na Application:** `IFontePublicaDePontos` com `ConsultaDeFontePublica` (município/UF ou
  latitude/longitude/raio, códigos de tipo e limite) e `ResultadoDaFontePublica` (pontos,
  `Consultada`, `Disponivel`, `Aviso`). `FontePublicaDesativada` é a implementação usada quando
  `FontesPublicas:Pontos:Ativa=false`.
- **Adaptador na Infrastructure:** `OverpassFontePublicaDePontos` resolve o recorte com Nominatim
  (somente para município) e consulta `nwr["amenity"="recycling"]` no Overpass. Interpreta tags,
  mapeia para os códigos do catálogo e devolve `PontoDeFontePublica`. Nada é gravado.
- **Mescla na consulta:** `ConsultaPublicaDePontos` chama a porta apenas quando existe recorte
  geográfico e o filtro `abertoAgora` não está ativo. Os externos entram na mesma lista, antes de
  ordenar e paginar, garantindo `total` e páginas consistentes.
- **Deduplicação:** mesmo nome normalizado e até 120 m de um ponto cadastrado ⇒ o ponto cadastrado
  prevalece e o externo é descartado.
- **Contrato:** `PontoPublico` ganha `Origem`, `Atribuicao` e `UrlDaFonte`; endereço, município e
  `AbertoAgora` passam a ser opcionais (externos não têm CEP nem horário confiável). A página traz
  `AvisoDeFonteExterna` e `AtribuicaoDeFonteExterna`.
- **Cache e resiliência:** cache em memória no adaptador (10 min para pontos, 1 dia para a caixa do
  município), timeout de 8 s, `HttpClient` único com User-Agent identificável. Falha vira
  `Disponivel=false` + aviso, nunca exceção para o usuário.
- **Filtros:** tipo de resíduo é traduzido para o código do catálogo e aplicado no adaptador;
  `abertoAgora=true` considera somente pontos cadastrados, pois horário de fonte externa não é
  confiável.
- **Licença:** atribuição ODbL visível na interface, no JSON e no link para o registro original.

## Alternativas descartadas

- **Importar pontos externos para o banco:** voltaria a depender de dados criados no banco e
  exigiria sincronização, além de misturar dados de terceiros ao isolamento por organização.
- **Somente fallback quando não há pontos cadastrados:** esconde pontos reais quando a organização
  cadastrou poucos pontos.
- **Dataset municipal (ex.: Recife):** cobertura limitada a municípios que publicam dados.

## Riscos

- Limite e indisponibilidade dos endpoints públicos do Overpass/Nominatim: mitigados por cache,
  timeout, uma consulta por recorte e aviso na interface. Política de uso precisa de revisão antes
  de uso intenso.
- Cobertura e qualidade variam por cidade; a interface deixa claro que a fonte é externa.
- Duplicidade residual quando o nome no OpenStreetMap difere do cadastro; aceitável na v1.
