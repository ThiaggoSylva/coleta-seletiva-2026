import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { classeDoEstado } from '../../../core/retiradas/estado-visual';
import {
  formatarData,
  formatarDataHora,
  RetiradaDetalhe,
  rotuloDeEstado,
  rotuloDeMotivoNaoRealizacao,
  rotuloDePeriodo,
} from '../../../core/retiradas/retiradas.models';
import { RetiradasDoCidadaoService } from './retiradas.service';

@Component({
  selector: 'app-minha-retirada',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <a routerLink="/cidadao/retiradas" class="text-sm font-medium text-emerald-700 underline">
        ← Minhas retiradas
      </a>

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
        <p class="mt-6 text-slate-600" role="status">Carregando solicitação…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else if (retirada() !== null) {
        @let item = retirada()!;

        <div class="mt-4 flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 class="text-2xl font-semibold">Solicitação de retirada</h1>
            <p class="mt-1 text-sm text-slate-600">
              {{ item.organizacaoNome }} · {{ formatarData(item.data) }} ·
              {{ rotuloDePeriodo(item.periodo) }}
            </p>
          </div>
          <span class="etiqueta" [class]="classeDoEstado(item.estado)">
            {{ rotuloDeEstado(item.estado) }}
          </span>
        </div>

        <div class="mt-6 grid gap-4 md:grid-cols-2">
          <div class="cartao">
            <h2 class="text-lg font-semibold">Retirada</h2>
            <dl class="mt-3 space-y-2 text-sm">
              <div class="flex justify-between gap-3">
                <dt class="text-slate-600">Data</dt>
                <dd class="font-medium">{{ formatarData(item.data) }}</dd>
              </div>
              <div class="flex justify-between gap-3">
                <dt class="text-slate-600">Período</dt>
                <dd class="font-medium">{{ rotuloDePeriodo(item.periodo) }}</dd>
              </div>
              <div class="flex justify-between gap-3">
                <dt class="text-slate-600">Solicitada em</dt>
                <dd>{{ formatarDataHora(item.criadoEmUtc) }}</dd>
              </div>
              <div class="flex justify-between gap-3">
                <dt class="text-slate-600">Atualizada em</dt>
                <dd>{{ formatarDataHora(item.atualizadoEmUtc) }}</dd>
              </div>
              @if (item.motivo !== null) {
                <div class="flex justify-between gap-3">
                  <dt class="text-slate-600">Motivo</dt>
                  <dd class="text-right">{{ rotuloDeMotivoNaoRealizacao(item.motivo) }}</dd>
                </div>
              }
            </dl>
          </div>

          <div class="cartao">
            <h2 class="text-lg font-semibold">Endereço de retirada</h2>
            <p class="mt-3 text-sm text-slate-700">
              {{ item.rua ?? '—' }}, {{ item.numero ?? '—' }}
              @if (item.complemento !== null) {
                — {{ item.complemento }}
              }
            </p>
            <p class="text-sm text-slate-700">
              {{ item.bairro ?? '—' }} · {{ item.municipioNome }}
            </p>
            <p class="text-sm text-slate-700">CEP {{ item.cep ?? '—' }}</p>
            @if (item.pontoReferencia !== null) {
              <p class="mt-2 text-sm text-slate-600">Referência: {{ item.pontoReferencia }}</p>
            }
            @if (item.observacoes !== null) {
              <p class="mt-2 text-sm text-slate-600">Observações: {{ item.observacoes }}</p>
            }
          </div>
        </div>

        <div class="mt-4 cartao">
          <h2 class="text-lg font-semibold">Resíduos informados</h2>
          <div class="mt-3 overflow-x-auto">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead>
                <tr>
                  <th
                    scope="col"
                    class="py-2 text-left text-xs font-semibold uppercase text-slate-600"
                  >
                    Tipo
                  </th>
                  <th
                    scope="col"
                    class="py-2 text-left text-xs font-semibold uppercase text-slate-600"
                  >
                    Unidade
                  </th>
                  <th
                    scope="col"
                    class="py-2 text-right text-xs font-semibold uppercase text-slate-600"
                  >
                    Quantidade estimada
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                @for (residuo of item.itens; track residuo.tipoResiduoId) {
                  <tr>
                    <td class="py-2 text-slate-900">{{ residuo.nome }}</td>
                    <td class="py-2 text-slate-600">{{ residuo.unidade }}</td>
                    <td class="py-2 text-right text-slate-900">
                      {{ residuo.quantidadeEstimada }} {{ residuo.unidade }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>

        @if (item.volumes.length > 0) {
          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Volumes coletados</h2>
            <ul class="mt-3 space-y-2 text-sm">
              @for (volume of item.volumes; track volume.tipoResiduoId) {
                <li class="flex justify-between gap-3">
                  <span class="text-slate-700">{{ volume.nome }}</span>
                  <span class="font-medium">
                    {{ volume.quantidadeLiquida }} {{ volume.unidade }}
                  </span>
                </li>
              }
            </ul>
          </div>
        }

        <div class="mt-4 cartao">
          <h2 class="text-lg font-semibold">Histórico</h2>
          <ol class="mt-3 space-y-3 text-sm">
            @for (evento of item.historico; track evento.ocorridoEmUtc + evento.estadoNovo) {
              <li class="border-l-2 border-emerald-200 pl-3">
                <p class="font-medium text-slate-900">
                  {{ rotuloDeEstado(evento.estadoNovo) }}
                </p>
                <p class="text-xs text-slate-500">
                  {{ formatarDataHora(evento.ocorridoEmUtc) }}
                </p>
                @if (evento.motivo !== null) {
                  <p class="text-slate-600">{{ evento.motivo }}</p>
                }
              </li>
            }
          </ol>
        </div>

        @if (item.estado === 'Solicitada' || item.estado === 'Agendada') {
          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Cancelamento</h2>
            <p class="mt-1 text-sm text-slate-600">
              O cancelamento pode ser feito até 24 horas antes do início do período de retirada.
            </p>

            @if (confirmando()) {
              <div class="mt-3 flex flex-wrap items-center gap-3 text-sm">
                <span>Cancelar esta solicitação?</span>
                <button
                  type="button"
                  class="font-medium text-red-700 underline disabled:opacity-60"
                  [disabled]="cancelando()"
                  (click)="cancelar()"
                >
                  {{ cancelando() ? 'Cancelando…' : 'Confirmar cancelamento' }}
                </button>
                <button
                  type="button"
                  class="font-medium text-slate-600 underline"
                  (click)="confirmando.set(false)"
                >
                  Voltar
                </button>
              </div>
            } @else {
              <button
                type="button"
                class="botao-secundario mt-3 !border-red-300 !text-red-800 hover:!bg-red-50"
                (click)="confirmando.set(true)"
              >
                Cancelar solicitação
              </button>
            }
          </div>
        }
      }
    </section>
  `,
})
export class MinhaRetiradaPage implements OnInit {
  private readonly servico = inject(RetiradasDoCidadaoService);
  private readonly rota = inject(ActivatedRoute);

  protected readonly retirada = signal<RetiradaDetalhe | null>(null);
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly confirmando = signal(false);
  protected readonly cancelando = signal(false);

  protected readonly formatarData = formatarData;
  protected readonly formatarDataHora = formatarDataHora;
  protected readonly rotuloDePeriodo = rotuloDePeriodo;
  protected readonly rotuloDeEstado = rotuloDeEstado;
  protected readonly rotuloDeMotivoNaoRealizacao = rotuloDeMotivoNaoRealizacao;
  protected readonly classeDoEstado = classeDoEstado;

  private get id(): string {
    return this.rota.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    if (this.rota.snapshot.queryParamMap.get('criada') === '1') {
      this.mensagem.set('Solicitação registrada. A organização fará a confirmação.');
    }

    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.servico.obter(this.id).subscribe({
      next: (retirada) => {
        this.retirada.set(retirada);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected cancelar(): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.cancelando.set(true);

    this.servico.cancelar(this.id).subscribe({
      next: (retirada) => {
        this.cancelando.set(false);
        this.confirmando.set(false);
        this.retirada.set(retirada);
        this.mensagem.set('Solicitação cancelada.');
      },
      error: (erro: unknown) => {
        this.cancelando.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }
}
