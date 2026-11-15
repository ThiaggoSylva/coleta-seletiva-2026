import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { ContaDaEquipe } from './equipe.service';
import { EquipePage } from './equipe.page';

const ATIVO: ContaDaEquipe = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Bruno Lima',
  usuario: 'bruno',
  email: 'bruno@example.org',
  ativo: true,
  trocaSenhaPendente: true,
};

const INATIVO: ContaDaEquipe = {
  id: '22222222-2222-2222-2222-222222222222',
  nome: 'Carla Souza',
  usuario: 'carla',
  email: 'carla@example.org',
  ativo: false,
  trocaSenhaPendente: false,
};

interface PaginaDeEquipe {
  fixture: ComponentFixture<EquipePage>;
  elemento: HTMLElement;
}

function criarPagina(): PaginaDeEquipe {
  const fixture = TestBed.createComponent(EquipePage);
  fixture.detectChanges();

  return { fixture, elemento: fixture.nativeElement as HTMLElement };
}

function linhas(pagina: PaginaDeEquipe): HTMLElement[] {
  return Array.from(pagina.elemento.querySelectorAll('tbody tr'));
}

function botao(elemento: HTMLElement, texto: string): HTMLButtonElement | undefined {
  return Array.from(elemento.querySelectorAll('button')).find(
    (candidato) => candidato.textContent?.trim() === texto,
  );
}

describe('EquipePage', () => {
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

  it('lista os operadores com situação e pendência de senha', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/equipe').flush([ATIVO, INATIVO]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Bruno Lima');
    expect(pagina.elemento.textContent).toContain('bruno');
    expect(pagina.elemento.textContent).toContain('bruno@example.org');
    expect(pagina.elemento.textContent).toContain('Ativo');
    expect(pagina.elemento.textContent).toContain('Troca de senha pendente');
    expect(pagina.elemento.textContent).toContain('Carla Souza');
    expect(pagina.elemento.textContent).toContain('Inativo');

    const [linhaAtiva, linhaInativa] = linhas(pagina);
    expect(linhaAtiva.textContent).toContain('Editar');
    expect(botao(linhaAtiva, 'Inativar')).toBeDefined();
    expect(botao(linhaInativa, 'Inativar')).toBeUndefined();
  });

  it('inativa somente após confirmação e recarrega a lista', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/equipe').flush([ATIVO, INATIVO]);
    pagina.fixture.detectChanges();

    const linha = linhas(pagina)[0];
    botao(linha, 'Inativar')?.click();
    pagina.fixture.detectChanges();

    expect(linha.textContent).toContain('Inativar esta conta?');
    expect(http.match((requisicao) => requisicao.method === 'DELETE').length).toBe(0);

    botao(linha, 'Confirmar')?.click();
    const inativacao = http.expectOne(`/api/v1/equipe/${ATIVO.id}`);
    expect(inativacao.request.method).toBe('DELETE');
    inativacao.flush(null, { status: 204, statusText: 'No Content' });
    pagina.fixture.detectChanges();

    http
      .expectOne('/api/v1/equipe')
      .flush([{ ...ATIVO, ativo: false, trocaSenhaPendente: false }, INATIVO]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Operador inativado.');
    expect(linhas(pagina)[0].textContent).toContain('Inativo');
  });

  it('cancela a confirmação sem chamar a API', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/equipe').flush([ATIVO]);
    pagina.fixture.detectChanges();

    const linha = linhas(pagina)[0];
    botao(linha, 'Inativar')?.click();
    pagina.fixture.detectChanges();
    botao(linha, 'Cancelar')?.click();
    pagina.fixture.detectChanges();

    expect(linha.textContent).not.toContain('Inativar esta conta?');
    expect(http.match((requisicao) => requisicao.method === 'DELETE').length).toBe(0);
  });

  it('exibe o erro da inativação sem remover a conta da lista', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/equipe').flush([ATIVO]);
    pagina.fixture.detectChanges();

    const linha = linhas(pagina)[0];
    botao(linha, 'Inativar')?.click();
    pagina.fixture.detectChanges();
    botao(linha, 'Confirmar')?.click();
    http
      .expectOne(`/api/v1/equipe/${ATIVO.id}`)
      .flush(null, { status: 500, statusText: 'Server Error' });
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Ocorreu um erro inesperado. Tente novamente.');
    expect(linhas(pagina)[0].textContent).toContain('Ativo');
    expect(http.match('/api/v1/equipe').length).toBe(0);
  });

  it('permite tentar novamente quando a carga falha', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/equipe').flush(null, { status: 500, statusText: 'Server Error' });
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Ocorreu um erro inesperado. Tente novamente.');

    const tentarNovamente = pagina.elemento.querySelector('button') as HTMLButtonElement;
    tentarNovamente.click();
    pagina.fixture.detectChanges();
    http.expectOne('/api/v1/equipe').flush([ATIVO]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Bruno Lima');
  });

  it('confirma a criação de um operador ao voltar para a lista', () => {
    const rota = TestBed.inject(ActivatedRoute);
    vi.spyOn(rota.snapshot.queryParamMap, 'get').mockImplementation((nome) =>
      nome === 'criado' ? '1' : null,
    );

    const pagina = criarPagina();
    http.expectOne('/api/v1/equipe').flush([]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Operador criado.');
  });
});
