import { Component, input } from '@angular/core';

@Component({
  selector: 'app-mensagem-de-erro',
  template: `
    @if (mensagens().length > 0) {
      <p class="mt-1 text-sm text-red-700" role="alert">
        {{ mensagens()[0] }}
      </p>
    }
  `,
})
export class MensagemDeErro {
  readonly mensagens = input<string[]>([]);
}
