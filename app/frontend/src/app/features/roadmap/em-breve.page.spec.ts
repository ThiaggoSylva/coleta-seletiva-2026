import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { CATALOGO_DE_PLACEHOLDERS } from './catalogo-de-placeholders';
import { EmBrevePage } from './em-breve.page';

async function criarPagina(moduloId: string): Promise<ComponentFixture<EmBrevePage>> {
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [EmBrevePage],
    providers: [
      provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { data: { modulo: moduloId } } } },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(EmBrevePage);
  await fixture.whenStable();
  return fixture;
}

describe('EmBrevePage', () => {
  it('exibe título, descrição, fase, aviso e retorno a partir dos dados da rota', async () => {
    const fixture = await criarPagina('preferencias-email');
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.querySelector('h1')?.textContent).toContain('Preferências de e-mail');
    expect(elemento.textContent).toContain(
      'Escolha das notificações por e-mail que o cidadão deseja receber sobre suas solicitações de retirada.',
    );
    expect(elemento.textContent).toContain('F7');
    expect(elemento.textContent).toContain('Em breve');
    expect(elemento.textContent).toContain('ainda não está disponível');
    expect(elemento.querySelector('a')?.getAttribute('href')).toBe('/pontos');
  });

  it('apresenta o conteúdo de cada módulo do catálogo', async () => {
    for (const modulo of CATALOGO_DE_PLACEHOLDERS) {
      const fixture = await criarPagina(modulo.id);
      const elemento = fixture.nativeElement as HTMLElement;

      expect(elemento.querySelector('h1')?.textContent).toContain(modulo.titulo);
      expect(elemento.textContent).toContain(modulo.descricao);
      expect(elemento.textContent).toContain(modulo.fase);
    }
  });

  it('não oferece formulário, botão de envio nem dado de exemplo', async () => {
    const fixture = await criarPagina('rotas-coletor');
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.querySelector('form')).toBeNull();
    expect(elemento.querySelector('button')).toBeNull();
    expect(elemento.querySelector('input')).toBeNull();
    expect(elemento.textContent).not.toContain('Ex.:');
  });

  it('mostra aviso genérico quando a rota não informa um módulo válido', async () => {
    const fixture = await criarPagina('modulo-inexistente');
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.querySelector('h1')?.textContent).toContain('Em breve');
    expect(elemento.textContent).toContain('Este conteúdo ainda não está disponível.');
  });
});
