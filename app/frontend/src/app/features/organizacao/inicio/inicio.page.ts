import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-organizacao-inicio',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-3xl px-6 py-10">
      <h1 class="text-2xl font-semibold">Área da organização</h1>
      <p class="mt-2 text-slate-700">Bem-vindo(a), {{ auth.contaAtual()?.nome }}.</p>
      <ul class="mt-6 space-y-2 text-slate-700">
        <li>
          <a routerLink="/organizacao/perfil" class="font-medium text-emerald-700 underline">
            Perfil, municípios, períodos e tipos aceitos
          </a>
        </li>
        <li>
          <a routerLink="/organizacao/pontos" class="font-medium text-emerald-700 underline">
            Pontos de coleta
          </a>
        </li>
        <li>
          <a routerLink="/organizacao/equipe" class="font-medium text-emerald-700 underline">
            Equipe
          </a>
        </li>
      </ul>
    </section>
  `,
})
export class InicioPage {
  protected readonly auth = inject(AuthService);
}
