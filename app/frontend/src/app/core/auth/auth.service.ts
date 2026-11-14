import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { API_BASE, CABECALHO_SOLICITADO_COM, VALOR_SOLICITADO_COM } from '../configuracao-api';
import { ContaAutenticada, Papel, SessaoResposta } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly token = signal<string | null>(null);
  private readonly conta = signal<ContaAutenticada | null>(null);
  private renovacao: Observable<boolean> | null = null;

  readonly contaAtual = this.conta.asReadonly();
  readonly autenticado = computed(() => this.token() !== null && this.conta() !== null);
  readonly trocaSenhaPendente = computed(() => this.conta()?.trocaSenhaPendente === true);
  readonly papel = computed(() => this.conta()?.papel ?? null);

  login(identificador: string, senha: string): Observable<ContaAutenticada> {
    return this.http.post<SessaoResposta>(`${API_BASE}/auth/login`, { identificador, senha }).pipe(
      tap((resposta) => this.aplicarSessao(resposta)),
      map((resposta) => resposta.usuario),
    );
  }

  renovarSessao(): Observable<boolean> {
    if (this.renovacao !== null) {
      return this.renovacao;
    }

    this.renovacao = this.http
      .post<SessaoResposta>(`${API_BASE}/auth/refresh`, {}, { headers: this.cabecalhosDeSessao() })
      .pipe(
        tap((resposta) => this.aplicarSessao(resposta)),
        map(() => true),
        catchError(() => {
          this.limparSessao();
          return of(false);
        }),
        finalize(() => {
          this.renovacao = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.renovacao;
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(`${API_BASE}/auth/logout`, {}, { headers: this.cabecalhosDeSessao() })
      .pipe(
        catchError(() => of(void 0)),
        finalize(() => this.limparSessao()),
        map(() => void 0),
      );
  }

  alterarSenha(senhaAtual: string, novaSenha: string): Observable<void> {
    return this.http.post<void>(`${API_BASE}/auth/alterar-senha`, { senhaAtual, novaSenha }).pipe(
      tap(() => this.limparSessao()),
      map(() => void 0),
    );
  }

  obterToken(): string | null {
    return this.token();
  }

  possuiPapel(...papeis: Papel[]): boolean {
    const papel = this.conta()?.papel;
    return papel !== undefined && papeis.includes(papel);
  }

  limparSessao(): void {
    this.token.set(null);
    this.conta.set(null);
  }

  private aplicarSessao(resposta: SessaoResposta): void {
    this.token.set(resposta.tokenDeAcesso);
    this.conta.set(resposta.usuario);
  }

  private cabecalhosDeSessao(): HttpHeaders {
    return new HttpHeaders({
      [CABECALHO_SOLICITADO_COM]: VALOR_SOLICITADO_COM,
    });
  }
}
