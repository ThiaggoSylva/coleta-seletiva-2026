import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { errosPorCampo, mensagemDeErro } from '../../../core/erros/erro-api';
import {
  ErrosDoServidor,
  campoInvalido,
  focarPrimeiroInvalido,
  mensagensDoCampo,
} from '../../../shared/formulario';
import { MensagemDeErro } from '../../../shared/mensagem-de-erro';
import {
  DESCRICAO_DA_POLITICA_DE_SENHA,
  confirmacaoDeSenha,
  politicaDeSenha,
} from '../../../shared/politica-de-senha';

@Component({
  selector: 'app-trocar-senha',
  imports: [ReactiveFormsModule, MensagemDeErro],
  template: `
    <section class="mx-auto max-w-md px-6 py-10">
      @if (auth.trocaSenhaPendente()) {
        <h1 class="text-2xl font-semibold">Primeiro acesso</h1>
        <p class="mt-1 text-sm text-slate-600">
          Defina uma nova senha para concluir o primeiro acesso e liberar os demais recursos.
        </p>
      } @else {
        <h1 class="text-2xl font-semibold">Alterar senha</h1>
        <p class="mt-1 text-sm text-slate-600">
          Informe a senha atual e defina uma nova senha. Ao concluir, entre novamente.
        </p>
      }

      @if (erroGeral() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroGeral() }}
        </p>
      }

      <form class="mt-6 space-y-4" [formGroup]="formulario" (ngSubmit)="alterar()" novalidate>
        <div>
          <label for="senhaAtual" class="block text-sm font-medium text-slate-700">
            Senha atual
          </label>
          <input
            id="senhaAtual"
            #senhaAtual
            type="password"
            formControlName="senhaAtual"
            autocomplete="current-password"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
            [attr.aria-invalid]="campoInvalido('senhaAtual')"
            [attr.aria-describedby]="mensagens('senhaAtual').length > 0 ? 'senhaAtual-erro' : null"
          />
          <app-mensagem-de-erro id="senhaAtual-erro" [mensagens]="mensagens('senhaAtual')" />
        </div>

        <div>
          <label for="novaSenha" class="block text-sm font-medium text-slate-700">
            Nova senha
          </label>
          <input
            id="novaSenha"
            #novaSenha
            type="password"
            formControlName="novaSenha"
            autocomplete="new-password"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
            [attr.aria-invalid]="campoInvalido('novaSenha')"
            [attr.aria-describedby]="
              mensagens('novaSenha').length > 0 ? 'novaSenha-dica novaSenha-erro' : 'novaSenha-dica'
            "
          />
          <p id="novaSenha-dica" class="mt-1 text-xs text-slate-500">
            {{ descricaoDaPolitica }}
          </p>
          <app-mensagem-de-erro id="novaSenha-erro" [mensagens]="mensagens('novaSenha')" />
        </div>

        <div>
          <label for="confirmacao" class="block text-sm font-medium text-slate-700">
            Confirmar nova senha
          </label>
          <input
            id="confirmacao"
            #confirmacao
            type="password"
            formControlName="confirmacao"
            autocomplete="new-password"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 focus:border-emerald-600 focus:outline-none"
            [attr.aria-invalid]="campoInvalido('confirmacao')"
            [attr.aria-describedby]="
              mensagens('confirmacao').length > 0 ? 'confirmacao-erro' : null
            "
          />
          <app-mensagem-de-erro id="confirmacao-erro" [mensagens]="mensagens('confirmacao')" />
        </div>

        <button
          type="submit"
          class="w-full rounded-md bg-emerald-700 px-4 py-2 font-medium text-white hover:bg-emerald-800 disabled:opacity-60"
          [disabled]="enviando()"
        >
          {{ enviando() ? 'Alterando…' : 'Alterar senha' }}
        </button>
      </form>
    </section>
  `,
})
export class TrocarSenhaPage {
  private readonly formularioBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  @ViewChild('senhaAtual') private campoSenhaAtual?: ElementRef<HTMLElement>;
  @ViewChild('novaSenha') private campoNovaSenha?: ElementRef<HTMLElement>;
  @ViewChild('confirmacao') private campoConfirmacao?: ElementRef<HTMLElement>;

  protected readonly descricaoDaPolitica = DESCRICAO_DA_POLITICA_DE_SENHA;
  protected readonly formulario = this.formularioBuilder.group({
    senhaAtual: ['', [Validators.required]],
    novaSenha: ['', [Validators.required, politicaDeSenha]],
    confirmacao: ['', [Validators.required, confirmacaoDeSenha]],
  });
  protected readonly enviando = signal(false);
  protected readonly erroGeral = signal<string | null>(null);
  protected readonly errosDoServidor = signal<ErrosDoServidor>({});

  constructor() {
    this.formulario.controls.novaSenha.valueChanges.subscribe(() => {
      this.formulario.controls.confirmacao.updateValueAndValidity({ emitEvent: false });
    });
  }

  protected campoInvalido(nome: string): boolean {
    return campoInvalido(this.formulario, nome);
  }

  protected mensagens(nome: string): string[] {
    return mensagensDoCampo(this.formulario, nome, this.errosDoServidor());
  }

  protected alterar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.focarPrimeiroInvalido();
      return;
    }

    this.enviando.set(true);
    this.erroGeral.set(null);
    this.errosDoServidor.set({});

    const { senhaAtual, novaSenha } = this.formulario.getRawValue();
    this.auth.alterarSenha(senhaAtual, novaSenha).subscribe({
      next: () => {
        this.enviando.set(false);
        void this.router.navigate(['/acesso/login'], {
          queryParams: { senhaAlterada: '1' },
        });
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
        senhaAtual: this.campoSenhaAtual,
        novaSenha: this.campoNovaSenha,
        confirmacao: this.campoConfirmacao,
      },
      errosDoServidor,
    );
  }
}
