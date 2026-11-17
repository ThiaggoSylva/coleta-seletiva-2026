import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';

export interface FiltroDoPainel {
  de?: string;
  ate?: string;
  tipoResiduoId?: string;
  origem?: string;
}

export interface VolumePorTipo {
  tipoResiduoId: string;
  codigo: string;
  nome: string;
  unidade: string;
  quantidade: number;
}

export interface TotalPorUnidade {
  unidade: string;
  quantidade: number;
}

export interface ContagemPorEstado {
  estado: string;
  quantidade: number;
}

export interface VolumePorOrigem {
  origem: string;
  unidade: string;
  quantidade: number;
}

export interface VolumePorPonto {
  pontoId: string;
  pontoNome: string;
  unidade: string;
  quantidade: number;
}

export interface ResumoDoPainel {
  de: string;
  ate: string;
  porTipo: VolumePorTipo[];
  totaisPorUnidade: TotalPorUnidade[];
  retiradasPorEstado: ContagemPorEstado[];
  porOrigem: VolumePorOrigem[];
  porPonto: VolumePorPonto[];
}

export interface PontoDaSerie {
  inicio: string;
  itens: VolumePorTipo[];
}

export interface SerieDoPainel {
  agrupamento: string;
  de: string;
  ate: string;
  pontos: PontoDaSerie[];
}

@Injectable({ providedIn: 'root' })
export class PainelService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/painel`;
  private readonly baseDeRelatorios = `${API_BASE}/relatorios`;

  obterResumo(filtro: FiltroDoPainel): Observable<ResumoDoPainel> {
    return this.http.get<ResumoDoPainel>(`${this.base}/resumo`, {
      params: this.parametros(filtro),
    });
  }

  obterSerie(filtro: FiltroDoPainel, agrupamento: string): Observable<SerieDoPainel> {
    return this.http.get<SerieDoPainel>(`${this.base}/serie`, {
      params: this.parametros(filtro).set('agrupamento', agrupamento),
    });
  }

  exportarCsv(filtro: FiltroDoPainel): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseDeRelatorios}/volumes.csv`, {
      params: this.parametros(filtro),
      observe: 'response',
      responseType: 'blob',
    });
  }

  private parametros(filtro: FiltroDoPainel): HttpParams {
    let parametros = new HttpParams();

    if (filtro.de !== undefined && filtro.de.length > 0) {
      parametros = parametros.set('de', filtro.de);
    }

    if (filtro.ate !== undefined && filtro.ate.length > 0) {
      parametros = parametros.set('ate', filtro.ate);
    }

    if (filtro.tipoResiduoId !== undefined && filtro.tipoResiduoId.length > 0) {
      parametros = parametros.set('tipoResiduoId', filtro.tipoResiduoId);
    }

    if (filtro.origem !== undefined && filtro.origem.length > 0) {
      parametros = parametros.set('origem', filtro.origem);
    }

    return parametros;
  }
}
