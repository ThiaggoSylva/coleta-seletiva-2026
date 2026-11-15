import { expect, test } from '@playwright/test';
import {
  SENHA_INICIAL,
  SENHA_NOVA,
  alterarSenhaNoPrimeiroAcesso,
  cadastrarCidadao,
  cadastrarOrganizacao,
  criarOperador,
  dadosDaOrganizacao,
  dadosDoCidadao,
  entrar,
  sair,
  sufixoUnico,
} from './apoio';

test('administrador cria, primeiro acesso troca a senha e inativa o operador', async ({
  page,
}) => {
  const organizacao = dadosDaOrganizacao();
  const sufixo = sufixoUnico();
  const operador = {
    nome: `Operador ${sufixo}`,
    usuario: `op${sufixo}`,
    email: `op${sufixo}@example.org`,
    senha: SENHA_INICIAL,
  };

  await cadastrarOrganizacao(page, organizacao);
  await entrar(page, organizacao.usuario);
  await expect(page).toHaveURL(/\/organizacao\/equipe$/);

  await criarOperador(page, operador);
  const linhaDoOperador = page.getByRole('row', { name: new RegExp(operador.nome) });
  await expect(linhaDoOperador).toContainText('Ativo');
  await expect(linhaDoOperador).toContainText('Troca de senha pendente');

  await sair(page);
  await entrar(page, operador.usuario, operador.senha);
  await expect(page).toHaveURL(/\/acesso\/trocar-senha$/);

  await page.goto('/organizacao/equipe');
  await expect(page).toHaveURL(/\/acesso\/trocar-senha$/);
  await alterarSenhaNoPrimeiroAcesso(page, SENHA_INICIAL, SENHA_NOVA);

  await entrar(page, operador.usuario, SENHA_NOVA);
  await expect(page).toHaveURL(/\/organizacao\/inicio$/);
  await expect(page.getByRole('heading', { name: 'Área da organização' })).toBeVisible();
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Equipe' })).toHaveCount(
    0,
  );

  await page.goto('/organizacao/equipe');
  await expect(page).toHaveURL(/\/organizacao\/inicio$/);

  await sair(page);
  await entrar(page, organizacao.usuario);
  const linhaParaInativar = page.getByRole('row', {
    name: new RegExp(operador.nome),
  });
  await linhaParaInativar.getByRole('button', { name: 'Inativar', exact: true }).click();
  await linhaParaInativar.getByRole('button', { name: 'Confirmar', exact: true }).click();
  await expect(page.getByText('Operador inativado.')).toBeVisible();
  await expect(page.getByRole('row', { name: new RegExp(operador.nome) })).toContainText(
    'Inativo',
  );

  await sair(page);
  await entrar(page, operador.usuario, SENHA_NOVA, { aguardarAutenticacao: false });
  await expect(page.getByRole('alert')).toContainText('Usuário ou senha inválidos.');
});

test('administrador de outra organização não edita operador alheio', async ({ page }) => {
  const organizacaoA = dadosDaOrganizacao('orga');
  const organizacaoB = dadosDaOrganizacao('orgb');
  const sufixo = sufixoUnico();
  const operadorB = {
    nome: `Operador B ${sufixo}`,
    usuario: `opb${sufixo}`,
    email: `opb${sufixo}@example.org`,
    senha: SENHA_INICIAL,
  };

  await cadastrarOrganizacao(page, organizacaoA);
  await cadastrarOrganizacao(page, organizacaoB);
  await entrar(page, organizacaoB.usuario);
  await criarOperador(page, operadorB);

  await page
    .getByRole('row', { name: new RegExp(operadorB.nome) })
    .getByRole('link', { name: 'Editar' })
    .click();
  await expect(page).toHaveURL(/\/organizacao\/equipe\/[0-9a-f-]+\/editar$/);
  const urlDeEdicaoDaOrganizacaoB = page.url();

  await sair(page);
  await entrar(page, organizacaoA.usuario);
  await page.goto(urlDeEdicaoDaOrganizacaoB);

  await expect(page.getByText('Operador não encontrado.')).toBeVisible();
  await expect(page.locator('form')).toHaveCount(0);
});

test('cidadão não acessa a gestão de equipe nem a área da organização', async ({ page }) => {
  const cidadao = dadosDoCidadao();
  await cadastrarCidadao(page, cidadao);
  await entrar(page, cidadao.email);
  await expect(page).toHaveURL(/\/cidadao\/perfil$/);

  await page.goto('/organizacao/equipe');
  await expect(page).toHaveURL(/\/cidadao\/perfil$/);

  await page.goto('/organizacao/inicio');
  await expect(page).toHaveURL(/\/cidadao\/perfil$/);

  await expect(page.getByRole('navigation').getByRole('link', { name: 'Equipe' })).toHaveCount(
    0,
  );
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Meu perfil' })).toBeVisible();
});
