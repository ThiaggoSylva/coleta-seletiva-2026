import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { RegistroCidadaoPage } from './registro-cidadao.page';

interface PaginaDeRegistro {
  fixture: ComponentFixture<RegistroCidadaoPage>;
  elemento: HTMLElement;
  formulario: FormGroup;
}

function criarPagina(): PaginaDeRegistro {
  const fixture = TestBed.createComponent(RegistroCidadaoPage);
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

describe('RegistroCidadaoPage', () => {
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

    expect(http.match('/api/v1/auth/registro-cidadao').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#nome'));
    expect(pagina.elemento.textContent).toContain('Informe este campo.');
  });

  it('envia os dados válidos e confirma o cadastro', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nome: 'Ana Souza',
      email: 'ana@example.org',
      telefone: '(11) 98888-7777',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    const requisicao = http.expectOne('/api/v1/auth/registro-cidadao');
    expect(requisicao.request.body).toEqual({
      nome: 'Ana Souza',
      email: 'ana@example.org',
      telefone: '(11) 98888-7777',
      senha: 'SenhaForte!123',
    });
    requisicao.flush(
      { usuarioId: '44444444-4444-4444-4444-444444444444' },
      { status: 201, statusText: 'Created' },
    );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Conta criada');
    expect(pagina.elemento.querySelector('form')).toBeNull();
  });

  it('exibe erro por campo devolvido pela API e foca o campo', () => {
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nome: 'Ana Souza',
      email: 'ana@example.org',
      telefone: '123',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/auth/registro-cidadao').flush(
      {
        title: 'Dados inválidos.',
        code: 'validacao',
        errors: { telefone: ['Informe um telefone válido.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Dados inválidos.');
    expect(pagina.elemento.textContent).toContain('Informe um telefone válido.');
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#telefone'));
  });
});
