import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CATALOGO_DE_PLACEHOLDERS, ModuloEmBreve } from './catalogo-de-placeholders';

/**
 * Página única de sinalização do roadmap (DT-40). Recebe o módulo por dados de rota,
 * não oferece formulário, dado de exemplo nem prazo de disponibilidade.
 */
@Component({
  selector: 'app-em-breve',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-xl px-6 py-10">
      @if (modulo; as modulo) {
        <div
          class="mb-4 inline-flex items-center rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-amber-800"
        >
          Em breve
        </div>

        <h1 class="text-2xl font-semibold text-emerald-950 sm:text-3xl">{{ modulo.titulo }}</h1>
        <p class="mt-3 text-slate-700">{{ modulo.descricao }}</p>

        <p
          class="mt-6 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"
          role="status"
        >
          Este recurso ainda não está disponível. Ele está planejado para a fase
          {{ modulo.fase }} do roadmap. Nenhuma data de lançamento foi definida.
        </p>

        <p class="mt-4 text-sm text-slate-600">Fase do roadmap: {{ modulo.fase }}</p>
      } @else {
        <h1 class="text-2xl font-semibold text-emerald-950">Em breve</h1>
        <p class="mt-3 text-slate-700" role="status">Este conteúdo ainda não está disponível.</p>
      }

      <a
        routerLink="/pontos"
        class="mt-8 inline-block rounded-md border border-emerald-700 px-4 py-2 text-sm font-medium text-emerald-800 hover:bg-emerald-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
      >
        Voltar para pontos de coleta
      </a>
    </section>
  `,
})
export class EmBrevePage {
  private readonly rota = inject(ActivatedRoute);

  protected readonly modulo: ModuloEmBreve | undefined = CATALOGO_DE_PLACEHOLDERS.find(
    (item) => item.id === this.rota.snapshot.data['modulo'],
  );
}
