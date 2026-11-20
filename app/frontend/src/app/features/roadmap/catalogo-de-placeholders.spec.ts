import { CATALOGO_DE_PLACEHOLDERS } from './catalogo-de-placeholders';

describe('Catálogo de placeholders (espelho do contrato da API)', () => {
  it('contém exatamente os quatro módulos com fases e rotas estáveis', () => {
    expect(
      CATALOGO_DE_PLACEHOLDERS.map((modulo) => [
        modulo.id,
        modulo.fase,
        modulo.rota,
        modulo.situacao,
      ]),
    ).toEqual([
      ['preferencias-email', 'F7', '/em-breve/preferencias-email', 'em_breve'],
      ['extrato-pontos', 'F8', '/em-breve/extrato-pontos', 'em_breve'],
      ['rotas-coletor', 'F9', '/em-breve/rotas-coletor', 'em_breve'],
      ['transparencia-impacto', 'F10', '/em-breve/transparencia-impacto', 'em_breve'],
    ]);
  });

  it('não expõe campos de dados pessoais ou de organização', () => {
    const permitidos = ['descricao', 'fase', 'id', 'rota', 'situacao', 'titulo'];

    CATALOGO_DE_PLACEHOLDERS.forEach((modulo) => {
      expect(Object.keys(modulo).sort()).toEqual(permitidos);
      Object.values(modulo).forEach((valor) => {
        expect(valor).not.toMatch(/\d{3}\.\d{3}\.\d{3}-\d{2}/);
      });
    });
  });
});
