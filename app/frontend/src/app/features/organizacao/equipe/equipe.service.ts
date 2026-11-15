import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';

export interface ContaDaEquipe {
  id: string;
  nome: string;
  usuario: string;
  email: string;
  ativo: boolean;
  trocaSenhaPendente: boolean;
}

export interface CriarOperadorEntrada {
  nome: string;
  usuario: string;
  email: string;
  senha: string;
}

export interface EditarOperadorEntrada {
  nome: string;
  usuario: string;
  email: string;
}

@Injectable({ providedIn: 'root' })
export class EquipeService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/equipe`;

  listar(): Observable<ContaDaEquipe[]> {
    return this.http.get<ContaDaEquipe[]>(this.base);
  }

  obter(id: string): Observable<ContaDaEquipe> {
    return this.http.get<ContaDaEquipe>(`${this.base}/${id}`);
  }

  criar(entrada: CriarOperadorEntrada): Observable<ContaDaEquipe> {
    return this.http.post<ContaDaEquipe>(this.base, entrada);
  }

  editar(id: string, entrada: EditarOperadorEntrada): Observable<ContaDaEquipe> {
    return this.http.put<ContaDaEquipe>(`${this.base}/${id}`, entrada);
  }

  inativar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
