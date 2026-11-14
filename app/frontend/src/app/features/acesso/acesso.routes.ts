import { Routes } from '@angular/router';
import { autenticadoGuard } from '../../core/auth/auth.guards';

export const ACESSO_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login.page').then((m) => m.LoginPage),
  },
  {
    path: 'registro-organizacao',
    loadComponent: () =>
      import('./registro-organizacao/registro-organizacao.page').then(
        (m) => m.RegistroOrganizacaoPage,
      ),
  },
  {
    path: 'registro-cidadao',
    loadComponent: () =>
      import('./registro-cidadao/registro-cidadao.page').then((m) => m.RegistroCidadaoPage),
  },
  {
    path: 'trocar-senha',
    canActivate: [autenticadoGuard],
    loadComponent: () => import('./trocar-senha/trocar-senha.page').then((m) => m.TrocarSenhaPage),
  },
];
