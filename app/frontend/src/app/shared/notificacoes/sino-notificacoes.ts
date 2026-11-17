import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { mensagemDeErro } from '../../core/erros/erro-api';
import { Notificacao, NotificacoesService } from '../../core/servicos/notificacoes.service';

@Component({
  selector: 'app-sino-notificacoes',
  imports: [DatePipe],
  template: `
    <div class="relative">
      <button
        type="button"
        class="relative grid h-9 w-9 place-items-center rounded-full border border-slate-200 bg-white text-slate-600 hover:bg-emerald-50 hover:text-emerald-800"
        [attr.aria-expanded]="aberto()"
        aria-haspopup="true"
        aria-label="Notificações"
        (click)="alternar()"
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.8"
          stroke-linecap="round"
          stroke-linejoin="round"
          class="h-5 w-5"
          aria-hidden="true"
        >
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.7 21a2 2 0 0 1-3.4 0" />
        </svg>
        @if (notificacoes.naoLidas() > 0) {
          <span
            class="absolute -right-1 -top-1 grid h-5 min-w-5 place-items-center rounded-full bg-red-600 px-1 text-[10px] font-bold text-white"
            aria-live="polite"
          >
            {{ notificacoes.naoLidas() > 9 ? '9+' : notificacoes.naoLidas() }}
          </span>
        }
      </button>

      @if (aberto()) {
        <button
          type="button"
          class="fixed inset-0 z-30 cursor-default"
          aria-label="Fechar notificações"
          tabindex="-1"
          (click)="fechar()"
        ></button>

        <div
          class="absolute right-0 z-40 mt-2 w-80 max-w-[calc(100vw-3rem)] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl"
        >
          <div class="flex items-center justify-between gap-2 border-b border-slate-100 px-4 py-3">
            <h2 class="text-sm font-semibold text-slate-900">Notificações</h2>
            @if (notificacoes.naoLidas() > 0) {
              <button
                type="button"
                class="text-xs font-medium text-emerald-700 underline disabled:opacity-60"
                [disabled]="processando()"
                (click)="marcarTodas()"
              >
                Marcar todas como lidas
              </button>
            }
          </div>

          @if (erro() !== null) {
            <p
              class="border-b border-red-100 bg-red-50 px-4 py-2 text-xs text-red-800"
              role="alert"
            >
              {{ erro() }}
            </p>
          }

          @if (notificacoes.itens().length === 0) {
            <p class="px-4 py-6 text-sm text-slate-600">Nenhuma notificação por enquanto.</p>
          } @else {
            <ul class="max-h-96 divide-y divide-slate-100 overflow-y-auto">
              @for (item of notificacoes.itens(); track item.id) {
                <li>
                  <button
                    type="button"
                    class="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-slate-50"
                    (click)="abrir(item)"
                  >
                    <span
                      class="mt-1.5 h-2 w-2 shrink-0 rounded-full"
                      [class]="item.lida ? 'bg-transparent' : 'bg-emerald-600'"
                      aria-hidden="true"
                    ></span>
                    <span class="min-w-0 flex-1">
                      <span class="block text-sm font-medium text-slate-900">{{
                        item.titulo
                      }}</span>
                      <span class="mt-0.5 block text-xs text-slate-600">{{ item.mensagem }}</span>
                      <span class="mt-1 block text-[11px] text-slate-500">
                        {{ item.criadaEmUtc | date: 'dd/MM/yyyy HH:mm' }}
                      </span>
                    </span>
                  </button>
                </li>
              }
            </ul>
          }
        </div>
      }
    </div>
  `,
})
export class SinoNotificacoes {
  protected readonly notificacoes = inject(NotificacoesService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly aberto = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly processando = signal(false);

  protected alternar(): void {
    this.aberto.update((valor) => !valor);
    if (this.aberto()) {
      this.atualizar();
    }
  }

  protected fechar(): void {
    this.aberto.set(false);
  }

  protected marcarTodas(): void {
    this.processando.set(true);
    this.erro.set(null);

    this.notificacoes.marcarTodasComoLidas().subscribe({
      next: () => this.processando.set(false),
      error: (erro: unknown) => {
        this.processando.set(false);
        this.erro.set(mensagemDeErro(erro));
      },
    });
  }

  protected abrir(item: Notificacao): void {
    const destino = this.auth.possuiPapel('Cidadao')
      ? ['/cidadao/retiradas', item.referenciaId]
      : ['/organizacao/retiradas', item.referenciaId];

    this.aberto.set(false);

    if (item.lida) {
      void this.router.navigate(destino);
      return;
    }

    this.notificacoes.marcarComoLida(item.id).subscribe({
      next: () => void this.router.navigate(destino),
      error: () => void this.router.navigate(destino),
    });
  }

  private atualizar(): void {
    this.erro.set(null);
    this.notificacoes.atualizar().subscribe({
      error: (erro: unknown) => this.erro.set(mensagemDeErro(erro)),
    });
  }
}
