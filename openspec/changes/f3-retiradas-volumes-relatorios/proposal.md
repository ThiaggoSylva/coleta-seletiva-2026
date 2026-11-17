# Proposal

## Why

A F2 entregou perfil, catálogo, pontos de coleta e consulta pública. O produto ainda não permite
que o cidadão solicite uma retirada, que a organização a atenda e registre o volume real coletado,
nem que o Administrador acompanhe os indicadores. Sem essa camada, os objetivos de previsibilidade
operacional e de dados confiáveis de volume não são atendidos.

A planilha de status do projeto marca como não implementados os módulos **Retiradas**, **Volumes**
e **Relatórios e Notificações**, tanto na API quanto no frontend. Esta mudança cobre exatamente
esses módulos, correspondentes às fases F3 e F4 do TDD.

## What Changes

- Configurar disponibilidade de retirada por dia da semana e período, com capacidade e bloqueios
  de data que não cancelam solicitações existentes (RF-10; RN-12).
- Permitir que o cidadão consulte organizações, tipos com retirada domiciliar e janelas com vagas,
  crie solicitações com endereço e itens estimados, acompanhe, cancele e receba notificações
  (RF-11, RF-12, RF-20; RN-01–RN-04, RN-10).
- Permitir que Administrador e Operador triem, agendem, reagendem, recusem, executem ou encerrem
  solicitações como não realizadas, com histórico e notificações (RF-13, RF-14; RN-03, RN-11).
- Registrar volumes de retirada e de recebimento em ponto, com ajuste rastreável e imutabilidade
  (RF-15, RF-16; RN-05–RN-07).
- Expor painel, série temporal, volume por origem/ponto e exportação CSV sem dados pessoais
  (RF-17, RF-18; RNF-02, RNF-08).
- Entregar notificações internas com contagem de não lidas no cabeçalho (RF-20; DT-14).
- Cobrir os fluxos com testes de domínio e de integração e atualizar a migration e o script de
  produção.

## Capabilities

### New Capabilities

- `disponibilidade-de-retirada`: capacidade por dia/período e bloqueios de data.
- `solicitacoes-de-retirada`: solicitação do cidadão, fila da organização, estados, histórico,
  capacidade concorrente e privacidade por estado.
- `volumes`: registros append-only de retirada e recebimento, ajustes e totais por tipo/unidade.
- `painel-e-relatorios`: agregações, série, totais por origem e CSV sem PII.
- `notificacoes-internas`: eventos, leitura e contagem de não lidas por usuário.

### Modified Capabilities

- `isolamento-de-organizacoes`: novas entidades entram nos filtros e na guarda de tenant.
- `auditoria`: novas ações críticas passam a ser registradas na mesma transação.

## Out of Scope

- E-mail, SMS, mensageria, rotas, coletores, operação offline, pontos/gamificação, fotos,
  impacto ambiental, administração da plataforma, exclusão de conta de cidadão (RF-21) e cadastro
  de coletor.
- Geocodificação externa e mapa operacional da fila.
- Idempotência persistida por `Idempotency-Key`; o contrato fica registrado como pendência e as
  operações continuam transacionais e protegidas por estado e versão.
- E2E Playwright e metas quantitativas de desempenho.

## Impact

- **Código:** entidades e configurações em `Domain`/`Infrastructure`, contratos, validadores e
  handlers em `Application`, serviços EF em `Infrastructure`, controllers e policies em `Api`,
  telas e serviços Angular em `app/frontend/src`.
- **API:** `/api/v1/retiradas`, `/api/v1/disponibilidade-retirada`, `/api/v1/bloqueios-retirada`,
  `/api/v1/volumes`, `/api/v1/painel`, `/api/v1/relatorios/volumes.csv` e `/api/v1/notificacoes`.
- **Banco:** tabelas `DisponibilidadeRetirada`, `BloqueioData`, `JanelaRetirada`,
  `SolicitacaoRetirada`, `SolicitacaoItem`, `SolicitacaoHistorico`, `RegistroVolume` e
  `Notificacao`; migration aditiva e atualização de `app/backend/migrations-monsterasp.sql`.
- **Compatibilidade:** a migration é aditiva; dados e rotas da F1/F2 permanecem válidos.
- **Rastreabilidade:** RF-10–RF-20; RN-01–RN-07, RN-10–RN-12; RNF-02, RNF-03, RNF-05, RNF-07,
  RNF-08; DT-07, DT-09, DT-14, DT-16, DT-17, DT-19; CA-06–CA-10.
