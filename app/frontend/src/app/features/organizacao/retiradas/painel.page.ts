import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { formatarData, rotuloDeEstado } from '../../../core/retiradas/retiradas.models';
import { CatalogoService, TipoResiduo } from '../../../core/servicos/catalogo.service';
import { classeDoEstado } from '../../../core/retiradas/estado-visual';
import { PainelService, ResumoDoPainel, SerieDoPainel } from './painel.service';

const ORIGENS = [
  { valor: '', rotulo: 'Todas as origens' },
  { valor: 'Retirada', rotulo: 'Retirada' },
  { valor: 'RecebimentoEmPonto', rotulo: 'Recebimento em ponto' },
  { valor: 'Ajuste', rotulo: 'Ajuste' },
];

const AGRUPAMENTOS = [
  { valor: 'dia', rotulo: 'Por dia' },
  { valor: 'semana', rotulo: 'Por semana' },
  { valor: 'mes', rotulo: 'Por mês' },
];

const ROTULOS_DE_ORIGEM: Record<string, string> = {
  Retirada: 'Retirada',
  RecebimentoEmPonto: 'Recebimento em ponto',
  Ajuste: 'Ajuste',
};

function dataIso(data: Date): string {
  const ano = data.getFullYear();
  const mes = `${data.getMonth() + 1}`.padStart(2, '0');
  const dia = `${data.getDate()}`.padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
}

