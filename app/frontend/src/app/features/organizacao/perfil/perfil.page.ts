import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import { CatalogoService, Municipio, TipoResiduo } from '../../../core/servicos/catalogo.service';
import { campoInvalido, ErrosDoServidor, mensagensDoCampo } from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';
import {
  AtualizarPerfilEntrada,
  PerfilDaOrganizacao,
  PerfilService,
  PeriodoDaOrganizacao,
  TipoHabilitadoEntrada,
  TipoOrganizacao,
} from './perfil.service';

const UFS = [
  'AC',
  'AL',
  'AM',
  'AP',
  'BA',
  'CE',
  'DF',
  'ES',
  'GO',
  'MA',
  'MG',
  'MS',
  'MT',
  'PA',
  'PB',
  'PE',
  'PI',
  'PR',
  'RJ',
  'RN',
  'RO',
  'RR',
  'RS',
  'SC',
  'SE',
  'SP',
  'TO',
];

const TIPOS_DE_ORGANIZACAO: { valor: TipoOrganizacao; rotulo: string }[] = [
  { valor: 'Cooperativa', rotulo: 'Cooperativa' },
  { valor: 'Associacao', rotulo: 'Associação' },
  { valor: 'Prefeitura', rotulo: 'Prefeitura' },
  { valor: 'Outro', rotulo: 'Outro' },
];

