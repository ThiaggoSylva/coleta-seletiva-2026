using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Application.ConsultaPublica;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ColetaSeletiva.IntegrationTests.Publico;

/// <summary>
/// Verifica a mescla dos pontos do OpenStreetMap com os pontos cadastrados sobre o
/// banco real: a fonte externa é substituída por um dublê, então nenhuma chamada
/// sai para a rede.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MesclaDePontosExternosApiTests
{
    private const string NomeBanco = "ColetaSeletivaMesclaDePontosExternosApiTests";
    private const int MunicipioGoiania = 5208707;
    private const string AvisoDeIndisponibilidade =
        "Pontos do OpenStreetMap indisponíveis no momento; exibindo apenas os cadastrados.";

    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly FontePublicaFalsa Fonte = new();

    private static string _connectionString = null!;
    private static string _nomeDoMunicipio = null!;
    private static ApiDeTeste _fabrica = null!;
    private static Guid _tipoMetalId;

    [ClassInitialize]
    public static void Inicializar(TestContext _)
    {
        InicializarAsync().GetAwaiter().GetResult();
    }

    [ClassCleanup]
    public static void Limpar()
    {
        LimparAsync().GetAwaiter().GetResult();
    }

    [TestMethod]
    public async Task Mescla_externos_aos_cadastrados_e_marca_origem_e_atribuicao()
    {
        Fonte.Configurar(RespostaComPontos());
        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new { municipioId = MunicipioGoiania });
        var raiz = documento.RootElement;

        Assert.AreEqual(4, raiz.GetProperty("total").GetInt32());
        Assert.IsTrue(raiz.GetProperty("avisoDeFonteExterna").ValueKind is JsonValueKind.Null);
        StringAssert.Contains(
            raiz.GetProperty("atribuicaoDeFonteExterna").GetString(),
            "OpenStreetMap");

        var externo = Item(raiz, "Ecoponto Novo");
        Assert.AreEqual("OpenStreetMap", externo.GetProperty("origem").GetString());
        Assert.AreEqual("Cooperativa Externo", externo.GetProperty("organizacaoNome").GetString());
        Assert.AreEqual(
            "https://www.openstreetmap.org/node/10",
            externo.GetProperty("urlDaFonte").GetString());
        StringAssert.Contains(externo.GetProperty("atribuicao").GetString(), "ODbL");
        Assert.IsTrue(externo.GetProperty("contatoEmail").ValueKind is JsonValueKind.Null);
        Assert.IsTrue(externo.GetProperty("abertoAgora").ValueKind is JsonValueKind.Null);
        Assert.AreEqual(0, externo.GetProperty("horarios").GetArrayLength());
        StringAssert.Contains(
            externo.GetProperty("observacoes").GetString(),
            "Mo-Fr 08:00-17:00");
        Assert.AreEqual(
            "metal",
            externo.GetProperty("tipos")[0].GetProperty("codigo").GetString());

        var externoSemOperador = Item(raiz, "Ponto Longe");
        Assert.AreEqual("OpenStreetMap", externoSemOperador.GetProperty("organizacaoNome").GetString());

        var cadastrado = Item(raiz, "Ponto Alfa Centro");
        Assert.AreEqual("Organizacao", cadastrado.GetProperty("origem").GetString());
        Assert.IsTrue(cadastrado.GetProperty("urlDaFonte").ValueKind is JsonValueKind.Null);
        Assert.IsTrue(cadastrado.GetProperty("atribuicao").ValueKind is JsonValueKind.Null);
    }

    [TestMethod]
    public async Task Externo_com_mesmo_nome_e_proximidade_nao_duplica_o_ponto_cadastrado()
    {
        Fonte.Configurar(RespostaComPontos());
        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new { municipioId = MunicipioGoiania });
        var itens = documento.RootElement.GetProperty("itens").EnumerateArray().ToList();

        var comMesmoNome = itens
            .Where(item => item.GetProperty("nome").GetString() == "Ecoponto Central")
            .ToList();

        Assert.AreEqual(1, comMesmoNome.Count);
        Assert.AreEqual("Organizacao", comMesmoNome[0].GetProperty("origem").GetString());
        Assert.AreEqual(4, documento.RootElement.GetProperty("total").GetInt32());
    }

    [TestMethod]
    public async Task Filtro_de_tipo_chega_a_fonte_com_o_codigo_do_catalogo()
    {
        Fonte.Configurar(RespostaComPontos());
        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new
        {
            municipioId = MunicipioGoiania,
            tipoResiduoId = _tipoMetalId,
        });

        Assert.AreEqual(1, Fonte.Consultas.Count);
        var consulta = Fonte.Consultas[0];
        Assert.AreEqual(_nomeDoMunicipio, consulta.MunicipioNome);
        Assert.AreEqual("GO", consulta.Uf);
        CollectionAssert.AreEqual(new[] { "metal" }, consulta.CodigosDeTipos?.ToArray());
        Assert.IsNull(consulta.Latitude);
        Assert.AreEqual(100, consulta.MaximoDePontos);

        Assert.AreEqual(3, documento.RootElement.GetProperty("total").GetInt32());
    }

    [TestMethod]
    public async Task Busca_por_proximidade_informa_a_fonte_e_calcula_a_distancia_dos_externos()
    {
        Fonte.Configurar(RespostaComPontos());
        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new
        {
            latitude = -16.6869,
            longitude = -49.2648,
            raioKm = 20,
        });

        Assert.AreEqual(1, Fonte.Consultas.Count);
        var consulta = Fonte.Consultas[0];
        Assert.IsNull(consulta.MunicipioNome);
        Assert.AreEqual(-16.6869, consulta.Latitude);
        Assert.AreEqual(-49.2648, consulta.Longitude);
        Assert.AreEqual(20, consulta.RaioKm);

        var externo = Item(documento.RootElement, "Ecoponto Novo");
        var distancia = externo.GetProperty("distanciaKm").GetDouble();
        Assert.IsTrue(distancia is > 0 and < 1, $"Distância inesperada: {distancia} km.");
    }

    [TestMethod]
    public async Task Aberto_agora_nao_consulta_a_fonte_externa()
    {
        Fonte.Configurar(RespostaComPontos());
        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new
        {
            municipioId = MunicipioGoiania,
            abertoAgora = true,
        });

        Assert.IsEmpty(Fonte.Consultas);
        Assert.IsTrue(documento.RootElement.GetProperty("avisoDeFonteExterna").ValueKind is JsonValueKind.Null);

        var nomes = documento.RootElement.GetProperty("itens")
            .EnumerateArray()
            .Select(item => item.GetProperty("nome").GetString())
            .ToList();

        CollectionAssert.DoesNotContain(nomes, "Ecoponto Novo");
        CollectionAssert.DoesNotContain(nomes, "Ponto Longe");
    }

    [TestMethod]
    public async Task Busca_sem_municipio_nem_coordenadas_nao_consulta_a_fonte_externa()
    {
        Fonte.Configurar(RespostaComPontos());
        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new { });

        Assert.IsEmpty(Fonte.Consultas);
        Assert.AreEqual(2, documento.RootElement.GetProperty("total").GetInt32());
        Assert.IsTrue(documento.RootElement.GetProperty("atribuicaoDeFonteExterna").ValueKind is JsonValueKind.Null);
    }

    [TestMethod]
    public async Task Falha_da_fonte_vira_aviso_sem_esconder_os_pontos_cadastrados()
    {
        Fonte.Configurar(new ResultadoDaFontePublica(
            Pontos: [],
            Consultada: true,
            Disponivel: false,
            Aviso: AvisoDeIndisponibilidade));

        using var cliente = _fabrica.CreateClient();

        using var documento = await BuscarAsync(cliente, new { municipioId = MunicipioGoiania });
        var raiz = documento.RootElement;

        Assert.AreEqual(1, Fonte.Consultas.Count);
        Assert.AreEqual(2, raiz.GetProperty("total").GetInt32());
        Assert.AreEqual(AvisoDeIndisponibilidade, raiz.GetProperty("avisoDeFonteExterna").GetString());
        Assert.IsTrue(raiz.GetProperty("atribuicaoDeFonteExterna").ValueKind is JsonValueKind.Null);
    }

    private static ResultadoDaFontePublica RespostaComPontos()
    {
        return new ResultadoDaFontePublica(
            Pontos:
            [
                new PontoDeFontePublica(
                    Identificador: "osm:node/10",
                    Nome: "Ecoponto Novo",
                    Operador: "Cooperativa Externo",
                    Latitude: -16.6875,
                    Longitude: -49.2655,
                    Rua: "Rua das Palmeiras",
                    Bairro: "Setor Central",
                    HorarioLivre: "Mo-Fr 08:00-17:00",
                    CodigosDeTipos: ["metal"],
                    UrlDoRegistro: "https://www.openstreetmap.org/node/10",
                    Atribuicao: MapeadorDoOpenStreetMap.Atribuicao),
                // Mesmo nome e a poucos metros de um ponto cadastrado: deve ser descartado.
                new PontoDeFontePublica(
                    Identificador: "osm:node/11",
                    Nome: "Ecoponto Central",
                    Operador: null,
                    Latitude: -16.68691,
                    Longitude: -49.26501,
                    Rua: null,
                    Bairro: null,
                    HorarioLivre: null,
                    CodigosDeTipos: ["papel-e-papelao"],
                    UrlDoRegistro: "https://www.openstreetmap.org/node/11",
                    Atribuicao: MapeadorDoOpenStreetMap.Atribuicao),
                new PontoDeFontePublica(
                    Identificador: "osm:node/12",
                    Nome: "Ponto Longe",
                    Operador: null,
                    Latitude: -16.8000,
                    Longitude: -49.3200,
                    Rua: null,
                    Bairro: null,
                    HorarioLivre: null,
                    CodigosDeTipos: ["metal"],
                    UrlDoRegistro: "https://www.openstreetmap.org/node/12",
                    Atribuicao: MapeadorDoOpenStreetMap.Atribuicao),
            ],
            Consultada: true,
            Disponivel: true,
            Aviso: null);
    }

    private static async Task<JsonDocument> BuscarAsync(HttpClient cliente, object filtros)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/v1/publico/pontos/busca", filtros);
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.AreEqual(
            HttpStatusCode.OK,
            resposta.StatusCode,
            $"A busca pública respondeu {(int)resposta.StatusCode}: {corpo}");

        return JsonDocument.Parse(corpo);
    }

    private static JsonElement Item(JsonElement raiz, string nome)
    {
        return raiz.GetProperty("itens")
            .EnumerateArray()
            .Single(item => item.GetProperty("nome").GetString() == nome);
    }

    private static async Task InicializarAsync()
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using (var contexto = CriarContexto())
        {
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        await using (var contexto = CriarContexto())
        {
            var tipos = await contexto.TiposResiduo
                .AsNoTracking()
                .Where(tipo => tipo.Codigo == "metal" || tipo.Codigo == "papel-e-papelao")
                .ToListAsync();

            Assert.AreEqual(2, tipos.Count, "O catálogo semeado precisa ter metal e papel.");
            _tipoMetalId = tipos.Single(tipo => tipo.Codigo == "metal").Id;
            var tipoPapelId = tipos.Single(tipo => tipo.Codigo == "papel-e-papelao").Id;

            _nomeDoMunicipio = await contexto.Municipios
                .AsNoTracking()
                .Where(municipio => municipio.Id == MunicipioGoiania)
                .Select(municipio => municipio.Nome)
                .SingleAsync();

            var cooperativa = Organizacao.Criar("Cooperativa Alfa", TipoOrganizacao.Cooperativa, Agora);
            cooperativa.DefinirPublicacao(publicada: true, Agora);

            var municipio = OrganizacaoMunicipio.Criar(MunicipioGoiania, Agora);
            ((IEntidadeDaOrganizacao)municipio).OrganizacaoId = cooperativa.Id;

            var tiposHabilitados = new List<OrganizacaoTipo>
            {
                CriarVinculoDeTipo(cooperativa.Id, _tipoMetalId),
                CriarVinculoDeTipo(cooperativa.Id, tipoPapelId),
            };

            var pontos = new List<PontoDeColeta>
            {
                CriarPonto(cooperativa.Id, "Ponto Alfa Centro", -16.6869, -49.2648, _tipoMetalId),
                CriarPonto(cooperativa.Id, "Ecoponto Central", -16.6869, -49.2650, tipoPapelId),
                CriarPonto(
                    cooperativa.Id,
                    "Ponto Alfa Inativo",
                    -16.6870,
                    -49.2650,
                    _tipoMetalId,
                    StatusPonto.Inativo),
            };

            contexto.Organizacoes.Add(cooperativa);
            contexto.OrganizacaoMunicipios.Add(municipio);
            contexto.OrganizacaoTipos.AddRange(tiposHabilitados);
            contexto.PontosDeColeta.AddRange(pontos);

            await contexto.SaveChangesAsync();
        }

        _fabrica = new ApiDeTeste(
            _connectionString,
            new Dictionary<string, string?>
            {
                ["LimitesDeTaxa:ConsultaPublica:PermitLimit"] = "1000",
                ["LimitesDeTaxa:ConsultaPublica:WindowSeconds"] = "60",
            },
            configurarServicos: servicos =>
            {
                servicos.RemoveAll<IFontePublicaDePontos>();
                servicos.AddSingleton<IFontePublicaDePontos>(Fonte);
            });
    }

    private static async Task LimparAsync()
    {
        await _fabrica.DisposeAsync();

        await using var contexto = CriarContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private static ColetaSeletivaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<ColetaSeletivaDbContext>()
            .UseSqlServer(_connectionString, sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new ColetaSeletivaDbContext(options, new TenantContext());
    }

    private static OrganizacaoTipo CriarVinculoDeTipo(Guid organizacaoId, Guid tipoResiduoId)
    {
        var vinculo = OrganizacaoTipo.Criar(tipoResiduoId, aceitaRetiradaDomiciliar: false, Agora);
        ((IEntidadeDaOrganizacao)vinculo).OrganizacaoId = organizacaoId;
        return vinculo;
    }

    private static PontoDeColeta CriarPonto(
        Guid organizacaoId,
        string nome,
        double latitude,
        double longitude,
        Guid tipoResiduoId,
        StatusPonto status = StatusPonto.Ativo)
    {
        var ponto = PontoDeColeta.Criar(
            nome,
            MunicipioGoiania,
            "GO",
            "Setor Central",
            "Rua de Teste",
            "100",
            null,
            "74000000",
            latitude,
            longitude,
            null,
            status,
            [new HorarioDeFuncionamento(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            [tipoResiduoId],
            Agora);

        ((IEntidadeDaOrganizacao)ponto).OrganizacaoId = organizacaoId;
        return ponto;
    }

    private sealed class FontePublicaFalsa : IFontePublicaDePontos
    {
        private List<ConsultaDeFontePublica> _consultas = [];
        private ResultadoDaFontePublica _resposta = new([], Consultada: false, Disponivel: false, Aviso: null);

        public IReadOnlyList<ConsultaDeFontePublica> Consultas => _consultas;

        public void Configurar(ResultadoDaFontePublica resposta)
        {
            _resposta = resposta;
            _consultas = [];
        }

        public Task<ResultadoDaFontePublica> BuscarAsync(
            ConsultaDeFontePublica consulta,
            CancellationToken cancellationToken = default)
        {
            _consultas.Add(consulta);
            return Task.FromResult(_resposta);
        }
    }
}
