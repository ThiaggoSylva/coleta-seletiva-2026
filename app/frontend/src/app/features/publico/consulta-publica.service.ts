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

export type OrigemDoPonto = 'Organizacao' | 'OpenStreetMap';

/**
 * Ponto exibido na consulta pública. Pontos de fonte externa
 * (`origem === 'OpenStreetMap'`) podem não ter endereço estruturado, horário
 * conhecido ou tipos aceitos informados.
 */
export interface PontoPublico {
  id: string;
  origem: OrigemDoPonto;
  nome: string;
  organizacaoNome: string;
  contatoEmail: string | null;
  contatoTelefone: string | null;
  municipio: Municipio | null;
  bairro: string | null;
  rua: string | null;
  numero: string | null;
  complemento: string | null;
  cep: string | null;
  latitude: number;
  longitude: number;
  observacoes: string | null;
  abertoAgora: boolean | null;
  distanciaKm: number | null;
  horarios: HorarioDoPonto[];
  tipos: TipoDoPonto[];
  atribuicao: string | null;
  urlDaFonte: string | null;
}

export interface PaginaDePontosPublicos {
  itens: PontoPublico[];
  pagina: number;
  tamanhoPagina: number;
  total: number;
  avisoDeFonteExterna: string | null;
  atribuicaoDeFonteExterna: string | null;
}

@Injectable({ providedIn: 'root' })
export class ConsultaPublicaService {
  private readonly http = inject(HttpClient);

  buscar(entrada: BuscaPublicaEntrada): Observable<PaginaDePontosPublicos> {
    return this.http.post<PaginaDePontosPublicos>(`${API_BASE}/publico/pontos/busca`, entrada);
  }
}
