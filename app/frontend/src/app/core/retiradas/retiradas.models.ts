export interface TipoDomiciliar {
  tipoResiduoId: string;
  codigo: string;
  nome: string;
  categoria: string;
  unidade: string;
}

export interface OrganizacaoParaRetirada {
  id: string;
  nome: string;
  tipo: string;
  contatoEmail: string | null;
  contatoTelefone: string | null;
  tipos: TipoDomiciliar[];
}

export interface JanelaDeRetirada {
  data: string;
  periodo: string;
  horaInicio: string;
  horaFim: string;
  capacidade: number;
  ocupacao: number;
  vagas: number;
}

export interface ItemDeRetiradaEntrada {
  tipoResiduoId: string;
  quantidadeEstimada: number;
}

export interface CriarRetiradaEntrada {
  organizacaoId: string;
  municipioId: number;
  bairro: string;
  rua: string;
  numero: string;
  complemento: string | null;
  cep: string;
  pontoReferencia: string | null;
  observacoes: string | null;
  data: string;
  periodo: string;
  itens: ItemDeRetiradaEntrada[];
}

export interface RetiradaResumo {
  id: string;
  estado: string;
  data: string;
  periodo: string;
  organizacaoId: string;
  organizacaoNome: string;
  municipioId: number;
  municipioNome: string;
  bairro: string;
  criadoEmUtc: string;
}

export interface ItemDaRetirada {
  tipoResiduoId: string;
  codigo: string;
  nome: string;
  unidade: string;
  quantidadeEstimada: number;
}

export interface HistoricoDaRetirada {
  estadoAnterior: string | null;
  estadoNovo: string;
  atorUsuarioId: string;
  ocorridoEmUtc: string;
  motivo: string | null;
}

export interface VolumeDaRetirada {
  tipoResiduoId: string;
  codigo: string;
  nome: string;
  unidade: string;
  quantidadeLiquida: number;
}

export interface RetiradaDetalhe {
  id: string;
  estado: string;
  organizacaoId: string;
  organizacaoNome: string;
  nomeCidadao: string | null;
  telefoneCidadao: string | null;
  dadosPessoaisOcultos: boolean;
  municipioNome: string;
  bairro: string | null;
  rua: string | null;
  numero: string | null;
  complemento: string | null;
  cep: string | null;
  pontoReferencia: string | null;
  observacoes: string | null;
  data: string;
  periodo: string;
  motivo: string | null;
  criadoEmUtc: string;
  atualizadoEmUtc: string;
  itens: ItemDaRetirada[];
  historico: HistoricoDaRetirada[];
  volumes: VolumeDaRetirada[];
}

export interface PaginaDeRetiradas {
  itens: RetiradaResumo[];
  pagina: number;
  tamanhoPagina: number;
  total: number;
}

export const ROTULOS_DE_ESTADO: Record<string, string> = {
  Solicitada: 'Solicitada',
  Agendada: 'Agendada',
  Coletada: 'Coletada',
  NaoRealizada: 'Não realizada',
  Recusada: 'Recusada',
  Cancelada: 'Cancelada',
};

export const ROTULOS_DE_PERIODO: Record<string, string> = {
  Manha: 'Manhã',
  Tarde: 'Tarde',
};

export const ROTULOS_DE_MOTIVO_NAO_REALIZACAO: Record<string, string> = {
  CidadaoAusente: 'Cidadão ausente',
  MaterialInadequado: 'Material inadequado',
  EnderecoNaoLocalizado: 'Endereço não localizado',
  ImpossibilidadeOperacional: 'Impossibilidade operacional',
};

export function formatarData(data: string | null | undefined): string {
  if (data === null || data === undefined || data.length === 0) {
    return '—';
  }

  const partes = data.slice(0, 10).split('-');
  if (partes.length !== 3) {
    return data;
  }

  return `${partes[2]}/${partes[1]}/${partes[0]}`;
}

export function formatarDataHora(valor: string | null | undefined): string {
  if (valor === null || valor === undefined || valor.length === 0) {
    return '—';
  }

  const data = new Date(valor);
  return Number.isNaN(data.getTime())
    ? valor
    : data.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

export function rotuloDePeriodo(periodo: string | null | undefined): string {
  return periodo === null || periodo === undefined ? '—' : (ROTULOS_DE_PERIODO[periodo] ?? periodo);
}

export function rotuloDeEstado(estado: string | null | undefined): string {
  return estado === null || estado === undefined ? '—' : (ROTULOS_DE_ESTADO[estado] ?? estado);
}

export function rotuloDeMotivoNaoRealizacao(motivo: string | null | undefined): string {
  return motivo === null || motivo === undefined
    ? '—'
    : (ROTULOS_DE_MOTIVO_NAO_REALIZACAO[motivo] ?? motivo);
}
