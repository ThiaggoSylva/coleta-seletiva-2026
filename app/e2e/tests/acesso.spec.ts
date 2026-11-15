import { expect, test } from '@playwright/test';
import {
  SENHA_INICIAL,
  cadastrarCidadao,
  cadastrarOrganizacao,
  dadosDaOrganizacao,
  dadosDoCidadao,
  entrar,
  sair,
  sufixoUnico,
} from './apoio';

test('registra organização, autentica e encerra a sessão', async ({ page }) => {
  const organizacao = dadosDaOrganizacao();

  await cadastrarOrganizacao(page, organizacao);
  await entrar(page, organizacao.usuario);

  await expect(page).toHaveURL(/\/organizacao\/equipe$/);
  await expect(page.getByRole('heading', { name: 'Equipe', exact: true })).toBeVisible();
  await expect(page.getByText('Nenhum operador cadastrado ainda.')).toBeVisible();

  const cookies = await page.context().cookies('http://localhost:4200/api/v1/auth');
  const refresh = cookies.find((cookie) => cookie.name === 'coleta_refresh');
  expect(refresh).toMatchObject({
    httpOnly: true,
    secure: true,
    sameSite: 'Strict',
    path: '/api/v1/auth',
  });
  expect(await page.evaluate(() => localStorage.length)).toBe(0);

  await page.reload();
  await expect(page).toHaveURL(/\/organizacao\/equipe$/);
  await expect(page.getByRole('heading', { name: 'Equipe', exact: true })).toBeVisible();

  await sair(page);
  await expect(page.getByRole('link', { name: 'Entrar' })).toBeVisible();
});

test('rejeita usuário e e-mail duplicados no cadastro de organização', async ({ page }) => {
  const original = dadosDaOrganizacao();
  await cadastrarOrganizacao(page, original);

  const outroUsuario = `dup${sufixoUnico()}`;
  await page.goto('/acesso/registro-organizacao');
  await page.getByLabel('Nome da organização').fill(`Cooperativa ${sufixoUnico()}`);
  await page.getByLabel('Tipo').selectOption('Associacao');
  await page.getByLabel('Usuário', { exact: true }).fill(original.usuario);
  await page.getByLabel('E-mail', { exact: true }).fill(outroUsuario + '@example.org');
  await page.getByLabel('Senha', { exact: true }).fill(SENHA_INICIAL);
  await page.getByRole('button', { name: 'Cadastrar organização' }).click();
  await expect(page.getByRole('alert')).toContainText('usuário informado já está em uso');

  await page.getByLabel('Usuário', { exact: true }).fill(outroUsuario);
  await page.getByLabel('E-mail', { exact: true }).fill(original.email);
  await page.getByRole('button', { name: 'Cadastrar organização' }).click();
  await expect(page.getByRole('alert')).toContainText('e-mail informado já está em uso');
});

test('registra cidadão, rejeita e-mail duplicado e autentica por e-mail', async ({ page }) => {
  const cidadao = dadosDoCidadao();
  await cadastrarCidadao(page, cidadao);

  await page.goto('/acesso/registro-cidadao');
  await page.getByLabel('Nome', { exact: true }).fill(`Outro ${sufixoUnico()}`);
  await page.getByLabel('E-mail', { exact: true }).fill(cidadao.email);
  await page.getByLabel('Telefone', { exact: true }).fill('(11) 98888-0000');
  await page.getByLabel('Senha', { exact: true }).fill(SENHA_INICIAL);
  await page.getByRole('button', { name: 'Cadastrar', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('e-mail informado já está em uso');

  await entrar(page, cidadao.email);
  await expect(page).toHaveURL(/\/cidadao\/perfil$/);
  await expect(page.getByRole('heading', { name: 'Meu perfil' })).toBeVisible();
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue(cidadao.nome);
});

test('não autentica com senha incorreta e nega rota protegida sem sessão', async ({ page }) => {
  const cidadao = dadosDoCidadao();
  await cadastrarCidadao(page, cidadao);

  await entrar(page, cidadao.email, 'SenhaErrada!123', { aguardarAutenticacao: false });
  await expect(page.getByRole('alert')).toContainText('Usuário ou senha inválidos.');
  await expect(page).toHaveURL(/\/acesso\/login$/);

  await page.goto('/cidadao/perfil');
  await expect(page).toHaveURL(
    /\/acesso\/login\?returnUrl=%2Fcidadao%2Fperfil$/,
  );
  await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();
});
