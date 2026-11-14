import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE } from '../configuracao-api';
import { AuthService } from './auth.service';

export const TENTOU_RENOVAR = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.obterToken();
  const rotaDeSessao = ehRotaDeSessao(requisicao.url);
  const comToken =
    token !== null && ehRotaDaApi(requisicao.url) && !rotaDeSessao
      ? requisicao.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : requisicao;

  return proximo(comToken).pipe(
    catchError((erro: unknown) => {
      const deveRenovar =
        erro instanceof HttpErrorResponse &&
        erro.status === 401 &&
        !rotaDeSessao &&
        !requisicao.context.get(TENTOU_RENOVAR);

      if (!deveRenovar) {
        return throwError(() => erro);
      }

      return auth.renovarSessao().pipe(
        switchMap((renovou) => {
          if (!renovou) {
            void router.navigate(['/acesso/login'], {
              queryParams: { returnUrl: router.url },
            });
            return throwError(() => erro);
          }

          const novoToken = auth.obterToken();
          const repetida = requisicao.clone({
            context: requisicao.context.set(TENTOU_RENOVAR, true),
            setHeaders: novoToken === null ? {} : { Authorization: `Bearer ${novoToken}` },
          });

          return proximo(repetida);
        }),
      );
    }),
  );
};

function ehRotaDaApi(url: string): boolean {
  return url.startsWith(API_BASE);
}

function ehRotaDeSessao(url: string): boolean {
  return (
    url.startsWith(`${API_BASE}/auth/login`) ||
    url.startsWith(`${API_BASE}/auth/refresh`) ||
    url.startsWith(`${API_BASE}/auth/logout`)
  );
}
