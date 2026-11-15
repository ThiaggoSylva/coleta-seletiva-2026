import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ContaAutenticada, SessaoResposta } from './auth.models';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

const CONTA: ContaAutenticada = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Ana',
  email: 'ana@example.org',
  papel: 'Cidadao',
  organizacaoId: null,
  trocaSenhaPendente: false,
};

function criarSessao(token: string): SessaoResposta {
  return {
    tokenDeAcesso: token,
    expiraEmUtc: new Date(Date.now() + 900_000).toISOString(),
    usuario: CONTA,
  };
}

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let cliente: HttpClient;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    cliente = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    http.verify();
  });

  function entrar(token = 'token-1'): void {
    auth.login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush(criarSessao(token));
  }

  it('envia o bearer somente para a API', () => {
    entrar();

    cliente.get('/api/v1/equipe').subscribe();
    const api = http.expectOne('/api/v1/equipe');
    expect(api.request.headers.get('Authorization')).toBe('Bearer token-1');
    api.flush([]);

    cliente.get('https://exemplo.org/dados').subscribe();
    const externo = http.expectOne('https://exemplo.org/dados');
    expect(externo.request.headers.has('Authorization')).toBe(false);
    externo.flush({});
  });

  it('não envia bearer nas rotas de sessão', () => {
    entrar();

    auth.login('ana@example.org', 'SenhaForte!123').subscribe();
    const login = http.expectOne('/api/v1/auth/login');
    expect(login.request.headers.has('Authorization')).toBe(false);
    login.flush(criarSessao('token-2'));

    auth.logout().subscribe();
    const logout = http.expectOne('/api/v1/auth/logout');
    expect(logout.request.headers.has('Authorization')).toBe(false);
    logout.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('renova uma única vez e repete as requisições que receberam 401', () => {
    entrar();
    const recebidos: string[] = [];

    cliente.get('/api/v1/a').subscribe((valor) => recebidos.push(valor as string));
    cliente.get('/api/v1/b').subscribe((valor) => recebidos.push(valor as string));

    http.expectOne('/api/v1/a').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/v1/b').flush(null, { status: 401, statusText: 'Unauthorized' });

    const renovacoes = http.match('/api/v1/auth/refresh');
    expect(renovacoes.length).toBe(1);
    renovacoes[0].flush(criarSessao('token-2'));

    const repeticaoA = http.expectOne('/api/v1/a');
    const repeticaoB = http.expectOne('/api/v1/b');
    expect(repeticaoA.request.headers.get('Authorization')).toBe('Bearer token-2');
    expect(repeticaoB.request.headers.get('Authorization')).toBe('Bearer token-2');
    repeticaoA.flush('a');
    repeticaoB.flush('b');

    expect(recebidos).toEqual(['a', 'b']);
  });

  it('leva ao login quando a renovação falha', () => {
    const router = TestBed.inject(Router);
    const navegar = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    entrar();
    let erroRecebido = false;

    cliente.get('/api/v1/equipe').subscribe({
      error: () => {
        erroRecebido = true;
      },
    });

    http.expectOne('/api/v1/equipe').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.autenticado()).toBe(false);
    expect(erroRecebido).toBe(true);
    expect(navegar).toHaveBeenCalledWith(['/acesso/login'], {
      queryParams: { returnUrl: router.url },
    });
  });
});
