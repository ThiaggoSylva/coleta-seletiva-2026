import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';
import {
  CriarRetiradaEntrada,
  JanelaDeRetirada,
  OrganizacaoParaRetirada,
  PaginaDeRetiradas,
  RetiradaDetalhe,
} from '../../../core/retiradas/retiradas.models';

@Injectable({ providedIn: 'root' })
export class RetiradasDoCidadaoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/retiradas`;

  listarOrganizacoes(municipioId: number): Observable<OrganizacaoParaRetirada[]> {
    return this.http.get<OrganizacaoParaRetirada[]>(`${this.base}/organizacoes`, {
      params: new HttpParams().set('municipioId', municipioId),
    });
  }

  listarJanelas(organizacaoId: string): Observable<JanelaDeRetirada[]> {
    return this.http.get<JanelaDeRetirada[]>(`${this.base}/janelas`, {
      params: new HttpParams().set('organizacaoId', organizacaoId),
    });
  }

  criar(entrada: CriarRetiradaEntrada): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(this.base, entrada);
  }

  listarMinhas(pagina: number, tamanhoPagina: number): Observable<PaginaDeRetiradas> {
    return this.http.get<PaginaDeRetiradas>(`${this.base}/minhas`, {
      params: new HttpParams().set('page', pagina).set('pageSize', tamanhoPagina),
    });
  }

  obter(id: string): Observable<RetiradaDetalhe> {
    return this.http.get<RetiradaDetalhe>(`${this.base}/minhas/${id}`);
  }

  cancelar(id: string): Observable<RetiradaDetalhe> {
    return this.http.post<RetiradaDetalhe>(`${this.base}/minhas/${id}/cancelar`, {});
  }
}
