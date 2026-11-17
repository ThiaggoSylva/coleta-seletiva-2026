import { HttpClient, HttpParams } from '@angular/common/http';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE } from '../configuracao-api';

export interface Notificacao {
  id: string;
  tipo: string;
  referenciaId: string;
  titulo: string;
  mensagem: string;
  lida: boolean;
  lidaEmUtc: string | null;
  criadaEmUtc: string;
}

export interface ListaDeNotificacoes {
  itens: Notificacao[];
  naoLidas: number;
}

const INTERVALO_DE_ATUALIZACAO_MS = 60_000;

/** Notificações do usuário autenticado, com polling enquanto a sessão estiver ativa (RF-20). */
@Injectable({ providedIn: 'root' })
export class NotificacoesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/notificacoes`;
  private readonly itensInternos = signal<Notificacao[]>([]);
  private readonly naoLidasInternas = signal(0);
  private temporizador: ReturnType<typeof setInterval> | null = null;

  readonly itens = this.itensInternos.asReadonly();
  readonly naoLidas = this.naoLidasInternas.asReadonly();

  constructor() {
    inject(DestroyRef).onDestroy(() => this.parar());
  }

  iniciar(): void {
    if (this.temporizador !== null) {
      return;
    }

    this.atualizar().subscribe({ error: () => undefined });
    this.temporizador = setInterval(() => {
      this.atualizar().subscribe({ error: () => undefined });
    }, INTERVALO_DE_ATUALIZACAO_MS);
  }

  parar(): void {
    if (this.temporizador !== null) {
      clearInterval(this.temporizador);
      this.temporizador = null;
    }

    this.itensInternos.set([]);
    this.naoLidasInternas.set(0);
  }

  atualizar(apenasNaoLidas = false): Observable<ListaDeNotificacoes> {
    let parametros = new HttpParams();
    if (apenasNaoLidas) {
      parametros = parametros.set('apenasNaoLidas', true);
    }

    return this.http.get<ListaDeNotificacoes>(this.base, { params: parametros }).pipe(
      tap((lista) => {
        this.itensInternos.set(lista.itens);
        this.naoLidasInternas.set(lista.naoLidas);
      }),
    );
  }

  marcarComoLida(id: string): Observable<Notificacao> {
    return this.http.post<Notificacao>(`${this.base}/${id}/lida`, {}).pipe(
      tap((atualizada) => {
        this.itensInternos.update((itens) =>
          itens.map((item) => (item.id === atualizada.id ? atualizada : item)),
        );
        if (atualizada.lida) {
          this.naoLidasInternas.update((total) => Math.max(0, total - 1));
        }
      }),
    );
  }

  marcarTodasComoLidas(): Observable<{ atualizadas: number }> {
    return this.http.post<{ atualizadas: number }>(`${this.base}/lidas`, {}).pipe(
      tap(() => {
        const agora = new Date().toISOString();
        this.itensInternos.update((itens) =>
          itens.map((item) => (item.lida ? item : { ...item, lida: true, lidaEmUtc: agora })),
        );
        this.naoLidasInternas.set(0);
      }),
    );
  }
}
