import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { Papel } from './auth.models';
import { AuthService } from './auth.service';

export const autenticadoGuard: CanActivateFn = (_rota, estado) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.autenticado()) {
    return true;
  }

  return auth.renovarSessao().pipe(
    map((renovou) =>
      renovou
        ? true
        : router.createUrlTree(['/acesso/login'], {
            queryParams: { returnUrl: estado.url },
          }),
    ),
  );
};

export const primeiroAcessoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.trocaSenhaPendente() ? router.createUrlTree(['/acesso/trocar-senha']) : true;
};

export function papelGuard(...papeis: Papel[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.possuiPapel(...papeis)) {
      return true;
    }

    return auth.possuiPapel('Cidadao')
      ? router.createUrlTree(['/cidadao/perfil'])
      : router.createUrlTree(['/organizacao/inicio']);
  };
}
