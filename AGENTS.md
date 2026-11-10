# Instruções para agentes

## Contexto do projeto

- **Produto:** plataforma web de coleta seletiva e pontos de descarte, multi-tenant (um tenant por cooperativa, associação ou prefeitura). Documentos de referência: `docs/prd.md` (o quê e por quê) e `docs/tdd.md` (como).
- **Idioma:** artefatos, mensagens de commit, títulos e descrições de PR em português do Brasil.
- **Domínio sensível:** dados pessoais de cidadãos (contato e endereço de retirada) e isolamento entre organizações. Nunca registrar esses dados em logs, mensagens de erro ou exemplos de teste com dados reais.

## Geral

- Sempre utilizar o sistema de questões do ambiente (harness) ao invés de fazer perguntas na resposta.
- Criar as implementações da aplicação na subpasta `app/`, nunca diretamente na raiz do projeto. Manter o código, os testes e as configurações específicas da aplicação dentro de `app/`; a raiz permanece destinada à documentação e aos arquivos de governança e ferramentas do repositório.

## Fluxo de trabalho

Para novas funcionalidades e alterações de comportamento, executar o fluxo completo em uma **feature branch** criada a partir da `main`, seguindo a ordem **PRD → TDD → SDD → Pull Request**, sem pular etapas nem trabalhar diretamente na `main`:

1. **PRD (Product Requirements Document):** definir e validar problema, escopo, requisitos e critérios de aceite em `docs/prd.md` antes do desenho técnico.
2. **TDD (Technical Design Document):** registrar em `docs/tdd.md` as decisões de arquitetura, contratos, dados, segurança, testes e o roadmap de implementação, alinhados ao PRD, antes de especificar a mudança.
3. **SDD (Spec-Driven Development):** detalhar a mudança em especificações e tarefas rastreáveis ao PRD e ao TDD; revisar e validar essas especificações antes de implementar. Implementar e testar seguindo as tarefas aprovadas, mantendo documentação e especificações coerentes com eventuais decisões posteriores.
4. **Pull Request:** somente após implementação e verificações aplicáveis, abrir PR da feature branch para a `main` com o template de `.github/pull_request_template.md`, relacionando requisitos, decisões e especificações, descrevendo testes executados e pendências. Integrar na `main` por meio do PR, após revisão, aprovações e verificações exigidas; não fazer merge direto nem contornar proteções da branch.

Em mudanças com documentos existentes, reutilizar e atualizar os artefatos pertinentes em vez de recriá-los. Se uma etapa não se aplicar (por exemplo, correção puramente editorial), registrar a justificativa no PR; não tratar pendências de validação ou aprovação como concluídas.

## Ferramentas necessárias ao workflow

Verificar a disponibilidade das ferramentas antes da etapa que depende delas. Esta lista define requisitos de execução, não afirma que as ferramentas já estejam instaladas ou configuradas. Não instalar dependências, configurar serviços ou obter credenciais sem autorização.

