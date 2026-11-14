import { AbstractControl, ValidationErrors } from '@angular/forms';

export const TAMANHO_MINIMO_DA_SENHA = 12;
export const CARACTERES_DISTINTOS_MINIMOS = 6;

export const DESCRICAO_DA_POLITICA_DE_SENHA =
  'Use pelo menos 12 caracteres, com maiúscula, minúscula, número, símbolo e 6 caracteres distintos.';

export function senhaAtendePolitica(senha: string): boolean {
  return (
    senha.length >= TAMANHO_MINIMO_DA_SENHA &&
    new Set(senha).size >= CARACTERES_DISTINTOS_MINIMOS &&
    /[a-z]/.test(senha) &&
    /[A-Z]/.test(senha) &&
    /\d/.test(senha) &&
    /[^A-Za-z0-9]/.test(senha)
  );
}

export function politicaDeSenha(controle: AbstractControl): ValidationErrors | null {
  const senha = typeof controle.value === 'string' ? controle.value : '';

  return senhaAtendePolitica(senha) ? null : { senhaFraca: true };
}

export function confirmacaoDeSenha(controle: AbstractControl): ValidationErrors | null {
  const novaSenha = controle.parent?.get('novaSenha')?.value;

  if (novaSenha === undefined || novaSenha === controle.value) {
    return null;
  }

  return { senhasDiferentes: true };
}
