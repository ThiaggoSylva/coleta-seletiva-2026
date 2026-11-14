import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ContaAutenticada } from '../../../core/auth/auth.models';
import { errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import {
  ErrosDoServidor,
  campoInvalido,
  focarPrimeiroInvalido,
  mensagensDoCampo,
} from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-md px-6 py-10">
      <h1 class="text-2xl font-semibold">Entrar</h1>
      <p class="mt-1 text-sm text-slate-600">Acesse sua conta de organização ou de cidadão.</p>

      @if (senhaAlterada()) {
        <p class="mt-4 rounded-md bg-emerald-50 p-3 text-sm text-emerald-900" role="status">
          Senha alterada. Entre novamente com a nova senha.
        </p>
      }

      @if (erroGeral() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroGeral() }}
        </p>
      }

      <form class="mt-6 space-y-4" [formGroup]="formulario" (ngSubmit)="entrar()" novalidate>
        <div>
          <label for="identificador" class="block text-sm font-medium text-slate-700">
            Usuário ou e-mail
          </label>
          <input
            id="identificador"
            #identificador
            type="text"
            formControlName="identificador"
            autocomplete="username"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
            [attr.aria-invalid]="campoInvalido('identificador')"
            [attr.aria-describedby]="
              mensagens('identificador').length > 0 ? 'identificador-erro' : null
            "
          />
          <app-mensagem-de-erro id="identificador-erro" [mensagens]="mensagens('identificador')" />
        </div>

        <div>
          <label for="senha" class="block text-sm font-medium text-slate-700">Senha</label>
          <input
            id="senha"
            #senha
            type="password"
            formControlName="senha"
            autocomplete="current-password"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
            [attr.aria-invalid]="campoInvalido('senha')"
            [attr.aria-describedby]="mensagens('senha').length > 0 ? 'senha-erro' : null"
          />
          <app-mensagem-de-erro id="senha-erro" [mensagens]="mensagens('senha')" />
        </div>

        <button
          type="submit"
          class="w-full rounded-md bg-emerald-700 px-4 py-2 font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
          [disabled]="enviando()"
        >
          {{ enviando() ? 'Entrando…' : 'Entrar' }}
        </button>
      </form>

      <p class="mt-6 text-sm text-slate-600">
        Ainda não tem conta?
        <a routerLink="/acesso/registro-cidadao" class="font-medium text-emerald-700 underline">
          Cadastre-se como cidadão
        </a>
        ou
        <a routerLink="/acesso/registro-organizacao" class="font-medium text-emerald-700 underline">
          cadastre sua organização </a
        >.
      </p>
    </section>
  `,
})
export class LoginPage {
  private readonly formularioBuilder = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly rota = inject(ActivatedRoute);

  @ViewChild('identificador') private campIdentificador?: ElementRef<HTMLElement>;
  @ViewChild('senha') private campoSenha?: ElementRef<HTMLElement>;

  protected readonly senhaAlterada = signal(
    this.rota.snapshot.queryParamMap.get('senhaAlterada') === '1',
  );
  protected readonly formulario = this.formularioBuilder.group({
    identificador: ['', [Validators.required]],
    senha: ['', [Validators.required]],
  });
  protected readonly enviando = signal(false);
  protected readonly erroGeral = signal<string | null>(null);
  protected readonly errosDoServidor = signal<ErrosDoServidor>({});

  protected campoInvalido(nome: string): boolean {
    return campoInvalido(this.formulario, nome);
  }

  protected mensagens(nome: string): string[] {
    return mensagensDoCampo(this.formulario, nome, this.errosDoServidor());
  }

  protected entrar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      focarPrimeiroInvalido(this.formulario, {
        identificador: this.campIdentificador,
        senha: this.campoSenha,
      });
      return;
    }

    this.enviando.set(true);
    this.erroGeral.set(null);
    this.errosDoServidor.set({});

    const { identificador, senha } = this.formulario.getRawValue();
    this.auth.login(identificador, senha).subscribe({
      next: (conta) => {
        this.enviando.set(false);
        void this.router.navigateByUrl(this.destino(conta));
      },
      error: (erro: unknown) => {
        this.enviando.set(false);
        this.erroGeral.set(mensagemDeErro(erro));
        this.errosDoServidor.set(errosPorCampo(erro));
        focarPrimeiroInvalido(
          this.formulario,
          {
            identificador: this.campIdentificador,
            senha: this.campoSenha,
          },
          this.errosDoServidor(),
        );
      },
    });
  }

  private destino(conta: ContaAutenticada): string {
    const returnUrl = this.rota.snapshot.queryParamMap.get('returnUrl');
    if (returnUrl !== null && returnUrl.startsWith('/')) {
      return returnUrl;
    }

    if (conta.trocaSenhaPendente) {
      return '/acesso/trocar-senha';
    }

    if (conta.papel === 'Cidadao') {
      return '/cidadao/perfil';
    }

    return conta.papel === 'Administrador' ? '/organizacao/equipe' : '/organizacao/inicio';
  }
}
