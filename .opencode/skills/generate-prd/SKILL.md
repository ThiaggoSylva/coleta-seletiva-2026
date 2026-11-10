---
name: generate-prd
description: Gerar um documento de requisitos de produto (PRD) completo a partir de um prompt inicial. Use quando o usuário pedir um PRD, especificação de produto, ou invocar /generate-prd.
---

# Gerar PRD

Transforme a ideia inicial do usuário em um PRD em português, pronto para revisão por produto, design e engenharia. Este fluxo produz **somente documentação**: não escreva código, não inicie implementação e não crie uma mudança OpenSpec a menos que o usuário peça explicitamente.

## Entrada e descoberta

### Regra obrigatória de interação

- Toda solicitação de informação, esclarecimento, escolha, confirmação ou aprovação deve ser feita **exclusivamente pelo sistema de questões do harness, usando a ferramenta `question`**. Isso inclui identificar a ideia inicial, resolver ambiguidades e autorizar alterações em documentos existentes.
- Não faça perguntas no output textual do LLM: mensagens intermediárias e resposta final podem informar contexto, progresso, bloqueios e resultados, mas não substituir a ferramenta por listas de perguntas, pedidos de resposta em texto ou pedidos como “confirme para continuar”.
- Leia primeiro as fontes relevantes e aproveite respostas já fornecidas; não pergunte novamente o que estiver explicitamente confirmado. Agrupe lacunas relacionadas em rodadas curtas e objetivas. Quando oferecer opções, explique suas implicações e identifique recomendações como propostas, não como decisões aprovadas. Use seleção múltipla somente quando as escolhas puderem coexistir.
- Aguarde as respostas da ferramenta antes de avançar em decisões dependentes. Respostas parciais ou ambíguas exigem nova rodada pelo mesmo sistema; ausência de resposta não significa aprovação. Se a ferramenta estiver indisponível ou falhar, informe o bloqueio e interrompa o fluxo, sem recorrer a perguntas em texto nem finalizar o documento.

### Etapas

1. Use o prompt fornecido na conversa ou pelo comando `/generate-prd`. Se não houver uma ideia de produto identificável, pergunte o que o usuário quer criar antes de continuar.
2. Leia apenas o contexto relevante do projeto, incluindo **`docs/entrevista.md` quando existir e disser respeito ao produto**, além de `README.md`, requisitos existentes e documentação do domínio. Trate a entrevista como fonte de necessidades, não como especificação definitiva; identifique quem foi entrevistado, o que foi dito e o que ainda precisa de validação. Se o documento indicar que a entrevista é fictícia, registre essa limitação explicitamente no PRD. Não confunda comportamento atual com funcionalidade proposta. Não dependa de OpenSpec nem altere seus artefatos.
3. Faça uma **entrevista de esclarecimento com o usuário** antes de finalizar o artefato. Compare o prompt e as fontes com o escopo, as regras de negócio, os fluxos, os requisitos e os critérios de aceite pretendidos. Identifique especificações ausentes, conflitantes ou ambíguas que admitam interpretações diferentes; não escolha uma interpretação por conta própria. Use obrigatoriamente a ferramenta `question`, conforme a regra de interação acima. Faça rodadas adicionais se as respostas ainda deixarem especificações ambíguas. Em particular, esclareça regras de negócio citadas como pendentes na entrevista (por exemplo, cálculo de diárias e identificação do cliente) antes de descrevê-las como requisitos fechados.
4. Se o usuário não responder, aguarde ou interrompa o fluxo indicando a pendência, sem cobrar respostas em texto. Se o sistema de questões estiver indisponível, informe o bloqueio e pare; **não finalize nem entregue um PRD com especificações ambíguas disfarçadas de decisões**. Informações que não representem decisões de especificação e não possam ser verificadas (como métricas basais ou dados de mercado) podem ficar como `A definir`, com ação de validação. Não invente entrevistas, dados, obrigações legais, integrações ou decisões como se fossem fatos confirmados.
5. Gere o artefato em **`docs/prd.md`**, sem slug por padrão. Use `docs/prd-<slug>.md` somente se o usuário solicitar explicitamente um slug; se ele pedir slug sem informar qual, esclareça pela ferramenta `question`. Não deduza autorização a partir do nome do produto/funcionalidade, do tema ou dos argumentos e não ofereça variantes sem necessidade. Outro nome ou caminho também exige pedido explícito. Se o caminho já existir, leia-o e peça confirmação pela ferramenta `question` antes de sobrescrever ou mesclar; não substitua trabalho existente silenciosamente nem crie um arquivo com slug, data ou sufixo para evitar essa confirmação.

## Estrutura do documento

Use Markdown com títulos claros e detalhe proporcional ao produto. Todas as seções abaixo devem aparecer; quando uma informação não puder ser determinada, registre `A definir` e a decisão pendente, em vez de preencher com afirmações fictícias.

