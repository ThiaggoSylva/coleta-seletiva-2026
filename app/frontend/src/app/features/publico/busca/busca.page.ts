import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { CatalogoService, Municipio, TipoResiduo } from '../../../core/servicos/catalogo.service';
import { Coordenada, MarcadorDoMapa, MapaPontos } from '../../../shared/mapa/mapa-pontos';
import { ConsultaPublicaService, PontoPublico } from '../consulta-publica.service';

const DIAS: Record<string, string> = {
  Monday: 'Segunda',
  Tuesday: 'Terça',
  Wednesday: 'Quarta',
  Thursday: 'Quinta',
  Friday: 'Sexta',
  Saturday: 'Sábado',
  Sunday: 'Domingo',
};

@Component({
  selector: 'app-busca-publica',
  imports: [ReactiveFormsModule, RouterLink, MapaPontos],
  template: `
    <section class="mx-auto max-w-5xl px-6 py-10">
      <div class="hero">
        <p class="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200">
          Mapa da coleta seletiva
        </p>
        <h1 class="mt-3 text-3xl font-semibold text-white sm:text-4xl">
          Onde entregar seus resíduos
        </h1>
        <p class="mt-3 max-w-2xl text-sm text-emerald-50/90">
          Consulte os pontos de coleta publicados pelas organizações cadastradas.
        </p>
      </div>

      <form class="cartao mt-6" [formGroup]="filtros" (ngSubmit)="buscar()">
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div class="text-sm">
            <label class="block">
              <span class="font-medium text-slate-700">Município</span>
              <div class="mt-1 flex gap-2">
                <input
                  class="w-full"
                  placeholder="Ex.: Goiânia"
                  [value]="textoMunicipio()"
                  (input)="digitarMunicipio($event)"
                  (keydown.enter)="procurarMunicipios($event)"
                />
                <button type="button" class="botao-secundario !px-3" (click)="procurarMunicipios()">
                  Buscar
                </button>
              </div>
            </label>

            @if (sugestoes().length > 0) {
              <ul
                class="mt-2 max-h-40 overflow-y-auto rounded-lg border border-slate-200 bg-white shadow-sm"
              >
                @for (municipio of sugestoes(); track municipio.id) {
                  <li>
                    <button
                      type="button"
                      class="w-full px-3 py-1 text-left hover:bg-slate-100"
                      (click)="escolherMunicipio(municipio)"
                    >
                      {{ municipio.nome }} — {{ municipio.uf }}
                    </button>
                  </li>
                }
              </ul>
            }

            @if (municipioEscolhido() !== null) {
              <p class="mt-1 flex items-center gap-2 text-slate-700">
                {{ municipioEscolhido()?.nome }} — {{ municipioEscolhido()?.uf }}
                <button type="button" class="text-red-700 underline" (click)="limparMunicipio()">
                  Remover
                </button>
              </p>
            }
          </div>

          <label class="block text-sm">
            <span class="font-medium text-slate-700">Tipo de resíduo</span>
            <select formControlName="tipoResiduoId" class="mt-1.5 w-full">
              <option value="">Todos os tipos</option>
              @for (tipo of tipos(); track tipo.id) {
                <option [value]="tipo.id">{{ tipo.nome }}</option>
              }
            </select>
          </label>

          <div class="text-sm">
            <span class="block font-medium text-slate-700">Localização</span>
            @if (coordenadas() === null) {
              <button
                type="button"
                class="botao-secundario mt-1 w-full"
                (click)="usarMinhaLocalizacao()"
              >
                Usar minha localização
              </button>
            } @else {
              <p class="mt-1 flex items-center gap-2 text-slate-700">
                Buscando perto de você
                <button type="button" class="text-red-700 underline" (click)="limparLocalizacao()">
                  Remover
                </button>
              </p>
            }

            @if (coordenadas() !== null) {
              <label class="mt-2 block">
                <span class="font-medium text-slate-700">Raio (km)</span>
                <select formControlName="raioKm" class="mt-1.5 w-full">
                  <option [value]="5">5 km</option>
                  <option [value]="10">10 km</option>
                  <option [value]="25">25 km</option>
                  <option [value]="50">50 km</option>
                </select>
              </label>
            }
          </div>
        </div>

        <div class="mt-4 flex flex-wrap items-center justify-between gap-3">
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" formControlName="abertoAgora" />
            Somente abertos agora
          </label>

          <button type="submit" class="botao-primario" [disabled]="carregando()">
            {{ carregando() ? 'Buscando…' : 'Buscar pontos' }}
          </button>
        </div>
      </form>

      @if (erro() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">{{ erro() }}</p>
      }

      @if (buscou()) {
        <p class="mt-6 text-sm text-slate-600" role="status">
          {{ total() }} {{ total() === 1 ? 'ponto encontrado' : 'pontos encontrados' }}
        </p>
      }

      @if (avisoFonteExterna() !== null) {
        <p class="mt-2 rounded-md bg-amber-50 p-3 text-sm text-amber-900" role="status">
          {{ avisoFonteExterna() }}
        </p>
      }

      @if (resultados().length > 0) {
        <div class="mt-6 overflow-hidden rounded-2xl border border-slate-200 shadow-sm">
          <app-mapa-pontos [marcadores]="marcadores()" rotulo="Mapa dos pontos encontrados" />
        </div>

        @if (atribuicaoFonteExterna() !== null) {
          <p class="mt-2 text-xs text-slate-500">{{ atribuicaoFonteExterna() }}</p>
        }

        <ul class="mt-6 grid gap-4 lg:grid-cols-2">
          @for (ponto of resultados(); track ponto.id) {
            <li class="cartao">
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 class="text-lg font-semibold">{{ ponto.nome }}</h2>
                  <p class="text-sm text-slate-600">{{ ponto.organizacaoNome }}</p>
                  @if (endereco(ponto) !== null) {
                    <p class="mt-2 text-sm text-slate-700">{{ endereco(ponto) }}</p>
                  }
                  @if (localidade(ponto) !== null) {
                    <p class="text-sm text-slate-700">{{ localidade(ponto) }}</p>
                  }
                  @if (ponto.origem === 'OpenStreetMap') {
                    <p class="mt-1 text-xs text-slate-500">Fonte pública (OpenStreetMap)</p>
                  }
                </div>

                <div class="text-right text-sm">
                  <span
                    class="rounded-full px-3 py-1 font-medium"
                    [class]="
                      ponto.abertoAgora === true
                        ? 'bg-emerald-100 text-emerald-900'
                        : 'bg-slate-100 text-slate-700'
                    "
                  >
                    {{
                      ponto.abertoAgora === null
                        ? 'Horário não informado'
                        : ponto.abertoAgora
                          ? 'Aberto agora'
                          : 'Fechado agora'
                    }}
                  </span>
                  @if (ponto.distanciaKm !== null) {
                    <p class="mt-2 text-slate-600">{{ distancia(ponto.distanciaKm) }}</p>
                  }
                </div>
              </div>

              <p class="mt-3 text-sm text-slate-700">
                <span class="font-medium">Recebe:</span>
                {{ ponto.tipos.length > 0 ? nomesDosTipos(ponto) : 'tipos não informados' }}
              </p>

              @if (ponto.horarios.length > 0) {
                <ul class="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-sm text-slate-600">
                  @for (horario of ponto.horarios; track $index) {
                    <li>
                      {{ dia(horario.diaDaSemana) }}: {{ horario.horaInicio }}–{{ horario.horaFim }}
                    </li>
                  }
                </ul>
              }

              @if (ponto.observacoes !== null) {
                <p class="mt-2 text-sm text-slate-600">{{ ponto.observacoes }}</p>
              }

              @if (ponto.urlDaFonte !== null) {
                <p class="mt-2 text-sm">
                  <a
                    [href]="ponto.urlDaFonte"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="font-medium text-emerald-700 underline"
                  >
                    Ver no OpenStreetMap
                  </a>
                </p>
              }

              @if (ponto.contatoEmail !== null || ponto.contatoTelefone !== null) {
                <p class="mt-2 text-sm text-slate-700">
                  Contato:
                  @if (ponto.contatoTelefone !== null) {
                    <span>{{ ponto.contatoTelefone }}</span>
                  }
                  @if (ponto.contatoEmail !== null) {
                    <span class="ml-2">{{ ponto.contatoEmail }}</span>
                  }
                </p>
              }
            </li>
          }
        </ul>

        @if (temMais()) {
          <button
            type="button"
            class="botao-secundario mt-6"
            [disabled]="carregando()"
            (click)="carregarMais()"
          >
            Carregar mais pontos
          </button>
        }
      } @else if (buscou() && !carregando()) {
        <p class="cartao mt-6 text-slate-600">
          Nenhum ponto encontrado com esses filtros. Tente ampliar o raio, escolher outro tipo ou
          remover o filtro de município.
        </p>
      }

      <p class="mt-8 text-sm text-slate-600">
        Representa uma organização?
        <a routerLink="/acesso/login" class="font-medium text-emerald-700 underline">
          Entre para cadastrar seus pontos
        </a>
        .
      </p>
    </section>
  `,
})
export class BuscaPage implements OnInit {
  private readonly servico = inject(ConsultaPublicaService);
  private readonly catalogo = inject(CatalogoService);
  private readonly fb = inject(FormBuilder);

