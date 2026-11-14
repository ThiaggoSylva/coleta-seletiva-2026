import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { Router, provideRouter } from '@angular/router';
import { ContaAutenticada, SessaoResposta } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { TrocarSenhaPage } from './trocar-senha.page';

const ORGANIZACAO_ID = '22222222-2222-2222-2222-222222222222';

function criarConta(parcial: Partial<ContaAutenticada> = {}): ContaAutenticada {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    nome: 'Ana',
    email: 'ana@example.org',
    papel: 'Operador',
    organizacaoId: ORGANIZACAO_ID,
    trocaSenhaPendente: false,
    ...parcial,
  };
}

function criarSessao(conta: ContaAutenticada): SessaoResposta {
  return {
    tokenDeAcesso: 'token-de-acesso',
    expiraEmUtc: new Date(Date.now() + 900_000).toISOString(),
    usuario: conta,
  };
}

interface PaginaDeSenha {
  fixture: ComponentFixture<TrocarSenhaPage>;
  elemento: HTMLElement;
  formulario: FormGroup;
}

function criarPagina(): PaginaDeSenha {
  const fixture = TestBed.createComponent(TrocarSenhaPage);
  fixture.detectChanges();

  return {
    fixture,
    elemento: fixture.nativeElement as HTMLElement,
    formulario: (fixture.componentInstance as unknown as { formulario: FormGroup }).formulario,
  };
}

function enviarFormulario(pagina: PaginaDeSenha): void {
  pagina.elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
  pagina.fixture.detectChanges();
}

describe('TrocarSenhaPage', () => {
  let http: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    http.verify();
  });

  function entrar(conta: ContaAutenticada = criarConta()): void {
    auth.login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush(criarSessao(conta));
  }

  it('foca a senha atual quando o formulário está vazio', () => {
    const pagina = criarPagina();

    enviarFormulario(pagina);

    expect(http.match('/api/v1/auth/alterar-senha').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#senhaAtual'));
    expect(pagina.elemento.textContent).toContain('Informe este campo.');
  });

  it('rejeita nova senha fora da política antes de chamar a API', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      senhaAtual: 'SenhaForte!123',
      novaSenha: 'senha123',
      confirmacao: 'senha123',
    });

    enviarFormulario(pagina);

    expect(http.match('/api/v1/auth/alterar-senha').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#novaSenha'));
    expect(pagina.elemento.textContent).toContain('12 caracteres');
  });

  it('rejeita confirmação diferente da nova senha', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      senhaAtual: 'SenhaForte!123',
      novaSenha: 'NovaSenhaForte!456',
      confirmacao: 'OutraSenhaForte!789',
    });

    enviarFormulario(pagina);

    expect(http.match('/api/v1/auth/alterar-senha').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#confirmacao'));
    expect(pagina.elemento.textContent).toContain('As senhas não conferem.');
  });

  it('mostra o primeiro acesso e redireciona ao login após a troca', async () => {
    entrar(criarConta({ trocaSenhaPendente: true }));
    const pagina = criarPagina();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    expect(pagina.elemento.textContent).toContain('Primeiro acesso');

    pagina.formulario.setValue({
      senhaAtual: 'SenhaForte!123',
      novaSenha: 'NovaSenhaForte!456',
      confirmacao: 'NovaSenhaForte!456',
    });
    enviarFormulario(pagina);

    const requisicao = http.expectOne('/api/v1/auth/alterar-senha');
    expect(requisicao.request.body).toEqual({
      senhaAtual: 'SenhaForte!123',
      novaSenha: 'NovaSenhaForte!456',
    });
    requisicao.flush(null, { status: 204, statusText: 'No Content' });
    await pagina.fixture.whenStable();

    expect(auth.autenticado()).toBe(false);
    expect(navegar).toHaveBeenCalledWith(['/acesso/login'], {
      queryParams: { senhaAlterada: '1' },
    });
  });

  it('exibe erro do servidor e foca a senha atual incorreta', () => {
    entrar();
    const pagina = criarPagina();
    pagina.formulario.setValue({
      senhaAtual: 'SenhaErrada!123',
      novaSenha: 'NovaSenhaForte!456',
      confirmacao: 'NovaSenhaForte!456',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/auth/alterar-senha').flush(
      {
        title: 'Dados inválidos.',
        code: 'validacao',
        errors: { senhaAtual: ['A senha atual informada está incorreta.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('A senha atual informada está incorreta.');
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#senhaAtual'));
  });
});
