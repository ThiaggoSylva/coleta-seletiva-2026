import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Papel } from './core/auth/auth.models';
import { AuthService } from './core/auth/auth.service';

const ROTULOS_DE_PAPEL: Record<Papel, string> = {
  Administrador: 'Administrador',
  Operador: 'Operador',
  Cidadao: 'Cidadão',
};

@Component({
  imports: [RouterOutlet, RouterLink],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  protected readonly rotuloDoPapel = computed(() => {
    const papel = this.auth.papel();
    return papel === null ? '' : ROTULOS_DE_PAPEL[papel];
  });

  protected sair(): void {
    this.auth.logout().subscribe(() => {
      void this.router.navigate(['/acesso/login']);
    });
  }
}
