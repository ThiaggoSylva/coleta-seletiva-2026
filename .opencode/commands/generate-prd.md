---
description: Gerar um PRD completo a partir de uma ideia inicial
---

Use a skill `generate-prd` para transformar o prompt abaixo em um documento de requisitos de produto (PRD). Carregue a skill pelo ID e siga seu fluxo integralmente; não implemente o produto.

Conduza a entrevista e obtenha esclarecimentos, escolhas, confirmações e aprovações **exclusivamente pelo sistema de questões do harness (ferramenta `question`)**, nunca por perguntas no texto de mensagens ou da resposta final. Se a ferramenta estiver indisponível, informe o bloqueio e interrompa o fluxo sem substituí-la por perguntas em texto nem presumir respostas.

Este comando é a primeira etapa do fluxo **PRD → TDD**. Prepare o handoff conforme a skill: registre a validação explícita do PRD e entregue o caminho, escopo, requisitos e pendências para `/generate-tdd`. Não inicie o TDD automaticamente nem trate a geração do arquivo como aprovação.

Gere o artefato em **`docs/prd.md`**. Use `docs/prd-<slug>.md` somente se o usuário pedir explicitamente um slug; não derive um slug do nome do produto, da funcionalidade ou dos argumentos. Outro caminho também exige pedido explícito. A existência do arquivo padrão não autoriza criar uma variante: confirme sua atualização pela ferramenta `question`.

Prompt inicial: $ARGUMENTS
