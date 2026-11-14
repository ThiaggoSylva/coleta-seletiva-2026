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
];
