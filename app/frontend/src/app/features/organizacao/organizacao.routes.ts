import { Routes } from '@angular/router';
import { autenticadoGuard, papelGuard, primeiroAcessoGuard } from '../../core/auth/auth.guards';

const GUARDAS_DE_ADMINISTRADOR = [
  autenticadoGuard,
  primeiroAcessoGuard,
  papelGuard('Administrador'),
];

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
  {
    path: 'equipe',
    pathMatch: 'full',
    canActivate: GUARDAS_DE_ADMINISTRADOR,
    loadComponent: () => import('./equipe/equipe.page').then((m) => m.EquipePage),
  },
  {
    path: 'equipe/nova',
    canActivate: GUARDAS_DE_ADMINISTRADOR,
    loadComponent: () => import('./equipe/operador.page').then((m) => m.OperadorPage),
  },
  {
    path: 'equipe/:id/editar',
    canActivate: GUARDAS_DE_ADMINISTRADOR,
    loadComponent: () => import('./equipe/operador.page').then((m) => m.OperadorPage),
  },
];
