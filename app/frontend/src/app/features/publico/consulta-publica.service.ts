import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../core/configuracao-api';
import { Municipio } from '../../core/servicos/catalogo.service';
import { HorarioDoPonto, TipoDoPonto } from '../organizacao/pontos/pontos.service';

export interface BuscaPublicaEntrada {
  municipioId?: number | null;
  tipoResiduoId?: string | null;
  abertoAgora?: boolean | null;
  latitude?: number | null;
  longitude?: number | null;
  raioKm?: number | null;
  pagina?: number;
  tamanhoPagina?: number;
}

export interface PontoPublico {
  id: string;
  nome: string;
  organizacaoNome: string;
  contatoEmail: string | null;
  contatoTelefone: string | null;
  municipio: Municipio;
  bairro: string;
  rua: string;
  numero: string;
  complemento: string | null;
  cep: string;
  latitude: number;
  longitude: number;
  observacoes: string | null;
  abertoAgora: boolean;
  distanciaKm: number | null;
  horarios: HorarioDoPonto[];
  tipos: TipoDoPonto[];
}

export interface PaginaDePontosPublicos {
  itens: PontoPublico[];
  pagina: number;
  tamanhoPagina: number;
  total: number;
}

@Injectable({ providedIn: 'root' })
export class ConsultaPublicaService {
  private readonly http = inject(HttpClient);

  buscar(entrada: BuscaPublicaEntrada): Observable<PaginaDePontosPublicos> {
    return this.http.post<PaginaDePontosPublicos>(`${API_BASE}/publico/pontos/busca`, entrada);
  }
}
