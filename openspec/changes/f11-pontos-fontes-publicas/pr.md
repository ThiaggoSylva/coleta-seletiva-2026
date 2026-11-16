## Resumo

A consulta pública deixa de depender apenas dos pontos cadastrados no banco: passa a ler pontos reais de coleta do OpenStreetMap (`amenity=recycling`) via Overpass e a mesclá-los aos pontos das organizações, sempre identificados como fonte externa e com atribuição ODbL. A mescla ocorre antes da ordenação e da paginação, com deduplicação por nome e proximidade. Nenhum ponto externo é persistido, editável ou usado em retiradas e volumes.

Fora do escopo: importar pontos externos para o banco, adaptadores de datasets municipais e uso de pontos externos em retiradas, volumes ou relatórios.

## Rastreabilidade

- PRD (`docs/prd.md`): RF-23 (revisão de 2026-11-16), RF-09, RNF-02, RNF-04.
- TDD (`docs/tdd.md`): DT-35 e fase F11; DT-11 e DT-13.
- SDD: `openspec/changes/f11-pontos-fontes-publicas` (proposal, design, specs e tarefas 1.1–5.3; a tarefa 5.2 está bloqueada por ausência de Docker).

## Validação

- [x] Implementação consistente com PRD, TDD e especificações aplicáveis.
- [x] Testes e verificações aplicáveis executados (descritos abaixo).
- [x] Documentação e especificações atualizadas, se necessário.

**Testes executados e resultados:**

- `dotnet build ColetaSeletiva.slnx`: sucesso, 0 avisos e 0 erros.
- `dotnet test` em `ColetaSeletiva.Application.Tests`: 55/55 aprovados, incluindo os 8 novos testes de tags, normalização, distância, deduplicação e fonte desativada.
- `dotnet test` filtrado em `FontePublicaOverpassTests`: 6/6 aprovados com `HttpMessageHandler` simulado (mapeamento, filtro de tipo, cache, falha e busca por raio).
- `dotnet format --verify-no-changes`: sem alterações pendentes.
- `npm run build` no frontend: sucesso.
- `npm test` no frontend: 68/68 aprovados.
- Consulta real ao Overpass com a query gerada pelo adaptador: 17 elementos reais no Recife.
- Não executados: testes de integração com SQL Server (sem Docker neste ambiente) e inspeção visual da tela.

## Riscos e pendências

- Dependência de endpoints públicos do Overpass e do Nominatim: mitigada com cache de 10 minutos, timeout de 8 s, aviso na interface e degradação para apenas os pontos cadastrados; a política de uso precisa de revisão antes de uso intenso.
- Cobertura e qualidade dos dados variam por município; a interface identifica a origem externa e oferece link para o registro original.
- Sem migration: rollback por configuração (`FontesPublicas__Pontos__Ativa=false`) ou revert do PR.
- Pendente: executar os testes de integração com SQL Server em ambiente com Docker e revisar a interface em execução.
