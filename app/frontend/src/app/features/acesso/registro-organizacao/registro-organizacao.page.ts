import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { API_BASE } from '../../../core/configuracao-api';
import { errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import {
  ErrosDoServidor,
  campoInvalido,
  focarPrimeiroInvalido,
  mensagensDoCampo,
} from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';

@Component({
  selector: 'app-registro-organizacao',
  imports: [ReactiveFormsModule, RouterLink, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-md px-6 py-10">
      <h1 class="text-2xl font-semibold">Cadastrar organização</h1>
      <p class="mt-1 text-sm text-slate-600">
        A organização nasce não publicada e a conta administradora é criada na mesma operação.
      </p>

      @if (sucesso()) {
        <div class="mt-4 rounded-md bg-emerald-50 p-3 text-sm text-emerald-900" role="status">
          Organização cadastrada. Agora entre com suas credenciais.
          <a routerLink="/acesso/login" class="font-medium underline">Ir para o login</a>.
        </div>
      } @else {
        @if (erroGeral() !== null) {
          <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
            {{ erroGeral() }}
          </p>
        }

        <form class="mt-6 space-y-4" [formGroup]="formulario" (ngSubmit)="cadastrar()" novalidate>
          <div>
            <label for="nomeOrganizacao" class="block text-sm font-medium text-slate-700">
              Nome da organização
            </label>
            <input
              id="nomeOrganizacao"
              #nomeOrganizacao
              type="text"
              formControlName="nomeOrganizacao"
              autocomplete="organization"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('nomeOrganizacao')"
              [attr.aria-describedby]="
                mensagens('nomeOrganizacao').length > 0 ? 'nomeOrganizacao-erro' : null
              "
            />
            <app-mensagem-de-erro
              id="nomeOrganizacao-erro"
              [mensagens]="mensagens('nomeOrganizacao')"
            />
          </div>

          <div>
            <label for="tipo" class="block text-sm font-medium text-slate-700">Tipo</label>
            <select
              id="tipo"
              #tipo
              formControlName="tipo"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('tipo')"
            >
              <option value="Cooperativa">Cooperativa</option>
              <option value="Associacao">Associação</option>
              <option value="Prefeitura">Prefeitura</option>
              <option value="Outro">Outro</option>
            </select>
            <app-mensagem-de-erro id="tipo-erro" [mensagens]="mensagens('tipo')" />
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

          <div>
            <label for="senha" class="block text-sm font-medium text-slate-700">Senha</label>
            <input
              id="senha"
              #senha
              type="password"
              formControlName="senha"
              autocomplete="new-password"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
              [attr.aria-invalid]="campoInvalido('senha')"
              [attr.aria-describedby]="mensagens('senha').length > 0 ? 'senha-erro' : 'senha-dica'"
            />
            <p id="senha-dica" class="mt-1 text-xs text-slate-500">
              Use pelo menos 8 caracteres, com letras, número e símbolo.
            </p>
            <app-mensagem-de-erro id="senha-erro" [mensagens]="mensagens('senha')" />
          </div>

          <button
            type="submit"
            class="w-full rounded-md bg-emerald-700 px-4 py-2 font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
            [disabled]="enviando()"
          >
            {{ enviando() ? 'Cadastrando…' : 'Cadastrar organização' }}
          </button>
        </form>

        <p class="mt-6 text-sm text-slate-600">
          Já tem conta?
          <a routerLink="/acesso/login" class="font-medium text-emerald-700 underline">Entrar</a>.
        </p>
      }
    </section>
  `,
})
export class RegistroOrganizacaoPage {
  private readonly formularioBuilder = inject(NonNullableFormBuilder);
  private readonly http = inject(HttpClient);

  @ViewChild('nomeOrganizacao') private campoNomeOrganizacao?: ElementRef<HTMLElement>;
  @ViewChild('tipo') private campoTipo?: ElementRef<HTMLElement>;
  @ViewChild('usuario') private campoUsuario?: ElementRef<HTMLElement>;
  @ViewChild('email') private campoEmail?: ElementRef<HTMLElement>;
  @ViewChild('senha') private campoSenha?: ElementRef<HTMLElement>;

  protected readonly formulario = this.formularioBuilder.group({
    nomeOrganizacao: ['', [Validators.required]],
    tipo: ['Cooperativa', [Validators.required]],
    usuario: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8)]],
  });
  protected readonly enviando = signal(false);
  protected readonly sucesso = signal(false);
  protected readonly erroGeral = signal<string | null>(null);
  protected readonly errosDoServidor = signal<ErrosDoServidor>({});

  protected campoInvalido(nome: string): boolean {
    return campoInvalido(this.formulario, nome);
  }

  protected mensagens(nome: string): string[] {
    return mensagensDoCampo(this.formulario, nome, this.errosDoServidor());
  }

  protected cadastrar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.focarPrimeiroInvalido();
      return;
    }

    this.enviando.set(true);
    this.erroGeral.set(null);
    this.errosDoServidor.set({});

    this.http
      .post(`${API_BASE}/auth/registro-organizacao`, this.formulario.getRawValue())
      .subscribe({
        next: () => {
          this.enviando.set(false);
          this.sucesso.set(true);
        },
        error: (erro: unknown) => {
          this.enviando.set(false);
          this.erroGeral.set(mensagemDeErro(erro));
          this.errosDoServidor.set(errosPorCampo(erro));
          this.focarPrimeiroInvalido(this.errosDoServidor());
        },
      });
  }

  private focarPrimeiroInvalido(errosDoServidor: ErrosDoServidor = {}): void {
    focarPrimeiroInvalido(
      this.formulario,
      {
        nomeOrganizacao: this.campoNomeOrganizacao,
        tipo: this.campoTipo,
        usuario: this.campoUsuario,
        email: this.campoEmail,
        senha: this.campoSenha,
      },
      errosDoServidor,
    );
  }
}
