import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { ContaAutenticada, Papel } from './core/auth/auth.models';
import { AuthService } from './core/auth/auth.service';

function criarConta(papel: Papel): ContaAutenticada {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    nome: 'Ana',
    email: 'ana@example.org',
    papel,
    organizacaoId: papel === 'Cidadao' ? null : '22222222-2222-2222-2222-222222222222',
    trocaSenhaPendente: false,
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
    http.verify();
  });

  function entrar(papel: Papel): void {
    TestBed.inject(AuthService).login('ana@example.org', 'SenhaForte!123').subscribe();
    http.expectOne('/api/v1/auth/login').flush({
      tokenDeAcesso: 'token-de-acesso',
      expiraEmUtc: new Date(Date.now() + 900_000).toISOString(),
      usuario: criarConta(papel),
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
});
