import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'acesso',
    loadChildren: () => import('./features/acesso/acesso.routes').then((m) => m.ACESSO_ROUTES),
  },
  {
    path: 'cidadao',
    loadChildren: () => import('./features/cidadao/cidadao.routes').then((m) => m.CIDADAO_ROUTES),
  },
  {
    path: 'organizacao',
    loadChildren: () =>
      import('./features/organizacao/organizacao.routes').then((m) => m.ORGANIZACAO_ROUTES),
  },
];
