import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import { Municipio } from '../../../core/servicos/catalogo.service';
import { campoInvalido, ErrosDoServidor, mensagensDoCampo } from '../../../shared/formulario';
import { Coordenada, MapaPontos } from '../../../shared/mapa/mapa-pontos';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';
import { PerfilService, TipoHabilitado } from '../perfil/perfil.service';
import { HorarioDoPonto, PontoEntrada, PontosService, StatusPonto } from './pontos.service';

const DIAS: { valor: string; rotulo: string }[] = [
  { valor: 'Monday', rotulo: 'Segunda-feira' },
  { valor: 'Tuesday', rotulo: 'Terça-feira' },
  { valor: 'Wednesday', rotulo: 'Quarta-feira' },
  { valor: 'Thursday', rotulo: 'Quinta-feira' },
  { valor: 'Friday', rotulo: 'Sexta-feira' },
  { valor: 'Saturday', rotulo: 'Sábado' },
  { valor: 'Sunday', rotulo: 'Domingo' },
];

const STATUS: { valor: StatusPonto; rotulo: string }[] = [
  { valor: 'Ativo', rotulo: 'Ativo' },
  { valor: 'Inativo', rotulo: 'Inativo' },
  { valor: 'IndisponivelTemporariamente', rotulo: 'Indisponível temporariamente' },
];

const CENTRO_PADRAO: Coordenada = { latitude: -23.5505, longitude: -46.6333 };

