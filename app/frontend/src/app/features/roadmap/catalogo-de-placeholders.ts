/**
 * Espelho estável do catálogo da Application (DT-38). Os identificadores, fases
 * e rotas são verificados por teste de paridade; textos e rotas mudam junto com
 * a API, na mesma entrega. Nenhum item contém dado pessoal ou de organização.
 */
export interface ModuloEmBreve {
  readonly id: string;
  readonly titulo: string;
  readonly descricao: string;
  readonly fase: string;
  readonly rota: string;
  readonly situacao: 'em_breve';
}

export const CATALOGO_DE_PLACEHOLDERS: readonly ModuloEmBreve[] = [
  {
    id: 'preferencias-email',
    titulo: 'Preferências de e-mail',
    descricao:
      'Escolha das notificações por e-mail que o cidadão deseja receber sobre suas solicitações de retirada.',
    fase: 'F7',
    rota: '/em-breve/preferencias-email',
    situacao: 'em_breve',
  },
  {
    id: 'extrato-pontos',
    titulo: 'Extrato de pontos',
    descricao:
      'Consulta do histórico de pontos de reconhecimento obtidos e utilizados pelo cidadão.',
    fase: 'F8',
    rota: '/em-breve/extrato-pontos',
    situacao: 'em_breve',
  },
  {
    id: 'rotas-coletor',
    titulo: 'Rotas do coletor',
    descricao:
      'Acompanhamento das rotas de coleta e das retiradas programadas pela equipe da organização.',
    fase: 'F9',
    rota: '/em-breve/rotas-coletor',
    situacao: 'em_breve',
  },
  {
    id: 'transparencia-impacto',
    titulo: 'Transparência e impacto',
    descricao:
      'Divulgação pública dos volumes coletados e da estimativa de impacto ambiental, sem dados pessoais.',
    fase: 'F10',
    rota: '/em-breve/transparencia-impacto',
    situacao: 'em_breve',
  },
];
