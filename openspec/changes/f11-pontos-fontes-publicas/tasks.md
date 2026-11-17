# Tasks

## 1. Contratos e configuração

- [x] 1.1 Criar `IFontePublicaDePontos`, `ConsultaDeFontePublica`, `ResultadoDaFontePublica`, `OpcoesDaFontePublica` e `FontePublicaDesativada` na Application (RF-23; DT-35).
- [x] 1.2 Ampliar `PontoPublico` e `PaginaDePontosPublicos` com origem, atribuição, link e aviso, mantendo endereço e horário opcionais (RF-23, RF-09; DT-13, DT-35).
- [x] 1.3 Ler a seção `FontesPublicas:Pontos` em `StartupConfiguration` e registrar o adaptador na Infrastructure (DT-35).

## 2. Leitura da fonte pública

- [x] 2.1 Mapear tags `recycling:*` para os códigos do catálogo, derivar nome e normalizar texto para deduplicação (RF-23; DT-35).
- [x] 2.2 Implementar o adaptador Overpass com resolução de município pelo Nominatim, limite de pontos e interpretação de `node`/`way`/`relation` com `center` (RF-23; DT-35).
- [x] 2.3 Adicionar cache em memória, timeout, User-Agent e degradação com aviso quando a fonte falha (RNF-02; DT-35).

## 3. Mescla na consulta pública

- [x] 3.1 Consultar a fonte apenas com município ou coordenadas/raio e nunca quando `abertoAgora=true` (RF-23; DT-35).
- [x] 3.2 Traduzir o filtro de tipo de resíduo para o código do catálogo e aplicá-lo na fonte externa (RF-23).
- [x] 3.3 Mesclar os externos antes da ordenação e paginação, deduplicando por nome e proximidade (RF-23; DT-11).
- [x] 3.4 Expor `AvisoDeFonteExterna` e `AtribuicaoDeFonteExterna` sem incluir dados pessoais (RF-23; RNF-02).

## 4. Frontend

- [x] 4.1 Atualizar o contrato de `PontoPublico` no serviço de consulta pública (RF-23).
- [x] 4.2 Exibir selo de fonte externa, link para o OpenStreetMap, atribuição, aviso de indisponibilidade e estados de endereço/horário ausentes (RF-23; RNF-04).

## 5. Verificação

- [x] 5.1 Cobrir mapeamento de tags, normalização, distância, deduplicação e fonte desativada com testes MSTest (DT-17; DT-35).
- [x] 5.2 Cobrir o adaptador com `HttpMessageHandler` simulado e um teste de integração da mescla com SQL Server (DT-17; DT-35). Os testes foram escritos; a execução dos de integração segue bloqueada neste ambiente por ausência de Docker.
- [x] 5.3 Executar `dotnet build`, testes unitários da Application e `npm run build`; registrar bloqueios (DT-17).

## Workflow follow-up

- Branch `feat/pontos-fontes-publicas` criada a partir do estado atual da `main` local.
- Abrir o PR somente com autorização explícita, citando RF-23, DT-35, F11 e estas tarefas.
