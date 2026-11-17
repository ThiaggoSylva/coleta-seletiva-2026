import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { formatarData, formatarDataHora } from '../../../core/retiradas/retiradas.models';
import {
  BloqueioDeData,
  DisponibilidadeDia,
  DisponibilidadeService,
} from './disponibilidade.service';

const DIAS = [
  { valor: 'Monday', rotulo: 'Segunda-feira' },
  { valor: 'Tuesday', rotulo: 'Terça-feira' },
  { valor: 'Wednesday', rotulo: 'Quarta-feira' },
  { valor: 'Thursday', rotulo: 'Quinta-feira' },
  { valor: 'Friday', rotulo: 'Sexta-feira' },
  { valor: 'Saturday', rotulo: 'Sábado' },
  { valor: 'Sunday', rotulo: 'Domingo' },
];

const PERIODOS = [
  { valor: 'Manha', rotulo: 'Manhã' },
  { valor: 'Tarde', rotulo: 'Tarde' },
];

@Component({
  selector: 'app-disponibilidade-de-retirada',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Disponibilidade de retirada</h1>
          <p class="mt-1 text-sm text-slate-600">
            Capacidade semanal por dia e período, além de bloqueios pontuais de data.
          </p>
        </div>
        <a routerLink="/organizacao/retiradas" class="font-medium text-emerald-700 underline">
          Retiradas
        </a>
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
        <p class="mt-6 text-slate-600" role="status">Carregando disponibilidade…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else {
        <div class="cartao mt-6">
          <h2 class="text-lg font-semibold">Capacidade semanal</h2>
          <p class="mt-1 text-sm text-slate-600">
            Informe quantas retiradas a organização consegue atender em cada combinação. Use 0 para
            desativar a combinação.
          </p>

          <div class="mt-4 overflow-x-auto">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead>
                <tr>
                  <th
                    scope="col"
                    class="py-2 text-left text-xs font-semibold uppercase text-slate-600"
                  >
                    Dia
                  </th>
                  @for (periodo of periodos; track periodo.valor) {
                    <th
                      scope="col"
                      class="py-2 text-right text-xs font-semibold uppercase text-slate-600"
                    >
                      {{ periodo.rotulo }}
                    </th>
                  }
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                @for (dia of dias; track dia.valor) {
                  <tr>
                    <th scope="row" class="py-2 text-left font-medium text-slate-800">
                      {{ dia.rotulo }}
                    </th>
                    @for (periodo of periodos; track periodo.valor) {
                      <td class="py-2 text-right">
                        <input
                          type="number"
                          min="0"
                          max="1000"
                          class="w-24 text-right"
                          [value]="capacidade(dia.valor, periodo.valor)"
                          (input)="alterarCapacidade(dia.valor, periodo.valor, $event)"
                        />
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <button
            type="button"
            class="botao-primario mt-4"
            [disabled]="salvando()"
            (click)="salvarCapacidades()"
          >
            {{ salvando() ? 'Salvando…' : 'Salvar capacidade' }}
          </button>
        </div>

        <div class="cartao mt-4">
          <h2 class="text-lg font-semibold">Bloqueios de data</h2>
          <p class="mt-1 text-sm text-slate-600">
            Datas bloqueadas não aparecem para novas solicitações, mesmo que o dia da semana tenha
            capacidade configurada.
          </p>

          <div class="mt-4 flex flex-wrap items-end gap-3">
            <label class="campo">
              <span class="campo-rotulo">Data</span>
              <input
                type="date"
                class="mt-1"
                [value]="novaDataDeBloqueio()"
                (change)="alterarNovaDataDeBloqueio($event)"
              />
            </label>
            <label class="campo">
              <span class="campo-rotulo">Motivo (opcional)</span>
              <input
                class="mt-1"
                maxlength="300"
                [value]="novoMotivoDeBloqueio()"
                (input)="alterarNovoMotivoDeBloqueio($event)"
              />
            </label>
            <button
              type="button"
              class="botao-primario"
              [disabled]="criandoBloqueio()"
              (click)="criarBloqueio()"
            >
              {{ criandoBloqueio() ? 'Bloqueando…' : 'Bloquear data' }}
            </button>
          </div>

          @if (bloqueios().length === 0) {
            <p class="mt-4 text-sm text-slate-600">Nenhuma data bloqueada.</p>
          } @else {
            <ul class="mt-4 divide-y divide-slate-100">
              @for (bloqueio of bloqueios(); track bloqueio.id) {
                <li class="flex flex-wrap items-center justify-between gap-3 py-2 text-sm">
                  <div>
                    <p class="font-medium text-slate-900">{{ formatarData(bloqueio.data) }}</p>
                    <p class="text-xs text-slate-500">
                      @if (bloqueio.motivo !== null) {
                        {{ bloqueio.motivo }} ·
                      }
                      criado em {{ formatarDataHora(bloqueio.criadoEmUtc) }}
                    </p>
                  </div>

                  @if (removendoId() === bloqueio.id) {
                    <span class="flex items-center gap-2">
                      <span class="text-slate-700">Remover bloqueio?</span>
                      <button
                        type="button"
                        class="font-medium text-red-700 underline disabled:opacity-60"
                        [disabled]="removendoId() !== null"
                        (click)="removerBloqueio(bloqueio)"
                      >
                        Confirmar
                      </button>
                      <button
                        type="button"
                        class="font-medium text-slate-600 underline"
                        (click)="removendoId.set(null)"
                      >
                        Voltar
                      </button>
                    </span>
                  } @else {
                    <button
                      type="button"
                      class="font-medium text-red-700 underline"
                      (click)="confirmarRemocao(bloqueio)"
                    >
                      Remover
                    </button>
                  }
                </li>
              }
            </ul>
          }
        </div>
      }
    </section>
  `,
})
export class DisponibilidadePage implements OnInit {
  private readonly servico = inject(DisponibilidadeService);

  protected readonly dias = DIAS;
  protected readonly periodos = PERIODOS;
  protected readonly capacidades = signal<Record<string, number>>({});
  protected readonly bloqueios = signal<BloqueioDeData[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly salvando = signal(false);
  protected readonly criandoBloqueio = signal(false);
  protected readonly removendoId = signal<string | null>(null);

  protected readonly novaDataDeBloqueio = signal('');
  protected readonly novoMotivoDeBloqueio = signal('');

  protected readonly formatarData = formatarData;
  protected readonly formatarDataHora = formatarDataHora;

  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.servico.obter().subscribe({
      next: (dias) => {
        this.definirCapacidades(dias);
        this.carregarBloqueios();
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected capacidade(dia: string, periodo: string): number {
    return this.capacidades()[`${dia}|${periodo}`] ?? 0;
  }

  protected alterarCapacidade(dia: string, periodo: string, evento: Event): void {
    const valor = Number((evento.target as HTMLInputElement).value);

    this.capacidades.update((atuais) => ({
      ...atuais,
      [`${dia}|${periodo}`]: Number.isFinite(valor) ? Math.max(0, valor) : 0,
    }));
  }

  protected salvarCapacidades(): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.salvando.set(true);

    const entradas = Object.entries(this.capacidades())
      .filter(([, capacidade]) => capacidade > 0)
      .map(([chave, capacidade]) => {
        const [diaDaSemana, periodo] = chave.split('|');
        return { diaDaSemana, periodo, capacidade };
      });

    this.servico.definir(entradas).subscribe({
      next: (dias) => {
        this.salvando.set(false);
        this.definirCapacidades(dias);
        this.mensagem.set('Disponibilidade atualizada.');
      },
      error: (erro: unknown) => {
        this.salvando.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  protected alterarNovaDataDeBloqueio(evento: Event): void {
    this.novaDataDeBloqueio.set((evento.target as HTMLInputElement).value);
  }

  protected alterarNovoMotivoDeBloqueio(evento: Event): void {
    this.novoMotivoDeBloqueio.set((evento.target as HTMLInputElement).value);
  }

  protected criarBloqueio(): void {
    const data = this.novaDataDeBloqueio();
    if (data.length === 0) {
      this.erroDeAcao.set('Informe a data a bloquear.');
      return;
    }

    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.criandoBloqueio.set(true);

    this.servico.criarBloqueio(data, vazioComoNulo(this.novoMotivoDeBloqueio())).subscribe({
      next: () => {
        this.criandoBloqueio.set(false);
        this.novaDataDeBloqueio.set('');
        this.novoMotivoDeBloqueio.set('');
        this.mensagem.set('Data bloqueada.');
        this.carregarBloqueios();
      },
      error: (erro: unknown) => {
        this.criandoBloqueio.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  protected confirmarRemocao(bloqueio: BloqueioDeData): void {
    this.erroDeAcao.set(null);
    this.removendoId.set(bloqueio.id);
  }

  protected removerBloqueio(bloqueio: BloqueioDeData): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);

    this.servico.removerBloqueio(bloqueio.id).subscribe({
      next: () => {
        this.removendoId.set(null);
        this.mensagem.set('Bloqueio removido.');
        this.carregarBloqueios();
      },
      error: (erro: unknown) => {
        this.removendoId.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  private carregarBloqueios(): void {
    this.servico.listarBloqueios().subscribe({
      next: (bloqueios) => {
        this.bloqueios.set(bloqueios);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  private definirCapacidades(dias: DisponibilidadeDia[]): void {
    const capacidades: Record<string, number> = {};

    for (const dia of this.dias) {
      for (const periodo of this.periodos) {
        capacidades[`${dia.valor}|${periodo.valor}`] =
          dias.find((item) => item.diaDaSemana === dia.valor && item.periodo === periodo.valor)
            ?.capacidade ?? 0;
      }
    }

    this.capacidades.set(capacidades);
  }
}

function vazioComoNulo(valor: string): string | null {
  const normalizado = valor.trim();
  return normalizado.length === 0 ? null : normalizado;
}
