import { Routes } from '@angular/router';
import { autenticadoGuard, papelGuard, primeiroAcessoGuard } from '../../core/auth/auth.guards';

const GUARDAS_DO_CIDADAO = [autenticadoGuard, primeiroAcessoGuard, papelGuard('Cidadao')];
const GUARDAS_DA_EQUIPE = [
  autenticadoGuard,
  primeiroAcessoGuard,
  papelGuard('Administrador', 'Operador'),
];

export const ROADMAP_ROUTES: Routes = [
  {
    path: 'preferencias-email',
    canActivate: GUARDAS_DO_CIDADAO,
    data: { modulo: 'preferencias-email' },
    loadComponent: () => import('./em-breve.page').then((m) => m.EmBrevePage),
  },
  {
    path: 'extrato-pontos',
    canActivate: GUARDAS_DO_CIDADAO,
    data: { modulo: 'extrato-pontos' },
    loadComponent: () => import('./em-breve.page').then((m) => m.EmBrevePage),
  },
  {
    path: 'rotas-coletor',
    canActivate: GUARDAS_DA_EQUIPE,
    data: { modulo: 'rotas-coletor' },
    loadComponent: () => import('./em-breve.page').then((m) => m.EmBrevePage),
  },
  {
    path: 'transparencia-impacto',
    data: { modulo: 'transparencia-impacto' },
    loadComponent: () => import('./em-breve.page').then((m) => m.EmBrevePage),
  },
];
