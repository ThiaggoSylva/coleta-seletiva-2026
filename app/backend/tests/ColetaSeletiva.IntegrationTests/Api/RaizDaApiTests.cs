using System.Net;
using System.Text.Json;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.IntegrationTests.Api;

/// <summary>
/// Garante que a raiz da API deixe de responder 404 e que a documentação
/// interativa (Swagger UI) fique disponível na mesma origem.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RaizDaApiTests
{
    private const string NomeBanco = "ColetaSeletivaRaizDaApiTests";

    private static string _connectionString = null!;
    private static ApiDeTeste _fabrica = null!;

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
    public async Task Raiz_da_api_responde_json_informativo_para_clientes()
    {
        using var cliente = _fabrica.CreateClient();
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/v1");
        requisicao.Headers.Accept.ParseAdd("application/json");

        var resposta = await cliente.SendAsync(requisicao);
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, corpo);
        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;

        Assert.AreEqual("ok", raiz.GetProperty("status").GetString());
        Assert.AreEqual("/swagger", raiz.GetProperty("documentacao").GetString());
        Assert.AreEqual("/openapi/v1.json", raiz.GetProperty("openapi").GetString());
        Assert.AreEqual("/health", raiz.GetProperty("health").GetString());
    }

    [TestMethod]
    public async Task Raiz_da_api_redireciona_navegador_para_a_documentacao()
    {
        using var cliente = _fabrica.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/v1");
        requisicao.Headers.Accept.ParseAdd("text/html");

        var resposta = await cliente.SendAsync(requisicao);

        Assert.AreEqual(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.AreEqual("/swagger", resposta.Headers.Location?.OriginalString);
    }

    [TestMethod]
    public async Task Swagger_ui_e_especificacao_openapi_ficam_disponiveis()
    {
        using var cliente = _fabrica.CreateClient();

        var interfaceSwagger = await cliente.GetAsync("/swagger/index.html");
        var html = await interfaceSwagger.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, interfaceSwagger.StatusCode, html);
        StringAssert.Contains(html, "swagger-ui");

        var especificacao = await cliente.GetAsync("/openapi/v1.json");
        var json = await especificacao.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, especificacao.StatusCode, json);
        using var documento = JsonDocument.Parse(json);
        StringAssert.StartsWith(
            documento.RootElement.GetProperty("openapi").GetString(),
            "3.");
    }

    private static async Task InicializarAsync()
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using (var contexto = CriarContexto())
        {
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        _fabrica = new ApiDeTeste(_connectionString);
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
}
