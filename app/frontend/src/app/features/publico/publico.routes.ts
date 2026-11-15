import { Routes } from '@angular/router';

export const PUBLICO_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./busca/busca.page').then((m) => m.BuscaPage),
  },
];