| Ferramenta ou capacidade | Uso no fluxo | Requisito |
| --- | --- | --- |
| Sistema de questões do harness (`question`) | Entrevistas, esclarecimentos, escolhas, confirmações de edição e aprovação dos artefatos | Obrigatório em todas as etapas que exigirem resposta do usuário. Se indisponível, informar o bloqueio e parar; nunca substituir por perguntas no texto do LLM nem presumir aprovação. |
| Skills `generate-prd` e `generate-tdd`, carregadas pela ferramenta `skill` | Executar os fluxos dos comandos `/generate-prd` e `/generate-tdd`, incluindo a validação e o handoff PRD → TDD | Obrigatórias nas respectivas etapas; as definições locais estão em `.opencode/skills/` e os comandos em `.opencode/commands/`. |
| Leitura, busca e edição de arquivos (`read`, `glob`, `grep` e `patch`, ou equivalentes do ambiente) | Consultar fontes e instruções; manter PRD, TDD, especificações, código e template de PR | Obrigatórias. Preferir ferramentas dedicadas para leitura, busca e edição. |
| Terminal do ambiente (`shell` ou equivalente) e Git | Inspecionar o repositório, criar a feature branch a partir da `main`, executar verificações e produzir commits pequenos e verificáveis | Obrigatórios para branch, implementação e entrega. Confirmar o estado do repositório e preservar alterações existentes antes de operar. |
| Ferramenta de SDD adotada pelo projeto | Criar, revisar e validar especificações e tarefas rastreáveis antes da implementação | Obrigatória na etapa SDD. Se OpenSpec for adotado, verificar a disponibilidade de sua CLI e as instruções/configuração aplicáveis; a existência de `.openspec/` sozinha não comprova configuração. Se não houver ferramenta definida, esclarecer pela `question` antes de avançar. |
| Documentação oficial atualizada (Context7 e/ou acesso às fontes oficiais) | Verificar APIs, versões, compatibilidade e configuração das tecnologias escolhidas | Necessária quando decisões ou implementação dependerem desses detalhes. Não inventar suporte ou compatibilidade sem verificação. |
| Runtime, gerenciador de dependências e ferramentas de qualidade definidos no TDD | Executar testes, lint, formatação, análise de tipos, build e demais verificações aplicáveis | Necessários conforme a stack e os critérios de aceite; não presumir uma stack antes da decisão técnica. |
| Integração GitHub via MCP ou GitHub CLI (`gh`), autenticada e com as permissões necessárias | Consultar o template e o contexto da entrega, abrir o PR para `main` e acompanhar revisão e verificações | Necessária para a etapa de Pull Request. Não declarar PR aberto, aprovado ou integrado sem confirmação da plataforma; respeitar as proteções da branch. |

Se uma ferramenta obrigatória estiver ausente ou sem acesso, registrar a etapa bloqueada e o que falta, sem simular execução, aprovação ou verificação. Usar uma alternativa equivalente somente quando ela preservar as validações e as restrições do fluxo; a interação com o usuário continua obrigatoriamente no sistema de questões do harness.

## Commits

- Dividir as mudanças em commits pequenos, atômicos e focados, agrupando arquivos por unidade lógica ou tarefa. Evitar misturar documentação, refatorações e funcionalidades sem relação.
- Cada commit deve ser verificável e ter uma mensagem descritiva. Não fazer commit de trabalho incompleto que não passe nas verificações necessárias.

## Padrão de Pull Requests

### Escopo e preparação

- Cada PR deve representar uma mudança coesa, com objetivo claro e tamanho adequado à revisão. Separar mudanças sem relação; não incluir refatorações oportunistas, arquivos gerados desnecessários ou alterações de configuração alheias ao objetivo.
- Usar a feature branch da mudança como origem e `main` como destino. Antes de abrir, conferir branch, commits e diff completo em relação à base, preservando trabalho de terceiros e verificando se não há segredos, credenciais ou dados pessoais indevidos.
- Concluir as etapas aplicáveis de PRD → TDD → SDD, a implementação e as verificações antes de abrir o PR. Não usar um PR como substituto das validações prévias dos artefatos. Para mudanças puramente documentais/editoriais, justificar explicitamente as etapas não aplicáveis, sem inventar aprovações.
- Consultar os PRs existentes da branch para evitar duplicidade. Se já houver um PR para a mesma mudança, atualizar seu conteúdo e sua descrição em vez de abrir outro.

### Título

- Usar o formato **`<tipo>(<escopo opcional>): <resumo>`**, com resumo objetivo em português, sem ponto final. Omitir os parênteses quando não houver um escopo útil.
- Tipos: `feat` para funcionalidade; `fix` para correção; `docs` para documentação; `refactor` para reorganização sem mudança de comportamento; `test` para testes; `ci` para integração contínua; `build` para build/dependências; `chore` para manutenção que não se enquadre nos anteriores.
- Escolher o tipo pelo objetivo principal do PR, não pela quantidade de arquivos de cada categoria. Identificar mudanças incompatíveis na descrição com seus impactos e a estratégia de migração.
- Exemplos: `feat(retiradas): adicionar agendamento de retirada domiciliar`, `fix(capacidade): impedir excesso de retiradas na mesma janela`, `docs(workflow): detalhar o handoff entre PRD e TDD`.

