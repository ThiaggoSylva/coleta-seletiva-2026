---
description: Gerar um documento técnico de design (TDD) com decisões arquiteturais validadas
---

Use a skill `generate-tdd` para transformar o contexto abaixo em um documento técnico de design detalhado. Carregue a skill pelo ID e siga seu fluxo integralmente; faça perguntas sobre decisões técnicas antes de finalizar e não implemente a solução.

Conduza a entrevista e obtenha esclarecimentos, escolhas, confirmações e aprovações **exclusivamente pelo sistema de questões do harness (ferramenta `question`)**, nunca por perguntas no texto de mensagens ou da resposta final. Apresente recomendações e trade-offs nas questões antes de pedir uma decisão. Se a ferramenta estiver indisponível, informe o bloqueio e interrompa o fluxo sem substituí-la por perguntas em texto nem presumir respostas.

Este comando é a segunda etapa do fluxo **PRD → TDD**. Leia primeiro o PRD indicado no contexto ou, por padrão, `docs/prd.md`, e verifique sua validação antes de desenhar a solução. Reutilize decisões e IDs dos requisitos, sem repetir a entrevista de produto. Se o PRD estiver ausente, não validado ou tiver pendências bloqueantes, siga o tratamento de handoff da skill antes de avançar.

Gere o artefato em **`docs/tdd.md`**. Use `docs/tdd-<slug>.md` somente se o usuário pedir explicitamente um slug para o TDD; não derive nem herde automaticamente um slug do produto, dos argumentos ou do PRD de entrada. Outro caminho também exige pedido explícito. A existência do arquivo padrão não autoriza criar uma variante: confirme sua atualização pela ferramenta `question`.

Contexto inicial: $ARGUMENTS
