import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ContaAutenticada, SessaoResposta } from './auth.models';
import { AuthService } from './auth.service';

const CIDADAO: ContaAutenticada = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Ana',
  email: 'ana@example.org',
  papel: 'Cidadao',
  organizacaoId: null,
  trocaSenhaPendente: false,
};

function criarSessao(token: string, conta: ContaAutenticada = CIDADAO): SessaoResposta {
  return {
    tokenDeAcesso: token,
    expiraEmUtc: new Date(Date.now() + 900_000).toISOString(),
    usuario: conta,
  };
}

describe('AuthService', () => {
  let servico: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    servico = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('mantém o token apenas em memória após o login', () => {
    servico.login('ana@example.org', 'SenhaForte!123').subscribe();

    const requisicao = http.expectOne('/api/v1/auth/login');
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body).toEqual({
      identificador: 'ana@example.org',
      senha: 'SenhaForte!123',
    });
    requisicao.flush(criarSessao('token-em-memoria'));

    expect(servico.obterToken()).toBe('token-em-memoria');
    expect(servico.autenticado()).toBe(true);
    expect(servico.papel()).toBe('Cidadao');
    expect(localStorage.length).toBe(0);
  });

  it('compartilha uma única renovação entre chamadas simultâneas', () => {
    const resultados: boolean[] = [];
    servico.renovarSessao().subscribe((valor) => resultados.push(valor));
    servico.renovarSessao().subscribe((valor) => resultados.push(valor));

    const renovacoes = http.match('/api/v1/auth/refresh');
    expect(renovacoes.length).toBe(1);
    expect(renovacoes[0].request.headers.get('X-Requested-With')).toBe('XMLHttpRequest');
    renovacoes[0].flush(criarSessao('token-renovado'));

    expect(resultados).toEqual([true, true]);
    expect(servico.obterToken()).toBe('token-renovado');
    expect(servico.autenticado()).toBe(true);
  });

  it('limpa a sessão quando a renovação falha', () => {
    const resultados: boolean[] = [];
    servico.renovarSessao().subscribe((valor) => resultados.push(valor));

    http.expectOne('/api/v1/auth/refresh').flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    expect(resultados).toEqual([false]);
    expect(servico.obterToken()).toBeNull();
    expect(servico.autenticado()).toBe(false);
  });

  it('limpa a sessão no logout mesmo quando a API falha', () => {
    servico.login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush(criarSessao('token-logout'));

    servico.logout().subscribe();
    const logout = http.expectOne('/api/v1/auth/logout');
    expect(logout.request.headers.get('X-Requested-With')).toBe('XMLHttpRequest');
    logout.flush(null, { status: 500, statusText: 'Server Error' });

    expect(servico.autenticado()).toBe(false);
    expect(servico.contaAtual()).toBeNull();
  });

  it('atualiza nome e e-mail da conta em memória', () => {
    servico.login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush(criarSessao('token-perfil'));

    servico.atualizarConta('Ana Silva', 'ana.silva@example.org');

    expect(servico.contaAtual()?.nome).toBe('Ana Silva');
    expect(servico.contaAtual()?.email).toBe('ana.silva@example.org');
    expect(servico.obterToken()).toBe('token-perfil');
  });

  it('limpa a sessão após alterar a senha', () => {
    servico.login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush(criarSessao('token-senha'));

    servico.alterarSenha('SenhaForte!123', 'NovaSenhaForte!456').subscribe();
    const alteracao = http.expectOne('/api/v1/auth/alterar-senha');
    expect(alteracao.request.body).toEqual({
      senhaAtual: 'SenhaForte!123',
      novaSenha: 'NovaSenhaForte!456',
    });
    alteracao.flush(null, { status: 204, statusText: 'No Content' });

    expect(servico.autenticado()).toBe(false);
  });
});
