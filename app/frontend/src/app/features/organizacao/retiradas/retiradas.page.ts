import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { classeDoEstado } from '../../../core/retiradas/estado-visual';
import {
  formatarData,
  formatarDataHora,
  PaginaDeRetiradas,
  rotuloDeEstado,
  rotuloDePeriodo,
} from '../../../core/retiradas/retiradas.models';
import { RetiradasDaOrganizacaoService } from './retiradas.service';

const TAMANHO_DA_PAGINA = 20;

const ESTADOS = [
  { valor: '', rotulo: 'Todos os estados' },
  { valor: 'Solicitada', rotulo: 'Solicitada' },
  { valor: 'Agendada', rotulo: 'Agendada' },
  { valor: 'Coletada', rotulo: 'Coletada' },
  { valor: 'NaoRealizada', rotulo: 'Não realizada' },
  { valor: 'Recusada', rotulo: 'Recusada' },
  { valor: 'Cancelada', rotulo: 'Cancelada' },
];

@Component({
  selector: 'app-retiradas-da-organizacao',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-6xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Retiradas</h1>
          <p class="mt-1 text-sm text-slate-600">
            Fila de solicitações, agendamento e registro da coleta.
          </p>
        </div>
        <div class="flex flex-wrap gap-3 text-sm">
          <a routerLink="/organizacao/volumes" class="font-medium text-emerald-700 underline">
            Volumes
          </a>
          <a routerLink="/organizacao/inicio" class="font-medium text-emerald-700 underline">
            Área da organização
          </a>
        </div>
      </div>

      @if (erroDeAcao() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeAcao() }}
        </p>
      }

      <div class="cartao mt-6">
        <h2 class="text-lg font-semibold">Filtros</h2>
        <div class="mt-3 flex flex-wrap items-end gap-3">
          <label class="campo">
            <span class="campo-rotulo">Estado</span>
            <select class="mt-1" [value]="estado()" (change)="alterarEstado($event)">
              @for (opcao of estados; track opcao.valor) {
                <option [value]="opcao.valor">{{ opcao.rotulo }}</option>
              }
            </select>
          </label>

          <label class="campo">
            <span class="campo-rotulo">Data da retirada</span>
            <input type="date" class="mt-1" [value]="data()" (change)="alterarData($event)" />
          </label>

          <button type="button" class="botao-primario" (click)="aplicarFiltros()">Filtrar</button>
          <button type="button" class="botao-secundario" (click)="limparFiltros()">Limpar</button>
        </div>
      </div>

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando retiradas…</p>
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
        <p class="mt-6 rounded-md bg-white p-4 text-slate-600 shadow-sm">
          Nenhuma solicitação encontrada com os filtros atuais.
        </p>
      } @else {
        <div class="mt-6 overflow-x-auto rounded-md bg-white shadow-sm">
          <table class="min-w-full divide-y divide-slate-200">
            <caption class="sr-only">
              Fila de retiradas da organização
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
                  Município / bairro
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Situação
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Solicitada em
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
                    <span class="block">{{ retirada.municipioNome }}</span>
                    <span class="block text-xs text-slate-500">{{ retirada.bairro }}</span>
                  </td>
                  <td class="px-4 py-3 text-sm">
                    <span class="etiqueta" [class]="classeDoEstado(retirada.estado)">
                      {{ rotuloDeEstado(retirada.estado) }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-600">
                    {{ formatarDataHora(retirada.criadoEmUtc) }}
                  </td>
                  <td class="px-4 py-3 text-right text-sm">
                    <a
                      [routerLink]="['/organizacao/retiradas', retirada.id]"
                      class="font-medium text-emerald-700 underline"
                    >
                      Abrir
                    </a>
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
export class RetiradasDaOrganizacaoPage implements OnInit {
  private readonly servico = inject(RetiradasDaOrganizacaoService);

  protected readonly estados = ESTADOS;
  protected readonly estado = signal('');
  protected readonly data = signal('');

  protected readonly pagina = signal<PaginaDeRetiradas>({
    itens: [],
    pagina: 1,
    tamanhoPagina: TAMANHO_DA_PAGINA,
    total: 0,
  });
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);

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

  protected alterarEstado(evento: Event): void {
    this.estado.set((evento.target as HTMLSelectElement).value);
  }

  protected alterarData(evento: Event): void {
    this.data.set((evento.target as HTMLInputElement).value);
  }

  protected aplicarFiltros(): void {
    this.erroDeAcao.set(null);
    this.carregar(1);
  }

  protected limparFiltros(): void {
    this.estado.set('');
    this.data.set('');
    this.carregar(1);
  }

  protected carregar(numeroDaPagina: number): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.servico
      .listar({
        estado: this.estado(),
        data: this.data(),
        page: numeroDaPagina,
        pageSize: TAMANHO_DA_PAGINA,
      })
      .subscribe({
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
}
