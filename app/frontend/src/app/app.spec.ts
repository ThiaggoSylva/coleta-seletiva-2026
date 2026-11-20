import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { ContaAutenticada, Papel } from './core/auth/auth.models';
import { AuthService } from './core/auth/auth.service';

function criarConta(papel: Papel, trocaSenhaPendente = false): ContaAutenticada {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    nome: 'Ana',
    email: 'ana@example.org',
    papel,
    organizacaoId: papel === 'Cidadao' ? null : '22222222-2222-2222-2222-222222222222',
    trocaSenhaPendente,
  };
}

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    responderNotificacoesPendentes();
    http.verify();
  });

  function responderNotificacoesPendentes(): void {
    http
      .match((requisicao) => requisicao.url.startsWith('/api/v1/notificacoes'))
      .forEach((requisicao) => requisicao.flush({ itens: [], naoLidas: 0 }));
  }

  function entrar(papel: Papel, trocaSenhaPendente = false): void {
    TestBed.inject(AuthService).login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush({
      tokenDeAcesso: 'token-de-acesso',
      expiraEmUtc: new Date(Date.now() + 900_000).toISOString(),
      usuario: criarConta(papel, trocaSenhaPendente),
    });
  }

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Coleta Seletiva');
  });

  it('mostra os atalhos públicos quando não há sessão', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Entrar');
    expect(compiled.textContent).toContain('Criar conta');
    expect(compiled.textContent).not.toContain('Sair');
  });

  it('mostra a gestão de equipe para o Administrador', async () => {
    entrar('Administrador');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Equipe');
    expect(compiled.textContent).toContain('Área da organização');
  });

  it('oculta a gestão de equipe do Operador', async () => {
    entrar('Operador');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Área da organização');
    expect(compiled.textContent).not.toContain('Equipe');
  });

  function linksDaPagina(fixture: { nativeElement: HTMLElement }): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('a')).map(
      (link) => link.getAttribute('href') ?? '',
    );
  }

  it('oferece apenas o link público de roadmap ao visitante', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = linksDaPagina(fixture);

    expect(links).toContain('/em-breve/transparencia-impacto');
    expect(links).not.toContain('/em-breve/preferencias-email');
    expect(links).not.toContain('/em-breve/extrato-pontos');
    expect(links).not.toContain('/em-breve/rotas-coletor');
  });

  it('oferece apenas os módulos do cidadão', async () => {
    entrar('Cidadao');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = linksDaPagina(fixture);

    expect(links).toContain('/em-breve/preferencias-email');
    expect(links).toContain('/em-breve/extrato-pontos');
    expect(links).not.toContain('/em-breve/rotas-coletor');
  });

  it('oferece rotas do coletor ao Administrador e oculta os módulos do cidadão', async () => {
    entrar('Administrador');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = linksDaPagina(fixture);

    expect(links).toContain('/em-breve/rotas-coletor');
    expect(links).not.toContain('/em-breve/preferencias-email');
    expect(links).not.toContain('/em-breve/extrato-pontos');
  });

  it('oferece rotas do coletor ao Operador', async () => {
    entrar('Operador');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = linksDaPagina(fixture);

    expect(links).toContain('/em-breve/rotas-coletor');
    expect(links).not.toContain('/em-breve/preferencias-email');
  });

  it('oculta os links do cabeçalho quando a troca de senha está pendente', async () => {
    entrar('Cidadao', true);
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = linksDaPagina(fixture);

    expect(links).not.toContain('/em-breve/preferencias-email');
    expect(links).not.toContain('/em-breve/extrato-pontos');
    expect(links).not.toContain('/em-breve/rotas-coletor');
    expect(links).toContain('/em-breve/transparencia-impacto');
  });
});
