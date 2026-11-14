export type Papel = 'Administrador' | 'Operador' | 'Cidadao';

export interface ContaAutenticada {
  id: string;
  nome: string;
  email: string;
  papel: Papel;
  organizacaoId: string | null;
  trocaSenhaPendente: boolean;
}

export interface SessaoResposta {
  tokenDeAcesso: string;
  expiraEmUtc: string;
  usuario: ContaAutenticada;
}

export interface RespostaProblema {
  status?: number;
  title?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
