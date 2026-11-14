import { Routes } from '@angular/router';
import { autenticadoGuard, papelGuard, primeiroAcessoGuard } from '../../core/auth/auth.guards';

export const ORGANIZACAO_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'inicio',
  },
  {
    path: 'inicio',
    canActivate: [autenticadoGuard, primeiroAcessoGuard, papelGuard('Administrador', 'Operador')],
    loadComponent: () => import('./inicio/inicio.page').then((m) => m.InicioPage),
  },
];
