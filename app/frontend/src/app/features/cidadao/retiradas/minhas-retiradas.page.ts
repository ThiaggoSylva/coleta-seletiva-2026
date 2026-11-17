import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { classeDoEstado } from '../../../core/retiradas/estado-visual';
import {
  formatarData,
  formatarDataHora,
  PaginaDeRetiradas,
  RetiradaResumo,
  rotuloDeEstado,
  rotuloDePeriodo,
} from '../../../core/retiradas/retiradas.models';
import { RetiradasDoCidadaoService } from './retiradas.service';

const TAMANHO_DA_PAGINA = 10;

@Component({
  selector: 'app-minhas-retiradas',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-5xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Minhas retiradas</h1>
          <p class="mt-1 text-sm text-slate-600">
            Acompanhe as solicitações de retirada domiciliar e cancele dentro do prazo.
          </p>
        </div>
        <a routerLink="/cidadao/retiradas/nova" class="botao-primario">Nova solicitação</a>
      </div>

      @if (mensagem() !== null) {
        <p class="mt-4 rounded-md bg-emerald-50 p-3 text-sm text-emerald-900" role="status">
          {{ mensagem() }}
        </p>
      }

      @if (erroDeAcao() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeAcao() }}
        </p>
      }

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando solicitações…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button
            type="button"
            class="ml-1 font-medium underline"
            (click)="carregar(pagina().pagina)"
          >
            Tentar novamente
          </button>
        </p>
      } @else if (pagina().itens.length === 0) {
        <div class="mt-6 rounded-md bg-white p-5 text-slate-600 shadow-sm">
          <p>Você ainda não fez nenhuma solicitação de retirada.</p>
          <a
            routerLink="/cidadao/retiradas/nova"
            class="mt-3 inline-block font-medium text-emerald-700 underline"
          >
            Solicitar a primeira retirada
          </a>
        </div>
      } @else {
        <div class="mt-6 overflow-x-auto rounded-md bg-white shadow-sm">
          <table class="min-w-full divide-y divide-slate-200">
            <caption class="sr-only">
              Solicitações de retirada do cidadão
            </caption>
            <thead class="bg-slate-50">
              <tr>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Data
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Organização
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Local
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Situação
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-right text-xs font-semibold uppercase text-slate-600"
                >
                  Ações
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              @for (retirada of pagina().itens; track retirada.id) {
                <tr>
                  <td class="px-4 py-3 text-sm text-slate-900">
                    <span class="block font-medium">{{ formatarData(retirada.data) }}</span>
                    <span class="block text-xs text-slate-500">
                      {{ rotuloDePeriodo(retirada.periodo) }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-700">
                    {{ retirada.organizacaoNome }}
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-700">
                    <span class="block">{{ retirada.municipioNome }}</span>
                    <span class="block text-xs text-slate-500">{{ retirada.bairro }}</span>
                  </td>
                  <td class="px-4 py-3 text-sm">
                    <span class="etiqueta" [class]="classeDoEstado(retirada.estado)">
                      {{ rotuloDeEstado(retirada.estado) }}
                    </span>
                    <span class="mt-1 block text-xs text-slate-500">
                      Solicitada em {{ formatarDataHora(retirada.criadoEmUtc) }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-right text-sm">
                    <div class="flex flex-wrap items-center justify-end gap-3">
                      <a
                        [routerLink]="['/cidadao/retiradas', retirada.id]"
                        class="font-medium text-emerald-700 underline"
                      >
                        Abrir
                      </a>

                      @if (retirada.estado === 'Solicitada' || retirada.estado === 'Agendada') {
                        @if (confirmandoId() === retirada.id) {
                          <span class="text-slate-700">Cancelar esta solicitação?</span>
                          <button
                            type="button"
                            class="font-medium text-red-700 underline disabled:opacity-60"
                            [disabled]="cancelandoId() === retirada.id"
                            (click)="cancelar(retirada)"
                          >
                            {{ cancelandoId() === retirada.id ? 'Cancelando…' : 'Confirmar' }}
                          </button>
                          <button
                            type="button"
                            class="font-medium text-slate-600 underline"
                            (click)="fecharConfirmacao()"
                          >
                            Voltar
                          </button>
                        } @else {
                          <button
                            type="button"
                            class="font-medium text-red-700 underline"
                            (click)="confirmarCancelamento(retirada.id)"
                          >
                            Cancelar
                          </button>
                        }
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
          <span>
            {{ pagina().total }} solicitação(ões) · página {{ pagina().pagina }} de
            {{ totalDePaginas() }}
          </span>
          <div class="flex gap-2">
            <button
              type="button"
              class="botao-secundario !px-3 !py-1.5"
              [disabled]="pagina().pagina <= 1"
              (click)="irPara(pagina().pagina - 1)"
            >
              Anterior
            </button>
            <button
              type="button"
              class="botao-secundario !px-3 !py-1.5"
              [disabled]="pagina().pagina >= totalDePaginas()"
              (click)="irPara(pagina().pagina + 1)"
            >
              Próxima
            </button>
          </div>
        </div>
      }
    </section>
  `,
})
export class MinhasRetiradasPage implements OnInit {
  private readonly servico = inject(RetiradasDoCidadaoService);

  protected readonly pagina = signal<PaginaDeRetiradas>({
    itens: [],
    pagina: 1,
    tamanhoPagina: TAMANHO_DA_PAGINA,
    total: 0,
  });
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly confirmandoId = signal<string | null>(null);
  protected readonly cancelandoId = signal<string | null>(null);

  protected readonly totalDePaginas = computed(() =>
    Math.max(1, Math.ceil(this.pagina().total / this.pagina().tamanhoPagina)),
  );

  protected readonly formatarData = formatarData;
  protected readonly formatarDataHora = formatarDataHora;
  protected readonly rotuloDePeriodo = rotuloDePeriodo;
  protected readonly rotuloDeEstado = rotuloDeEstado;
  protected readonly classeDoEstado = classeDoEstado;

  ngOnInit(): void {
    this.carregar(1);
  }

  protected carregar(numeroDaPagina: number): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);
    this.confirmandoId.set(null);

    this.servico.listarMinhas(numeroDaPagina, TAMANHO_DA_PAGINA).subscribe({
      next: (pagina) => {
        this.pagina.set(pagina);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected irPara(numeroDaPagina: number): void {
    if (numeroDaPagina < 1 || numeroDaPagina > this.totalDePaginas()) {
      return;
    }

    this.carregar(numeroDaPagina);
  }

  protected confirmarCancelamento(id: string): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.confirmandoId.set(id);
  }

  protected fecharConfirmacao(): void {
    this.confirmandoId.set(null);
  }

  protected cancelar(retirada: RetiradaResumo): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.cancelandoId.set(retirada.id);

    this.servico.cancelar(retirada.id).subscribe({
      next: () => {
        this.cancelandoId.set(null);
        this.confirmandoId.set(null);
        this.mensagem.set('Solicitação cancelada.');
        this.carregar(this.pagina().pagina);
      },
      error: (erro: unknown) => {
        this.cancelandoId.set(null);
        this.confirmandoId.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }
}
