import { expect, test } from '@playwright/test';
import {
  cadastrarCidadao,
  dadosDoCidadao,
  entrar,
  sufixoUnico,
} from './apoio';

test('cidadão edita o próprio perfil e recebe conflito de e-mail', async ({ page }) => {
  const cidadao = dadosDoCidadao('perfil');
  const outroCidadao = dadosDoCidadao('outro');
  await cadastrarCidadao(page, cidadao);
  await cadastrarCidadao(page, outroCidadao);

  await entrar(page, cidadao.email);
  await expect(page).toHaveURL(/\/cidadao\/perfil$/);
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue(cidadao.nome);
  await expect(page.getByLabel('E-mail', { exact: true })).toHaveValue(cidadao.email);

  const novoNome = `Cidadão Atualizado ${sufixoUnico()}`;
  await page.getByLabel('Nome', { exact: true }).fill(novoNome);
  await page.getByLabel('Telefone').fill('(11) 97777-1111');
  await page.getByRole('button', { name: 'Salvar alterações' }).click();
  await expect(page.getByText('Perfil atualizado.')).toBeVisible();
  await expect(page.getByRole('navigation')).toContainText(novoNome);

  await page.getByLabel('E-mail', { exact: true }).fill(outroCidadao.email);
  await page.getByRole('button', { name: 'Salvar alterações' }).click();
  await expect(page.getByText('O e-mail informado já está em uso.')).toBeVisible();
  await expect(page.getByLabel('E-mail', { exact: true })).toBeFocused();
  await expect(page.getByLabel('E-mail', { exact: true })).toHaveAttribute(
    'aria-invalid',
    'true',
  );
});
