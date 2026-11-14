import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { ContaDaEquipe } from './equipe.service';
import { OperadorPage } from './operador.page';

const OPERADOR: ContaDaEquipe = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Bruno Lima',
  usuario: 'bruno',
  email: 'bruno@example.org',
  ativo: true,
  trocaSenhaPendente: true,
};

interface PaginaDeOperador {
  fixture: ComponentFixture<OperadorPage>;
  elemento: HTMLElement;
  formulario: FormGroup;
}

function criarPagina(): PaginaDeOperador {
  const fixture = TestBed.createComponent(OperadorPage);
  fixture.detectChanges();

  return {
    fixture,
    elemento: fixture.nativeElement as HTMLElement,
    formulario: (fixture.componentInstance as unknown as { formulario: FormGroup }).formulario,
  };
}

function enviarFormulario(pagina: PaginaDeOperador): void {
  pagina.elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
  pagina.fixture.detectChanges();
}

function configurar(id: string | null): void {
  const rota = TestBed.inject(ActivatedRoute);
  vi.spyOn(rota.snapshot.paramMap, 'get').mockImplementation((nome) => (nome === 'id' ? id : null));
}

describe('OperadorPage', () => {
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

  it('foca o primeiro campo inválido sem criar a conta', () => {
    configurar(null);
    const pagina = criarPagina();

    enviarFormulario(pagina);

    expect(http.match((requisicao) => requisicao.method === 'POST').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#nome'));
    expect(pagina.elemento.textContent).toContain('Informe este campo.');
  });

  it('rejeita senha inicial fora da política antes de chamar a API', () => {
    configurar(null);
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nome: 'Bruno Lima',
      usuario: 'bruno',
      email: 'bruno@example.org',
      senha: 'senha123',
    });

    enviarFormulario(pagina);

    expect(http.match((requisicao) => requisicao.method === 'POST').length).toBe(0);
    expect(document.activeElement).toBe(pagina.elemento.querySelector('#senha'));
    expect(pagina.elemento.textContent).toContain('12 caracteres');
  });

  it('cria o operador e volta para a lista', async () => {
    configurar(null);
    const pagina = criarPagina();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    pagina.formulario.setValue({
      nome: 'Bruno Lima',
      usuario: 'bruno',
      email: 'bruno@example.org',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);

    const requisicao = http.expectOne('/api/v1/equipe');
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body).toEqual({
      nome: 'Bruno Lima',
      usuario: 'bruno',
      email: 'bruno@example.org',
      senha: 'SenhaForte!123',
    });
    requisicao.flush(OPERADOR, { status: 201, statusText: 'Created' });
    await pagina.fixture.whenStable();

    expect(navegar).toHaveBeenCalledWith(['/organizacao/equipe'], {
      queryParams: { criado: '1' },
    });
  });

  it('trata usuário em uso como erro do campo', () => {
    configurar(null);
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nome: 'Bruno Lima',
      usuario: 'bruno',
      email: 'bruno@example.org',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/equipe').flush(
      {
        title: 'Este usuário já está em uso.',
        code: 'usuario_em_uso',
      },
      { status: 409, statusText: 'Conflict' },
    );
    pagina.fixture.detectChanges();

    expect(document.activeElement).toBe(pagina.elemento.querySelector('#usuario'));
    const ocorrencias = pagina.elemento.textContent?.match(/já está em uso/g)?.length ?? 0;
    expect(ocorrencias).toBe(1);
  });

  it('carrega o operador, oculta a senha e envia a edição', async () => {
    configurar(OPERADOR.id);
    const pagina = criarPagina();
    http.expectOne(`/api/v1/equipe/${OPERADOR.id}`).flush(OPERADOR);
    pagina.fixture.detectChanges();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    expect(pagina.formulario.getRawValue()).toEqual({
      nome: 'Bruno Lima',
      usuario: 'bruno',
      email: 'bruno@example.org',
      senha: '',
    });
    expect(pagina.elemento.querySelector('#senha')).toBeNull();

    pagina.formulario.patchValue({
      nome: 'Bruno Costa',
      email: 'bruno.costa@example.org',
    });
    enviarFormulario(pagina);

    const requisicao = http.expectOne(`/api/v1/equipe/${OPERADOR.id}`);
    expect(requisicao.request.method).toBe('PUT');
    expect(requisicao.request.body).toEqual({
      nome: 'Bruno Costa',
      usuario: 'bruno',
      email: 'bruno.costa@example.org',
    });
    requisicao.flush({
      ...OPERADOR,
      nome: 'Bruno Costa',
      email: 'bruno.costa@example.org',
    });
    await pagina.fixture.whenStable();

    expect(navegar).toHaveBeenCalledWith(['/organizacao/equipe'], {
      queryParams: { editado: '1' },
    });
  });

  it('permite tentar novamente quando o operador não é encontrado', () => {
    configurar(OPERADOR.id);
    const pagina = criarPagina();
    http
      .expectOne(`/api/v1/equipe/${OPERADOR.id}`)
      .flush(
        { title: 'Operador não encontrado.', code: 'nao_encontrado' },
        { status: 404, statusText: 'Not Found' },
      );
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Operador não encontrado.');
    expect(pagina.elemento.querySelector('form')).toBeNull();

    const tentarNovamente = pagina.elemento.querySelector('button') as HTMLButtonElement;
    tentarNovamente.click();
    pagina.fixture.detectChanges();
    http.expectOne(`/api/v1/equipe/${OPERADOR.id}`).flush(OPERADOR);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.querySelector('form')).not.toBeNull();
  });

  it('exibe erros de validação devolvidos pela API e foca o campo', () => {
    configurar(null);
    const pagina = criarPagina();
    pagina.formulario.setValue({
      nome: 'Bruno Lima',
      usuario: 'bruno',
      email: 'bruno@example.org',
      senha: 'SenhaForte!123',
    });

    enviarFormulario(pagina);
    http.expectOne('/api/v1/equipe').flush(
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
