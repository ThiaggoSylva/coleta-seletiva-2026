import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../configuracao-api';

export interface TipoResiduo {
  id: string;
  codigo: string;
  nome: string;
  categoria: string;
  unidade: string;
  orientacoes: string | null;
}

export interface Municipio {
  id: number;
  nome: string;
  uf: string;
}

/** Catálogo global consultado também por visitantes anônimos. */
@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private readonly http = inject(HttpClient);

  listarTipos(): Observable<TipoResiduo[]> {
    return this.http.get<TipoResiduo[]>(`${API_BASE}/publico/tipos`);
  }

  listarMunicipios(uf: string, texto: string): Observable<Municipio[]> {
    let parametros = new HttpParams();
    if (uf.trim().length > 0) {
      parametros = parametros.set('uf', uf.trim().toUpperCase());
    }
    if (texto.trim().length > 0) {
      parametros = parametros.set('texto', texto.trim());
    }

    return this.http.get<Municipio[]>(`${API_BASE}/publico/municipios`, { params: parametros });
  }
}