@Component({
  selector: 'app-perfil-da-organizacao',
  imports: [ReactiveFormsModule, RouterLink, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Perfil da organização</h1>
          <p class="mt-1 text-sm text-slate-600">
            Dados de contato, áreas de atendimento e tipos de resíduo aceitos.
          </p>
        </div>
        <a routerLink="/organizacao/pontos" class="font-medium text-emerald-700 underline">
          Pontos de coleta
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
        <p class="mt-6 text-slate-600" role="status">Carregando perfil…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else {
        <div class="mt-6 space-y-6">
          <form
            class="rounded-md bg-white p-5 shadow-sm"
            [formGroup]="formulario"
            (ngSubmit)="salvarPerfil()"
          >
            <h2 class="text-lg font-semibold">Dados da organização</h2>

            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <label class="block text-sm">
                <span class="font-medium text-slate-700">Nome</span>
                <input
                  formControlName="nome"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('nome')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('nome')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Tipo</span>
                <select
                  formControlName="tipo"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                >
                  @for (tipo of tiposDeOrganizacao; track tipo.valor) {
                    <option [value]="tipo.valor">{{ tipo.rotulo }}</option>
                  }
                </select>
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">E-mail de contato</span>
                <input
                  formControlName="contatoEmail"
                  type="email"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('contatoEmail')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('contatoEmail')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Telefone de contato</span>
                <input
                  formControlName="contatoTelefone"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('contatoTelefone')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('contatoTelefone')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Fuso horário (IANA)</span>
                <input
                  formControlName="fuso"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  placeholder="America/Sao_Paulo"
                  [class.border-red-500]="invalido('fuso')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('fuso')" />
              </label>

              <label class="block text-sm">
                <span class="font-medium text-slate-700">Limite de solicitações abertas</span>
                <input
                  formControlName="limiteSolicitacoesAbertas"
                  type="number"
                  min="1"
                  max="50"
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [class.border-red-500]="invalido('limiteSolicitacoesAbertas')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('limiteSolicitacoesAbertas')" />
              </label>
            </div>

            <button
              type="submit"
              class="mt-4 rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
              [disabled]="salvando() !== null"
            >
              {{ salvando() === 'perfil' ? 'Salvando…' : 'Salvar dados' }}
            </button>
          </form>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <div class="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 class="text-lg font-semibold">Publicação</h2>
                <p class="mt-1 text-sm text-slate-600">
                  Organizações publicadas aparecem na busca pública, desde que tenham município e
                  tipo de resíduo configurados.
                </p>
              </div>
              <span
                class="rounded-full px-3 py-1 text-sm font-medium"
                [class]="
                  perfil()?.publicada
                    ? 'bg-emerald-100 text-emerald-900'
                    : 'bg-slate-100 text-slate-700'
                "
              >
                {{ perfil()?.publicada ? 'Publicada' : 'Não publicada' }}
              </span>
            </div>

            <button
              type="button"
              class="mt-4 rounded-md border border-emerald-700 px-4 py-2 text-sm font-medium text-emerald-800 hover:bg-emerald-50 disabled:opacity-60"
              [disabled]="salvando() !== null"
              (click)="alternarPublicacao()"
            >
              {{ perfil()?.publicada ? 'Despublicar organização' : 'Publicar organização' }}
            </button>
          </div>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Municípios atendidos</h2>
            <p class="mt-1 text-sm text-slate-600">
              Pontos de coleta só podem ser cadastrados nos municípios selecionados aqui.
            </p>

            <div class="mt-4 flex flex-wrap items-end gap-3">
              <label class="block text-sm">
                <span class="font-medium text-slate-700">UF</span>
                <select
                  class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                  [value]="uf()"
                  (change)="selecionarUf($event)"
                >
                  @for (estado of ufs; track estado) {
                    <option [value]="estado">{{ estado }}</option>
                  }
                </select>
              </label>

              <label class="block flex-1 text-sm">
                <span class="font-medium text-slate-700">Nome do município</span>
                <input
                  class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  [value]="textoMunicipio()"
                  (input)="digitarMunicipio($event)"
                  (keydown.enter)="buscarMunicipios()"
                />
              </label>

              <button
                type="button"
                class="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
                (click)="buscarMunicipios()"
              >
                Buscar
              </button>
            </div>

            @if (resultadosMunicipio().length > 0) {
              <ul class="mt-3 divide-y divide-slate-100 rounded-md border border-slate-200">
                @for (municipio of resultadosMunicipio(); track municipio.id) {
                  <li class="flex items-center justify-between gap-3 px-3 py-2 text-sm">
                    <span>{{ municipio.nome }} — {{ municipio.uf }}</span>
                    <button
                      type="button"
                      class="font-medium text-emerald-700 underline"
                      (click)="adicionarMunicipio(municipio)"
                    >
                      Adicionar
                    </button>
                  </li>
                }
              </ul>
            }

            @if (municipios().length === 0) {
              <p class="mt-3 text-sm text-slate-600">Nenhum município selecionado.</p>
            } @else {
              <ul class="mt-3 flex flex-wrap gap-2">
                @for (municipio of municipios(); track municipio.id) {
                  <li class="flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-sm">
                    {{ municipio.nome }} — {{ municipio.uf }}
                    <button
                      type="button"
                      class="text-red-700"
                      [attr.aria-label]="'Remover ' + municipio.nome"
                      (click)="removerMunicipio(municipio.id)"
                    >
                      ×
                    </button>
                  </li>
                }
              </ul>
            }

            <button
              type="button"
              class="mt-4 rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
              [disabled]="salvando() !== null"
              (click)="salvarMunicipios()"
            >
              {{ salvando() === 'municipios' ? 'Salvando…' : 'Salvar municípios' }}
            </button>
          </div>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Períodos de retirada</h2>
            <p class="mt-1 text-sm text-slate-600">
              Horários usados no agendamento de retirada domiciliar; até dois períodos.
            </p>

            @for (periodo of periodos(); track $index) {
              <div class="mt-3 flex flex-wrap items-end gap-3">
                <label class="block text-sm">
                  <span class="font-medium text-slate-700">Período</span>
                  <select
                    class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                    [value]="periodo.periodo"
                    (change)="alterarPeriodo($index, 'periodo', $event)"
                  >
                    <option value="Manha">Manhã</option>
                    <option value="Tarde">Tarde</option>
                  </select>
                </label>

                <label class="block text-sm">
                  <span class="font-medium text-slate-700">Início</span>
                  <input
                    type="time"
                    class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                    [value]="periodo.horaInicio"
                    (input)="alterarPeriodo($index, 'horaInicio', $event)"
                  />
                </label>

                <label class="block text-sm">
                  <span class="font-medium text-slate-700">Fim</span>
                  <input
                    type="time"
                    class="mt-1 rounded-md border border-slate-300 px-3 py-2"
                    [value]="periodo.horaFim"
                    (input)="alterarPeriodo($index, 'horaFim', $event)"
                  />
                </label>

                <button
                  type="button"
                  class="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100"
                  (click)="removerPeriodo($index)"
                >
                  Remover
                </button>
              </div>
            }

            <div class="mt-4 flex flex-wrap gap-3">
              <button
                type="button"
                class="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-60"
                [disabled]="periodos().length >= 2"
                (click)="adicionarPeriodo()"
              >
                Adicionar período
              </button>
              <button
                type="button"
                class="rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
                [disabled]="salvando() !== null"
                (click)="salvarPeriodos()"
              >
                {{ salvando() === 'periodos' ? 'Salvando…' : 'Salvar períodos' }}
              </button>
            </div>
          </div>

          <div class="rounded-md bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold">Tipos de resíduo aceitos</h2>
            <p class="mt-1 text-sm text-slate-600">
              Marque os tipos que a organização recebe. A retirada domiciliar é opcional por tipo.
            </p>

            <ul class="mt-4 divide-y divide-slate-100">
              @for (tipo of tiposCatalogo(); track tipo.id) {
                <li class="flex flex-wrap items-center justify-between gap-3 py-2 text-sm">
                  <label class="flex items-center gap-2">
                    <input
                      type="checkbox"
                      [checked]="selecionados()[tipo.id] === true"
                      (change)="alternarTipo(tipo.id, $event)"
                    />
                    <span class="font-medium text-slate-800">{{ tipo.nome }}</span>
                    <span class="text-slate-500">{{ tipo.categoria }} · {{ tipo.unidade }}</span>
                  </label>

                  @if (selecionados()[tipo.id] === true) {
                    <label class="flex items-center gap-2 text-slate-700">
                      <input
                        type="checkbox"
                        [checked]="retiradaDomiciliar()[tipo.id] === true"
                        (change)="alternarRetirada(tipo.id, $event)"
                      />
                      Aceita retirada domiciliar
                    </label>
                  }
                </li>
              }
            </ul>

            <button
              type="button"
              class="mt-4 rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
              [disabled]="salvando() !== null"
              (click)="salvarTipos()"
            >
              {{ salvando() === 'tipos' ? 'Salvando…' : 'Salvar tipos' }}
            </button>
          </div>
        </div>
      }
    </section>
  `,
})
export class PerfilPage implements OnInit {
  private readonly servico = inject(PerfilService);
  private readonly catalogo = inject(CatalogoService);
  private readonly fb = inject(FormBuilder);

  protected readonly ufs = UFS;
  protected readonly tiposDeOrganizacao = TIPOS_DE_ORGANIZACAO;

  protected readonly perfil = signal<PerfilDaOrganizacao | null>(null);
  protected readonly tiposCatalogo = signal<TipoResiduo[]>([]);
  protected readonly municipios = signal<Municipio[]>([]);
  protected readonly resultadosMunicipio = signal<Municipio[]>([]);
  protected readonly periodos = signal<PeriodoDaOrganizacao[]>([]);
  protected readonly selecionados = signal<Record<string, boolean>>({});
  protected readonly retiradaDomiciliar = signal<Record<string, boolean>>({});

  protected readonly uf = signal('SP');
  protected readonly textoMunicipio = signal('');
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly salvando = signal<string | null>(null);
  protected readonly erros = signal<ErrosDoServidor>({});

  protected readonly formulario = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(120)]],
    tipo: ['Cooperativa' as TipoOrganizacao, [Validators.required]],
    contatoEmail: ['', [Validators.email, Validators.maxLength(254)]],
    contatoTelefone: ['', [Validators.maxLength(30)]],
    fuso: ['America/Sao_Paulo', [Validators.required, Validators.maxLength(60)]],
    limiteSolicitacoesAbertas: [10, [Validators.required, Validators.min(1), Validators.max(50)]],
  });

  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    forkJoin({ perfil: this.servico.obter(), tipos: this.catalogo.listarTipos() }).subscribe({
      next: ({ perfil, tipos }) => {
        this.tiposCatalogo.set(tipos);
        this.aplicarPerfil(perfil);
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

  protected salvarPerfil(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();
    const entrada: AtualizarPerfilEntrada = {
      nome: valores.nome.trim(),
      tipo: valores.tipo,
      contatoEmail: vazioComoNulo(valores.contatoEmail),
      contatoTelefone: vazioComoNulo(valores.contatoTelefone),
      fuso: valores.fuso.trim(),
      limiteSolicitacoesAbertas: Number(valores.limiteSolicitacoesAbertas),
    };

    this.salvar('perfil', this.servico.atualizar(entrada), 'Dados da organização atualizados.');
  }

  protected alternarPublicacao(): void {
    const perfil = this.perfil();
    if (perfil === null) {
      return;
    }

    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.salvando.set('publicacao');

    this.servico.definirPublicacao(!perfil.publicada).subscribe({
      next: (resposta) => {
        this.salvando.set(null);
        this.perfil.set({ ...perfil, publicada: resposta.publicada });
        this.mensagem.set(
          resposta.publicada ? 'Organização publicada.' : 'Organização despublicada.',
        );
      },
      error: (erro: unknown) => {
        this.salvando.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  protected selecionarUf(evento: Event): void {
    this.uf.set((evento.target as HTMLSelectElement).value);
  }

  protected digitarMunicipio(evento: Event): void {
    this.textoMunicipio.set((evento.target as HTMLInputElement).value);
  }

  protected buscarMunicipios(): void {
    this.erroDeAcao.set(null);

    this.catalogo.listarMunicipios(this.uf(), this.textoMunicipio()).subscribe({
      next: (municipios) => this.resultadosMunicipio.set(municipios),
      error: (erro: unknown) => this.erroDeAcao.set(mensagemDeErro(erro)),
    });
  }

  protected adicionarMunicipio(municipio: Municipio): void {
    this.resultadosMunicipio.set([]);
    this.textoMunicipio.set('');

    if (this.municipios().some((item) => item.id === municipio.id)) {
      return;
    }

    this.municipios.update((atuais) => [...atuais, municipio]);
  }

  protected removerMunicipio(id: number): void {
    this.municipios.update((atuais) => atuais.filter((municipio) => municipio.id !== id));
  }

  protected salvarMunicipios(): void {
    this.salvar(
      'municipios',
      this.servico.definirMunicipios(this.municipios().map((municipio) => municipio.id)),
      'Municípios atualizados.',
    );
  }

  protected adicionarPeriodo(): void {
    if (this.periodos().length >= 2) {
      return;
    }

    const manha = this.periodos().some((periodo) => periodo.periodo === 'Manha');
    this.periodos.update((atuais) => [
      ...atuais,
      manha
        ? { periodo: 'Tarde', horaInicio: '13:00', horaFim: '17:00' }
        : { periodo: 'Manha', horaInicio: '08:00', horaFim: '12:00' },
    ]);
  }

  protected removerPeriodo(indice: number): void {
    this.periodos.update((atuais) => atuais.filter((_, posicao) => posicao !== indice));
  }

  protected alterarPeriodo(indice: number, campo: keyof PeriodoDaOrganizacao, evento: Event): void {
    const valor = (evento.target as HTMLInputElement | HTMLSelectElement).value;

    this.periodos.update((atuais) =>
      atuais.map((periodo, posicao) =>
        posicao === indice ? { ...periodo, [campo]: valor } : periodo,
      ),
    );
  }

  protected salvarPeriodos(): void {
    this.salvar(
      'periodos',
      this.servico.definirPeriodos(this.periodos()),
      'Períodos de retirada atualizados.',
    );
  }

  protected alternarTipo(id: string, evento: Event): void {
    const marcado = (evento.target as HTMLInputElement).checked;

    this.selecionados.update((atuais) => ({ ...atuais, [id]: marcado }));
    if (!marcado) {
      this.retiradaDomiciliar.update((atuais) => ({ ...atuais, [id]: false }));
    }
  }

  protected alternarRetirada(id: string, evento: Event): void {
    const marcado = (evento.target as HTMLInputElement).checked;

    this.retiradaDomiciliar.update((atuais) => ({ ...atuais, [id]: marcado }));
  }

  protected salvarTipos(): void {
    const tipos: TipoHabilitadoEntrada[] = Object.entries(this.selecionados())
      .filter(([, marcado]) => marcado)
      .map(([tipoResiduoId]) => ({
        tipoResiduoId,
        aceitaRetiradaDomiciliar: this.retiradaDomiciliar()[tipoResiduoId] === true,
      }));

    this.salvar('tipos', this.servico.definirTipos(tipos), 'Tipos de resíduo atualizados.');
  }

  private salvar(
    chave: string,
    requisicao: ReturnType<PerfilService['atualizar']>,
    sucesso: string,
  ): void {
    this.erroDeAcao.set(null);
    this.mensagem.set(null);
    this.erros.set({});
    this.salvando.set(chave);

    requisicao.subscribe({
      next: (perfil) => {
        this.salvando.set(null);
        this.aplicarPerfil(perfil);
        this.mensagem.set(sucesso);
      },
      error: (erro: unknown) => {
        this.salvando.set(null);
        this.erros.set(errosPorCampo(erro));
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  private aplicarPerfil(perfil: PerfilDaOrganizacao): void {
    this.perfil.set(perfil);
    this.formulario.patchValue(
      {
        nome: perfil.nome,
        tipo: perfil.tipo,
        contatoEmail: perfil.contatoEmail ?? '',
        contatoTelefone: perfil.contatoTelefone ?? '',
        fuso: perfil.fuso,
        limiteSolicitacoesAbertas: perfil.limiteSolicitacoesAbertas,
      },
      { emitEvent: false },
    );
    this.municipios.set(perfil.municipios);
    this.periodos.set(perfil.periodos.map((periodo) => ({ ...periodo })));

    const selecionados: Record<string, boolean> = {};
    const retirada: Record<string, boolean> = {};
    for (const tipo of perfil.tipos) {
      selecionados[tipo.tipoResiduoId] = true;
      retirada[tipo.tipoResiduoId] = tipo.aceitaRetiradaDomiciliar;
    }

    this.selecionados.set(selecionados);
    this.retiradaDomiciliar.set(retirada);
  }
}

function vazioComoNulo(valor: string): string | null {
  const normalizado = valor.trim();
  return normalizado.length === 0 ? null : normalizado;
}
