import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';
import { PaginaDeRetiradas, RetiradaDetalhe } from '../../../core/retiradas/retiradas.models';

export interface FiltroDeRetiradasDaOrganizacao {
  estado?: string;
  data?: string;
  municipioId?: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class RetiradasDaOrganizacaoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/retiradas`;

  listar(filtro: FiltroDeRetiradasDaOrganizacao): Observable<PaginaDeRetiradas> {
    let parametros = new HttpParams().set('page', filtro.page).set('pageSize', filtro.pageSize);

    if (filtro.estado !== undefined && filtro.estado.length > 0) {
      parametros = parametros.set('estado', filtro.estado);
    }

    if (filtro.data !== undefined && filtro.data.length > 0) {
      parametros = parametros.set('data', filtro.data);
    }

    if (filtro.municipioId !== undefined) {
      parametros = parametros.set('municipioId', filtro.municipioId);
    }

    return this.http.get<PaginaDeRetiradas>(this.base, { params: parametros });
  }

  obter(id: string): Observable<RetiradaDetalhe> {
    return this.http.get<RetiradaDetalhe>(`${this.base}/${id}`);
  }

  agendar(id: string, data: string, periodo: string): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(`${this.base}/${id}/agendar`, { data, periodo });
  }

  reagendar(id: string, data: string, periodo: string): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(`${this.base}/${id}/reagendar`, { data, periodo });
  }

  recusar(id: string, motivo: string): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(`${this.base}/${id}/recusar`, { motivo });
  }

  executar(
    id: string,
    itens: { tipoResiduoId: string; quantidade: number }[],
  ): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(`${this.base}/${id}/executar`, { itens });
  }

  marcarNaoRealizada(id: string, motivo: string): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(`${this.base}/${id}/nao-realizada`, { motivo });
  }
}
