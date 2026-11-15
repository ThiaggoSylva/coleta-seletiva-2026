using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Publico;

/// <summary>
/// Regressão do erro 500 que a busca pública devolvia ao receber qualquer filtro
/// (município, tipo de resíduo ou proximidade), por falha de tradução do LINQ.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ConsultaPublicaPontosApiTests
{
    private const string NomeBanco = "ColetaSeletivaConsultaPublicaApiTests";
    private const int MunicipioGoiania = 5208707;
    private const int MunicipioAparecida = 5201405;

    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static string _connectionString = null!;
    private static ApiDeTeste _fabrica = null!;
    private static Guid _tipoPapelId;

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
    public async Task Busca_sem_filtros_lista_apenas_pontos_ativos_de_organizacoes_publicadas()
    {
        using var cliente = _fabrica.CreateClient();

        var (status, total, nomes) = await BuscarAsync(cliente, new { });

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(3, total);
        CollectionAssert.AreEquivalent(
            new[] { "Ponto Alfa Centro", "Ponto Alfa Sul", "Ponto Beta Aparecida" },
            nomes.ToArray());
    }

    [TestMethod]
    public async Task Filtro_por_municipio_retorna_somente_os_pontos_do_municipio()
    {
        using var cliente = _fabrica.CreateClient();

        var (status, total, nomes) = await BuscarAsync(cliente, new { municipioId = MunicipioGoiania });

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(2, total);
        CollectionAssert.AreEquivalent(
            new[] { "Ponto Alfa Centro", "Ponto Alfa Sul" },
            nomes.ToArray());
    }

    [TestMethod]
    public async Task Filtro_por_tipo_de_residuo_retorna_somente_pontos_que_o_aceitam()
    {
        using var cliente = _fabrica.CreateClient();

        var (status, total, nomes) = await BuscarAsync(cliente, new { tipoResiduoId = _tipoPapelId });

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(2, total);
        CollectionAssert.AreEquivalent(
            new[] { "Ponto Alfa Sul", "Ponto Beta Aparecida" },
            nomes.ToArray());
    }

    [TestMethod]
    public async Task Filtro_por_proximidade_retorna_somente_pontos_dentro_do_raio()
    {
        using var cliente = _fabrica.CreateClient();

        var (status, total, nomes) = await BuscarAsync(cliente, new
        {
            latitude = -16.6869,
            longitude = -49.2648,
            raioKm = 5,
        });

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(1, total);
        CollectionAssert.AreEquivalent(new[] { "Ponto Alfa Centro" }, nomes.ToArray());
    }

    [TestMethod]
    public async Task Filtros_combinados_de_municipio_tipo_e_raio_funcionam_juntos()
    {
        using var cliente = _fabrica.CreateClient();

        var (status, total, nomes) = await BuscarAsync(cliente, new
        {
            municipioId = MunicipioGoiania,
            tipoResiduoId = _tipoPapelId,
            latitude = -16.6869,
            longitude = -49.2648,
            raioKm = 10,
        });

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(1, total);
        CollectionAssert.AreEquivalent(new[] { "Ponto Alfa Sul" }, nomes.ToArray());
    }

    [TestMethod]
    public async Task Filtro_por_municipio_sem_pontos_publicados_retorna_lista_vazia()
    {
        using var cliente = _fabrica.CreateClient();

        var (status, total, nomes) = await BuscarAsync(cliente, new { municipioId = 5200050 });

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(0, total);
        Assert.IsEmpty(nomes);
    }

    private static async Task<(HttpStatusCode Status, int Total, IReadOnlyList<string> Nomes)> BuscarAsync(
        HttpClient cliente,
        object filtros)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/v1/publico/pontos/busca", filtros);
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.IsTrue(
            resposta.IsSuccessStatusCode,
            $"A busca pública respondeu {(int)resposta.StatusCode}: {corpo}");

        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;
        var itens = raiz.GetProperty("itens").EnumerateArray().ToList();

        return (
            resposta.StatusCode,
            raiz.GetProperty("total").GetInt32(),
            itens.Select(item => item.GetProperty("nome").GetString() ?? string.Empty).ToList());
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
            // O catálogo é semeado pela migration; os tipos usados precisam existir em
            // OrganizacaoTipo para satisfazer a FK composta de PontoTipo.
            var tipos = await contexto.TiposResiduo
                .AsNoTracking()
                .OrderBy(tipo => tipo.Codigo)
                .Take(2)
                .ToListAsync();

            Assert.AreEqual(2, tipos.Count, "O catálogo sembrado precisa ter ao menos dois tipos.");

            var tipoMetalId = tipos[0].Id;
            _tipoPapelId = tipos[1].Id;

            var cooperativaAlfa = CriarOrganizacao("Cooperativa Alfa", TipoOrganizacao.Cooperativa, publicada: true);
            var associacaoBeta = CriarOrganizacao("Associação Beta", TipoOrganizacao.Associacao, publicada: true);
            var ongGama = CriarOrganizacao("ONG Gama", TipoOrganizacao.Cooperativa, publicada: false);

            var vinculos = new List<OrganizacaoMunicipio>
            {
                CriarVinculoDeMunicipio(cooperativaAlfa.Id, MunicipioGoiania),
                CriarVinculoDeMunicipio(associacaoBeta.Id, MunicipioAparecida),
                CriarVinculoDeMunicipio(ongGama.Id, MunicipioGoiania),
            };

            var tiposHabilitados = new List<OrganizacaoTipo>
            {
                CriarVinculoDeTipo(cooperativaAlfa.Id, tipoMetalId),
                CriarVinculoDeTipo(cooperativaAlfa.Id, _tipoPapelId),
                CriarVinculoDeTipo(associacaoBeta.Id, tipoMetalId),
                CriarVinculoDeTipo(associacaoBeta.Id, _tipoPapelId),
                CriarVinculoDeTipo(ongGama.Id, tipoMetalId),
            };

            var pontos = new List<PontoDeColeta>
            {
                CriarPonto(
                    cooperativaAlfa.Id,
                    "Ponto Alfa Centro",
                    MunicipioGoiania,
                    -16.6869,
                    -49.2648,
                    StatusPonto.Ativo,
                    tipoMetalId),
                CriarPonto(
                    cooperativaAlfa.Id,
                    "Ponto Alfa Sul",
                    MunicipioGoiania,
                    -16.7500,
                    -49.3000,
                    StatusPonto.Ativo,
                    _tipoPapelId),
                CriarPonto(
                    cooperativaAlfa.Id,
                    "Ponto Alfa Inativo",
                    MunicipioGoiania,
                    -16.6900,
                    -49.2700,
                    StatusPonto.Inativo,
                    tipoMetalId),
                CriarPonto(
                    associacaoBeta.Id,
                    "Ponto Beta Aparecida",
                    MunicipioAparecida,
                    -16.8230,
                    -49.2200,
                    StatusPonto.Ativo,
                    tipoMetalId,
                    _tipoPapelId),
                CriarPonto(
                    ongGama.Id,
                    "Ponto Gama Nao Publicado",
                    MunicipioGoiania,
                    -16.6800,
                    -49.2600,
                    StatusPonto.Ativo,
                    tipoMetalId),
            };

            contexto.Organizacoes.AddRange(cooperativaAlfa, associacaoBeta, ongGama);
            contexto.OrganizacaoMunicipios.AddRange(vinculos);
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

    private static Organizacao CriarOrganizacao(string nome, TipoOrganizacao tipo, bool publicada)
    {
        var organizacao = Organizacao.Criar(nome, tipo, Agora);
        organizacao.DefinirPublicacao(publicada, Agora);
        return organizacao;
    }

    private static OrganizacaoMunicipio CriarVinculoDeMunicipio(Guid organizacaoId, int municipioId)
    {
        var vinculo = OrganizacaoMunicipio.Criar(municipioId, Agora);
        ((IEntidadeDaOrganizacao)vinculo).OrganizacaoId = organizacaoId;
        return vinculo;
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
        int municipioId,
        double latitude,
        double longitude,
        StatusPonto status,
        params Guid[] tiposResiduoIds)
    {
        var ponto = PontoDeColeta.Criar(
            nome,
            municipioId,
            "GO",
            "Centro",
            "Rua de Teste",
            "100",
            null,
            "74000000",
            latitude,
            longitude,
            null,
            status,
            [new HorarioDeFuncionamento(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            tiposResiduoIds,
            Agora);

        ((IEntidadeDaOrganizacao)ponto).OrganizacaoId = organizacaoId;
        return ponto;
    }
}
