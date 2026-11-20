using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ColetaSeletiva.IntegrationTests.Roadmap;

/// <summary>
/// Contrato de sinalização do roadmap: índice público, 501 estruturado, 404 e autorização
/// por papel (RF-24–RF-27; CA-13–CA-15; DT-36–DT-39). Não usa dados pessoais.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class EmBreveApiTests
{
    private const string NomeBanco = "ColetaSeletivaEmBreveApiTests";
    private const string ModuloDesconhecido = "modulo-fora-do-catalogo";

    private static readonly (string Id, string Fase)[] Modulos =
    [
        ("preferencias-email", "F7"),
        ("extrato-pontos", "F8"),
        ("rotas-coletor", "F9"),
        ("transparencia-impacto", "F10"),
    ];

    private static ApiDeTeste _fabrica = null!;
    private static string _conexao = null!;

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
    public async Task Indice_publico_devolve_quatro_modulos_sem_dados_pessoais()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/api/v1/em-breve");

        AssertStatus(HttpStatusCode.OK, resposta);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var modulos = corpo.RootElement;

        Assert.AreEqual(Modulos.Length, modulos.GetArrayLength());
        foreach (var esperado in Modulos)
        {
            var modulo = modulos.EnumerateArray()
                .Single(item => item.GetProperty("id").GetString() == esperado.Id);
            Assert.AreEqual(esperado.Fase, modulo.GetProperty("fase").GetString());
            Assert.AreEqual("em_breve", modulo.GetProperty("situacao").GetString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(modulo.GetProperty("titulo").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(modulo.GetProperty("descricao").GetString()));
            Assert.IsTrue(modulo.GetProperty("rota").GetString()!.StartsWith("/em-breve/", StringComparison.Ordinal));

            var propriedades = modulo.EnumerateObject().Select(propriedade => propriedade.Name).ToArray();
            CollectionAssert.AreEquivalent(
                new[] { "id", "titulo", "descricao", "fase", "rota", "situacao" },
                propriedades);
        }
    }

    [TestMethod]
    public async Task Cada_modulo_responde_501_com_metadados_e_sem_dado_ficticio()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = CriarToken(Papeis.Cidadao, comOrganizacao: false);
        var tokenAdmin = CriarToken(Papeis.Administrador, comOrganizacao: true);

        var tokens = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["preferencias-email"] = tokenCidadao,
            ["extrato-pontos"] = tokenCidadao,
            ["rotas-coletor"] = tokenAdmin,
            ["transparencia-impacto"] = null,
        };

        foreach (var (id, fase) in Modulos)
        {
            var resposta = await EnviarAsync(cliente, $"/api/v1/em-breve/{id}", tokens[id]);
            var texto = await resposta.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.NotImplemented, resposta.StatusCode, texto);
            StringAssert.Contains(resposta.Content.Headers.ContentType?.MediaType ?? string.Empty, "problem");
            using var corpo = JsonDocument.Parse(texto);
            var problema = corpo.RootElement;

            Assert.AreEqual(501, problema.GetProperty("status").GetInt32());
            Assert.AreEqual("Recurso em breve.", problema.GetProperty("title").GetString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(problema.GetProperty("detail").GetString()));
            Assert.AreEqual("em_breve", problema.GetProperty("code").GetString());
            Assert.AreEqual(id, problema.GetProperty("modulo").GetString());
            Assert.AreEqual(fase, problema.GetProperty("fase").GetString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(problema.GetProperty("traceId").GetString()));
        }
    }

    [TestMethod]
    public async Task Modulo_desconhecido_responde_404_sem_ecoar_a_entrada()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync($"/api/v1/em-breve/{ModuloDesconhecido}");
        var texto = await resposta.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode, texto);
        using var corpo = JsonDocument.Parse(texto);
        Assert.AreEqual("nao_encontrado", corpo.RootElement.GetProperty("code").GetString());
        Assert.IsFalse(corpo.RootElement.TryGetProperty("modulo", out _));
        Assert.IsFalse(texto.Contains(ModuloDesconhecido, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Detalhes_protegidos_exigem_sessao_e_papel()
    {
        using var cliente = _fabrica.CreateClient();
        var tokenCidadao = CriarToken(Papeis.Cidadao, comOrganizacao: false);
        var tokenAdmin = CriarToken(Papeis.Administrador, comOrganizacao: true);
        var tokenOperador = CriarToken(Papeis.Operador, comOrganizacao: true);

        foreach (var modulo in new[] { "preferencias-email", "extrato-pontos", "rotas-coletor" })
        {
            var anonimo = await EnviarAsync(cliente, $"/api/v1/em-breve/{modulo}", null);
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        }

        var transparenciaAnonima = await EnviarAsync(cliente, "/api/v1/em-breve/transparencia-impacto", null);
        Assert.AreEqual(HttpStatusCode.NotImplemented, transparenciaAnonima.StatusCode);

        await AssertarPapelAsync(cliente, tokenCidadao, "preferencias-email", HttpStatusCode.NotImplemented);
        await AssertarPapelAsync(cliente, tokenCidadao, "extrato-pontos", HttpStatusCode.NotImplemented);
        await AssertarPapelAsync(cliente, tokenCidadao, "rotas-coletor", HttpStatusCode.Forbidden);
        await AssertarPapelAsync(cliente, tokenAdmin, "rotas-coletor", HttpStatusCode.NotImplemented);
        await AssertarPapelAsync(cliente, tokenOperador, "rotas-coletor", HttpStatusCode.NotImplemented);
        await AssertarPapelAsync(cliente, tokenAdmin, "preferencias-email", HttpStatusCode.Forbidden);
        await AssertarPapelAsync(cliente, tokenAdmin, "extrato-pontos", HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Contrato_openapi_documenta_indice_e_respostas_501()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/openapi/v1.json");

        AssertStatus(HttpStatusCode.OK, resposta);
        var contrato = await resposta.Content.ReadAsStringAsync();

        StringAssert.Contains(contrato, "/api/v1/em-breve");
        StringAssert.Contains(contrato, "/api/v1/em-breve/{modulo}");
        foreach (var (id, _) in Modulos)
        {
            StringAssert.Contains(contrato, $"/api/v1/em-breve/{id}");
        }

        Assert.IsTrue(contrato.Contains("\"501\"", StringComparison.Ordinal));
        Assert.IsTrue(contrato.Contains("\"404\"", StringComparison.Ordinal));
        Assert.IsTrue(contrato.Contains("\"200\"", StringComparison.Ordinal));
    }

    private static async Task AssertarPapelAsync(
        HttpClient cliente,
        string token,
        string modulo,
        HttpStatusCode esperado)
    {
        var resposta = await EnviarAsync(cliente, $"/api/v1/em-breve/{modulo}", token);
        var texto = await resposta.Content.ReadAsStringAsync();

        Assert.AreEqual(esperado, resposta.StatusCode, texto);

        if (esperado == HttpStatusCode.Forbidden)
        {
            Assert.IsFalse(texto.Contains("\"fase\"", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(texto.Contains("\"modulo\"", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente,
        string caminho,
        string? token)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
        if (token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await cliente.SendAsync(requisicao);
    }

    private static string CriarToken(string papel, bool comOrganizacao)
    {
        var agora = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["sub"] = Guid.NewGuid().ToString(),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["role"] = papel,
        };

        if (comOrganizacao)
        {
            claims[ClaimsColetaSeletiva.OrganizacaoId] = Guid.NewGuid().ToString();
        }

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = ApiDeTeste.Emissor,
            Audience = ApiDeTeste.Publico,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = agora.AddMinutes(15).UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiDeTeste.Chave)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descritor);
    }

    private static void AssertStatus(HttpStatusCode esperado, HttpResponseMessage resposta)
    {
        var corpo = resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.AreEqual(esperado, resposta.StatusCode, corpo);
    }

    private static ColetaSeletivaDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<ColetaSeletivaDbContext>()
            .UseSqlServer(_conexao, sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new ColetaSeletivaDbContext(opcoes, new TenantContext());
    }

    private static async Task InicializarAsync()
    {
        _conexao = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using (var contexto = CriarContexto())
        {
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        _fabrica = new ApiDeTeste(
            _conexao,
            new Dictionary<string, string?>
            {
                ["LimitesDeTaxa:ConsultaPublica:PermitLimit"] = "1000",
            });
        _ = _fabrica.Services;
    }

    private static async Task LimparAsync()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.EnsureDeletedAsync();
        _fabrica.Dispose();
    }
}