@Component({
  selector: 'app-ponto',
  imports: [ReactiveFormsModule, RouterLink, MapaPontos, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">
            {{ editando() ? 'Editar ponto de coleta' : 'Novo ponto de coleta' }}
          </h1>
          <p class="mt-1 text-sm text-slate-600">
            Endereço, localização, horários e tipos de resíduo aceitos.
          </p>
        </div>
        <a routerLink="/organizacao/pontos" class="font-medium text-emerald-700 underline">
          Voltar para a lista
        </a>
      </div>

      @if (erroDeCarga() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
        </p>
      }

      @if (erroDeAcao() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeAcao() }}
        </p>
      }

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando ponto…</p>
      } @else if (municipios().length === 0) {
        <p class="mt-6 rounded-md bg-amber-50 p-4 text-sm text-amber-900" role="alert">
          Cadastre ao menos um município atendido no
          <a routerLink="/organizacao/perfil" class="font-medium underline"
            >perfil da organização</a
          >
          antes de criar pontos de coleta.
        </p>
      } @else if (tipos().length === 0) {
        <p class="mt-6 rounded-md bg-amber-50 p-4 text-sm text-amber-900" role="alert">
          Habilite ao menos um tipo de resíduo no
          <a routerLink="/organizacao/perfil" class="font-medium underline"
            >perfil da organização</a
          >
          antes de criar pontos de coleta.
        </p>
      } @else {
        <form class="mt-6 space-y-6" [formGroup]="formulario" (ngSubmit)="salvar()">
          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Identificação e endereço</h2>

            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <label class="block text-sm sm:col-span-2">
                <span class="font-medium text-slate-700">Nome do ponto</span>
                <input
                  formControlName="nome"
                  placeholder="Ecoponto Setor Central"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('nome')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('nome')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Município</span>
                <select
                  formControlName="municipioId"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                >
                  @for (municipio of municipios(); track municipio.id) {
                    <option [value]="municipio.id">
                      {{ municipio.nome }} — {{ municipio.uf }}
                    </option>
                  }
                </select>
                <app-mensagem-de-erro [mensagens]="mensagens('municipioId')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Status</span>
                <select
                  formControlName="status"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                >
                  @for (status of statusDisponiveis; track status.valor) {
                    <option [value]="status.valor">{{ status.rotulo }}</option>
                  }
                </select>
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Bairro</span>
                <input
                  formControlName="bairro"
                  placeholder="Setor Central"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('bairro')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('bairro')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Rua</span>
                <input
                  formControlName="rua"
                  placeholder="Rua 7"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('rua')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('rua')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Número</span>
                <input
                  formControlName="numero"
                  placeholder="123"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('numero')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('numero')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Complemento</span>
                <input
                  formControlName="complemento"
                  placeholder="Ao lado da praça (opcional)"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">CEP</span>
                <input
                  formControlName="cep"
                  inputmode="numeric"
                  placeholder="74000-000"
                  maxlength="9"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('cep')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('cep')" />
              </label>

              <label class="block text-sm sm:col-span-2">
                <span class="font-medium text-slate-700">Observações</span>
                <textarea
                  formControlName="observacoes"
                  rows="2"
                  placeholder="Ex.: instruções de acesso"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                ></textarea>
              </label>
            </div>
          </div>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Localização</h2>
            <p class="mt-1 text-sm text-slate-600">
              Clique no mapa para ajustar o marcador ou informe as coordenadas.
            </p>

            <div class="mt-3">
              <app-mapa-pontos
                [interativo]="true"
                [centro]="coordenadas()"
                [zoom]="15"
                rotulo="Selecione a localização do ponto de coleta"
                (pontoEscolhido)="definirNoMapa($event)"
              />
            </div>

            <div class="mt-3 grid gap-4 sm:grid-cols-2">
              <label class="block text-sm">
                <span class="font-medium text-slate-700">Latitude</span>
                <input
                  formControlName="latitude"
                  type="number"
                  placeholder="-16.6869"
                  step="0.000001"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  (input)="atualizarCoordenadas()"
                  [class.border-red-500]="invalido('latitude')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('latitude')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Longitude</span>
                <input
                  formControlName="longitude"
                  type="number"
                  placeholder="-49.2648"
                  step="0.000001"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  (input)="atualizarCoordenadas()"
                  [class.border-red-500]="invalido('longitude')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('longitude')" />
              </label>
            </div>
          </div>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Horários de funcionamento</h2>
            <p class="mt-1 text-sm text-slate-600">
              Cada horário deve terminar no mesmo dia, com fim maior que o início.
            </p>

            @if (errosDeHorario().length > 0) {
              <p class="mt-2 rounded-md bg-red-50 p-2 text-sm text-red-800" role="alert">
                {{ errosDeHorario().join(' ') }}
              </p>
            }

            @for (horario of horarios(); track $index) {
              <div class="mt-3 flex flex-wrap items-end gap-3">
                <label class="block text-sm">
                  <span class="font-medium text-slate-700">Dia</span>
                  <select
                    class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                    [value]="horario.diaDaSemana"
                    (change)="alterarHorario($index, 'diaDaSemana', $event)"
                  >
                    @for (dia of dias; track dia.valor) {
                      <option [value]="dia.valor">{{ dia.rotulo }}</option>
                    }
                  </select>
                </label>

                <label class="block text-sm">
                  <span class="font-medium text-slate-700">Abre</span>
                  <input
                    type="time"
                    class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                    [value]="horario.horaInicio"
                    (input)="alterarHorario($index, 'horaInicio', $event)"
                  />
                </label>

                <label class="block text-sm">
                  <span class="font-medium text-slate-700">Fecha</span>
                  <input
                    type="time"
                    class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                    [value]="horario.horaFim"
                    (input)="alterarHorario($index, 'horaFim', $event)"
                  />
                </label>

                <button
                  type="button"
                  class="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100"
                  (click)="removerHorario($index)"
                >
                  Remover
                </button>
              </div>
            }

            <button
              type="button"
              class="mt-4 rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              (click)="adicionarHorario()"
            >
              Adicionar horário
            </button>
          </div>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Tipos de resíduo aceitos</h2>

            @if (errosDeTipos() !== null) {
              <p class="mt-2 rounded-md bg-red-50 p-2 text-sm text-red-800" role="alert">
                {{ errosDeTipos() }}
              </p>
            }

            <ul class="mt-3 divide-y divide-slate-100">
              @for (tipo of tipos(); track tipo.tipoResiduoId) {
                <li class="py-2 text-sm">
                  <label class="flex items-center gap-2">
                    <input
                      type="checkbox"
                      [checked]="tiposSelecionados()[tipo.tipoResiduoId] === true"
                      (change)="alternarTipo(tipo.tipoResiduoId, $event)"
                    />
                    <span class="font-medium text-slate-800">{{ tipo.nome }}</span>
                    <span class="text-slate-500">{{ tipo.unidade }}</span>
                  </label>
                </li>
              }
            </ul>
          </div>

          <div class="flex flex-wrap gap-3">
            <button
              type="submit"
              class="rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
              [disabled]="salvando()"
            >
              {{ salvando() ? 'Salvando…' : 'Salvar ponto' }}
            </button>
            <a
              routerLink="/organizacao/pontos"
              class="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
            >
              Cancelar
            </a>
          </div>
        </form>
      }
    </section>
  `,
})
export class PontoPage implements OnInit {
  private readonly servico = inject(PontosService);
  private readonly perfil = inject(PerfilService);
  private readonly rota = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly dias = DIAS;
  protected readonly statusDisponiveis = STATUS;

  protected readonly id = signal<string | null>(null);
  protected readonly editando = signal(false);
  protected readonly carregando = signal(true);
  protected readonly salvando = signal(false);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly erros = signal<ErrosDoServidor>({});
  protected readonly errosDeHorario = signal<string[]>([]);
  protected readonly errosDeTipos = signal<string | null>(null);

  protected readonly municipios = signal<Municipio[]>([]);
  protected readonly tipos = signal<TipoHabilitado[]>([]);
  protected readonly horarios = signal<HorarioDoPonto[]>([]);
  protected readonly tiposSelecionados = signal<Record<string, boolean>>({});
  protected readonly coordenadas = signal<Coordenada>(CENTRO_PADRAO);

  protected readonly formulario = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(120)]],
    municipioId: [0, [Validators.required, Validators.min(1)]],
    status: ['Ativo' as StatusPonto, [Validators.required]],
    bairro: ['', [Validators.required, Validators.maxLength(80)]],
    rua: ['', [Validators.required, Validators.maxLength(120)]],
    numero: ['', [Validators.required, Validators.maxLength(20)]],
    complemento: ['', [Validators.maxLength(60)]],
    cep: ['', [Validators.required, Validators.pattern(/^\d{5}-?\d{3}$/)]],
    observacoes: ['', [Validators.maxLength(500)]],
    latitude: [CENTRO_PADRAO.latitude, [Validators.required]],
    longitude: [CENTRO_PADRAO.longitude, [Validators.required]],
  });

  ngOnInit(): void {
    const identificador = this.rota.snapshot.paramMap.get('id');
    this.id.set(identificador);
    this.editando.set(identificador !== null);

    forkJoin({
      perfil: this.perfil.obter(),
      ponto: identificador === null ? of(null) : this.servico.obter(identificador),
    }).subscribe({
      next: ({ perfil, ponto }) => {
        this.municipios.set(perfil.municipios);
        this.tipos.set(perfil.tipos);

        if (ponto !== null) {
          this.preencher(ponto);
        } else {
          this.horarios.set([{ diaDaSemana: 'Monday', horaInicio: '08:00', horaFim: '17:00' }]);
          if (perfil.municipios.length > 0) {
            this.formulario.patchValue({ municipioId: perfil.municipios[0].id });
          }
        }

        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected invalido(campo: string): boolean {
    return campoInvalido(this.formulario, campo, this.erros());
  }

  protected mensagens(campo: string): string[] {
    return mensagensDoCampo(this.formulario, campo, this.erros());
  }

  protected definirNoMapa(coordenada: Coordenada): void {
    this.formulario.patchValue({
      latitude: Number(coordenada.latitude.toFixed(6)),
      longitude: Number(coordenada.longitude.toFixed(6)),
    });
    this.coordenadas.set(coordenada);
  }

  protected atualizarCoordenadas(): void {
    const valores = this.formulario.getRawValue();
    const latitude = Number(valores.latitude);
    const longitude = Number(valores.longitude);

    if (Number.isFinite(latitude) && Number.isFinite(longitude)) {
      this.coordenadas.set({ latitude, longitude });
    }
  }

  protected adicionarHorario(): void {
    this.horarios.update((horarios) => [
      ...horarios,
      { diaDaSemana: 'Monday', horaInicio: '08:00', horaFim: '17:00' },
    ]);
  }

  protected removerHorario(indice: number): void {
    this.horarios.update((horarios) => horarios.filter((_, posicao) => posicao !== indice));
  }

  protected alterarHorario(indice: number, campo: keyof HorarioDoPonto, evento: Event): void {
    const valor = (evento.target as HTMLInputElement | HTMLSelectElement).value;

    this.horarios.update((horarios) =>
      horarios.map((horario, posicao) =>
        posicao === indice ? { ...horario, [campo]: valor } : horario,
      ),
    );
  }

  protected alternarTipo(id: string, evento: Event): void {
    const marcado = (evento.target as HTMLInputElement).checked;
    this.tiposSelecionados.update((atuais) => ({ ...atuais, [id]: marcado }));
  }

  protected salvar(): void {
    this.erroDeAcao.set(null);
    this.erros.set({});
    this.errosDeHorario.set([]);
    this.errosDeTipos.set(null);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.atualizarCoordenadas();
      return;
    }

    if (this.horarios().length === 0) {
      this.errosDeHorario.set(['Informe ao menos um horário de funcionamento.']);
      return;
    }

    const tiposResiduoIds = Object.entries(this.tiposSelecionados())
      .filter(([, marcado]) => marcado)
      .map(([tipoId]) => tipoId);

    if (tiposResiduoIds.length === 0) {
      this.errosDeTipos.set('Selecione ao menos um tipo de resíduo.');
      return;
    }

    const valores = this.formulario.getRawValue();
    const entrada: PontoEntrada = {
      nome: valores.nome.trim(),
      municipioId: Number(valores.municipioId),
      bairro: valores.bairro.trim(),
      rua: valores.rua.trim(),
      numero: valores.numero.trim(),
      complemento: textoOuNulo(valores.complemento),
      cep: valores.cep.trim(),
      latitude: Number(valores.latitude),
      longitude: Number(valores.longitude),
      observacoes: textoOuNulo(valores.observacoes),
      status: valores.status,
      horarios: this.horarios(),
      tiposResiduoIds,
    };

    const identificador = this.id();
    const requisicao =
      identificador === null
        ? this.servico.criar(entrada)
        : this.servico.atualizar(identificador, entrada);

    this.salvando.set(true);
    requisicao.subscribe({
      next: () => {
        this.salvando.set(false);
        void this.router.navigate(['/organizacao/pontos'], {
          queryParams: identificador === null ? { criado: '1' } : { editado: '1' },
        });
      },
      error: (erro: unknown) => {
        this.salvando.set(false);
        this.erros.set(errosPorCampo(erro));
        this.aplicarErrosDeColecao(erro);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  private aplicarErrosDeColecao(erro: unknown): void {
    const erros = this.erros();
    const horarios = erros['Horarios'] ?? erros['horarios'] ?? [];
    const tipos = erros['TiposResiduoIds'] ?? erros['tiposResiduoIds'] ?? [];

    if (horarios.length > 0) {
      this.errosDeHorario.set(horarios);
    }

    if (tipos.length > 0) {
      this.errosDeTipos.set(tipos.join(' '));
    }
  }

  private preencher(ponto: {
    nome: string;
    municipio: Municipio;
    bairro: string;
    rua: string;
    numero: string;
    complemento: string | null;
    cep: string;
    latitude: number;
    longitude: number;
    observacoes: string | null;
    status: StatusPonto;
    horarios: HorarioDoPonto[];
    tipos: { tipoResiduoId: string }[];
  }): void {
    this.formulario.patchValue({
      nome: ponto.nome,
      municipioId: ponto.municipio.id,
      status: ponto.status,
      bairro: ponto.bairro,
      rua: ponto.rua,
      numero: ponto.numero,
      complemento: ponto.complemento ?? '',
      cep: ponto.cep,
      observacoes: ponto.observacoes ?? '',
      latitude: ponto.latitude,
      longitude: ponto.longitude,
    });
    this.coordenadas.set({ latitude: ponto.latitude, longitude: ponto.longitude });
    this.horarios.set(ponto.horarios.map((horario) => ({ ...horario })));

    const selecionados: Record<string, boolean> = {};
    for (const tipo of ponto.tipos) {
      selecionados[tipo.tipoResiduoId] = true;
    }
    this.tiposSelecionados.set(selecionados);
  }
}

function textoOuNulo(valor: string): string | null {
  const normalizado = valor.trim();
  return normalizado.length === 0 ? null : normalizado;
}
