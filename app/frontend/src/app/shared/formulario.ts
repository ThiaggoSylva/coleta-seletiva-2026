import { ElementRef } from '@angular/core';
import { FormGroup } from '@angular/forms';

export type ErrosDoServidor = Record<string, string[]>;

export function campoInvalido(formulario: FormGroup, nome: string): boolean {
  const controle = formulario.get(nome);
  return controle !== null && controle.invalid && (controle.touched || controle.dirty);
}

export function mensagensDoCampo(
  formulario: FormGroup,
  nome: string,
  errosDoServidor: ErrosDoServidor,
): string[] {
  const doServidor = errosDoServidor[nome] ?? [];
  if (doServidor.length > 0) {
    return doServidor;
  }

  const controle = formulario.get(nome);
  if (controle === null || !controle.invalid || !(controle.touched || controle.dirty)) {
    return [];
  }

  if (controle.hasError('required')) {
    return ['Informe este campo.'];
  }

  if (controle.hasError('email')) {
    return ['Informe um e-mail válido.'];
  }

  if (controle.hasError('minlength')) {
    const minimo = (controle.getError('minlength') as { requiredLength?: number }).requiredLength;
    return [`Use pelo menos ${minimo ?? 0} caracteres.`];
  }

  if (controle.hasError('maxlength')) {
    const maximo = (controle.getError('maxlength') as { requiredLength?: number }).requiredLength;
    return [`Use no máximo ${maximo ?? 0} caracteres.`];
  }

  if (controle.hasError('senhasDiferentes')) {
    return ['As senhas não conferem.'];
  }

  return ['Valor inválido.'];
}

export function focarPrimeiroInvalido(
  formulario: FormGroup,
  campos: Record<string, ElementRef<HTMLElement> | undefined>,
  errosDoServidor: ErrosDoServidor = {},
): void {
  const nome = Object.keys(campos).find(
    (campo) => formulario.get(campo)?.invalid === true || (errosDoServidor[campo]?.length ?? 0) > 0,
  );

  if (nome !== undefined) {
    campos[nome]?.nativeElement.focus();
  }
}
