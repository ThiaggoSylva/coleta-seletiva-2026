import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { ContaAutenticada, SessaoResposta } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { PerfilPage } from './perfil.page';

const CIDADAO: ContaAutenticada = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Ana Souza',
  email: 'ana@example.org',
  papel: 'Cidadao',
  organizacaoId: null,
  trocaSenhaPendente: false,
};

const PERFIL = {
  id: CIDADAO.id,
  nome: 'Ana Souza',
  email: 'ana@example.org',
  telefone: '(11) 98888-7777',
};

function criarSessao(conta: ContaAutenticada): SessaoResposta {
  return {
    tokenDeAcesso: 'token-de-acesso',
    expiraEmUtc: new Date(Date.now() + 900_000).toISOString(),
    usuario: conta,
  };
}

interface PaginaDePerfil {
  fixture: ComponentFixture<PerfilPage>;
  elemento: HTMLElement;
  formulario: FormGroup;
}

function criarPagina(): PaginaDePerfil {
  const fixture = TestBed.createComponent(PerfilPage);
  fixture.detectChanges();

  return {
    fixture,
    elemento: fixture.nativeElement as HTMLElement,
    formulario: (fixture.componentInstance as unknown as { formulario: FormGroup }).formulario,
  };
}

function enviarFormulario(pagina: PaginaDePerfil): void {
  pagina.elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
  pagina.fixture.detectChanges();
}

describe('PerfilPage', () => {
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

  function entrar(): void {
    auth.login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush(criarSessao(CIDADAO));
  }

  function carregarPerfil(pagina: PaginaDePerfil): void {
    http.expectOne('/api/v1/conta').flush(PERFIL);
    pagina.fixture.detectChanges();
  }

  it('carrega o perfil do cidadão e preenche o formulário', () => {
    const pagina = criarPagina();

    carregarPerfil(pagina);

    expect(pagina.formulario.getRawValue()).toEqual({
      nome: 'Ana Souza',
      email: 'ana@example.org',
      telefone: '(11) 98888-7777',
    });
    expect((pagina.elemento.querySelector('#nome') as HTMLInputElement).value).toBe('Ana Souza');
    expect(pagina.elemento.textContent).not.toContain('Carregando perfil');
  });

  it('foca o primeiro campo inválido sem enviar a atualização', () => {
    const pagina = criarPagina();
    carregarPerfil(pagina);
    pagina.formulario.setValue({ nome: '', email: '', telefone: '' });

    enviarFormulario(pagina);

    expect(http.match('/api/v1/conta').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#nome'));
    expect(pagina.elemento.textContent).toContain('Informe este campo.');
  });

  it('salva o perfil, confirma e atualiza a conta em memória', () => {
    entrar();
    const pagina = criarPagina();
    carregarPerfil(pagina);
    pagina.formulario.setValue({
      nome: 'Ana Silva',
      email: 'ana.silva@example.org',
      telefone: '(11) 99999-8888',
    });

    enviarFormulario(pagina);
    const requisicao = http.expectOne('/api/v1/conta');
    expect(requisicao.request.method).toBe('PUT');
    expect(requisicao.request.body).toEqual({
      nome: 'Ana Silva',
      email: 'ana.silva@example.org',
      telefone: '(11) 99999-8888',
    });
    requisicao.flush({
      id: CIDADAO.id,
      nome: 'Ana Silva',
      email: 'ana.silva@example.org',
      telefone: '(11) 99999-8888',
    });
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Perfil atualizado.');
    expect(auth.contaAtual()?.nome).toBe('Ana Silva');
    expect(auth.contaAtual()?.email).toBe('ana.silva@example.org');
  });

  it('trata conflito de e-mail como erro do campo', () => {
    const pagina = criarPagina();
    carregarPerfil(pagina);
    pagina.formulario.setValue({
      nome: 'Ana Souza',
      email: 'outra@example.org',
      telefone: '',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/conta').flush(
      {
        title: 'O e-mail informado já está em uso.',
        code: 'email_em_uso',
      },
      { status: 409, statusText: 'Conflict' },
    );
    pagina.fixture.detectChanges();

    expect(document.activeElement).toBe(pagina.elemento.querySelector('#email'));
    const ocorrencias = pagina.elemento.textContent?.match(/já está em uso/g)?.length ?? 0;
    expect(ocorrencias).toBe(1);
  });

  it('permite tentar novamente quando a carga falha', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/conta').flush(null, { status: 500, statusText: 'Server Error' });
    pagina.fixture.detectChanges();
    expect(pagina.elemento.textContent).toContain('Ocorreu um erro inesperado. Tente novamente.');

    const tentarNovamente = pagina.elemento.querySelector('button') as HTMLButtonElement;
    tentarNovamente.click();
    pagina.fixture.detectChanges();
    http.expectOne('/api/v1/conta').flush(PERFIL);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.querySelector('form')).not.toBeNull();
  });
});
