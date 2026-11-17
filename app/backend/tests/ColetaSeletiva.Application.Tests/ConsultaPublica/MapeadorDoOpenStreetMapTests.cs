using ColetaSeletiva.Application.ConsultaPublica;

namespace ColetaSeletiva.Application.Tests.ConsultaPublica;

[TestClass]
public sealed class MapeadorDoOpenStreetMapTests
{
    [TestMethod]
    public void Tags_conhecidas_viram_codigos_do_catalogo()
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["amenity"] = "recycling",
            ["recycling:paper"] = "yes",
            ["recycling:plastic"] = "yes",
            ["recycling:glass_bottles"] = "yes",
        };

        var codigos = MapeadorDoOpenStreetMap.CodigosDeTipos(tags);

        CollectionAssert.AreEquivalent(
            new[] { "papel-e-papelao", "plastico", "vidro" },
            codigos.ToArray());
    }

    [TestMethod]
    public void Tags_com_valor_no_sao_ignoradas()
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["recycling:paper"] = "no",
            ["recycling:metal"] = "yes",
        };

        var codigos = MapeadorDoOpenStreetMap.CodigosDeTipos(tags);

        CollectionAssert.AreEquivalent(new[] { "metal" }, codigos.ToArray());
    }

    [TestMethod]
    public void Tags_desconhecidas_nao_geram_tipo()
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["amenity"] = "recycling",
            ["recycling_type"] = "container",
        };

        Assert.IsEmpty(MapeadorDoOpenStreetMap.CodigosDeTipos(tags));
        Assert.IsEmpty(MapeadorDoOpenStreetMap.CodigosDeTipos(null));
    }

    [TestMethod]
    public void Nome_prefere_name_depois_operator_e_por_fim_o_padrao()
    {
        Assert.AreEqual(
            "Ecoponto Central",
            MapeadorDoOpenStreetMap.Nome(new Dictionary<string, string>
            {
                ["name"] = " Ecoponto Central ",
                ["operator"] = "Cooperativa",
            }));

        Assert.AreEqual(
            "Ponto de coleta — Cooperativa",
            MapeadorDoOpenStreetMap.Nome(new Dictionary<string, string>
            {
                ["operator"] = "Cooperativa",
            }));

        Assert.AreEqual(
            MapeadorDoOpenStreetMap.NomePadrao,
            MapeadorDoOpenStreetMap.Nome(new Dictionary<string, string>
            {
                ["amenity"] = "recycling",
            }));
    }

    [TestMethod]
    public void Normalizacao_remove_acentos_e_caixa()
    {
        Assert.AreEqual(
            "ponto sao joao",
            MapeadorDoOpenStreetMap.Normalizar("  Ponto São João "));
    }

    [TestMethod]
    public void Distancia_entre_recife_e_olinda_fica_na_ordem_de_grandeza_esperada()
    {
        var distancia = MapeadorDoOpenStreetMap.DistanciaEmKm(
            -8.0578,
            -34.8829,
            -7.9955,
            -34.8553);

        Assert.IsTrue(distancia is > 5 and < 10, $"Distância inesperada: {distancia} km.");
    }

    [TestMethod]
    public void Ponto_externo_com_mesmo_nome_e_proximo_e_considerado_duplicado()
    {
        Assert.IsTrue(MapeadorDoOpenStreetMap.CorrespondeAPontoCadastrado(
            "Ecoponto Central",
            -8.05780,
            -34.88290,
            "ECOPONTO CENTRAL",
            -8.05800,
            -34.88300));

        Assert.IsFalse(MapeadorDoOpenStreetMap.CorrespondeAPontoCadastrado(
            "Ecoponto Central",
            -8.05780,
            -34.88290,
            "Ecoponto Central",
            -8.10000,
            -34.90000));
    }

    [TestMethod]
    public async Task Fonte_desativada_responde_vazio_sem_consultar()
    {
        var resultado = await new FontePublicaDesativada().BuscarAsync(
            new ConsultaDeFontePublica(MunicipioNome: "Recife", Uf: "PE"));

        Assert.IsFalse(resultado.Consultada);
        Assert.IsEmpty(resultado.Pontos);
        Assert.IsNull(resultado.Aviso);
    }
}
