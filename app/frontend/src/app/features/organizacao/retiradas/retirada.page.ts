import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { classeDoEstado } from '../../../core/retiradas/estado-visual';
import {
  formatarData,
  formatarDataHora,
  RetiradaDetalhe,
  ROTULOS_DE_MOTIVO_NAO_REALIZACAO,
  rotuloDeEstado,
  rotuloDeMotivoNaoRealizacao,
  rotuloDePeriodo,
} from '../../../core/retiradas/retiradas.models';
import { RetiradasDaOrganizacaoService } from './retiradas.service';

const MOTIVOS_DE_NAO_REALIZACAO = Object.entries(ROTULOS_DE_MOTIVO_NAO_REALIZACAO).map(
  ([valor, rotulo]) => ({ valor, rotulo }),
);

@Component({
  selector: 'app-retirada-da-organizacao',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <a routerLink="/organizacao/retiradas" class="text-sm font-medium text-emerald-700 underline">
        ← Retiradas
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
              {{ formatarData(item.data) }} · {{ rotuloDePeriodo(item.periodo) }} ·
              {{ item.municipioNome }}
            </p>
          </div>
          <span class="etiqueta" [class]="classeDoEstado(item.estado)">
            {{ rotuloDeEstado(item.estado) }}
          </span>
        </div>

        <div class="mt-6 grid gap-4 md:grid-cols-2">
          <div class="cartao">
            <h2 class="text-lg font-semibold">Cidadão e endereço</h2>

            @if (item.dadosPessoaisOcultos) {
              <p class="mt-3 rounded-md bg-amber-50 p-3 text-sm text-amber-900">
                Dados pessoais e endereço ficam visíveis apenas enquanto a solicitação está aberta
                (RN-11).
              </p>
            } @else {
              <dl class="mt-3 space-y-2 text-sm">
                <div>
                  <dt class="text-slate-600">Nome</dt>
                  <dd class="font-medium text-slate-900">{{ item.nomeCidadao ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-slate-600">Telefone</dt>
                  <dd class="text-slate-900">{{ item.telefoneCidadao ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-slate-600">Endereço</dt>
                  <dd class="text-slate-900">
                    {{ item.rua ?? '—' }}, {{ item.numero ?? '—' }}
                    @if (item.complemento !== null) {
                      — {{ item.complemento }}
                    }
                    <span class="block">{{ item.bairro ?? '—' }} · CEP {{ item.cep ?? '—' }}</span>
                  </dd>
                </div>
                @if (item.pontoReferencia !== null) {
                  <div>
                    <dt class="text-slate-600">Referência</dt>
                    <dd class="text-slate-900">{{ item.pontoReferencia }}</dd>
                  </div>
                }
              </dl>
            }

            @if (item.observacoes !== null) {
              <p class="mt-3 text-sm text-slate-600">Observações: {{ item.observacoes }}</p>
            }

            @if (item.motivo !== null) {
              <p class="mt-3 text-sm text-slate-600">
                Motivo registrado: {{ rotuloDeMotivoNaoRealizacao(item.motivo) }}
              </p>
            }
          </div>

          <div class="cartao">
            <h2 class="text-lg font-semibold">Resíduos estimados</h2>
            <ul class="mt-3 space-y-2 text-sm">
              @for (residuo of item.itens; track residuo.tipoResiduoId) {
                <li class="flex justify-between gap-3">
                  <span class="text-slate-700">{{ residuo.nome }}</span>
                  <span class="font-medium">
                    {{ residuo.quantidadeEstimada }} {{ residuo.unidade }}
                  </span>
                </li>
              }
            </ul>

            @if (item.volumes.length > 0) {
              <h3 class="mt-4 text-sm font-semibold text-slate-800">Volumes coletados</h3>
              <ul class="mt-2 space-y-1 text-sm">
                @for (volume of item.volumes; track volume.tipoResiduoId) {
                  <li class="flex justify-between gap-3">
                    <span class="text-slate-700">{{ volume.nome }}</span>
                    <span class="font-medium"
                      >{{ volume.quantidadeLiquida }} {{ volume.unidade }}</span
                    >
                  </li>
                }
              </ul>
            }
          </div>
        </div>

        @if (item.estado === 'Solicitada') {
          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Agendar retirada</h2>
            <p class="mt-1 text-sm text-slate-600">
              A data deve estar entre amanhã e 30 dias, conforme a disponibilidade da organização.
            </p>
            <div class="mt-3 flex flex-wrap items-end gap-3">
              <label class="campo">
                <span class="campo-rotulo">Data</span>
                <input
                  type="date"
                  class="mt-1"
                  [value]="novaData()"
                  (change)="alterarNovaData($event)"
                />
              </label>
              <label class="campo">
                <span class="campo-rotulo">Período</span>
                <select class="mt-1" [value]="novoPeriodo()" (change)="alterarNovoPeriodo($event)">
                  <option value="Manha">Manhã</option>
                  <option value="Tarde">Tarde</option>
                </select>
              </label>
              <button
                type="button"
                class="botao-primario"
                [disabled]="processando() !== null"
                (click)="agendar()"
              >
                {{ processando() === 'agendar' ? 'Agendando…' : 'Agendar' }}
              </button>
            </div>
          </div>

          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Recusar solicitação</h2>
            <p class="mt-1 text-sm text-slate-600">
              Informe o motivo para registrar a recusa e notificar o cidadão.
            </p>
            <textarea
              rows="3"
              class="mt-3 w-full"
              maxlength="500"
              placeholder="Motivo da recusa"
              [value]="motivoDaRecusa()"
              (input)="alterarMotivoDaRecusa($event)"
            ></textarea>
            <button
              type="button"
              class="botao-secundario mt-3 !border-red-300 !text-red-800 hover:!bg-red-50"
              [disabled]="processando() !== null"
              (click)="recusar()"
            >
              {{ processando() === 'recusar' ? 'Recusando…' : 'Recusar solicitação' }}
            </button>
          </div>
        }

        @if (item.estado === 'Agendada') {
          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Reagendar</h2>
            <div class="mt-3 flex flex-wrap items-end gap-3">
              <label class="campo">
                <span class="campo-rotulo">Nova data</span>
                <input
                  type="date"
                  class="mt-1"
                  [value]="novaData()"
                  (change)="alterarNovaData($event)"
                />
              </label>
              <label class="campo">
                <span class="campo-rotulo">Período</span>
                <select class="mt-1" [value]="novoPeriodo()" (change)="alterarNovoPeriodo($event)">
                  <option value="Manha">Manhã</option>
                  <option value="Tarde">Tarde</option>
                </select>
              </label>
              <button
                type="button"
                class="botao-secundario"
                [disabled]="processando() !== null"
                (click)="reagendar()"
              >
                {{ processando() === 'reagendar' ? 'Reagendando…' : 'Reagendar' }}
              </button>
            </div>
          </div>

          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Registrar coleta</h2>
            <p class="mt-1 text-sm text-slate-600">
              Informe a quantidade efetivamente coletada por tipo. Os valores são gravados como
              volumes da organização.
            </p>
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
                      class="py-2 text-right text-xs font-semibold uppercase text-slate-600"
                    >
                      Quantidade ({{ item.itens[0]?.unidade ?? 'unidade' }})
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (residuo of item.itens; track residuo.tipoResiduoId) {
                    <tr>
                      <td class="py-2 text-slate-900">
                        {{ residuo.nome }}
                        <span class="block text-xs text-slate-500">
                          Estimado: {{ residuo.quantidadeEstimada }} {{ residuo.unidade }}
                        </span>
                      </td>
                      <td class="py-2 text-right">
                        <input
                          type="number"
                          min="0.001"
                          step="0.001"
                          class="w-28 text-right"
                          [value]="quantidadeExecutada(residuo.tipoResiduoId)"
                          (input)="alterarQuantidadeExecutada(residuo.tipoResiduoId, $event)"
                        />
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            <button
              type="button"
              class="botao-primario mt-3"
              [disabled]="processando() !== null"
              (click)="executar()"
            >
              {{ processando() === 'executar' ? 'Registrando…' : 'Marcar como coletada' }}
            </button>
          </div>

          <div class="mt-4 cartao">
            <h2 class="text-lg font-semibold">Marcar como não realizada</h2>
            <div class="mt-3 flex flex-wrap items-end gap-3">
              <label class="campo">
                <span class="campo-rotulo">Motivo</span>
                <select
                  class="mt-1"
                  [value]="motivoNaoRealizacao()"
                  (change)="alterarMotivoNaoRealizacao($event)"
                >
                  @for (motivo of motivosDeNaoRealizacao; track motivo.valor) {
                    <option [value]="motivo.valor">{{ motivo.rotulo }}</option>
                  }
                </select>
              </label>
              <button
                type="button"
                class="botao-secundario"
                [disabled]="processando() !== null"
                (click)="marcarNaoRealizada()"
              >
                {{
                  processando() === 'nao-realizada' ? 'Registrando…' : 'Confirmar não realização'
                }}
              </button>
            </div>
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
      }
    </section>
  `,
})
export class RetiradaDaOrganizacaoPage implements OnInit {
  private readonly servico = inject(RetiradasDaOrganizacaoService);
  private readonly rota = inject(ActivatedRoute);

  protected readonly motivosDeNaoRealizacao = MOTIVOS_DE_NAO_REALIZACAO;
  protected readonly retirada = signal<RetiradaDetalhe | null>(null);
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly processando = signal<string | null>(null);

  protected readonly novaData = signal('');
  protected readonly novoPeriodo = signal('Manha');
  protected readonly motivoDaRecusa = signal('');
  protected readonly motivoNaoRealizacao = signal('CidadaoAusente');
  protected readonly execucao = signal<Record<string, number>>({});

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
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.servico.obter(this.id).subscribe({
      next: (retirada) => {
        this.aplicar(retirada);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected alterarNovaData(evento: Event): void {
    this.novaData.set((evento.target as HTMLInputElement).value);
  }

  protected alterarNovoPeriodo(evento: Event): void {
    this.novoPeriodo.set((evento.target as HTMLSelectElement).value);
  }

  protected alterarMotivoDaRecusa(evento: Event): void {
    this.motivoDaRecusa.set((evento.target as HTMLTextAreaElement).value);
  }

  protected alterarMotivoNaoRealizacao(evento: Event): void {
    this.motivoNaoRealizacao.set((evento.target as HTMLSelectElement).value);
  }

  protected quantidadeExecutada(tipoResiduoId: string): number {
    return this.execucao()[tipoResiduoId] ?? 0;
  }

  protected alterarQuantidadeExecutada(tipoResiduoId: string, evento: Event): void {
    const quantidade = Number((evento.target as HTMLInputElement).value);

    this.execucao.update((atuais) => ({ ...atuais, [tipoResiduoId]: quantidade }));
  }

  protected agendar(): void {
    if (this.novaData().length === 0) {
      this.erroDeAcao.set('Informe a data do agendamento.');
      return;
    }

    this.executarAcao(
      'agendar',
      this.servico.agendar(this.id, this.novaData(), this.novoPeriodo()),
      'Retirada agendada.',
    );
  }

  protected reagendar(): void {
    if (this.novaData().length === 0) {
      this.erroDeAcao.set('Informe a nova data.');
      return;
    }

    this.executarAcao(
      'reagendar',
      this.servico.reagendar(this.id, this.novaData(), this.novoPeriodo()),
      'Retirada reagendada.',
    );
  }

  protected recusar(): void {
    const motivo = this.motivoDaRecusa().trim();
    if (motivo.length === 0) {
      this.erroDeAcao.set('Informe o motivo da recusa.');
      return;
    }

    this.executarAcao('recusar', this.servico.recusar(this.id, motivo), 'Solicitação recusada.');
  }

  protected executar(): void {
    const itens = Object.entries(this.execucao())
      .filter(([, quantidade]) => Number.isFinite(quantidade) && quantidade > 0)
      .map(([tipoResiduoId, quantidade]) => ({ tipoResiduoId, quantidade }));

    if (itens.length === 0) {
      this.erroDeAcao.set('Informe a quantidade coletada de ao menos um tipo.');
      return;
    }

    this.executarAcao(
      'executar',
      this.servico.executar(this.id, itens),
      'Coleta registrada e volumes gerados.',
    );
  }

  protected marcarNaoRealizada(): void {
    this.executarAcao(
      'nao-realizada',
      this.servico.marcarNaoRealizada(this.id, this.motivoNaoRealizacao()),
      'Retirada marcada como não realizada.',
    );
  }

  private executarAcao(
    chave: string,
    requisicao: ReturnType<RetiradasDaOrganizacaoService['agendar']>,
    sucesso: string,
  ): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.processando.set(chave);

    requisicao.subscribe({
      next: (retirada) => {
        this.processando.set(null);
        this.motivoDaRecusa.set('');
        this.aplicar(retirada);
        this.mensagem.set(sucesso);
      },
      error: (erro: unknown) => {
        this.processando.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  private aplicar(retirada: RetiradaDetalhe): void {
    this.retirada.set(retirada);
    this.novaData.set(retirada.data);
    this.novoPeriodo.set(retirada.periodo);

    const execucao: Record<string, number> = {};
    for (const item of retirada.itens) {
      execucao[item.tipoResiduoId] = item.quantidadeEstimada;
    }

    this.execucao.set(execucao);
  }
}