@Component({
  selector: 'app-painel-de-volumes',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-6xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Painel de volumes</h1>
          <p class="mt-1 text-sm text-slate-600">
            Totais por tipo, origem e ponto, além da evolução por período.
          </p>
        </div>
        <div class="flex flex-wrap gap-3 text-sm">
          <a routerLink="/organizacao/volumes" class="font-medium text-emerald-700 underline">
            Volumes
          </a>
          <button
            type="button"
            class="font-medium text-emerald-700 underline disabled:opacity-60"
            [disabled]="exportando()"
            (click)="exportarCsv()"
          >
            {{ exportando() ? 'Gerando CSV…' : 'Baixar CSV' }}
          </button>
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

      <div class="cartao mt-6">
        <h2 class="text-lg font-semibold">Filtros</h2>
        <div class="mt-3 flex flex-wrap items-end gap-3">
          <label class="campo">
            <span class="campo-rotulo">De</span>
            <input type="date" class="mt-1" [value]="de()" (change)="alterarDe($event)" />
          </label>
          <label class="campo">
            <span class="campo-rotulo">Até</span>
            <input type="date" class="mt-1" [value]="ate()" (change)="alterarAte($event)" />
          </label>
          <label class="campo">
            <span class="campo-rotulo">Tipo</span>
            <select class="mt-1" [value]="tipoFiltro()" (change)="alterarTipoFiltro($event)">
              <option value="">Todos os tipos</option>
              @for (tipo of tipos(); track tipo.id) {
                <option [value]="tipo.id">{{ tipo.nome }}</option>
              }
            </select>
          </label>
          <label class="campo">
            <span class="campo-rotulo">Origem</span>
            <select class="mt-1" [value]="origem()" (change)="alterarOrigem($event)">
              @for (opcao of origens; track opcao.valor) {
                <option [value]="opcao.valor">{{ opcao.rotulo }}</option>
              }
            </select>
          </label>
          <label class="campo">
            <span class="campo-rotulo">Agrupamento</span>
            <select class="mt-1" [value]="agrupamento()" (change)="alterarAgrupamento($event)">
              @for (opcao of agrupamentos; track opcao.valor) {
                <option [value]="opcao.valor">{{ opcao.rotulo }}</option>
              }
            </select>
          </label>
          <button type="button" class="botao-primario" (click)="carregar()">Atualizar</button>
        </div>
      </div>

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando painel…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else if (resumo() !== null && serie() !== null) {
        @let dados = resumo()!;

        <p class="mt-6 text-sm text-slate-600">
          Período: {{ formatarData(dados.de) }} a {{ formatarData(dados.ate) }}
        </p>

        <div class="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          @for (total of dados.totaisPorUnidade; track total.unidade) {
            <div class="cartao">
              <p class="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Total em {{ total.unidade }}
              </p>
              <p class="mt-2 text-3xl font-semibold text-emerald-900">{{ total.quantidade }}</p>
            </div>
          }
          @if (dados.totaisPorUnidade.length === 0) {
            <p class="text-sm text-slate-600">Nenhum volume registrado no período.</p>
          }
        </div>

        <div class="mt-4 grid gap-4 lg:grid-cols-2">
          <div class="cartao">
            <h2 class="text-lg font-semibold">Volumes por tipo</h2>
            @if (dados.porTipo.length === 0) {
              <p class="mt-3 text-sm text-slate-600">Sem volumes no período.</p>
            } @else {
              <ul class="mt-3 space-y-3 text-sm">
                @for (tipo of dados.porTipo; track tipo.tipoResiduoId) {
                  <li>
                    <div class="flex items-baseline justify-between gap-3">
                      <span class="text-slate-700">{{ tipo.nome }}</span>
                      <span class="font-medium">{{ tipo.quantidade }} {{ tipo.unidade }}</span>
                    </div>
                    <div class="mt-1 h-2 overflow-hidden rounded-full bg-slate-100">
                      <div
                        class="h-full rounded-full bg-emerald-600"
                        [style.width.%]="percentualDoTipo(tipo.quantidade)"
                      ></div>
                    </div>
                  </li>
                }
              </ul>
            }
          </div>

          <div class="cartao">
            <h2 class="text-lg font-semibold">Retiradas por estado</h2>
            @if (dados.retiradasPorEstado.length === 0) {
              <p class="mt-3 text-sm text-slate-600">Sem retiradas no período.</p>
            } @else {
              <ul class="mt-3 flex flex-wrap gap-2">
                @for (item of dados.retiradasPorEstado; track item.estado) {
                  <li class="etiqueta" [class]="classeDoEstado(item.estado)">
                    {{ rotuloDeEstado(item.estado) }}: {{ item.quantidade }}
                  </li>
                }
              </ul>
            }

            <h3 class="mt-5 text-sm font-semibold text-slate-800">Volumes por origem</h3>
            @if (dados.porOrigem.length === 0) {
              <p class="mt-2 text-sm text-slate-600">Sem volumes no período.</p>
            } @else {
              <ul class="mt-2 space-y-1 text-sm">
                @for (item of dados.porOrigem; track item.origem + item.unidade) {
                  <li class="flex justify-between gap-3">
                    <span class="text-slate-700">{{ rotuloDeOrigem(item.origem) }}</span>
                    <span class="font-medium">{{ item.quantidade }} {{ item.unidade }}</span>
                  </li>
                }
              </ul>
            }
          </div>
        </div>

        @if (dados.porPonto.length > 0) {
          <div class="cartao mt-4">
            <h2 class="text-lg font-semibold">Volumes por ponto</h2>
            <ul class="mt-3 space-y-1 text-sm">
              @for (item of dados.porPonto; track item.pontoId + item.unidade) {
                <li class="flex justify-between gap-3">
                  <span class="text-slate-700">{{ item.pontoNome }}</span>
                  <span class="font-medium">{{ item.quantidade }} {{ item.unidade }}</span>
                </li>
              }
            </ul>
          </div>
        }

        <div class="cartao mt-4">
          <h2 class="text-lg font-semibold">Evolução</h2>
          @if (serie()!.pontos.length === 0) {
            <p class="mt-3 text-sm text-slate-600">Sem volumes no período.</p>
          } @else {
            <div class="mt-3 overflow-x-auto">
              <table class="min-w-full divide-y divide-slate-200 text-sm">
                <thead>
                  <tr>
                    <th
                      scope="col"
                      class="py-2 text-left text-xs font-semibold uppercase text-slate-600"
                    >
                      Início
                    </th>
                    <th
                      scope="col"
                      class="py-2 text-left text-xs font-semibold uppercase text-slate-600"
                    >
                      Volumes
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (ponto of serie()!.pontos; track ponto.inicio) {
                    <tr>
                      <td class="py-2 text-slate-900">{{ formatarData(ponto.inicio) }}</td>
                      <td class="py-2 text-slate-700">
                        @for (item of ponto.itens; track item.tipoResiduoId) {
                          <span class="mr-3 inline-block whitespace-nowrap">
                            {{ item.nome }}: {{ item.quantidade }} {{ item.unidade }}
                          </span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }
    </section>
  `,
})
export class PainelPage implements OnInit {
  private readonly servico = inject(PainelService);
  private readonly catalogo = inject(CatalogoService);

  protected readonly origens = ORIGENS;
  protected readonly agrupamentos = AGRUPAMENTOS;

  protected readonly hoje = new Date();
  protected readonly de = signal(dataIso(new Date(Date.now() - 29 * 86_400_000)));
  protected readonly ate = signal(dataIso(new Date()));
  protected readonly tipoFiltro = signal('');
  protected readonly origem = signal('');
  protected readonly agrupamento = signal('dia');

  protected readonly tipos = signal<TipoResiduo[]>([]);
  protected readonly resumo = signal<ResumoDoPainel | null>(null);
  protected readonly serie = signal<SerieDoPainel | null>(null);
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly exportando = signal(false);

  protected readonly formatarData = formatarData;
  protected readonly rotuloDeEstado = rotuloDeEstado;
  protected readonly classeDoEstado = classeDoEstado;

  private readonly maiorVolume = computed(() =>
    Math.max(0, ...(this.resumo()?.porTipo ?? []).map((item) => item.quantidade)),
  );

  ngOnInit(): void {
    this.catalogo.listarTipos().subscribe({
      next: (tipos) => this.tipos.set(tipos),
      error: () => undefined,
    });

    this.carregar();
  }

  protected alterarDe(evento: Event): void {
    this.de.set((evento.target as HTMLInputElement).value);
  }

  protected alterarAte(evento: Event): void {
    this.ate.set((evento.target as HTMLInputElement).value);
  }

  protected alterarTipoFiltro(evento: Event): void {
    this.tipoFiltro.set((evento.target as HTMLSelectElement).value);
  }

  protected alterarOrigem(evento: Event): void {
    this.origem.set((evento.target as HTMLSelectElement).value);
  }

  protected alterarAgrupamento(evento: Event): void {
    this.agrupamento.set((evento.target as HTMLSelectElement).value);
  }

  protected rotuloDeOrigem(origem: string): string {
    return ROTULOS_DE_ORIGEM[origem] ?? origem;
  }

  protected percentualDoTipo(quantidade: number): number {
    const maior = this.maiorVolume();
    return maior <= 0 ? 0 : Math.max(2, Math.round((quantidade / maior) * 100));
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);
    this.mensagem.set(null);

    const filtro = {
      de: this.de(),
      ate: this.ate(),
      tipoResiduoId: this.tipoFiltro(),
      origem: this.origem(),
    };

    this.servico.obterResumo(filtro).subscribe({
      next: (resumo) => {
        this.resumo.set(resumo);
        this.servico.obterSerie(filtro, this.agrupamento()).subscribe({
          next: (serie) => {
            this.serie.set(serie);
            this.carregando.set(false);
          },
          error: (erro: unknown) => {
            this.carregando.set(false);
            this.erroDeCarga.set(mensagemDeErro(erro));
          },
        });
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected exportarCsv(): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.exportando.set(true);

    this.servico
      .exportarCsv({
        de: this.de(),
        ate: this.ate(),
        tipoResiduoId: this.tipoFiltro(),
        origem: this.origem(),
      })
      .subscribe({
        next: (resposta) => {
          this.exportando.set(false);

          const cabecalho = resposta.headers.get('content-disposition') ?? '';
          const encontrado = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(cabecalho);
          const nome = encontrado?.[1] ?? `volumes-${this.de()}-${this.ate()}.csv`;
          const conteudo = resposta.body;

          if (conteudo === null) {
            this.erroDeAcao.set('Não foi possível gerar o arquivo.');
            return;
          }

          const url = URL.createObjectURL(conteudo);
          const link = document.createElement('a');
          link.href = url;
          link.download = decodeURIComponent(nome);
          link.click();
          URL.revokeObjectURL(url);
          this.mensagem.set('Arquivo CSV gerado.');
        },
        error: (erro: unknown) => {
          this.exportando.set(false);
          this.erroDeAcao.set(mensagemDeErro(erro));
        },
      });
  }
}
