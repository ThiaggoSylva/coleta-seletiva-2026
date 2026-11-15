import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PontosPage } from './pontos.page';
import { PontoResumo } from './pontos.service';

const PONTO: PontoResumo = {
  id: '11111111-1111-1111-1111-111111111111',
  nome: 'Ponto Centro',
  municipio: 'São Paulo',
  uf: 'SP',
  status: 'Ativo',
  abertoAgora: true,
  excluido: false,
  tipos: ['Papel', 'Vidro'],
};

function criarPagina(): { fixture: ComponentFixture<PontosPage>; elemento: HTMLElement } {
  const fixture = TestBed.createComponent(PontosPage);
  fixture.detectChanges();

  return { fixture, elemento: fixture.nativeElement as HTMLElement };
}

function botao(elemento: HTMLElement, texto: string): HTMLButtonElement | undefined {
  return Array.from(elemento.querySelectorAll('button')).find(
    (candidato) => candidato.textContent?.trim() === texto,
  );
}

describe('PontosPage', () => {
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

  it('lista os pontos com município, tipos e situação', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/pontos').flush([PONTO]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Ponto Centro');
    expect(pagina.elemento.textContent).toContain('São Paulo — SP');
    expect(pagina.elemento.textContent).toContain('Papel, Vidro');
    expect(pagina.elemento.textContent).toContain('Aberto agora');
    expect(pagina.elemento.querySelector('select')).not.toBeNull();
  });

  it('exibe mensagem quando não há pontos', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/pontos').flush([]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Nenhum ponto cadastrado ainda.');
  });

  it('retira o ponto somente após confirmação', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/pontos').flush([PONTO]);
    pagina.fixture.detectChanges();

    const linha = pagina.elemento.querySelector('tbody tr') as HTMLElement;
    botao(linha, 'Retirar')?.click();
    pagina.fixture.detectChanges();

    expect(linha.textContent).toContain('Retirar este ponto?');
    expect(http.match((requisicao) => requisicao.method === 'DELETE').length).toBe(0);

    botao(linha, 'Confirmar')?.click();
    const retirada = http.expectOne(`/api/v1/pontos/${PONTO.id}`);
    expect(retirada.request.method).toBe('DELETE');
    retirada.flush(null, { status: 204, statusText: 'No Content' });

    http.expectOne('/api/v1/pontos').flush([]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Ponto retirado.');
  });

  it('altera o status do ponto e atualiza a linha', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/pontos').flush([PONTO]);
    pagina.fixture.detectChanges();

    const selecao = pagina.elemento.querySelector('select') as HTMLSelectElement;
    selecao.value = 'Inativo';
    selecao.dispatchEvent(new Event('change'));

    const requisicao = http.expectOne(`/api/v1/pontos/${PONTO.id}/status`);
    expect(requisicao.request.method).toBe('PUT');
    expect(requisicao.request.body).toEqual({ status: 'Inativo' });
    requisicao.flush({ ...PONTO, status: 'Inativo', abertoAgora: false });
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Status do ponto atualizado.');
    expect(pagina.elemento.textContent).toContain('Fechado agora');
  });

  it('permite tentar novamente quando a carga falha', () => {
    const pagina = criarPagina();
    http.expectOne('/api/v1/pontos').flush(null, { status: 500, statusText: 'Server Error' });
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Ocorreu um erro inesperado. Tente novamente.');

    botao(pagina.elemento, 'Tentar novamente')?.click();
    http.expectOne('/api/v1/pontos').flush([PONTO]);
    pagina.fixture.detectChanges();

    expect(pagina.elemento.textContent).toContain('Ponto Centro');
  });
});
