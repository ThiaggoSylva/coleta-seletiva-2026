import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { RegistroOrganizacaoPage } from './registro-organizacao.page';

interface PaginaDeRegistro {
  fixture: ComponentFixture<RegistroOrganizacaoPage>;
  elemento: HTMLElement;
  formulario: FormGroup;
}

function criarPagina(): PaginaDeRegistro {
  const fixture = TestBed.createComponent(RegistroOrganizacaoPage);
  fixture.detectChanges();

  return {
    fixture,
    elemento: fixture.nativeElement as HTMLElement,
    formulario: (fixture.componentInstance as unknown as { formulario: FormGroup }).formulario,
  };
}

function enviarFormulario(pagina: PaginaDeRegistro): void {
  pagina.elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
  pagina.fixture.detectChanges();
}

describe('RegistroOrganizacaoPage', () => {
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

  it('foca o primeiro campo inválido sem enviar o cadastro', () => {
    const pagina = criarPagina();

    enviarFormulario(pagina);

    expect(http.match('/api/v1/auth/registro-organizacao').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#nomeOrganizacao'));
    expect(pagina.elemento.textContent).toContain('Informe este campo.');
  });

  it('envia os dados válidos e confirma o cadastro', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nomeOrganizacao: 'Cooperativa Verde',
      tipo: 'Associacao',
      usuario: 'verde',
      email: 'verde@example.org',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    const requisicao = http.expectOne('/api/v1/auth/registro-organizacao');
    expect(requisicao.request.body).toEqual({
      nomeOrganizacao: 'Cooperativa Verde',
      tipo: 'Associacao',
      usuario: 'verde',
      email: 'verde@example.org',
      senha: 'SenhaForte!123',
    });
    requisicao.flush(
      {
        organizacaoId: '22222222-2222-2222-2222-222222222222',
        administradorId: '33333333-3333-3333-3333-333333333333',
      },
      { status: 201, statusText: 'Created' },
    );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Organização cadastrada');
    expect(pagina.elemento.querySelector('form')).toBeNull();
  });

  it('exibe erro por campo devolvido pela API e foca o campo', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nomeOrganizacao: 'Cooperativa Verde',
      tipo: 'Cooperativa',
      usuario: 'verde',
      email: 'verde@example.org',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/auth/registro-organizacao').flush(
      {
        title: 'Dados inválidos.',
        code: 'validacao',
        errors: { email: ['Este e-mail já está em uso.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Dados inválidos.');
    expect(pagina.elemento.textContent).toContain('Este e-mail já está em uso.');
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#email'));
  });
});
