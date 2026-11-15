import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { PontoResumo, PontosService, StatusPonto } from './pontos.service';

const STATUS: { valor: StatusPonto; rotulo: string }[] = [
  { valor: 'Ativo', rotulo: 'Ativo' },
  { valor: 'Inativo', rotulo: 'Inativo' },
  { valor: 'IndisponivelTemporariamente', rotulo: 'Indisponível temporariamente' },
];

@Component({
  selector: 'app-pontos-de-coleta',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-5xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Pontos de coleta</h1>
          <p class="mt-1 text-sm text-slate-600">
            Cadastre e mantenha os pontos de recebimento da organização.
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-4">
          <a routerLink="/organizacao/perfil" class="font-medium text-emerald-700 underline">
            Perfil da organização
          </a>
          <a
            routerLink="/organizacao/pontos/novo"
            class="rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800"
          >
            Novo ponto
          </a>
        </div>
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
        <p class="mt-6 text-slate-600" role="status">Carregando pontos…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else if (pontos().length === 0) {
        <p class="mt-6 rounded-md bg-white p-4 text-slate-600">
          Nenhum ponto cadastrado ainda. Use “Novo ponto” para cadastrar o primeiro.
        </p>
      } @else {
        <div class="mt-6 overflow-x-auto rounded-md bg-white shadow-sm">
          <table class="min-w-full divide-y divide-slate-200">
            <caption class="sr-only">
              Pontos de coleta da organização
            </caption>
            <thead class="bg-slate-50">
              <tr>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Nome
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Município
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Tipos
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
                  Status
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-right text-xs font-semibold uppercase text-slate-600"
                >
                  <span class="sr-only">Ações</span>
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              @for (ponto of pontos(); track ponto.id) {
                <tr>
                  <td class="px-4 py-3 text-sm text-slate-900">{{ ponto.nome }}</td>
                  <td class="px-4 py-3 text-sm text-slate-700">
                    {{ ponto.municipio }} — {{ ponto.uf }}
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-700">
                    {{ ponto.tipos.join(', ') }}
                  </td>
                  <td class="px-4 py-3 text-sm">
                    <span
                      [class]="
                        ponto.abertoAgora ? 'font-medium text-emerald-800' : 'text-slate-500'
                      "
                    >
                      {{ ponto.abertoAgora ? 'Aberto agora' : 'Fechado agora' }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-sm">
                    <select
                      class="rounded-md border border-slate-300 px-2 py-1"
                      [value]="ponto.status"
                      [disabled]="alterandoId() !== null"
                      (change)="alterarStatus(ponto, $event)"
                    >
                      @for (status of statusDisponiveis; track status.valor) {
                        <option [value]="status.valor">{{ status.rotulo }}</option>
                      }
                    </select>
                  </td>
                  <td class="px-4 py-3 text-right text-sm">
                    <div class="flex flex-wrap items-center justify-end gap-3">
                      <a
                        [routerLink]="['/organizacao/pontos', ponto.id, 'editar']"
                        class="font-medium text-emerald-700 underline"
                      >
                        Editar
                      </a>

                      @if (confirmandoId() === ponto.id) {
                        <span class="text-slate-700">Retirar este ponto?</span>
                        <button
                          type="button"
                          class="font-medium text-red-700 underline disabled:opacity-60"
                          [disabled]="retirandoId() === ponto.id"
                          (click)="retirar(ponto)"
                        >
                          {{ retirandoId() === ponto.id ? 'Retirando…' : 'Confirmar' }}
                        </button>
                        <button
                          type="button"
                          class="font-medium text-slate-600 underline"
                          (click)="cancelarRetirada()"
                        >
                          Cancelar
                        </button>
                      } @else {
                        <button
                          type="button"
                          class="font-medium text-red-700 underline"
                          (click)="confirmarRetirada(ponto.id)"
                        >
                          Retirar
                        </button>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
})
export class PontosPage implements OnInit {
  private readonly servico = inject(PontosService);
  private readonly rota = inject(ActivatedRoute);

  protected readonly statusDisponiveis = STATUS;
  protected readonly pontos = signal<PontoResumo[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly alterandoId = signal<string | null>(null);
  protected readonly confirmandoId = signal<string | null>(null);
  protected readonly retirandoId = signal<string | null>(null);

  constructor() {
    const parametros = this.rota.snapshot.queryParamMap;

    if (parametros.get('criado') === '1') {
      this.mensagem.set('Ponto cadastrado.');
    } else if (parametros.get('editado') === '1') {
      this.mensagem.set('Ponto atualizado.');
    }
  }

  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.servico.listar().subscribe({
      next: (pontos) => {
        this.pontos.set(pontos);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected alterarStatus(ponto: PontoResumo, evento: Event): void {
    const status = (evento.target as HTMLSelectElement).value as StatusPonto;

    if (status === ponto.status) {
      return;
    }

    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.alterandoId.set(ponto.id);

    this.servico.alterarStatus(ponto.id, status).subscribe({
      next: (atualizado) => {
        this.alterandoId.set(null);
        this.pontos.update((pontos) =>
          pontos.map((item) =>
            item.id === atualizado.id
              ? { ...item, status: atualizado.status, abertoAgora: atualizado.abertoAgora }
              : item,
          ),
        );
        this.mensagem.set('Status do ponto atualizado.');
      },
      error: (erro: unknown) => {
        this.alterandoId.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
        this.carregar();
      },
    });
  }

  protected confirmarRetirada(id: string): void {
    this.erroDeAcao.set(null);
    this.confirmandoId.set(id);
  }

  protected cancelarRetirada(): void {
    this.confirmandoId.set(null);
  }

  protected retirar(ponto: PontoResumo): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.retirandoId.set(ponto.id);

    this.servico.retirar(ponto.id).subscribe({
      next: () => {
        this.retirandoId.set(null);
        this.confirmandoId.set(null);
        this.mensagem.set('Ponto retirado.');
        this.carregar();
      },
      error: (erro: unknown) => {
        this.retirandoId.set(null);
        this.confirmandoId.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }
}
