# Proposal

## Why

A v1 ainda não tem aplicação: o repositório contém apenas documentação e governança. Sem uma fundação de identidade, isolamento entre organizações e verificação automatizada, as fases seguintes acumulariam retrabalho de autenticação, tenancy e testes. A F1 estabelece essa base antes de catálogo, pontos, retiradas e relatórios.

## What Changes

- Criar a estrutura da aplicação em `app/backend`, `app/frontend`, `app/e2e`, `app/infra` e `app/scripts`, com solução .NET em Clean Architecture, SPA Angular standalone/zoneless, SQL Server 2022 Developer em contêiner e SPA/API na mesma origem via proxy local.
- Implementar identidade com ASP.NET Core Identity, papéis Administrador, Operador e Cidadão, JWT de acesso de 15 minutos, refresh opaco rotativo em cookie `HttpOnly`, logout com revogação da família e troca de senha.
- Permitir registro de organização com conta administradora, registro de cidadão, login por usuário ou e-mail para contas de organização, login por e-mail para cidadão e edição do próprio perfil.
- Permitir que o Administrador crie, edite e inative Operadores da própria organização, exigindo troca da senha inicial e impedindo autenticação de conta inativa.
- Aplicar isolamento por `OrganizacaoId` derivado do token, com filtros globais de leitura, guarda de escrita, FKs compostas e negação de acesso cruzado entre organizações e entre cidadãos.
- Registrar auditoria transacional de alterações críticas com organização, ator, momento, ação, entidade e correlação, sem dados pessoais desnecessários.
- Criar infraestrutura de testes com MSTest/Moq, `WebApplicationFactory` e Testcontainers para SQL Server real, Vitest e Playwright; configurar CI no GitHub Actions executando build, testes unitários, integração com SQL real e E2E.
- Verificar e fixar a matriz de versões oficiais e a configuração de segredos por ambiente como pré-requisito herdado da F0, sem versionar credenciais.

## Capabilities

### New Capabilities

- `acesso-e-contas`: registro de organização e cidadão, autenticação, sessão, refresh, logout, troca de senha, perfil do cidadão, papéis e proteção contra abuso nas rotas de acesso.
- `equipe-da-organizacao`: gestão de contas de Operador pelo Administrador, senha inicial, inativação e restrições de papel.
- `isolamento-de-organizacoes`: contexto de tenant, isolamento de leitura e escrita entre organizações e isolamento entre cidadãos, inclusive com identificadores conhecidos.
- `auditoria`: registro rastreável de alterações críticas, com ator, organização, momento e correlação, sem expor segredos ou dados pessoais.

### Modified Capabilities

Nenhuma. O projeto ainda não possui specs em `openspec/specs/`.

## Impact

- **Código novo:** `app/backend`, `app/frontend`, `app/e2e`, `app/infra`, `app/scripts` e `.github/workflows/`.
- **API:** rotas `/api/v1/auth/*`, `/api/v1/equipe` e `/api/v1/conta`, com ProblemDetails, validação e autorização por policy.
- **Banco:** tabelas do Identity, `Organizacao`, `Usuario` com `OrganizacaoId` e papel, `RefreshToken` e `Auditoria`; migration inicial e seed mínimo de desenvolvimento sem dados pessoais reais.
- **Dependências:** .NET LTS, Angular, EF Core, ASP.NET Core Identity, FluentResults, FluentValidation, Serilog, MSTest, Moq, Testcontainers, Vitest e Playwright; versões exatas verificadas e travadas na primeira tarefa.
- **Operação:** SQL Server 2022 Developer em contêiner, user-secrets no desenvolvimento, variáveis de ambiente na CI e nenhum segredo versionado.
- **Fora do escopo:** publicação e perfil da organização (RF-06), catálogo e pontos (RF-07–09), retiradas (RF-10–14), volumes (RF-15–16), painel e CSV (RF-17–19), notificações (RF-20), exclusão de conta (RF-21), TOTP, verificação de organizações, deploy, backup/restore e qualquer funcionalidade das fases F2–F6.
- **Compatibilidade:** não há aplicação anterior; a mudança é greenfield e não introduz quebra de contrato.
- **Rastreabilidade:** RF-01–RF-05; RNF-01–RNF-03, RNF-05 e RNF-08; DT-03–DT-07, DT-10, DT-12, DT-17–DT-22; CA-01 e CA-02 (sem publicação).