  private readonly tamanhoDaPagina = 20;

  protected readonly tipos = signal<TipoResiduo[]>([]);
  protected readonly sugestoes = signal<Municipio[]>([]);
  protected readonly municipioEscolhido = signal<Municipio | null>(null);
  protected readonly textoMunicipio = signal('');
  protected readonly resultados = signal<PontoPublico[]>([]);
  protected readonly total = signal(0);
  protected readonly pagina = signal(1);
  protected readonly coordenadas = signal<Coordenada | null>(null);
  protected readonly carregando = signal(false);
  protected readonly buscou = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly avisoFonteExterna = signal<string | null>(null);
  protected readonly atribuicaoFonteExterna = signal<string | null>(null);

  protected readonly filtros = this.fb.nonNullable.group({
    tipoResiduoId: [''],
    abertoAgora: [false],
    raioKm: [10],
  });

  protected readonly marcadores = computed<MarcadorDoMapa[]>(() =>
    this.resultados().map((ponto) => {
      const local =
        ponto.municipio === null
          ? null
          : `${ponto.bairro === null ? '' : `${ponto.bairro}, `}${ponto.municipio.nome}/${ponto.municipio.uf}`;

      return {
        id: ponto.id,
        nome: ponto.nome,
        latitude: ponto.latitude,
        longitude: ponto.longitude,
        descricao: local === null ? ponto.organizacaoNome : `${ponto.organizacaoNome} · ${local}`,
      };
    }),
  );

