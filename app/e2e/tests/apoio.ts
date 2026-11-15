import { expect, Page } from '@playwright/test';

export const SENHA_INICIAL = 'SenhaForte!123';
export const SENHA_NOVA = 'NovaSenhaForte!456';

export interface DadosDaOrganizacao {
  nome: string;
  usuario: string;
  email: string;
  senha: string;
}

export interface DadosDoCidadao {
  nome: string;
  email: string;
  telefone: string;
  senha: string;
}

export interface DadosDoOperador {
  nome: string;
  usuario: string;
  email: string;
  senha: string;
}

export function sufixoUnico(): string {
  return `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 8)}`;
}

export function dadosDaOrganizacao(prefixo = 'org'): DadosDaOrganizacao {
  const sufixo = sufixoUnico();
  return {
    nome: `Cooperativa ${sufixo}`,
    usuario: `${prefixo}${sufixo}`,
    email: `${prefixo}${sufixo}@example.org`,
    senha: SENHA_INICIAL,
  };
}

export function dadosDoCidadao(prefixo = 'cidadao'): DadosDoCidadao {
  const sufixo = sufixoUnico();
  return {
    nome: `Cidadão ${sufixo}`,
    email: `${prefixo}${sufixo}@example.org`,
    telefone: '(11) 99999-0000',
    senha: SENHA_INICIAL,
  };
}

export async function cadastrarOrganizacao(
  page: Page,
  dados: DadosDaOrganizacao,
): Promise<void> {
  await page.goto('/acesso/registro-organizacao');
  await page.getByLabel('Nome da organização').fill(dados.nome);
  await page.getByLabel('Tipo').selectOption('Cooperativa');
  await page.getByLabel('Usuário', { exact: true }).fill(dados.usuario);
  await page.getByLabel('E-mail', { exact: true }).fill(dados.email);
  await page.getByLabel('Senha', { exact: true }).fill(dados.senha);
  await page.getByRole('button', { name: 'Cadastrar organização' }).click();
  await expect(page.getByText('Organização cadastrada.')).toBeVisible();
}

export async function cadastrarCidadao(page: Page, dados: DadosDoCidadao): Promise<void> {
  await page.goto('/acesso/registro-cidadao');
  await page.getByLabel('Nome', { exact: true }).fill(dados.nome);
  await page.getByLabel('E-mail', { exact: true }).fill(dados.email);
  await page.getByLabel('Telefone', { exact: true }).fill(dados.telefone);
  await page.getByLabel('Senha', { exact: true }).fill(dados.senha);
  await page.getByRole('button', { name: 'Cadastrar', exact: true }).click();
  await expect(page.getByText('Conta criada.')).toBeVisible();
}

export async function entrar(
  page: Page,
  identificador: string,
  senha: string = SENHA_INICIAL,
  opcoes: { aguardarAutenticacao?: boolean } = {},
): Promise<void> {
  await page.goto('/acesso/login');
  await page.getByLabel('Usuário ou e-mail').fill(identificador);
  await page.getByLabel('Senha', { exact: true }).fill(senha);
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();

  if (opcoes.aguardarAutenticacao ?? true) {
    await expect(page.getByRole('button', { name: 'Sair', exact: true })).toBeVisible();
  }
}

export async function sair(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Sair', exact: true }).click();
  await expect(page).toHaveURL(/\/acesso\/login$/);
}

export async function criarOperador(
  page: Page,
  dados: DadosDoOperador,
): Promise<void> {
  await page.getByRole('link', { name: 'Novo operador' }).click();
  await expect(page.getByRole('heading', { name: 'Novo operador' })).toBeVisible();

  await page.getByLabel('Nome', { exact: true }).fill(dados.nome);
  await page.getByLabel('Usuário', { exact: true }).fill(dados.usuario);
  await page.getByLabel('E-mail', { exact: true }).fill(dados.email);
  await page.getByLabel('Senha inicial').fill(dados.senha);
  await page.getByRole('button', { name: 'Criar operador' }).click();

  await expect(page).toHaveURL(/\/organizacao\/equipe(\?criado=1)?$/);
  await expect(page.getByText('Operador criado.')).toBeVisible();
}

export async function alterarSenhaNoPrimeiroAcesso(
  page: Page,
  senhaAtual: string,
  novaSenha: string,
): Promise<void> {
  await expect(page.getByRole('heading', { name: 'Primeiro acesso' })).toBeVisible();
  await page.getByLabel('Senha atual').fill(senhaAtual);
  await page.getByLabel('Nova senha', { exact: true }).fill(novaSenha);
  await page.getByLabel('Confirmar nova senha').fill(novaSenha);
  await page.getByRole('button', { name: 'Alterar senha' }).click();

  await expect(page).toHaveURL(/\/acesso\/login\?senhaAlterada=1$/);
  await expect(page.getByText('Senha alterada. Entre novamente com a nova senha.')).toBeVisible();
}
