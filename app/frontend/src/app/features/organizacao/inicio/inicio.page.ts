import { Component, inject } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-organizacao-inicio',
  template: `
    <section class="mx-auto max-w-3xl px-6 py-10">
      <h1 class="text-2xl font-semibold">Área da organização</h1>
      <p class="mt-2 text-slate-700">Bem-vindo(a), {{ auth.contaAtual()?.nome }}.</p>
      <p class="mt-4 text-slate-600">
        As demais funcionalidades da organização serão liberadas nas próximas fases.
      </p>
    </section>
  `,
})
export class InicioPage {
  protected readonly auth = inject(AuthService);
}
