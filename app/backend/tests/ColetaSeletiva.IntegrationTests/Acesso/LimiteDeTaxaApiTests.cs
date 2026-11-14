using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class LimiteDeTaxaApiTests
{
    private const string NomeBanco = "ColetaSeletivaLimiteDeTaxaApiTests";

    private static string _connectionString = null!;

    [ClassInitialize]
    public static async Task Inicializar(TestContext _)
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using var provedor = CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        await contexto.Database.EnsureDeletedAsync();
        await contexto.Database.MigrateAsync();
    }

    [ClassCleanup]
    public static async Task Limpar()
    {
        await using var provedor = CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        await contexto.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task Login_acima_do_limite_responde_429_generico_sem_cookie()
    {
        await using var fabrica = CriarFabricaComLimite("Login", limite: 2);
        using var cliente = fabrica.CreateClient();

        var primeira = await EntrarAsync(cliente, "inexistente.1@example.org", "SenhaErrada!111");
        var segunda = await EntrarAsync(cliente, "inexistente.2@example.org", "SenhaErrada!222");
        var terceira = await EntrarAsync(cliente, "inexistente.3@example.org", "SenhaErrada!333");

        Assert.AreEqual(HttpStatusCode.Unauthorized, primeira.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, segunda.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        await VerificarProblemaDeLimiteAsync(terceira);
    }

    [TestMethod]
    public async Task Refresh_acima_do_limite_responde_429_generico()
    {
        await using var fabrica = CriarFabricaComLimite("Refresh", limite: 2);
        using var cliente = fabrica.CreateClient();

        var primeira = await EnviarRefreshAsync(cliente);
        var segunda = await EnviarRefreshAsync(cliente);
        var terceira = await EnviarRefreshAsync(cliente);

        Assert.AreEqual(HttpStatusCode.Unauthorized, primeira.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, segunda.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        await VerificarProblemaDeLimiteAsync(terceira);
    }

    [TestMethod]
    public async Task Cadastro_acima_do_limite_responde_429_sem_revelar_dados()
    {
        await using var fabrica = CriarFabricaComLimite("Cadastro", limite: 2);
        using var cliente = fabrica.CreateClient();

        var primeira = await CadastrarCidadaoAsync(cliente, "cidadao.1@example.org", "(11) 90000-0001");
        var segunda = await CadastrarCidadaoAsync(cliente, "cidadao.2@example.org", "(11) 90000-0002");
        var terceira = await CadastrarCidadaoAsync(cliente, "cidadao.3@example.org", "(11) 90000-0003");

        Assert.AreEqual(HttpStatusCode.BadRequest, primeira.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, segunda.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, terceira.StatusCode);

        var corpo = await VerificarProblemaDeLimiteAsync(terceira);
        Assert.IsFalse(corpo.Contains("cidadao.3@example.org", StringComparison.Ordinal));
        Assert.IsFalse(corpo.Contains("90000-0003", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Limite_de_login_nao_bloqueia_a_rota_de_refresh()
    {
        await using var fabrica = CriarFabricaComLimite("Login", limite: 1);
        using var cliente = fabrica.CreateClient();

        var primeira = await EntrarAsync(cliente, "inexistente.1@example.org", "SenhaErrada!111");
        var segunda = await EntrarAsync(cliente, "inexistente.2@example.org", "SenhaErrada!222");
        var refresh = await EnviarRefreshAsync(cliente);

        Assert.AreEqual(HttpStatusCode.Unauthorized, primeira.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, segunda.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    private static ServiceProvider CriarProvedor()
    {
        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(_connectionString);

        return servicos.BuildServiceProvider();
    }

    private static ApiDeTeste CriarFabricaComLimite(string rota, int limite)
    {
        return new ApiDeTeste(
            _connectionString,
            new Dictionary<string, string?>
            {
                [$"LimitesDeTaxa:{rota}:PermitLimit"] =
                    limite.ToString(CultureInfo.InvariantCulture),
                [$"LimitesDeTaxa:{rota}:WindowSeconds"] = "60",
            });
    }

    private static async Task<HttpResponseMessage> EntrarAsync(
        HttpClient cliente,
        string identificador,
        string senha)
    {
        return await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador, senha });
    }

    private static async Task<HttpResponseMessage> CadastrarCidadaoAsync(
        HttpClient cliente,
        string email,
        string telefone)
    {
        return await cliente.PostAsJsonAsync(
            "/api/v1/auth/registro-cidadao",
            new
            {
                nome = "Cidadão de Teste",
                email,
                telefone,
                senha = "curta",
            });
    }

    private static async Task<HttpResponseMessage> EnviarRefreshAsync(HttpClient cliente)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        requisicao.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoDeOrigem,
            "http://localhost");
        requisicao.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoSolicitadoCom,
            ConstantesDeSessao.ValorSolicitadoCom);

        return await cliente.SendAsync(requisicao);
    }

    private static async Task<string> VerificarProblemaDeLimiteAsync(HttpResponseMessage resposta)
    {
        Assert.AreEqual("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue(
            resposta.Headers.TryGetValues("Cache-Control", out var cacheControl)
            && cacheControl.Any(valor =>
                valor.Contains("no-store", StringComparison.OrdinalIgnoreCase)));
        Assert.IsNotNull(resposta.Headers.RetryAfter);

        var corpo = await resposta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;

        Assert.AreEqual(429, raiz.GetProperty("status").GetInt32());
        Assert.AreEqual("limite_de_taxa", raiz.GetProperty("code").GetString());
        Assert.AreEqual(
            "Muitas tentativas. Tente novamente mais tarde.",
            raiz.GetProperty("title").GetString());
        Assert.IsFalse(string.IsNullOrWhiteSpace(raiz.GetProperty("traceId").GetString()));
        Assert.IsFalse(raiz.TryGetProperty("detail", out _));
        Assert.IsFalse(raiz.TryGetProperty("errors", out _));

        return corpo;
    }
}