### Descrição obrigatória

Ler e usar **`.github/pull_request_template.md`** a cada abertura, preservando suas seções e preenchendo-as com informações específicas. Não deixar comentários de orientação como conteúdo final nem remover seções para ocultar pendências. Complementar o template com detalhes pertinentes, sem alterá-lo como parte de outro trabalho sem necessidade.

- **Resumo:** explicar problema, motivação, solução e mudanças observáveis; delimitar o que não foi incluído e indicar impactos de compatibilidade quando houver. Referenciar issues relacionadas, se existirem; usar vínculo de fechamento apenas quando a entrega realmente resolver a issue.
- **Rastreabilidade:** indicar caminhos/links reais e IDs dos requisitos do PRD (`RF-*`, `RNF-*`), decisões do TDD (`DT-*`) e fase do roadmap, além das especificações e tarefas SDD atendidas. Usar `docs/prd.md` e `docs/tdd.md` por padrão; se houver nomes com slug explicitamente autorizados, referenciar esses arquivos reais. Para cada artefato não aplicável, justificar individualmente. Não afirmar cobertura de requisito que não tenha sido implementado e verificado.
- **Validação:** preencher as caixas do template de acordo com evidências reais. Marcar como concluído somente o que tiver sido efetivamente verificado; itens não aplicáveis devem ter justificativa explícita. Informar comandos executados, cenários cobertos, resultados e limitações relevantes. Distinguir verificações locais das executadas em CI; para testes não executados, indicar motivo e impacto, sem apresentar ausência de execução como sucesso.
- **Riscos e pendências:** registrar impactos operacionais, de segurança e privacidade, mudanças de contrato ou dados, migrações, configuração e dependências novas. Descrever rollout e rollback quando aplicáveis e indicar pendências, responsáveis conhecidos e condições de resolução. Escrever `Nenhum` somente quando não houver riscos ou pendências identificados; não inventar responsáveis nem garantias de reversibilidade.
- Para mudanças de interface, incluir evidências visuais quando disponíveis e pertinentes, sem expor dados sensíveis. Para alterações de API ou configuração, documentar o uso e os impactos necessários à revisão e à operação.

### Revisão e atualização

- Após abrir, confirmar na plataforma a URL, a branch de origem e a base do PR. Informar ao usuário o link, as verificações realizadas e os bloqueios reais; se a criação falhar, declarar a falha, sem inventar número ou URL.
- Acompanhar checks e comentários de revisão, corrigir problemas pertinentes em commits pequenos e atualizar descrição, rastreabilidade e evidências quando o escopo mudar. Reexecutar as verificações afetadas pelas alterações; aprovação ou resultado de CI anterior não comprova a validade de commits posteriores.
- Resolver discussões apenas quando o ponto tiver sido efetivamente tratado ou houver acordo explícito. Solicitações de decisão ao usuário devem usar `question`, nunca perguntas na resposta textual do agente.
- Não declarar o PR pronto para integração enquanto houver falhas obrigatórias, conflitos, bloqueios de produto/arquitetura ou revisões impeditivas pendentes. Não declarar uma revisão independente ou aprovação inexistente.

### Integração e encerramento

- Integrar somente por meio do PR, após as revisões, aprovações e verificações exigidas, respeitando a autorização aplicável para a operação. Abrir ou atualizar o PR não autoriza automaticamente o agente a fazer merge.
- Respeitar o método de merge e as regras configuradas no repositório. Não contornar proteções, dispensar checks, usar privilégios administrativos para forçar a integração nem fazer push direto para `main`.
- Confirmar o estado final na plataforma antes de informar que o PR foi integrado. Se a integração não tiver ocorrido, reportar o estado real e as pendências. Não excluir branches nem descartar trabalho local sem autorização.