  protected readonly temMais = computed(() => this.resultados().length < this.total());

  ngOnInit(): void {
    this.catalogo.listarTipos().subscribe({
      next: (tipos) => this.tipos.set(tipos),
      error: (erro: unknown) => this.erro.set(mensagemDeErro(erro)),
    });

    this.buscar();
  }

  protected digitarMunicipio(evento: Event): void {
    this.textoMunicipio.set((evento.target as HTMLInputElement).value);
  }

  protected procurarMunicipios(evento?: Event): void {
    evento?.preventDefault();

    if (this.textoMunicipio().trim().length === 0) {
      this.sugestoes.set([]);
      return;
    }

    this.erro.set(null);
    this.catalogo.listarMunicipios('', this.textoMunicipio()).subscribe({
      next: (municipios) => this.sugestoes.set(municipios),
      error: (erro: unknown) => this.erro.set(mensagemDeErro(erro)),
    });
  }

  protected escolherMunicipio(municipio: Municipio): void {
    this.municipioEscolhido.set(municipio);
    this.sugestoes.set([]);
    this.textoMunicipio.set('');
  }

  protected limparMunicipio(): void {
    this.municipioEscolhido.set(null);
  }

  protected usarMinhaLocalizacao(): void {
    if (!('geolocation' in navigator)) {
      this.erro.set('Seu navegador não oferece localização.');
      return;
    }

    this.erro.set(null);
    navigator.geolocation.getCurrentPosition(
      (posicao) => {
        this.coordenadas.set({
          latitude: posicao.coords.latitude,
          longitude: posicao.coords.longitude,
        });
        this.buscar();
      },
      () => this.erro.set('Não foi possível obter sua localização.'),
      { timeout: 10000 },
    );
  }

  protected limparLocalizacao(): void {
    this.coordenadas.set(null);
  }

  protected carregarMais(): void {
    this.buscar(this.pagina() + 1);
  }

  protected buscar(pagina = 1): void {
    const valores = this.filtros.getRawValue();
    const coordenadas = this.coordenadas();

    this.carregando.set(true);
    this.erro.set(null);

    this.servico
      .buscar({
        municipioId: this.municipioEscolhido()?.id ?? null,
        tipoResiduoId: valores.tipoResiduoId === '' ? null : valores.tipoResiduoId,
        abertoAgora: valores.abertoAgora,
        latitude: coordenadas?.latitude ?? null,
        longitude: coordenadas?.longitude ?? null,
        raioKm: coordenadas === null ? null : Number(valores.raioKm),
        pagina,
        tamanhoPagina: this.tamanhoDaPagina,
      })
      .subscribe({
        next: (resposta) => {
          this.carregando.set(false);
          this.buscou.set(true);
          this.pagina.set(resposta.pagina);
          this.total.set(resposta.total);
          this.avisoFonteExterna.set(resposta.avisoDeFonteExterna ?? null);
          this.atribuicaoFonteExterna.set(resposta.atribuicaoDeFonteExterna ?? null);
          this.resultados.update((atuais) =>
            pagina === 1 ? resposta.itens : [...atuais, ...resposta.itens],
          );
        },
        error: (erro: unknown) => {
          this.carregando.set(false);
          this.buscou.set(true);
          this.erro.set(mensagemDeErro(erro));
        },
      });
  }

  protected nomesDosTipos(ponto: PontoPublico): string {
    return ponto.tipos.map((tipo) => tipo.nome).join(', ');
  }

  protected endereco(ponto: PontoPublico): string | null {
    if (ponto.rua === null) {
      return null;
    }

    const numero = ponto.numero === null ? 's/n' : ponto.numero;
    const complemento = ponto.complemento === null ? '' : ` — ${ponto.complemento}`;

    return `${ponto.rua}, ${numero}${complemento}`;
  }

  protected localidade(ponto: PontoPublico): string | null {
    if (ponto.municipio === null) {
      return null;
    }

    const bairro = ponto.bairro === null ? '' : `${ponto.bairro} — `;
    const cep = ponto.cep === null ? '' : ` · CEP ${ponto.cep}`;

    return `${bairro}${ponto.municipio.nome}/${ponto.municipio.uf}${cep}`;
  }

  protected dia(diaDaSemana: string): string {
    return DIAS[diaDaSemana] ?? diaDaSemana;
  }

  protected distancia(km: number): string {
    return km < 1
      ? `${Math.round(km * 1000)} m de distância`
      : `${km.toFixed(1).replace('.', ',')} km de distância`;
  }
}
