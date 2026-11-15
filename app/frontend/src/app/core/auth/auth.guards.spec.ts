import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, GuardResult, Router, RouterStateSnapshot } from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';
import { Papel } from './auth.models';
import { AuthService } from './auth.service';
import { autenticadoGuard, papelGuard, primeiroAcessoGuard } from './auth.guards';

type ResultadoDeGuard = GuardResult | Observable<GuardResult> | Promise<GuardResult>;

const ROTA = {} as ActivatedRouteSnapshot;
const ESTADO = { url: '/organizacao/inicio' } as RouterStateSnapshot;

function configurar(opcoes: {
  autenticado?: boolean;
  renovou?: boolean;
  pendente?: boolean;
  papeis?: Papel[];
}): {
  auth: {
    renovarSessao: ReturnType<typeof vi.fn>;
  };
  router: {
    createUrlTree: ReturnType<typeof vi.fn>;
  };
} {
  const auth = {
    autenticado: vi.fn(() => opcoes.autenticado ?? false),
    renovarSessao: vi.fn(() => of(opcoes.renovou ?? false)),
    trocaSenhaPendente: vi.fn(() => opcoes.pendente ?? false),
    possuiPapel: vi.fn((...papeis: Papel[]) =>
      (opcoes.papeis ?? []).some((papel) => papeis.includes(papel)),
    ),
  };
  const router = {
    createUrlTree: vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras,
    })),
    navigate: vi.fn(),
  };

  TestBed.configureTestingModule({
    providers: [
      { provide: AuthService, useValue: auth },
      { provide: Router, useValue: router },
    ],
  });

  return { auth, router };
}

async function resolver(resultado: ResultadoDeGuard): Promise<GuardResult> {
  return resultado instanceof Observable ? await firstValueFrom(resultado) : await resultado;
}

describe('guards de autenticação', () => {
  it('libera a rota quando já existe sessão', async () => {
    configurar({ autenticado: true });

    const resultado = await resolver(
      TestBed.runInInjectionContext(() => autenticadoGuard(ROTA, ESTADO)),
    );

    expect(resultado).toBe(true);
  });

  it('renova a sessão antes de liberar a rota', async () => {
    const { auth } = configurar({ autenticado: false, renovou: true });

    const resultado = await resolver(
      TestBed.runInInjectionContext(() => autenticadoGuard(ROTA, ESTADO)),
    );

    expect(resultado).toBe(true);
    expect(auth.renovarSessao).toHaveBeenCalledTimes(1);
  });

  it('leva ao login com returnUrl quando não há sessão renovável', async () => {
    configurar({ autenticado: false, renovou: false });

    const resultado = await resolver(
      TestBed.runInInjectionContext(() => autenticadoGuard(ROTA, ESTADO)),
    );

    expect(resultado).toEqual({
      commands: ['/acesso/login'],
      extras: { queryParams: { returnUrl: '/organizacao/inicio' } },
    });
  });

  it('encaminha a troca de senha pendente', () => {
    configurar({ autenticado: true, pendente: true });

    const resultado = TestBed.runInInjectionContext(() => primeiroAcessoGuard(ROTA, ESTADO));

    expect(resultado).toEqual({
      commands: ['/acesso/trocar-senha'],
      extras: undefined,
    });
  });

  it('permite a rota quando o papel é autorizado', () => {
    configurar({ autenticado: true, papeis: ['Administrador'] });

    const resultado = TestBed.runInInjectionContext(() =>
      papelGuard('Administrador')(ROTA, ESTADO),
    );

    expect(resultado).toBe(true);
  });

  it('redireciona o cidadão para o próprio perfil', () => {
    configurar({ autenticado: true, papeis: ['Cidadao'] });

    const resultado = TestBed.runInInjectionContext(() =>
      papelGuard('Administrador', 'Operador')(ROTA, ESTADO),
    );

    expect(resultado).toEqual({
      commands: ['/cidadao/perfil'],
      extras: undefined,
    });
  });

  it('redireciona os demais papéis para a área da organização', () => {
    configurar({ autenticado: true, papeis: ['Operador'] });

    const resultado = TestBed.runInInjectionContext(() =>
      papelGuard('Administrador')(ROTA, ESTADO),
    );

    expect(resultado).toEqual({
      commands: ['/organizacao/inicio'],
      extras: undefined,
    });
  });

  it('restaura a sessão antes de decidir o papel', async () => {
    const { auth } = configurar({ autenticado: false, renovou: true, papeis: ['Administrador'] });

    const resultado = await resolver(
      TestBed.runInInjectionContext(() => papelGuard('Administrador')(ROTA, ESTADO)),
    );

    expect(resultado).toBe(true);
    expect(auth.renovarSessao).toHaveBeenCalledTimes(1);
  });

  it('restaura a sessão antes de encaminhar a troca pendente', async () => {
    configurar({ autenticado: false, renovou: true, pendente: true });

    const resultado = await resolver(
      TestBed.runInInjectionContext(() => primeiroAcessoGuard(ROTA, ESTADO)),
    );

    expect(resultado).toEqual({
      commands: ['/acesso/trocar-senha'],
      extras: undefined,
    });
  });

  it('leva ao login quando a sessão não pode ser restaurada no guard de papel', async () => {
    configurar({ autenticado: false, renovou: false });

    const resultado = await resolver(
      TestBed.runInInjectionContext(() => papelGuard('Administrador')(ROTA, ESTADO)),
    );

    expect(resultado).toEqual({
      commands: ['/acesso/login'],
      extras: { queryParams: { returnUrl: '/organizacao/inicio' } },
    });
  });
});
