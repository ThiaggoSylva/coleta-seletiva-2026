using System.Net;
using System.Text;
using ColetaSeletiva.Application.ConsultaPublica;
using ColetaSeletiva.Infrastructure.FontesPublicas;
using Microsoft.Extensions.Logging.Abstractions;

namespace ColetaSeletiva.IntegrationTests.Publico;

[TestClass]
public sealed class FontePublicaOverpassTests
{
    private const string CorpoDoNominatim = """
        [
          {
            "display_name": "Recife, Pernambuco, Brasil",
            "boundingbox": ["-8.1551869", "-7.9289670", "-35.0186478", "-34.8515401"]
          }
        ]
        """;

    private const string CorpoDoOverpass = """
        {
          "elements": [
            {
              "type": "node",
              "id": 1,
              "lat": -8.0578,
              "lon": -34.8829,
              "tags": {
                "amenity": "recycling",
                "name": "Ecoponto Central",
                "recycling:paper": "yes",
                "recycling:plastic": "yes",
                "opening_hours": "Mo-Fr 08:00-17:00"
              }
            },
            {
              "type": "way",
              "id": 2,
              "center": { "lat": -8.0683, "lon": -34.8845 },
              "tags": {
                "amenity": "recycling",
                "operator": "Cooperativa Verde",
                "recycling:glass_bottles": "yes"
              }
            },
            {
              "type": "node",
              "id": 3,
              "lat": -8.1000,
              "lon": -34.9000,
              "tags": { "amenity": "recycling", "recycling_type": "container" }
            }
          ]
        }
        """;

    [TestMethod]
    public async Task Busca_por_municipio_mapeia_pontos_reais_com_atribuicao()
    {
        var manipulador = new ManipuladorFalso();
        var fonte = CriarFonte(manipulador);

        var resultado = await fonte.BuscarAsync(
            new ConsultaDeFontePublica(MunicipioNome: "Recife", Uf: "PE"));

        Assert.IsTrue(resultado.Consultada);
        Assert.IsTrue(resultado.Disponivel);
        Assert.IsNull(resultado.Aviso);
        Assert.AreEqual(3, resultado.Pontos.Count);

        var ecoponto = resultado.Pontos.Single(ponto => ponto.Identificador == "osm:node/1");
        Assert.AreEqual("Ecoponto Central", ecoponto.Nome);
        CollectionAssert.AreEquivalent(
            new[] { "papel-e-papelao", "plastico" },
            ecoponto.CodigosDeTipos.ToArray());
        Assert.AreEqual("Mo-Fr 08:00-17:00", ecoponto.HorarioLivre);
        Assert.AreEqual("https://www.openstreetmap.org/node/1", ecoponto.UrlDoRegistro);
        StringAssert.Contains(ecoponto.Atribuicao, "OpenStreetMap");

        var caminhao = resultado.Pontos.Single(ponto => ponto.Identificador == "osm:way/2");
        Assert.AreEqual("Ponto de coleta — Cooperativa Verde", caminhao.Nome);
        Assert.AreEqual("Cooperativa Verde", caminhao.Operador);
        Assert.AreEqual(-8.0683, caminhao.Latitude, 0.00001);

        Assert.AreEqual(2, manipulador.Requisicoes.Count);
    }

    [TestMethod]
    public async Task Filtro_de_tipo_remove_pontos_sem_o_tipo_pedido()
    {
        var manipulador = new ManipuladorFalso();
        var fonte = CriarFonte(manipulador);

        var resultado = await fonte.BuscarAsync(
            new ConsultaDeFontePublica(
                MunicipioNome: "Recife",
                Uf: "PE",
                CodigosDeTipos: ["vidro"]));

        Assert.AreEqual(1, resultado.Pontos.Count);
        Assert.AreEqual("osm:way/2", resultado.Pontos[0].Identificador);
    }

    [TestMethod]
    public async Task Cache_evita_repetir_as_mesmas_chamadas()
    {
        var manipulador = new ManipuladorFalso();
        var fonte = CriarFonte(manipulador);

        var consulta = new ConsultaDeFontePublica(MunicipioNome: "Recife", Uf: "PE");
        await fonte.BuscarAsync(consulta);
        var segundo = await fonte.BuscarAsync(consulta);

        Assert.AreEqual(3, segundo.Pontos.Count);
        Assert.AreEqual(2, manipulador.Requisicoes.Count);
    }

    [TestMethod]
    public async Task Falha_da_fonte_vira_aviso_sem_excecao()
    {
        var manipulador = new ManipuladorFalso { RespostaComErro = true };
        var fonte = CriarFonte(manipulador);

        var resultado = await fonte.BuscarAsync(
            new ConsultaDeFontePublica(MunicipioNome: "Recife", Uf: "PE"));

        Assert.IsTrue(resultado.Consultada);
        Assert.IsFalse(resultado.Disponivel);
        Assert.IsEmpty(resultado.Pontos);
        Assert.IsNotNull(resultado.Aviso);
    }

    [TestMethod]
    public async Task Busca_por_raio_nao_consulta_o_nominatim()
    {
        var manipulador = new ManipuladorFalso();
        var fonte = CriarFonte(manipulador);

        await fonte.BuscarAsync(new ConsultaDeFontePublica(
            Latitude: -8.0578,
            Longitude: -34.8829,
            RaioKm: 5));

        Assert.AreEqual(1, manipulador.Requisicoes.Count);
        StringAssert.Contains(manipulador.Requisicoes[0].ToString(), "overpass");
    }

    [TestMethod]
    public async Task Fonte_desativada_nao_faz_chamadas()
    {
        var manipulador = new ManipuladorFalso();
        var fonte = new OverpassFontePublicaDePontos(
            new HttpClient(manipulador),
            NullLogger<OverpassFontePublicaDePontos>.Instance,
            new OpcoesDaFontePublica(Ativa: false));

        var resultado = await fonte.BuscarAsync(
            new ConsultaDeFontePublica(MunicipioNome: "Recife", Uf: "PE"));

        Assert.IsFalse(resultado.Consultada);
        Assert.IsEmpty(manipulador.Requisicoes);
    }

    private static OverpassFontePublicaDePontos CriarFonte(ManipuladorFalso manipulador)
    {
        return new OverpassFontePublicaDePontos(
            new HttpClient(manipulador),
            NullLogger<OverpassFontePublicaDePontos>.Instance,
            new OpcoesDaFontePublica(Ativa: true));
    }

    private sealed class ManipuladorFalso : HttpMessageHandler
    {
        public List<Uri> Requisicoes { get; } = [];

        public bool RespostaComErro { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri is null)
            {
                throw new InvalidOperationException("Requisição sem URL.");
            }

            Requisicoes.Add(request.RequestUri);

            if (RespostaComErro)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            var corpo = request.RequestUri.Host.Contains("nominatim", StringComparison.OrdinalIgnoreCase)
                ? CorpoDoNominatim
                : CorpoDoOverpass;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(corpo, Encoding.UTF8, "application/json"),
            });
        }
    }
}
