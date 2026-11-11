# Frontend

SPA Angular 22.2.5 da plataforma de coleta seletiva. A aplicação usa componentes
standalone, é zoneless, adota Signals e RxJS e estiliza com Tailwind CSS 4. Os
testes unitários usam Vitest.

## Requisitos

- Node.js `^22.22.3 || ^24.15.0 || >=26.0.0` (verificado com 24.21.0)
- npm 12.2.1 ou superior

## Instalação

```bash
npm ci
```

## Desenvolvimento

```bash
npm start
```

A SPA fica disponível em `http://localhost:4200/` e será servida na mesma origem
da API por meio do proxy local configurado nas tarefas de infraestrutura.

## Build

```bash
npm run build
```

Os artefatos são gerados em `dist/`.

## Testes

```bash
npm test
```

O script executa `ng test --watch=false` uma única vez. Para acompanhar alterações
durante o desenvolvimento, use `npm run test:watch`. O builder do Angular 22 usa
Vitest; por isso a flag `--run` do Vitest não é repassada pelo CLI do Angular.

## Estrutura

```text
src/app/
  core/        serviços singleton, sessão e interceptores
  shared/      componentes, diretivas e pipes reutilizáveis
  features/
    acesso/    registro, login, primeiro acesso e troca de senha
    cidadao/   área autenticada do cidadão e perfil
    organizacao/ área da organização e gestão de equipe
```

As rotas de cada área são carregadas sob demanda por `app.routes.ts`.

## Decisões da F1

- O token de acesso permanece somente em memória; o refresh usa cookie
  `HttpOnly`. Nenhum token ou dado pessoal é gravado em `localStorage`.
- A API é a autoridade de autorização; guards apenas melhoram a navegação.
- A interface não monta claims nem decide sozinha o escopo da organização.
- As telas serão implementadas nas tarefas 6.x da mudança `f1-fundacao-segura`.
