import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';
import { Municipio } from '../../../core/servicos/catalogo.service';

export type StatusPonto = 'Ativo' | 'Inativo' | 'IndisponivelTemporariamente';

export interface HorarioDoPonto {
  diaDaSemana: string;
  horaInicio: string;
  horaFim: string;
}

export interface TipoDoPonto {
  tipoResiduoId: string;
  codigo: string;
  nome: string;
  unidade: string;
}

export interface PontoResumo {
  id: string;
  nome: string;
  municipio: string;
  uf: string;
  status: StatusPonto;
  abertoAgora: boolean;
  excluido: boolean;
  tipos: string[];
}

export interface PontoDetalhe {
  id: string;
  nome: string;
  municipio: Municipio;
  uf: string;
  bairro: string;
  rua: string;
  numero: string;
  complemento: string | null;
  cep: string;
  latitude: number;
  longitude: number;
  observacoes: string | null;
  status: StatusPonto;
  abertoAgora: boolean;
  excluido: boolean;
  horarios: HorarioDoPonto[];
  tipos: TipoDoPonto[];
}

export interface PontoEntrada {
  nome: string;
  municipioId: number;
  bairro: string;
  rua: string;
  numero: string;
  complemento: string | null;
  cep: string;
  latitude: number;
  longitude: number;
  observacoes: string | null;
  status: StatusPonto;
  horarios: HorarioDoPonto[];
  tiposResiduoIds: string[];
}

export interface OpcaoDePonto {
  id: string;
  nome: string;
  municipio: string;
  uf: string;
}

@Injectable({ providedIn: 'root' })
export class PontosService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/pontos`;

  listar(): Observable<PontoResumo[]> {
    return this.http.get<PontoResumo[]>(this.base);
  }

  listarOpcoes(): Observable<OpcaoDePonto[]> {
    return this.http.get<OpcaoDePonto[]>(`${this.base}/opcoes`);
  }

  obter(id: string): Observable<PontoDetalhe> {
    return this.http.get<PontoDetalhe>(`${this.base}/${id}`);
  }

  criar(entrada: PontoEntrada): Observable<PontoDetalhe> {
    return this.http.post<PontoDetalhe>(this.base, entrada);
  }

  atualizar(id: string, entrada: PontoEntrada): Observable<PontoDetalhe> {
    return this.http.put<PontoDetalhe>(`${this.base}/${id}`, entrada);
  }

  alterarStatus(id: string, status: StatusPonto): Observable<PontoDetalhe> {
    return this.http.put<PontoDetalhe>(`${this.base}/${id}/status`, { status });
  }

  retirar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
