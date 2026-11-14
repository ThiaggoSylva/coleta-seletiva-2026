import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { ContaAutenticada, SessaoResposta } from '../../../core/auth/auth.models';
import { LoginPage } from './login.page';

const ORGANIZACAO_ID = '22222222-2222-2222-2222-222222222222';

function criarConta(parcial: Partial<ContaAutenticada> = {}): ContaAutenticada {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    nome: 'Ana',
    email: 'ana@example.org',
    papel: 'Cidadao',
    organizacaoId: null,
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

interface PaginaDeLogin {
  fixture: ComponentFixture<LoginPage>;
  elemento: HTMLElement;
  formulario: FormGroup;
}

function criarPagina(): PaginaDeLogin {
  const fixture = TestBed.createComponent(LoginPage);
  fixture.detectChanges();

  return {
    fixture,
    elemento: fixture.nativeElement as HTMLElement,
    formulario: (fixture.componentInstance as unknown as { formulario: FormGroup }).formulario,
  };
}

function enviarFormulario(pagina: PaginaDeLogin): void {
  pagina.elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
  pagina.fixture.detectChanges();
}

describe('LoginPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('marca os campos e foca o primeiro inválido sem chamar a API', () => {
    const pagina = criarPagina();

    enviarFormulario(pagina);

    expect(http.match('/api/v1/auth/login').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#identificador'));
    expect(pagina.elemento.textContent).toContain('Informe este campo.');
  });

  const cenarios: Array<{
    nome: string;
    conta: ContaAutenticada;
    destino: string;
  }> = [
    {
      nome: 'cidadão',
      conta: criarConta({ papel: 'Cidadao' }),
      destino: '/cidadao/perfil',
    },
    {
      nome: 'administrador',
      conta: criarConta({
        papel: 'Administrador',
        organizacaoId: ORGANIZACAO_ID,
      }),
      destino: '/organizacao/equipe',
    },
    {
      nome: 'operador',
      conta: criarConta({ papel: 'Operador', organizacaoId: ORGANIZACAO_ID }),
      destino: '/organizacao/inicio',
    },
    {
      nome: 'operador com troca de senha pendente',
      conta: criarConta({
        papel: 'Operador',
        organizacaoId: ORGANIZACAO_ID,
        trocaSenhaPendente: true,
      }),
      destino: '/acesso/trocar-senha',
    },
  ];

  for (const cenario of cenarios) {
    it(`redireciona o ${cenario.nome} para ${cenario.destino}`, async () => {
      const pagina = criarPagina();
      const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
      pagina.formulario.setValue({
        identificador: 'ana@example.org',
        senha: 'SenhaForte!123',
      });

      enviarFormulario(pagina);
      const requisicao = http.expectOne('/api/v1/auth/login');
      expect(requisicao.request.body).toEqual({
        identificador: 'ana@example.org',
        senha: 'SenhaForte!123',
      });
      requisicao.flush(criarSessao(cenario.conta));
      await pagina.fixture.whenStable();

      expect(navegar).toHaveBeenCalledWith(cenario.destino);
    });
  }

  it('confirma a troca de senha após voltar ao login', () => {
    const rota = TestBed.inject(ActivatedRoute);
    vi.spyOn(rota.snapshot.queryParamMap, 'get').mockImplementation((nome) =>
      nome === 'senhaAlterada' ? '1' : null,
    );

    const pagina = criarPagina();

    expect(pagina.elemento.textContent).toContain(
      'Senha alterada. Entre novamente com a nova senha.',
    );
  });

  it('respeita o returnUrl interno após o login', async () => {
    const rota = TestBed.inject(ActivatedRoute);
    vi.spyOn(rota.snapshot.queryParamMap, 'get').mockImplementation((nome) =>
      nome === 'returnUrl' ? '/organizacao/inicio' : null,
    );
    const pagina = criarPagina();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    pagina.formulario.setValue({
      identificador: 'ana@example.org',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/auth/login').flush(criarSessao(criarConta({ papel: 'Cidadao' })));
    await pagina.fixture.whenStable();

    expect(navegar).toHaveBeenCalledWith('/organizacao/inicio');
  });

  it('exibe o erro do servidor e foca o campo correspondente', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      identificador: 'ana@example.org',
      senha: 'senha-incorreta',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/auth/login').flush(
      {
        title: 'Credenciais inválidas.',
        code: 'nao_autenticado',
        errors: { senha: ['Senha incorreta.'] },
      },
      { status: 401, statusText: 'Unauthorized' },
    );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Credenciais inválidas.');
    expect(pagina.elemento.textContent).toContain('Senha incorreta.');
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#senha'));
  });
});
