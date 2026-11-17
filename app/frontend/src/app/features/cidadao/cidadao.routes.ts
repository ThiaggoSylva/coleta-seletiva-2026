import { Routes } from '@angular/router';
import { autenticadoGuard, papelGuard, primeiroAcessoGuard } from '../../core/auth/auth.guards';

export const CIDADAO_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'perfil',
  },
  {
    path: 'perfil',
    canActivate: [autenticadoGuard, primeiroAcessoGuard, papelGuard('Cidadao')],
    loadComponent: () => import('./perfil/perfil.page').then((m) => m.PerfilPage),
  },
  {
    path: 'retiradas',
    pathMatch: 'full',
    canActivate: [autenticadoGuard, primeiroAcessoGuard, papelGuard('Cidadao')],
    loadComponent: () =>
      import('./retiradas/minhas-retiradas.page').then((m) => m.MinhasRetiradasPage),
  },
  {
    path: 'retiradas/nova',
    canActivate: [autenticadoGuard, primeiroAcessoGuard, papelGuard('Cidadao')],
    loadComponent: () =>
      import('./retiradas/nova-retirada.page').then((m) => m.NovaRetiradaPage),
  },
  {
    path: 'retiradas/:id',
    canActivate: [autenticadoGuard, primeiroAcessoGuard, papelGuard('Cidadao')],
    loadComponent: () => import('./retiradas/minha-retirada.page').then((m) => m.MinhaRetiradaPage),
  },
];
