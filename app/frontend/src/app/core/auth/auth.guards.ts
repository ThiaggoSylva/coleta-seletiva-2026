import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Observable, map } from 'rxjs';
import { Papel } from './auth.models';
import { AuthService } from './auth.service';

type Decisao = boolean | UrlTree;

function comSessao(
  decidir: () => Decisao,
  destinoDoLogin: () => UrlTree,
): Decisao | Observable<Decisao> {
  const auth = inject(AuthService);

  if (auth.autenticado()) {
    return decidir();
  }

  return auth.renovarSessao().pipe(map((renovou) => (renovou ? decidir() : destinoDoLogin())));
}

function loginComRetorno(router: Router, url: string): UrlTree {
  return router.createUrlTree(['/acesso/login'], { queryParams: { returnUrl: url } });
}

export const autenticadoGuard: CanActivateFn = (_rota, estado) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return comSessao(
    () => true,
    () => loginComRetorno(router, estado.url),
  );
};

export const primeiroAcessoGuard: CanActivateFn = (_rota, estado) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return comSessao(
    () => (auth.trocaSenhaPendente() ? router.createUrlTree(['/acesso/trocar-senha']) : true),
    () => loginComRetorno(router, estado.url),
  );
};

export function papelGuard(...papeis: Papel[]): CanActivateFn {
  return (_rota, estado) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    return comSessao(
      () => {
        if (auth.possuiPapel(...papeis)) {
          return true;
        }

        return auth.possuiPapel('Cidadao')
          ? router.createUrlTree(['/cidadao/perfil'])
          : router.createUrlTree(['/organizacao/inicio']);
      },
      () => loginComRetorno(router, estado.url),
    );
  };
}
