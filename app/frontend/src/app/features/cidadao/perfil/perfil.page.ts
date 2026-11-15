import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { API_BASE } from '../../../core/configuracao-api';
import { AuthService } from '../../../core/auth/auth.service';
import { codigoDoErro, errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import {
  ErrosDoServidor,
  campoInvalido,
  focarPrimeiroInvalido,
  mensagensDoCampo,
} from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';

interface PerfilDoCidadao {
  id: string;
  nome: string;
  email: string;
  telefone: string | null;
}

@Component({
  selector: 'app-perfil',
  imports: [ReactiveFormsModule, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-md px-6 py-10">
      <h1 class="text-2xl font-semibold">Meu perfil</h1>
      <p class="mt-1 text-sm text-slate-600">
        Atualize seu nome, e-mail e telefone. Papel e organização não podem ser alterados por aqui.
      </p>

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando perfil…</p>
      } @else {
        @if (sucesso() !== null) {
          <p class="mt-4 rounded-md bg-emerald-50 p-3 text-sm text-emerald-900" role="status">
            {{ sucesso() }}
          </p>
        }

        @if (erroGeral() !== null) {
          <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
            {{ erroGeral() }}
            <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
              Tentar novamente
            </button>
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
            <label for="email" class="block text-sm font-medium text-slate-700">E-mail</label>
            <input
              id="email"
              #email
              type="email"
              formControlName="email"
              autocomplete="email"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('email')"
              [attr.aria-describedby]="mensagens('email').length > 0 ? 'email-erro' : 'email-dica'"
            />
            <p id="email-dica" class="mt-1 text-xs text-slate-500">
              Este e-mail é o seu identificador de acesso e deve ser único na plataforma.
            </p>
            <app-mensagem-de-erro id="email-erro" [mensagens]="mensagens('email')" />
          </div>

          <div>
            <label for="telefone" class="block text-sm font-medium text-slate-700">
              Telefone <span class="font-normal text-slate-500">(opcional)</span>
            </label>
            <input
              id="telefone"
              #telefone
              type="tel"
              formControlName="telefone"
              autocomplete="tel"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('telefone')"
              [attr.aria-describedby]="mensagens('telefone').length > 0 ? 'telefone-erro' : null"
            />
            <app-mensagem-de-erro id="telefone-erro" [mensagens]="mensagens('telefone')" />
          </div>

          <button
            type="submit"
            class="w-full rounded-md bg-emerald-700 px-4 py-2 font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
            [disabled]="salvando()"
          >
            {{ salvando() ? 'Salvando…' : 'Salvar alterações' }}
          </button>
        </form>
      }
    </section>
  `,
})
export class PerfilPage implements OnInit {
  private readonly formularioBuilder = inject(NonNullableFormBuilder);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  @ViewChild('nome') private campoNome?: ElementRef<HTMLElement>;
  @ViewChild('email') private campoEmail?: ElementRef<HTMLElement>;
  @ViewChild('telefone') private campoTelefone?: ElementRef<HTMLElement>;

  protected readonly formulario = this.formularioBuilder.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    telefone: ['', [Validators.maxLength(20)]],
  });
  protected readonly carregando = signal(true);
  protected readonly salvando = signal(false);
  protected readonly sucesso = signal<string | null>(null);
  protected readonly erroGeral = signal<string | null>(null);
  protected readonly errosDoServidor = signal<ErrosDoServidor>({});

  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroGeral.set(null);
    this.sucesso.set(null);
    this.errosDoServidor.set({});

    this.http.get<PerfilDoCidadao>(`${API_BASE}/conta`).subscribe({
      next: (perfil) => {
        this.formulario.setValue({
          nome: perfil.nome,
          email: perfil.email,
          telefone: perfil.telefone ?? '',
        });
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroGeral.set(mensagemDeErro(erro));
      },
    });
  }

  protected campoInvalido(nome: string): boolean {
    return campoInvalido(this.formulario, nome, this.errosDoServidor());
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

    this.salvando.set(true);
    this.sucesso.set(null);
    this.erroGeral.set(null);
    this.errosDoServidor.set({});

    const { nome, email, telefone } = this.formulario.getRawValue();
    this.http
      .put<PerfilDoCidadao>(`${API_BASE}/conta`, {
        nome,
        email,
        telefone: telefone.trim().length > 0 ? telefone : null,
      })
      .subscribe({
        next: (perfil) => {
          this.salvando.set(false);
          this.formulario.setValue({
            nome: perfil.nome,
            email: perfil.email,
            telefone: perfil.telefone ?? '',
          });
          this.auth.atualizarConta(perfil.nome, perfil.email);
          this.sucesso.set('Perfil atualizado.');
        },
        error: (erro: unknown) => {
          this.salvando.set(false);
          const erros = errosPorCampo(erro);

          if (codigoDoErro(erro) === 'email_em_uso') {
            erros['email'] = erros['email'] ?? [mensagemDeErro(erro)];
            this.erroGeral.set(null);
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
        email: this.campoEmail,
        telefone: this.campoTelefone,
      },
      errosDoServidor,
    );
  }
}
