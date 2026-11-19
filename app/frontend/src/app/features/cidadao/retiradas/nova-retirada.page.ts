import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import {
  formatarData,
  JanelaDeRetirada,
  OrganizacaoParaRetirada,
  rotuloDePeriodo,
  TipoDomiciliar,
} from '../../../core/retiradas/retiradas.models';
import { CatalogoService, Municipio } from '../../../core/servicos/catalogo.service';
import { campoInvalido, ErrosDoServidor, mensagensDoCampo } from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';
import { RetiradasDoCidadaoService } from './retiradas.service';

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

interface SelecaoDeTipo {
  marcado: boolean;
  quantidade: number;
}

@Component({
  selector: 'app-nova-retirada',
  imports: [ReactiveFormsModule, RouterLink, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <a routerLink="/cidadao/retiradas" class="text-sm font-medium text-emerald-700 underline">
        ← Minhas retiradas
      </a>

      <div class="mt-2">
        <h1 class="text-2xl font-semibold">Nova solicitação de retirada</h1>
        <p class="mt-1 text-sm text-slate-600">
          Escolha a organização, a data disponível e informe o endereço e os resíduos.
        </p>
      </div>

      @if (erroDeAcao() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeAcao() }}
        </p>
      }

      <div class="mt-6 space-y-4">
        <div class="cartao">
          <h2 class="text-lg font-semibold">1. Município</h2>
          <p class="mt-1 text-sm text-slate-600">
            A retirada está disponível nos municípios atendidos pelas organizações publicadas.
          </p>

          <div class="mt-4 flex flex-wrap items-end gap-3">
            <label class="campo">
              <span class="campo-rotulo">UF</span>
              <select class="mt-1" [value]="uf()" (change)="selecionarUf($event)">
                @for (estado of ufs; track estado) {
                  <option [value]="estado">{{ estado }}</option>
                }
              </select>
            </label>

            <label class="campo flex-1">
              <span class="campo-rotulo">Nome do município</span>
              <input
                class="mt-1 w-full"
                placeholder="Ex.: Campinas"
                [value]="textoMunicipio()"
                (input)="digitarMunicipio($event)"
                (keydown.enter)="buscarMunicipios()"
              />
            </label>

            <button type="button" class="botao-secundario" (click)="buscarMunicipios()">
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
                    (click)="escolherMunicipio(municipio)"
                  >
                    Selecionar
                  </button>
                </li>
              }
            </ul>
          }

          @if (municipio() !== null) {
            <p class="mt-3 flex flex-wrap items-center gap-2 text-sm text-slate-700">
              <span class="font-medium">{{ municipio()!.nome }} — {{ municipio()!.uf }}</span>
              <button type="button" class="text-red-700 underline" (click)="limparMunicipio()">
                Trocar município
              </button>
            </p>
          }
        </div>

        @if (municipio() !== null) {
          <div class="cartao">
            <h2 class="text-lg font-semibold">2. Organização e data</h2>

            @if (carregandoOrganizacoes()) {
              <p class="mt-3 text-sm text-slate-600" role="status">Carregando organizações…</p>
            } @else if (organizacoes().length === 0) {
              <p class="mt-3 rounded-md bg-slate-50 p-3 text-sm text-slate-600">
                Nenhuma organização publicada atende este município com retirada domiciliar.
              </p>
            } @else {
              <label class="campo mt-4 block">
                <span class="campo-rotulo">Organização</span>
                <select
                  class="mt-1 w-full"
                  [value]="organizacaoSelecionada()?.id ?? ''"
                  (change)="escolherOrganizacao($event)"
                >
                  <option value="">Selecione a organização</option>
                  @for (organizacao of organizacoes(); track organizacao.id) {
                    <option [value]="organizacao.id">{{ organizacao.nome }}</option>
                  }
                </select>
              </label>

              @if (organizacaoSelecionada(); as organizacao) {
                <p class="mt-2 text-xs text-slate-600">
                  {{ organizacao.nome }}
                  @if (organizacao.contatoTelefone !== null) {
                    · {{ organizacao.contatoTelefone }}
                  }
                  @if (organizacao.contatoEmail !== null) {
                    · {{ organizacao.contatoEmail }}
                  }
                </p>

                @if (carregandoJanelas()) {
                  <p class="mt-4 text-sm text-slate-600" role="status">
                    Carregando datas disponíveis…
                  </p>
                } @else if (janelas().length === 0) {
                  <p class="mt-4 rounded-md bg-slate-50 p-3 text-sm text-slate-600">
                    Não há datas com vagas disponíveis nos próximos 30 dias.
                  </p>
                } @else {
                  <fieldset class="mt-4">
                    <legend class="text-sm font-medium text-slate-700">Data e período</legend>
                    <div class="mt-2 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
                      @for (janela of janelas(); track janela.data + janela.periodo) {
                        <button
                          type="button"
                          class="rounded-lg border px-3 py-2 text-left text-sm transition"
                          [class]="
                            janelaSelecionada() === janela
                              ? 'border-emerald-600 bg-emerald-50 text-emerald-900'
                              : 'border-slate-300 bg-white text-slate-700 hover:border-emerald-300'
                          "
                          (click)="escolherJanela(janela)"
                        >
                          <span class="block font-semibold">{{ formatarData(janela.data) }}</span>
                          <span class="block text-xs">
                            {{ rotuloDePeriodo(janela.periodo) }} · {{ janela.horaInicio }}–{{
                              janela.horaFim
                            }}
                          </span>
                          <span class="mt-1 block text-xs text-slate-500">
                            {{ janela.vagas }} vaga(s) restante(s)
                          </span>
                        </button>
                      }
                    </div>
                  </fieldset>
                }
              }
            }
          </div>
        }

        @if (organizacaoSelecionada() !== null) {
          <form class="cartao" [formGroup]="formulario" (ngSubmit)="enviar()">
            <h2 class="text-lg font-semibold">3. Endereço de retirada</h2>

            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <label class="campo sm:col-span-2">
                <span class="campo-rotulo">Bairro</span>
                <input
                  formControlName="bairro"
                  placeholder="Ex.: Jardim das Flores"
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('bairro')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('bairro')" />
              </label>

              <label class="campo">
                <span class="campo-rotulo">Rua</span>
                <input
                  formControlName="rua"
                  placeholder="Ex.: Rua das Acácias"
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('rua')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('rua')" />
              </label>

              <label class="campo">
                <span class="campo-rotulo">Número</span>
                <input
                  formControlName="numero"
                  placeholder="Ex.: 120"
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('numero')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('numero')" />
              </label>

              <label class="campo">
                <span class="campo-rotulo">Complemento (opcional)</span>
                <input
                  formControlName="complemento"
                  placeholder="Ex.: Apto 12"
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('complemento')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('complemento')" />
              </label>

              <label class="campo">
                <span class="campo-rotulo">CEP</span>
                <input
                  formControlName="cep"
                  inputmode="numeric"
                  placeholder="00000-000"
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('cep')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('cep')" />
              </label>

              <label class="campo sm:col-span-2">
                <span class="campo-rotulo">Ponto de referência (opcional)</span>
                <input
                  formControlName="pontoReferencia"
                  placeholder="Ex.: Ao lado da padaria"
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('pontoReferencia')"
                />
                <app-mensagem-de-erro [mensagens]="mensagens('pontoReferencia')" />
              </label>

              <label class="campo sm:col-span-2">
                <span class="campo-rotulo">Observações (opcional)</span>
                <textarea
                  formControlName="observacoes"
                  rows="3"
                  placeholder="Ex.: Deixar os sacos na portaria."
                  class="mt-1 w-full"
                  [class.border-red-500]="invalido('observacoes')"
                ></textarea>
                <app-mensagem-de-erro [mensagens]="mensagens('observacoes')" />
              </label>
            </div>

            <h2 class="mt-6 text-lg font-semibold">4. Resíduos para retirada</h2>
            <p class="mt-1 text-sm text-slate-600">
              Informe uma estimativa por tipo. A organização confirma a quantidade na coleta.
            </p>

            <ul class="mt-3 divide-y divide-slate-100">
              @for (tipo of organizacaoSelecionada()!.tipos; track tipo.tipoResiduoId) {
                <li class="flex flex-wrap items-center justify-between gap-3 py-3 text-sm">
                  <label class="flex items-center gap-2">
                    <input
                      type="checkbox"
                      [checked]="selecaoDoTipo(tipo).marcado"
                      (change)="alternarTipo(tipo, $event)"
                    />
                    <span class="font-medium text-slate-900">{{ tipo.nome }}</span>
                    <span class="text-slate-500">{{ tipo.categoria }} · {{ tipo.unidade }}</span>
                  </label>

                  @if (selecaoDoTipo(tipo).marcado) {
                    <label class="flex items-center gap-2">
                      <span class="text-slate-600">Quantidade ({{ tipo.unidade }})</span>
                      <input
                        type="number"
                        min="0.001"
                        step="0.001"
                        placeholder="Ex.: 2"
                        class="w-28"
                        [value]="selecaoDoTipo(tipo).quantidade"
                        (input)="alterarQuantidade(tipo, $event)"
                      />
                    </label>
                  }
                </li>
              }
            </ul>

            @if (erroDeItens() !== null) {
              <p class="mt-3 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
                {{ erroDeItens() }}
              </p>
            }

            <div class="mt-6 flex flex-wrap items-center gap-3">
              <button type="submit" class="botao-primario" [disabled]="enviando()">
                {{ enviando() ? 'Enviando…' : 'Solicitar retirada' }}
              </button>
              <a routerLink="/cidadao/retiradas" class="botao-secundario">Voltar</a>
            </div>
          </form>
        }
      </div>
    </section>
  `,
})
export class NovaRetiradaPage {
  private readonly servico = inject(RetiradasDoCidadaoService);
  private readonly catalogo = inject(CatalogoService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  protected readonly ufs = UFS;
  protected readonly uf = signal('SP');
  protected readonly textoMunicipio = signal('');
  protected readonly resultadosMunicipio = signal<Municipio[]>([]);
  protected readonly municipio = signal<Municipio | null>(null);

  protected readonly organizacoes = signal<OrganizacaoParaRetirada[]>([]);
  protected readonly organizacaoSelecionada = signal<OrganizacaoParaRetirada | null>(null);
  protected readonly carregandoOrganizacoes = signal(false);
  protected readonly janelas = signal<JanelaDeRetirada[]>([]);
  protected readonly janelaSelecionada = signal<JanelaDeRetirada | null>(null);
  protected readonly carregandoJanelas = signal(false);

  protected readonly selecao = signal<Record<string, SelecaoDeTipo>>({});
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly erroDeItens = signal<string | null>(null);
  protected readonly erros = signal<ErrosDoServidor>({});
  protected readonly enviando = signal(false);

  protected readonly formatarData = formatarData;
  protected readonly rotuloDePeriodo = rotuloDePeriodo;

  protected readonly formulario = this.fb.nonNullable.group({
    bairro: ['', [Validators.required, Validators.maxLength(120)]],
    rua: ['', [Validators.required, Validators.maxLength(150)]],
    numero: ['', [Validators.required, Validators.maxLength(20)]],
    complemento: ['', [Validators.maxLength(80)]],
    cep: ['', [Validators.required, Validators.pattern(/^\d{5}-?\d{3}$/)]],
    pontoReferencia: ['', [Validators.maxLength(300)]],
    observacoes: ['', [Validators.maxLength(500)]],
  });

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

  protected escolherMunicipio(municipio: Municipio): void {
    this.municipio.set(municipio);
    this.resultadosMunicipio.set([]);
    this.textoMunicipio.set('');
    this.organizacaoSelecionada.set(null);
    this.organizacoes.set([]);
    this.janelas.set([]);
    this.janelaSelecionada.set(null);
    this.erroDeAcao.set(null);
    this.carregarOrganizacoes(municipio.id);
  }

  protected limparMunicipio(): void {
    this.municipio.set(null);
    this.organizacoes.set([]);
    this.organizacaoSelecionada.set(null);
    this.janelas.set([]);
    this.janelaSelecionada.set(null);
  }

  protected escolherOrganizacao(evento: Event): void {
    const id = (evento.target as HTMLSelectElement).value;
    const organizacao = this.organizacoes().find((item) => item.id === id) ?? null;

    this.organizacaoSelecionada.set(organizacao);
    this.janelas.set([]);
    this.janelaSelecionada.set(null);
    this.erroDeAcao.set(null);
    this.inicializarTipos(organizacao);

    if (organizacao !== null) {
      this.carregarJanelas(organizacao.id);
    }
  }

  protected escolherJanela(janela: JanelaDeRetirada): void {
    this.janelaSelecionada.set(janela);
  }

  protected selecaoDoTipo(tipo: TipoDomiciliar): SelecaoDeTipo {
    const atual = this.selecao()[tipo.tipoResiduoId];
    return atual ?? { marcado: false, quantidade: 1 };
  }

  protected alternarTipo(tipo: TipoDomiciliar, evento: Event): void {
    const marcado = (evento.target as HTMLInputElement).checked;

    this.selecao.update((atuais) => ({
      ...atuais,
      [tipo.tipoResiduoId]: {
        marcado,
        quantidade: atuais[tipo.tipoResiduoId]?.quantidade ?? 1,
      },
    }));
  }

  protected alterarQuantidade(tipo: TipoDomiciliar, evento: Event): void {
    const quantidade = Number((evento.target as HTMLInputElement).value);

    this.selecao.update((atuais) => ({
      ...atuais,
      [tipo.tipoResiduoId]: {
        marcado: atuais[tipo.tipoResiduoId]?.marcado ?? true,
        quantidade,
      },
    }));
  }

  protected invalido(campo: string): boolean {
    return campoInvalido(this.formulario, campo, this.erros());
  }

  protected mensagens(campo: string): string[] {
    return mensagensDoCampo(this.formulario, campo, this.erros());
  }

  protected enviar(): void {
    this.erroDeAcao.set(null);
    this.erroDeItens.set(null);
    this.erros.set({});

    const municipio = this.municipio();
    const organizacao = this.organizacaoSelecionada();
    const janela = this.janelaSelecionada();

    if (municipio === null || organizacao === null) {
      this.erroDeAcao.set('Selecione o município e a organização.');
      return;
    }

    if (janela === null) {
      this.erroDeAcao.set('Selecione a data e o período da retirada.');
      return;
    }

    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      this.erroDeAcao.set('Revise os dados do endereço para continuar.');
      return;
    }

    const itens = Object.entries(this.selecao())
      .filter(([, item]) => item.marcado)
      .map(([tipoResiduoId, item]) => ({
        tipoResiduoId,
        quantidadeEstimada: item.quantidade,
      }));

    if (itens.length === 0) {
      this.erroDeItens.set('Marque ao menos um tipo de resíduo.');
      return;
    }

    if (
      itens.some(
        (item) => !Number.isFinite(item.quantidadeEstimada) || item.quantidadeEstimada <= 0,
      )
    ) {
      this.erroDeItens.set('Informe uma quantidade maior que zero para os tipos marcados.');
      return;
    }

    if (
      itens.some(
        (item) => Math.round(item.quantidadeEstimada * 1000) !== item.quantidadeEstimada * 1000,
      )
    ) {
      this.erroDeItens.set('Informe quantidades com no máximo três casas decimais.');
      return;
    }

    const valores = this.formulario.getRawValue();
    this.enviando.set(true);

    this.servico
      .criar({
        organizacaoId: organizacao.id,
        municipioId: municipio.id,
        bairro: valores.bairro.trim(),
        rua: valores.rua.trim(),
        numero: valores.numero.trim(),
        complemento: vazioComoNulo(valores.complemento),
        cep: valores.cep.replace(/\D/g, ''),
        pontoReferencia: vazioComoNulo(valores.pontoReferencia),
        observacoes: vazioComoNulo(valores.observacoes),
        data: janela.data,
        periodo: janela.periodo,
        itens,
      })
      .subscribe({
        next: (retirada) => {
          this.enviando.set(false);
          void this.router.navigate(['/cidadao/retiradas', retirada.id], {
            queryParams: { criada: '1' },
          });
        },
        error: (erro: unknown) => {
          this.enviando.set(false);
          this.erros.set(errosPorCampo(erro));
          this.erroDeAcao.set(mensagemDeErro(erro));
        },
      });
  }

  private carregarOrganizacoes(municipioId: number): void {
    this.carregandoOrganizacoes.set(true);

    this.servico.listarOrganizacoes(municipioId).subscribe({
      next: (organizacoes) => {
        this.organizacoes.set(organizacoes);
        this.carregandoOrganizacoes.set(false);
      },
      error: (erro: unknown) => {
        this.carregandoOrganizacoes.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  private carregarJanelas(organizacaoId: string): void {
    this.carregandoJanelas.set(true);

    this.servico.listarJanelas(organizacaoId).subscribe({
      next: (janelas) => {
        this.janelas.set(janelas);
        this.carregandoJanelas.set(false);
      },
      error: (erro: unknown) => {
        this.carregandoJanelas.set(false);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }

  private inicializarTipos(organizacao: OrganizacaoParaRetirada | null): void {
    const selecao: Record<string, SelecaoDeTipo> = {};
    for (const tipo of organizacao?.tipos ?? []) {
      selecao[tipo.tipoResiduoId] = { marcado: false, quantidade: 1 };
    }

    this.selecao.set(selecao);
  }
}

function vazioComoNulo(valor: string): string | null {
  const normalizado = valor.trim();
  return normalizado.length === 0 ? null : normalizado;
}
