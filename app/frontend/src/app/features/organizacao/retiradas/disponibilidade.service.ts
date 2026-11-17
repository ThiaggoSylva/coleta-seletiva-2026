import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../../../core/configuracao-api';

export interface DisponibilidadeDia {
  diaDaSemana: string;
  periodo: string;
  capacidade: number;
}

export interface BloqueioDeData {
  id: string;
  data: string;
  motivo: string | null;
  criadoPorUsuarioId: string;
  criadoEmUtc: string;
}

@Injectable({ providedIn: 'root' })
export class DisponibilidadeService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/disponibilidade-retirada`;
  private readonly baseDeBloqueios = `${API_BASE}/bloqueios-retirada`;

  obter(): Observable<DisponibilidadeDia[]> {
    return this.http.get<DisponibilidadeDia[]>(this.base);
  }

  definir(dias: DisponibilidadeDia[]): Observable<DisponibilidadeDia[]> {
    return this.http.put<DisponibilidadeDia[]>(this.base, { dias });
  }

  listarBloqueios(): Observable<BloqueioDeData[]> {
    return this.http.get<BloqueioDeData[]>(this.baseDeBloqueios);
  }

  criarBloqueio(data: string, motivo: string | null): Observable<BloqueioDeData> {
    return this.http.post<BloqueioDeData>(this.baseDeBloqueios, { data, motivo });
  }

  removerBloqueio(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseDeBloqueios}/${id}`);
  }
}
