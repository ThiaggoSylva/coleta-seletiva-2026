import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { codigoDoErro, errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import {
  ErrosDoServidor,
  campoInvalido,
  focarPrimeiroInvalido,
  mensagensDoCampo,
} from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';
import { DESCRICAO_DA_POLITICA_DE_SENHA, politicaDeSenha } from '../../../shared/politica-de-senha';
import { ContaDaEquipe, EquipeService } from './equipe.service';

@Component({
  selector: 'app-operador',
  imports: [ReactiveFormsModule, RouterLink, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-md px-6 py-10">
      <a routerLink="/organizacao/equipe" class="text-sm font-medium text-emerald-700 underline">
        Voltar para a equipe
      </a>

      <h1 class="mt-4 text-2xl font-semibold">
        {{ edicao ? 'Editar operador' : 'Novo operador' }}
      </h1>
      <p class="mt-1 text-sm text-slate-600">
        @if (edicao) {
          Atualize nome, usuário e e-mail. Papel e organização não podem ser alterados por aqui.
        } @else {
          A conta será criada como Operador na sua organização e deverá trocar a senha no primeiro
          acesso.
        }
      </p>

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando operador…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else {
        @if (erroGeral() !== null) {
          <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
            {{ erroGeral() }}
          </p>
        }

        <form class="mt-6 space-y-4" [formGroup]="formulario" (ngSubmit)="salvar()" novalidate>
          <div>
            <label for="nome" class="block text-sm font-medium text-slate-700">Nome</label>
            <input
              id="nome"
              #nome
              type="text"
              formControlName="nome"
              autocomplete="name"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('nome')"
              [attr.aria-describedby]="mensagens('nome').length > 0 ? 'nome-erro' : null"
            />
            <app-mensagem-de-erro id="nome-erro" [mensagens]="mensagens('nome')" />
          </div>

          <div>
            <label for="usuario" class="block text-sm font-medium text-slate-700">Usuário</label>
            <input
              id="usuario"
              #usuario
              type="text"
              formControlName="usuario"
              autocomplete="username"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('usuario')"
              [attr.aria-describedby]="mensagens('usuario').length > 0 ? 'usuario-erro' : null"
            />
            <app-mensagem-de-erro id="usuario-erro" [mensagens]="mensagens('usuario')" />
          </div>

          <div>
            <label for="email" class="block text-sm font-medium text-slate-700">E-mail</label>
            <input
              id="email"
              #email
              type="email"
              formControlName="email"
              autocomplete="email"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('email')"
              [attr.aria-describedby]="mensagens('email').length > 0 ? 'email-erro' : null"
            />
            <app-mensagem-de-erro id="email-erro" [mensagens]="mensagens('email')" />
          </div>

          @if (!edicao) {
            <div>
              <label for="senha" class="block text-sm font-medium text-slate-700">
                Senha inicial
              </label>
              <input
                id="senha"
                #senha
                type="password"
                formControlName="senha"
                autocomplete="new-password"
                class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
                [attr.aria-invalid]="campoInvalido('senha')"
                [attr.aria-describedby]="
                  mensagens('senha').length > 0 ? 'senha-dica senha-erro' : 'senha-dica'
                "
              />
              <p id="senha-dica" class="mt-1 text-xs text-slate-500">
                {{ descricaoDaPolitica }}
              </p>
              <app-mensagem-de-erro id="senha-erro" [mensagens]="mensagens('senha')" />
            </div>
          }

          <button
            type="submit"
            class="w-full rounded-md bg-emerald-700 px-4 py-2 font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
            [disabled]="enviando()"
          >
            {{ enviando() ? 'Salvando…' : edicao ? 'Salvar alterações' : 'Criar operador' }}
          </button>
        </form>
      }
    </section>
  `,
})
export class OperadorPage implements OnInit {
  private readonly equipe = inject(EquipeService);
  private readonly rota = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formularioBuilder = inject(NonNullableFormBuilder);

  @ViewChild('nome') private campoNome?: ElementRef<HTMLElement>;
  @ViewChild('usuario') private campoUsuario?: ElementRef<HTMLElement>;
  @ViewChild('email') private campoEmail?: ElementRef<HTMLElement>;
  @ViewChild('senha') private campoSenha?: ElementRef<HTMLElement>;

  protected readonly operadorId = this.rota.snapshot.paramMap.get('id');
  protected readonly edicao = this.operadorId !== null;
  protected readonly descricaoDaPolitica = DESCRICAO_DA_POLITICA_DE_SENHA;
  protected readonly formulario = this.formularioBuilder.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    usuario: ['', [Validators.required, Validators.maxLength(256)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    senha: [
      '',
      this.edicao ? [] : [Validators.required, politicaDeSenha, Validators.maxLength(128)],
    ],
  });
  protected readonly carregando = signal(false);
  protected readonly enviando = signal(false);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroGeral = signal<string | null>(null);
  protected readonly errosDoServidor = signal<ErrosDoServidor>({});

  ngOnInit(): void {
    if (this.edicao) {
      this.carregar();
    }
  }

  protected carregar(): void {
    const id = this.operadorId;
    if (id === null) {
      return;
    }

    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.equipe.obter(id).subscribe({
      next: (operador) => {
        this.formulario.patchValue({
          nome: operador.nome,
          usuario: operador.usuario,
          email: operador.email,
        });
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected campoInvalido(nome: string): boolean {
    return campoInvalido(this.formulario, nome);
  }

  protected mensagens(nome: string): string[] {
    return mensagensDoCampo(this.formulario, nome, this.errosDoServidor());
  }

  protected salvar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.focarPrimeiroInvalido();
      return;
    }

    this.enviando.set(true);
    this.erroGeral.set(null);
    this.errosDoServidor.set({});

    const valores = this.formulario.getRawValue();
    const id = this.operadorId;
    const requisicao: Observable<ContaDaEquipe> =
      id === null
        ? this.equipe.criar({
            nome: valores.nome,
            usuario: valores.usuario,
            email: valores.email,
            senha: valores.senha,
          })
        : this.equipe.editar(id, {
            nome: valores.nome,
            usuario: valores.usuario,
            email: valores.email,
          });

    requisicao.subscribe({
      next: () => {
        this.enviando.set(false);
        void this.router.navigate(['/organizacao/equipe'], {
          queryParams: id === null ? { criado: '1' } : { editado: '1' },
        });
      },
      error: (erro: unknown) => {
        this.enviando.set(false);
        const erros = errosPorCampo(erro);
        const codigo = codigoDoErro(erro);

        if (codigo === 'usuario_em_uso') {
          erros['usuario'] = erros['usuario'] ?? [mensagemDeErro(erro)];
        } else if (codigo === 'email_em_uso') {
          erros['email'] = erros['email'] ?? [mensagemDeErro(erro)];
        } else {
          this.erroGeral.set(mensagemDeErro(erro));
        }

        this.errosDoServidor.set(erros);
        this.focarPrimeiroInvalido(erros);
      },
    });
  }

  private focarPrimeiroInvalido(errosDoServidor: ErrosDoServidor = {}): void {
    focarPrimeiroInvalido(
      this.formulario,
      {
        nome: this.campoNome,
        usuario: this.campoUsuario,
        email: this.campoEmail,
        senha: this.campoSenha,
      },
      errosDoServidor,
    );
  }
}
