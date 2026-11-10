# Coleta Seletiva 2026

Plataforma web de coleta seletiva e pontos de descarte, multi-tenant (um tenant por cooperativa, associação ou prefeitura). Projeto de TCC da Academia do Programador, preparado para ser desenvolvido com o **opencode** no fluxo **PRD → TDD → SDD → Pull Request**.

Esta pasta contém apenas documentação e ferramentas de governança. O código da aplicação será criado em `app/`.

## Estrutura

```text
.
├── AGENTS.md                       # regras do fluxo, commits e PRs (lido pelo opencode)
├── .github/pull_request_template.md
├── .opencode/
│   ├── commands/                   # comandos: /generate-prd, /generate-tdd, /opsx-*
│   └── skills/                     # skills: generate-prd, generate-tdd, openspec-*
├── docs/
│   ├── prd.md                      # requisitos de produto (Rascunho)
│   ├── tdd.md                      # design técnico (Rascunho, decisões propostas)
│   └── prd-desafios.md             # evoluções v1.1 e v2 (Rascunho)
└── openspec/
    ├── config.yaml                 # contexto e regras do projeto para o OpenSpec
    ├── specs/
    └── changes/archive/
```

## Estado atual

- `docs/prd.md` está **validado para TDD** (2026-11-10); `docs/tdd.md` é um rascunho com decisões propostas que precisa ser validado pela skill.
- O handoff PRD → TDD está liberado; a próxima etapa é `/generate-tdd Use docs/prd.md como base validada`.
- Nenhum código foi criado e nenhum PR foi aberto.

## Passo a passo

1. **Preparar o repositório.** Crie o repositório no GitHub, copie esta pasta para ele e faça o commit inicial na `main`. A partir daí, todo trabalho acontece em feature branches.
2. **Abrir o opencode na raiz.** Ele lê o `AGENTS.md` automaticamente. O fluxo depende da ferramenta de perguntas (`question`) do opencode; se ela estiver indisponível, os comandos interrompem o fluxo em vez de perguntar no texto.
3. **Validar o PRD.**

   ```text
   /generate-prd Revisar e validar docs/prd.md: plataforma de coleta seletiva e pontos de descarte, multi-tenant, com pontos de descarte, retirada agendada e registro de volume coletado
   ```

   Como o arquivo já existe, a skill pede confirmação antes de atualizá-lo, conduz a entrevista pelas perguntas e, ao final, pede sua aprovação para marcar `Validado para TDD`.
4. **Validar o TDD.**

   ```text
   /generate-tdd Use docs/prd.md como base validada
   ```

   A skill entrevista cada decisão técnica (`DT-*`). O rascunho já traz uma proposta para cada uma, então a conversa serve para confirmar ou trocar.
5. **Instalar a CLI do OpenSpec** conforme a documentação oficial do OpenSpec. As skills `openspec-*` e os comandos `/opsx-*` dependem dela.
6. **Especificar e implementar por fase.** Siga o roadmap do TDD (F0 a F6), uma fase por vez, em uma feature branch:

   ```text
   /opsx-explore             # pensar e esclarecer antes de propor
   /opsx-propose f1-fundacao-segura
   /opsx-update              # ajustar as specs depois de revisar
   /opsx-apply               # implementar as tarefas aprovadas, dentro de app/
   /opsx-archive             # arquivar a mudança concluída
   ```

7. **Abrir o PR** com o template de `.github/pull_request_template.md`, citando os IDs de `RF-*`, `RNF-*`, `DT-*` e a fase do roadmap.

## Comandos disponíveis

| Comando | Para que serve |
| --- | --- |
| `/generate-prd` | Gera ou valida `docs/prd.md` por entrevista |
| `/generate-tdd` | Gera ou valida `docs/tdd.md` com decisões técnicas entrevistadas |
| `/opsx-explore` | Modo de exploração: investigar e clarear requisitos sem implementar |
| `/opsx-propose` | Cria uma mudança com proposta, specs, design e tarefas |
| `/opsx-update` | Revisa os artefatos de uma mudança sem editar código |
| `/opsx-apply` | Implementa as tarefas de uma mudança |
| `/opsx-sync` | Sincroniza as specs da mudança com as specs principais |
| `/opsx-archive` | Arquiva uma mudança concluída |

## O que vem do modelo e o que foi adaptado

- **Copiados sem alteração** do modelo `locadora-de-veiculos-2026`: comandos e skills de `.opencode/` e o template de PR.
- **Adaptados:** `AGENTS.md` (contexto do projeto e exemplos de título de PR), `openspec/config.yaml` (contexto e regras do domínio).
- **Escritos para o tema:** `docs/prd.md` e `docs/tdd.md`, com a mesma estrutura do modelo.
- **Evoluções:** `docs/prd-desafios.md` detalha as versões v1.1 e v2 (seção 5.2 do PRD), também como rascunho. Valide-o somente depois da v1.
