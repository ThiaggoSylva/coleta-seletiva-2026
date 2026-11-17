import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';

export type OrigemDoVolume = 'Retirada' | 'RecebimentoEmPonto' | 'Ajuste';

export interface VolumeResposta {
  id: string;
  tipoResiduoId: string;
  tipoCodigo: string;
  tipoNome: string;
  unidade: string;
  quantidade: number;
  origem: OrigemDoVolume;
  solicitacaoId: string | null;
  pontoId: string | null;
  pontoNome: string | null;
  registroOriginalId: string | null;
  totalDoOriginal: number | null;
  operadorUsuarioId: string;
  operadorNome: string;
  ocorridoEmUtc: string;
  dataLocal: string;
  motivo: string | null;
}

export interface PaginaDeVolumes {
  itens: VolumeResposta[];
  pagina: number;
  tamanhoPagina: number;
  total: number;
}

export interface FiltroDeVolumes {
  de?: string;
  ate?: string;
  tipoResiduoId?: string;
  origem?: string;
  pontoId?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class VolumesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/volumes`;

  listar(filtro: FiltroDeVolumes): Observable<PaginaDeVolumes> {
    let parametros = new HttpParams().set('page', filtro.page).set('pageSize', filtro.pageSize);

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

    if (filtro.pontoId !== undefined && filtro.pontoId.length > 0) {
      parametros = parametros.set('pontoId', filtro.pontoId);
    }

    return this.http.get<PaginaDeVolumes>(this.base, { params: parametros });
  }

  registrarRecebimento(entrada: {
    pontoId: string;
    tipoResiduoId: string;
    quantidade: number;
  }): Observable<VolumeResposta> {
    return this.http.post<VolumeResposta>(`${this.base}/recebimentos`, entrada);
  }

  ajustar(id: string, entrada: { quantidade: number; motivo: string }): Observable<VolumeResposta> {
    return this.http.post<VolumeResposta>(`${this.base}/${id}/ajustes`, entrada);
  }
}
