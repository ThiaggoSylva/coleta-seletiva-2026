using ColetaSeletiva.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace ColetaSeletiva.IntegrationTests.Configuration;

[TestClass]
public sealed class StartupConfigurationTests
{
    private const string ChaveValida = "chave-de-teste-com-pelo-menos-32-bytes!!";

    [TestMethod]
    public void Validate_aceita_configuracao_completa()
    {
        StartupConfiguration.Validate(CriarConfiguracao());
    }

    [TestMethod]
    public void Validate_rejeita_connection_string_ausente()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.Validate(CriarConfiguracao(connectionString: " ")));

        StringAssert.Contains(excecao.Message, "ConnectionStrings__ColetaSeletiva");
    }

    [TestMethod]
    public void Validate_rejeita_chave_jwt_ausente()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.Validate(CriarConfiguracao(signingKey: null)));

        StringAssert.Contains(excecao.Message, "Jwt:SigningKey");
    }

    [TestMethod]
    public void Validate_rejeita_chave_jwt_com_menos_de_32_bytes()
    {
        var chaveCurta = new string('x', StartupConfiguration.MinimumJwtSigningKeyBytes - 1);

        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.Validate(CriarConfiguracao(signingKey: chaveCurta)));

        StringAssert.Contains(excecao.Message, "32 bytes");
        Assert.IsFalse(
            excecao.Message.Contains(chaveCurta, StringComparison.Ordinal),
            "A mensagem de erro não pode expor a chave informada.");
    }

    [TestMethod]
    public void Validate_aceita_chave_jwt_com_32_bytes()
    {
        var chaveNoLimite = new string('x', StartupConfiguration.MinimumJwtSigningKeyBytes);

        StartupConfiguration.Validate(CriarConfiguracao(signingKey: chaveNoLimite));
    }

    [TestMethod]
    public void Validate_rejeita_issuer_ausente()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.Validate(CriarConfiguracao(issuer: " ")));

        StringAssert.Contains(excecao.Message, "Jwt:Issuer");
    }

    [TestMethod]
    public void Validate_rejeita_audience_ausente()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.Validate(CriarConfiguracao(audience: null)));

        StringAssert.Contains(excecao.Message, "Jwt:Audience");
    }

    private static IConfiguration CriarConfiguracao(
        string? connectionString = "Server=localhost;Database=teste;User Id=teste;",
        string? issuer = "coleta-seletiva-api",
        string? audience = "coleta-seletiva-web",
        string? signingKey = ChaveValida)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:ColetaSeletiva"] = connectionString,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:SigningKey"] = signingKey,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }
}
