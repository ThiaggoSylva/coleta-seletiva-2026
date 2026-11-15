import { HttpErrorResponse } from '@angular/common/http';

export interface RespostaProblema {
  status?: number;
  title?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof HttpErrorResponse) {
    const corpo = erro.error as RespostaProblema | null;

    if (corpo?.code === 'limite_de_taxa') {
      return 'Muitas tentativas. Aguarde um instante e tente novamente.';
    }

    if (typeof corpo?.title === 'string' && corpo.title.trim().length > 0) {
      return corpo.title;
    }

    if (erro.status === 0) {
      return 'Não foi possível conectar ao servidor.';
    }

    if (erro.status >= 500) {
      return 'Ocorreu um erro inesperado. Tente novamente.';
    }
  }

  return 'Não foi possível concluir a operação.';
}

export function errosPorCampo(erro: unknown): Record<string, string[]> {
  if (erro instanceof HttpErrorResponse) {
    const corpo = erro.error as RespostaProblema | null;
    if (corpo?.errors !== undefined && corpo.errors !== null) {
      return corpo.errors;
    }
  }

  return {};
}

export function codigoDoErro(erro: unknown): string | null {
  if (erro instanceof HttpErrorResponse) {
    const corpo = erro.error as RespostaProblema | null;
    if (typeof corpo?.code === 'string') {
      return corpo.code;
    }
  }

  return null;
}