1. **Cabeçalho**: nome do produto/funcionalidade, status inicial `Rascunho`, data e origem do pedido; após aprovação explícita, registre `Validado para TDD`, data e origem da validação, sem inventar identidade do aprovador.
2. **Resumo executivo**: proposta de valor, problema e resultado pretendido.
3. **Contexto e problema**: situação atual, evidências disponíveis, dores e oportunidades; diferencie fatos observados de hipóteses.
4. **Objetivos e métricas de sucesso**: objetivos mensuráveis, indicadores, definição de medição e metas sugeridas quando justificáveis; identifique baselines e metas pendentes de validação.
5. **Público-alvo e stakeholders**: perfis, necessidades, responsáveis e interessados conhecidos ou a validar.
6. **Escopo**: funcionalidades incluídas no MVP, evoluções posteriores e itens fora de escopo; prioridades e dependências importantes.
7. **Jornadas e casos de uso**: fluxos principais, alternativas relevantes e estados de erro/ausência de dados, do ponto de vista dos usuários.
8. **Requisitos funcionais**: itens identificados (`RF-01`, etc.), priorizados, específicos e verificáveis; indique regras de negócio e dependências quando necessário.
9. **Requisitos não funcionais**: itens identificados (`RNF-01`, etc.) sobre acessibilidade, segurança/privacidade, desempenho, disponibilidade, observabilidade e demais atributos pertinentes; não invente números ou compromissos normativos sem base.
10. **Critérios de aceite**: critérios testáveis por funcionalidade ou caso de uso, associados aos IDs dos requisitos, incluindo cenários de falha relevantes.
11. **Dados e integrações**: entidades e informações necessárias, ciclo de vida, interfaces externas e restrições conhecidas; marque desconhecidos.
12. **Riscos, premissas e dependências**: impacto, mitigação ou ação de validação para cada ponto relevante.
13. **Questões em aberto e próximos passos**: decisões pendentes com responsável sugerido quando possível e forma de validá-las; inclua o handoff para TDD descrito abaixo.

## Handoff obrigatório: PRD → TDD

- O PRD define **o que e por quê**; o TDD define **como**. Registre restrições técnicas de produto já confirmadas, mas não escolha arquitetura, frameworks ou bibliotecas para preencher o trabalho da próxima etapa.
- Inclua no documento uma subseção **Handoff para TDD** com o caminho deste PRD, data/revisão quando disponível, escopo validado e fora de escopo, referências aos IDs de requisitos e critérios de aceite, regras de negócio e restrições confirmadas, além das pendências e dependências. Distinga pendências bloqueantes de não bloqueantes e indique quais decisões técnicas deverão ser entrevistadas no TDD, sem antecipar suas respostas.
- Indique **`docs/tdd.md`** como destino padrão da próxima etapa. Um slug autorizado apenas para o PRD não autoriza usá-lo no TDD; só indique `docs/tdd-<slug>.md` se o pedido explícito também abranger o TDD. Preserve no handoff o caminho real do PRD de entrada, independentemente do nome de saída do TDD.
- Após escrever o rascunho, disponibilize seu caminho e um resumo das decisões para revisão e solicite, pela ferramenta `question`, a aprovação explícita do conteúdo para seguir ao TDD ou a indicação de ajustes. Não confunda respostas da entrevista com aprovação do documento completo. Se houver ajustes, atualize o rascunho e repita a validação; essa rodada não autoriza alterações em arquivo preexistente sem a confirmação exigida na descoberta.
- Somente registre `Validado para TDD` quando houver aprovação explícita e nenhuma pendência de produto que impeça o desenho técnico. Sem aprovação, mantenha `Rascunho` e registre o handoff como bloqueado; não declare a etapa validada. Pendências não bloqueantes devem continuar visíveis.
- Na entrega, informe o estado do handoff e indique `/generate-tdd` com o caminho real do PRD como próximo passo, somente quando liberado. Por exemplo, para o caminho padrão: `/generate-tdd Use docs/prd.md como base validada`. Não execute o próximo comando, não carregue a skill de TDD e não comece o desenho técnico automaticamente. O handoff deve permitir continuar em outra sessão sem depender de memória da conversa.

## Revisão e entrega

- Verifique se o MVP é coerente com o problema, se os requisitos são rastreáveis aos critérios de aceite, se a entrevista foi considerada e se fatos, hipóteses e decisões pendentes estão distinguíveis. Antes de entregar, confirme que nenhuma especificação ambígua permaneceu sem esclarecimento pelo sistema de questões.
- Verifique que toda solicitação de resposta ou aprovação foi feita pela ferramenta `question`, inclusive a confirmação para sobrescrever ou mesclar um arquivo existente; não encerre a entrega com perguntas em texto.
- Escreva o documento no caminho escolhido e confirme sua existência. Responda com o link do arquivo e um resumo breve das principais decisões e pendências; não apresente o texto inteiro na resposta, salvo se solicitado.
