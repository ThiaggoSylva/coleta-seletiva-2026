import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { formatarData, formatarDataHora } from '../../../core/retiradas/retiradas.models';
import { CatalogoService, TipoResiduo } from '../../../core/servicos/catalogo.service';
import { OpcaoDePonto, PontosService } from '../pontos/pontos.service';
import { OrigemDoVolume, PaginaDeVolumes, VolumeResposta, VolumesService } from './volumes.service';

const TAMANHO_DA_PAGINA = 20;

const ORIGENS = [
  { valor: '', rotulo: 'Todas as origens' },
  { valor: 'Retirada', rotulo: 'Retirada' },
  { valor: 'RecebimentoEmPonto', rotulo: 'Recebimento em ponto' },
  { valor: 'Ajuste', rotulo: 'Ajuste' },
];

const ROTULOS_DE_ORIGEM: Record<string, string> = {
  Retirada: 'Retirada',
  RecebimentoEmPonto: 'Recebimento em ponto',
  Ajuste: 'Ajuste',
};

@Component({
  selector: 'app-volumes-da-organizacao',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-6xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Volumes</h1>
          <p class="mt-1 text-sm text-slate-600">
            Recebimentos em pontos, volumes de retiradas e ajustes com rastreabilidade.
          </p>
        </div>
        <div class="flex flex-wrap gap-3 text-sm">
          <a routerLink="/organizacao/retiradas" class="font-medium text-emerald-700 underline">
            Retiradas
          </a>
          <a routerLink="/organizacao/painel" class="font-medium text-emerald-700 underline">
            Painel
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

      <div class="cartao mt-6">
        <h2 class="text-lg font-semibold">Registrar recebimento em ponto</h2>
        <p class="mt-1 text-sm text-slate-600">
          Informe o ponto, o tipo aceito e a quantidade recebida.
        </p>

        <div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <label class="campo">
            <span class="campo-rotulo">Ponto de coleta</span>
            <select
              class="mt-1"
              [value]="pontoDoRecebimento()"
              (change)="alterarPontoDoRecebimento($event)"
            >
              <option value="">Selecione o ponto</option>
              @for (ponto of pontos(); track ponto.id) {
                <option [value]="ponto.id">
                  {{ ponto.nome }} — {{ ponto.municipio }}/{{ ponto.uf }}
                </option>
              }
            </select>
          </label>

          <label class="campo">
            <span class="campo-rotulo">Tipo de resíduo</span>
            <select
              class="mt-1"
              [value]="tipoDoRecebimento()"
              (change)="alterarTipoDoRecebimento($event)"
            >
              <option value="">Selecione o tipo</option>
              @for (tipo of tiposDoPontoSelecionado(); track tipo.id) {
                <option [value]="tipo.id">{{ tipo.nome }} ({{ tipo.unidade }})</option>
              }
            </select>
          </label>

          <label class="campo">
            <span class="campo-rotulo">Quantidade</span>
            <input
              type="number"
              min="0.001"
              step="0.001"
              placeholder="Ex.: 12.5"
              class="mt-1"
              [value]="quantidadeDoRecebimento()"
              (input)="alterarQuantidadeDoRecebimento($event)"
            />
          </label>

          <div class="flex items-end">
            <button
              type="button"
              class="botao-primario w-full"
              [disabled]="registrando()"
              (click)="registrarRecebimento()"
            >
              {{ registrando() ? 'Registrando…' : 'Registrar recebimento' }}
            </button>
          </div>
        </div>

        @if (pontos().length === 0) {
          <p class="mt-3 text-sm text-slate-600">
            Cadastre um ponto ativo para registrar recebimentos.
          </p>
        }
      </div>

      <div class="cartao mt-4">
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
            <span class="campo-rotulo">Ponto</span>
            <select class="mt-1" [value]="pontoFiltro()" (change)="alterarPontoFiltro($event)">
              <option value="">Todos os pontos</option>
              @for (ponto of pontos(); track ponto.id) {
                <option [value]="ponto.id">{{ ponto.nome }}</option>
              }
            </select>
          </label>
          <button type="button" class="botao-primario" (click)="aplicarFiltros()">Filtrar</button>
          <button type="button" class="botao-secundario" (click)="limparFiltros()">Limpar</button>
        </div>
      </div>

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando volumes…</p>
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
          Nenhum volume encontrado com os filtros atuais.
        </p>
      } @else {
        <div class="mt-6 overflow-x-auto rounded-md bg-white shadow-sm">
          <table class="min-w-full divide-y divide-slate-200">
            <caption class="sr-only">
              Registros de volume da organização
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
                  Tipo
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-right text-xs font-semibold uppercase text-slate-600"
                >
                  Quantidade
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Origem
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Operador
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
              @for (volume of pagina().itens; track volume.id) {
                <tr>
                  <td class="px-4 py-3 text-sm text-slate-900">
                    <span class="block font-medium">{{ formatarData(volume.dataLocal) }}</span>
                    <span class="block text-xs text-slate-500">
                      {{ formatarDataHora(volume.ocorridoEmUtc) }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-700">
                    <span class="block">{{ volume.tipoNome }}</span>
                    <span class="block text-xs text-slate-500">{{ volume.tipoCodigo }}</span>
                  </td>
                  <td class="px-4 py-3 text-right text-sm">
                    <span class="font-medium">{{ volume.quantidade }} {{ volume.unidade }}</span>
                    @if (volume.totalDoOriginal !== null) {
                      <span class="block text-xs text-slate-500">
                        Total do original: {{ volume.totalDoOriginal }} {{ volume.unidade }}
                      </span>
                    }
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-700">
                    <span class="block">{{ rotuloDeOrigem(volume.origem) }}</span>
                    @if (volume.pontoNome !== null) {
                      <span class="block text-xs text-slate-500">{{ volume.pontoNome }}</span>
                    }
                    @if (volume.motivo !== null) {
                      <span class="block text-xs text-slate-500">{{ volume.motivo }}</span>
                    }
                  </td>
                  <td class="px-4 py-3 text-sm text-slate-600">{{ volume.operadorNome }}</td>
                  <td class="px-4 py-3 text-right text-sm">
                    @if (volume.origem !== 'Ajuste' && volume.registroOriginalId === null) {
                      @if (ajustandoId() === volume.id) {
                        <div class="flex flex-col items-end gap-2">
                          <input
                            type="number"
                            step="0.001"
                            placeholder="Ex.: -2.5"
                            class="w-32 text-right"
                            [value]="quantidadeDoAjuste()"
                            (input)="alterarQuantidadeDoAjuste($event)"
                          />
                          <input
                            placeholder="Ex.: Correção de pesagem"
                            maxlength="500"
                            class="w-48"
                            [value]="motivoDoAjuste()"
                            (input)="alterarMotivoDoAjuste($event)"
                          />
                          <div class="flex gap-2">
                            <button
                              type="button"
                              class="font-medium text-emerald-700 underline disabled:opacity-60"
                              [disabled]="ajustando()"
                              (click)="salvarAjuste(volume)"
                            >
                              {{ ajustando() ? 'Salvando…' : 'Salvar' }}
                            </button>
                            <button
                              type="button"
                              class="font-medium text-slate-600 underline"
                              (click)="cancelarAjuste()"
                            >
                              Voltar
                            </button>
                          </div>
                        </div>
                      } @else {
                        <button
                          type="button"
                          class="font-medium text-emerald-700 underline"
                          (click)="iniciarAjuste(volume)"
                        >
                          Ajustar
                        </button>
                      }
                    } @else {
                      <span class="text-xs text-slate-500">—</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
          <span>
            {{ pagina().total }} registro(s) · página {{ pagina().pagina }} de
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
export class VolumesPage implements OnInit {
  private readonly servico = inject(VolumesService);
  private readonly pontosService = inject(PontosService);
  private readonly catalogo = inject(CatalogoService);

  protected readonly origens = ORIGENS;
  protected readonly tipos = signal<TipoResiduo[]>([]);
  protected readonly pontos = signal<OpcaoDePonto[]>([]);

  protected readonly de = signal('');
  protected readonly ate = signal('');
  protected readonly tipoFiltro = signal('');
  protected readonly origem = signal('');
  protected readonly pontoFiltro = signal('');

  protected readonly pontoDoRecebimento = signal('');
  protected readonly tipoDoRecebimento = signal('');
  protected readonly quantidadeDoRecebimento = signal(1);
  protected readonly registrando = signal(false);

  protected readonly ajustandoId = signal<string | null>(null);
  protected readonly quantidadeDoAjuste = signal(0);
  protected readonly motivoDoAjuste = signal('');
  protected readonly ajustando = signal(false);

  protected readonly pagina = signal<PaginaDeVolumes>({
    itens: [],
    pagina: 1,
    tamanhoPagina: TAMANHO_DA_PAGINA,
    total: 0,
  });
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);

  protected readonly tiposDoPontoSelecionado = computed(() => {
    const ponto = this.pontos().find((item) => item.id === this.pontoDoRecebimento());
    if (ponto === undefined) {
      return [];
    }

    const aceitos = new Set(ponto.tiposResiduoIds);
    return this.tipos().filter((tipo) => aceitos.has(tipo.id));
  });

  protected readonly totalDePaginas = computed(() =>
    Math.max(1, Math.ceil(this.pagina().total / this.pagina().tamanhoPagina)),
  );

  protected readonly formatarData = formatarData;
  protected readonly formatarDataHora = formatarDataHora;

  ngOnInit(): void {
    this.carregarCatalogos();
    this.carregar(1);
  }

  protected rotuloDeOrigem(origem: OrigemDoVolume): string {
    return ROTULOS_DE_ORIGEM[origem] ?? origem;
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

  protected alterarPontoFiltro(evento: Event): void {
    this.pontoFiltro.set((evento.target as HTMLSelectElement).value);
  }

  protected alterarPontoDoRecebimento(evento: Event): void {
    this.pontoDoRecebimento.set((evento.target as HTMLSelectElement).value);
    this.tipoDoRecebimento.set('');
  }

  protected alterarTipoDoRecebimento(evento: Event): void {
    this.tipoDoRecebimento.set((evento.target as HTMLSelectElement).value);
  }

  protected alterarQuantidadeDoRecebimento(evento: Event): void {
    this.quantidadeDoRecebimento.set(Number((evento.target as HTMLInputElement).value));
  }

  protected alterarQuantidadeDoAjuste(evento: Event): void {
    this.quantidadeDoAjuste.set(Number((evento.target as HTMLInputElement).value));
  }

  protected alterarMotivoDoAjuste(evento: Event): void {
    this.motivoDoAjuste.set((evento.target as HTMLInputElement).value);
  }

  protected aplicarFiltros(): void {
    this.erroDeAcao.set(null);
    this.carregar(1);
  }

  protected limparFiltros(): void {
    this.de.set('');
    this.ate.set('');
    this.tipoFiltro.set('');
    this.origem.set('');
    this.pontoFiltro.set('');
    this.carregar(1);
  }

  protected registrarRecebimento(): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);

    const pontoId = this.pontoDoRecebimento();
    const tipoResiduoId = this.tipoDoRecebimento();
    const quantidade = this.quantidadeDoRecebimento();

    if (pontoId.length === 0 || tipoResiduoId.length === 0) {
      this.erroDeAcao.set('Selecione o ponto e o tipo de resíduo.');
      return;
    }

    if (!Number.isFinite(quantidade) || quantidade <= 0) {
      this.erroDeAcao.set('Informe uma quantidade maior que zero.');
      return;
    }

    this.registrando.set(true);

    this.servico.registrarRecebimento({ pontoId, tipoResiduoId, quantidade }).subscribe({
      next: () => {
        this.registrando.set(false);
        this.tipoDoRecebimento.set('');
        this.quantidadeDoRecebimento.set(1);
        this.mensagem.set('Recebimento registrado.');
        this.carregar(1);
      },
      error: (erro: unknown) => {
        this.registrando.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  protected iniciarAjuste(volume: VolumeResposta): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.ajustandoId.set(volume.id);
    this.quantidadeDoAjuste.set(0);
    this.motivoDoAjuste.set('');
  }

  protected cancelarAjuste(): void {
    this.ajustandoId.set(null);
    this.quantidadeDoAjuste.set(0);
    this.motivoDoAjuste.set('');
  }

  protected salvarAjuste(volume: VolumeResposta): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);

    const quantidade = this.quantidadeDoAjuste();
    const motivo = this.motivoDoAjuste().trim();

    if (!Number.isFinite(quantidade) || quantidade === 0) {
      this.erroDeAcao.set('O ajuste deve ser diferente de zero.');
      return;
    }

    if (motivo.length === 0) {
      this.erroDeAcao.set('Informe o motivo do ajuste.');
      return;
    }

    this.ajustando.set(true);

    this.servico.ajustar(volume.id, { quantidade, motivo }).subscribe({
      next: () => {
        this.ajustando.set(false);
        this.ajustandoId.set(null);
        this.mensagem.set('Ajuste registrado.');
        this.carregar(this.pagina().pagina);
      },
      error: (erro: unknown) => {
        this.ajustando.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  protected carregar(numeroDaPagina: number): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.servico
      .listar({
        de: this.de(),
        ate: this.ate(),
        tipoResiduoId: this.tipoFiltro(),
        origem: this.origem(),
        pontoId: this.pontoFiltro(),
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

  private carregarCatalogos(): void {
    this.catalogo.listarTipos().subscribe({
      next: (tipos) => this.tipos.set(tipos),
      error: (erro: unknown) => this.erroDeAcao.set(mensagemDeErro(erro)),
    });

    this.pontosService.listarOpcoes().subscribe({
      next: (pontos) => this.pontos.set(pontos),
      error: (erro: unknown) => this.erroDeAcao.set(mensagemDeErro(erro)),
    });
  }
}
