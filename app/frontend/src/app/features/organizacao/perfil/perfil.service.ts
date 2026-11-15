import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';
import { Municipio } from '../../../core/servicos/catalogo.service';

export type TipoOrganizacao = 'Cooperativa' | 'Associacao' | 'Prefeitura' | 'Outro';

export interface PeriodoDaOrganizacao {
  periodo: string;
  horaInicio: string;
  horaFim: string;
}

export interface TipoHabilitado {
  tipoResiduoId: string;
  codigo: string;
  nome: string;
  categoria: string;
  unidade: string;
  aceitaRetiradaDomiciliar: boolean;
}

export interface PerfilDaOrganizacao {
  id: string;
  nome: string;
  tipo: TipoOrganizacao;
  contatoEmail: string | null;
  contatoTelefone: string | null;
  fuso: string;
  limiteSolicitacoesAbertas: number;
  publicada: boolean;
  municipios: Municipio[];
  periodos: PeriodoDaOrganizacao[];
  tipos: TipoHabilitado[];
}

export interface AtualizarPerfilEntrada {
  nome: string;
  tipo: TipoOrganizacao;
  contatoEmail: string | null;
  contatoTelefone: string | null;
  fuso: string;
  limiteSolicitacoesAbertas: number;
}

export interface TipoHabilitadoEntrada {
  tipoResiduoId: string;
  aceitaRetiradaDomiciliar: boolean;
}

@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/organizacao`;

  obter(): Observable<PerfilDaOrganizacao> {
    return this.http.get<PerfilDaOrganizacao>(this.base);
  }

  atualizar(entrada: AtualizarPerfilEntrada): Observable<PerfilDaOrganizacao> {
    return this.http.put<PerfilDaOrganizacao>(this.base, entrada);
  }

  definirMunicipios(municipioIds: number[]): Observable<PerfilDaOrganizacao> {
    return this.http.put<PerfilDaOrganizacao>(`${this.base}/municipios`, { municipioIds });
  }

  definirPeriodos(periodos: PeriodoDaOrganizacao[]): Observable<PerfilDaOrganizacao> {
    return this.http.put<PerfilDaOrganizacao>(`${this.base}/periodos`, { periodos });
  }

  definirTipos(tipos: TipoHabilitadoEntrada[]): Observable<PerfilDaOrganizacao> {
    return this.http.put<PerfilDaOrganizacao>(`${this.base}/tipos`, { tipos });
  }

  definirPublicacao(publicada: boolean): Observable<{ publicada: boolean }> {
    return this.http.put<{ publicada: boolean }>(`${this.base}/publicacao`, { publicada });
  }
}
