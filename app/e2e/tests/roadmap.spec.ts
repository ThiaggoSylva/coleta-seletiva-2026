import { expect, test, type Page } from '@playwright/test';
import {
  cadastrarCidadao,
  cadastrarOrganizacao,
  dadosDaOrganizacao,
  dadosDoCidadao,
  entrar,
  sair,
} from './apoio';

interface ExpectativaDeModulo {
  rota: string;
  titulo: string;
  descricao: string;
  fase: string;
}

const MODULOS: Record<string, ExpectativaDeModulo> = {
  preferenciasEmail: {
    rota: '/em-breve/preferencias-email',
    titulo: 'Preferências de e-mail',
    descricao:
      'Escolha das notificações por e-mail que o cidadão deseja receber sobre suas solicitações de retirada.',
    fase: 'F7',
  },
  extratoPontos: {
    rota: '/em-breve/extrato-pontos',
    titulo: 'Extrato de pontos',
    descricao:
      'Consulta do histórico de pontos de reconhecimento obtidos e utilizados pelo cidadão.',
    fase: 'F8',
  },
  rotasColetor: {
    rota: '/em-breve/rotas-coletor',
    titulo: 'Rotas do coletor',
    descricao:
      'Acompanhamento das rotas de coleta e das retiradas programadas pela equipe da organização.',
    fase: 'F9',
  },
  transparenciaImpacto: {
    rota: '/em-breve/transparencia-impacto',
    titulo: 'Transparência e impacto',
    descricao:
      'Divulgação pública dos volumes coletados e da estimativa de impacto ambiental, sem dados pessoais.',
    fase: 'F10',
  },
};

async function verificarPaginaEmBreve(page: Page, modulo: ExpectativaDeModulo): Promise<void> {
  await page.goto(modulo.rota);

  await expect(page).toHaveURL(new RegExp(`${modulo.rota.replaceAll('/', '\\/')}$`));
  await expect(page.getByRole('heading', { name: modulo.titulo, exact: true })).toBeVisible();
  await expect(page.getByText(modulo.descricao)).toBeVisible();
  await expect(page.getByText(`Fase do roadmap: ${modulo.fase}`, { exact: false })).toBeVisible();
  await expect(page.getByRole('status')).toContainText('ainda não está disponível');

  const secao = page.locator('app-em-breve');
  expect(await secao.locator('form').count()).toBe(0);
  expect(await secao.locator('input').count()).toBe(0);
  expect(await secao.locator('button').count()).toBe(0);
  expect(await secao.textContent()).not.toMatch(/Ex\.:|\d{2}\/\d{2}\/\d{4}/);

  const transbordo = await page.evaluate(() => {
    const raiz = document.documentElement;
    return raiz.scrollWidth - raiz.clientWidth;
  });
  expect(transbordo, 'a página não deve ter rolagem horizontal').toBeLessThanOrEqual(1);

  await page.getByRole('link', { name: 'Voltar para pontos de coleta' }).click();
  await expect(page).toHaveURL(/\/pontos$/);
  await expect(page.getByRole('heading', { name: 'Onde entregar seus resíduos' })).toBeVisible();
}

test('visitante abre transparência e impacto pelo rodapé, sem formulário ou rolagem horizontal', async ({
  page,
}) => {
  await page.goto('/');
  const link = page.getByRole('link', { name: 'Transparência e impacto (em breve)' });
  await expect(link).toBeVisible();
  await link.click();

  await verificarPaginaEmBreve(page, MODULOS.transparenciaImpacto);
});

test('cidadão abre preferências de e-mail e extrato de pontos', async ({ page }) => {
  const cidadao = dadosDoCidadao();
  await cadastrarCidadao(page, cidadao);
  await entrar(page, cidadao.email);
  await expect(page).toHaveURL(/\/cidadao\/perfil$/);

  await verificarPaginaEmBreve(page, MODULOS.preferenciasEmail);
  await verificarPaginaEmBreve(page, MODULOS.extratoPontos);
});

test('administrador abre rotas do coletor', async ({ page }) => {
  const organizacao = dadosDaOrganizacao();
  await cadastrarOrganizacao(page, organizacao);
  await entrar(page, organizacao.usuario);
  await expect(page).toHaveURL(/\/organizacao\/equipe$/);

  await verificarPaginaEmBreve(page, MODULOS.rotasColetor);
});

test('módulos protegidos redirecionam visitante para o login com retorno', async ({ page }) => {
  for (const modulo of [MODULOS.preferenciasEmail, MODULOS.extratoPontos, MODULOS.rotasColetor]) {
    await page.goto(modulo.rota);
    await expect(page).toHaveURL(
      new RegExp(`\\/acesso\\/login\\?returnUrl=${modulo.rota.replaceAll('/', '%2F')}$`),
    );
    await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();
  }
});

test('navegação oferece apenas os módulos permitidos a cada papel', async ({ page }) => {
  const links = async () =>
    (await page
      .locator('header a')
      .evaluateAll((elementos) =>
        elementos.map((elemento) => elemento.getAttribute('href') ?? ''),
      )) as string[];

  await page.goto('/');
  expect(await links()).not.toContain(MODULOS.preferenciasEmail.rota);
  await expect(
    page.getByRole('link', { name: 'Transparência e impacto (em breve)' }),
  ).toBeVisible();

  const cidadao = dadosDoCidadao();
  await cadastrarCidadao(page, cidadao);
  await entrar(page, cidadao.email);
  const linksCidadao = await links();
  expect(linksCidadao).toContain(MODULOS.preferenciasEmail.rota);
  expect(linksCidadao).toContain(MODULOS.extratoPontos.rota);
  expect(linksCidadao).not.toContain(MODULOS.rotasColetor.rota);

  await sair(page);

  const organizacao = dadosDaOrganizacao();
  await cadastrarOrganizacao(page, organizacao);
  await entrar(page, organizacao.usuario);
  const linksAdmin = await links();
  expect(linksAdmin).toContain(MODULOS.rotasColetor.rota);
  expect(linksAdmin).not.toContain(MODULOS.preferenciasEmail.rota);
  expect(linksAdmin).not.toContain(MODULOS.extratoPontos.rota);
});
