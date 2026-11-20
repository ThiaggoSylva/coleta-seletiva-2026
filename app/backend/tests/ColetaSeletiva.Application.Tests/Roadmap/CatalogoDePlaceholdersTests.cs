using ColetaSeletiva.Application.Roadmap;

namespace ColetaSeletiva.Application.Tests.Roadmap;

[TestClass]
public sealed class CatalogoDePlaceholdersTests
{
    private static readonly (string Id, string Fase, string Rota)[] Esperados =
    [
        ("preferencias-email", "F7", "/em-breve/preferencias-email"),
        ("extrato-pontos", "F8", "/em-breve/extrato-pontos"),
        ("rotas-coletor", "F9", "/em-breve/rotas-coletor"),
        ("transparencia-impacto", "F10", "/em-breve/transparencia-impacto"),
    ];

    private readonly ICatalogoDePlaceholders _catalogo = new CatalogoDePlaceholders();

    [TestMethod]
    public void Catalogo_contem_exatamente_os_quatro_modulos_com_fases_e_rotas_estaveis()
    {
        var modulos = _catalogo.Listar();

        Assert.AreEqual(Esperados.Length, modulos.Count);
        CollectionAssert.AreEquivalent(
            Esperados.Select(item => item.Id).ToArray(),
            modulos.Select(modulo => modulo.Id).ToArray());

        foreach (var esperado in Esperados)
        {
            var modulo = modulos.Single(item => item.Id == esperado.Id);
            Assert.AreEqual(esperado.Fase, modulo.Fase);
            Assert.AreEqual(esperado.Rota, modulo.Rota);
            Assert.AreEqual(CatalogoDePlaceholders.SituacaoEmBreve, modulo.Situacao);
            Assert.IsFalse(string.IsNullOrWhiteSpace(modulo.Titulo));
            Assert.IsFalse(string.IsNullOrWhiteSpace(modulo.Descricao));
        }
    }

    [TestMethod]
    public void Catalogo_nao_expõe_campos_de_dados_pessoais_ou_de_organizacao()
    {
        var permitidos = new[]
        {
            nameof(ModuloEmBreve.Id),
            nameof(ModuloEmBreve.Titulo),
            nameof(ModuloEmBreve.Descricao),
            nameof(ModuloEmBreve.Fase),
            nameof(ModuloEmBreve.Rota),
            nameof(ModuloEmBreve.Situacao),
        };

        var propriedades = typeof(ModuloEmBreve)
            .GetProperties()
            .Select(propriedade => propriedade.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(permitidos, propriedades);
    }

    [TestMethod]
    public void Consulta_por_id_devolve_modulo_ou_nulo_sem_ecoar_entrada()
    {
        foreach (var esperado in Esperados)
        {
            Assert.AreEqual(esperado.Id, _catalogo.ObterPorId(esperado.Id)?.Id);
        }

        Assert.IsNull(_catalogo.ObterPorId("modulo-inexistente"));
        Assert.IsNull(_catalogo.ObterPorId(string.Empty));
    }

    [TestMethod]
    public void Handler_de_consulta_devolve_o_catalogo_sem_efeito_colateral()
    {
        var handler = new ConsultarPlaceholdersHandler(_catalogo);

        var modulos = handler.Listar();

        Assert.AreEqual(_catalogo.Listar().Count, modulos.Count);
        Assert.AreEqual(
            "transparencia-impacto",
            handler.ObterPorId("transparencia-impacto")?.Id);
        Assert.AreSame(_catalogo.Listar(), modulos);
    }
}
