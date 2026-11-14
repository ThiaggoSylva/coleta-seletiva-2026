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

    [TestMethod]
    public void CriarJwtSettings_devolve_os_valores_validados()
    {
        var configuracao = StartupConfiguration.CriarJwtSettings(CriarConfiguracao());

        Assert.AreEqual("coleta-seletiva-api", configuracao.Issuer);
        Assert.AreEqual("coleta-seletiva-web", configuracao.Audience);
        Assert.AreEqual(ChaveValida, configuracao.SigningKey);
    }

    [TestMethod]
    public void CriarConfiguracaoDeSessao_usa_padroes_de_15_minutos_e_7_dias()
    {
        var configuracao = StartupConfiguration.CriarConfiguracaoDeSessao(CriarConfiguracao());

        Assert.AreEqual(TimeSpan.FromMinutes(15), configuracao.ValidadeDoTokenDeAcesso);
        Assert.AreEqual(TimeSpan.FromDays(7), configuracao.ValidadeDoRefresh);
    }

    [TestMethod]
    public void CriarConfiguracaoDeSessao_rejeita_duracao_nao_positiva()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.CriarConfiguracaoDeSessao(
                CriarConfiguracao(accessTokenMinutes: "0")));

        StringAssert.Contains(excecao.Message, "Sessao:AccessTokenMinutos");
    }

    [TestMethod]
    public void CriarConfiguracaoDeSessao_rejeita_refresh_menor_que_o_token()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.CriarConfiguracaoDeSessao(
                CriarConfiguracao(accessTokenMinutes: "1440", refreshTokenDias: "1")));

        StringAssert.Contains(excecao.Message, "refresh");
    }

    [TestMethod]
    public void Validate_rejeita_origem_permitida_invalida()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.Validate(
                CriarConfiguracao(origemPermitida: "localhost:4200")));

        StringAssert.Contains(excecao.Message, "Sessao:OrigemPermitida");
    }

    [TestMethod]
    public void CriarConfiguracaoDeLimitesDeTaxa_usa_padroes_por_rota()
    {
        var limites = StartupConfiguration.CriarConfiguracaoDeLimitesDeTaxa(CriarConfiguracao());

        Assert.AreEqual(5, limites.Login.PermitLimit);
        Assert.AreEqual(30, limites.Refresh.PermitLimit);
        Assert.AreEqual(5, limites.Cadastro.PermitLimit);
        Assert.AreEqual(60, limites.Login.WindowSeconds);
        Assert.AreEqual(60, limites.Refresh.WindowSeconds);
        Assert.AreEqual(60, limites.Cadastro.WindowSeconds);
    }

    [TestMethod]
    public void CriarConfiguracaoDeLimitesDeTaxa_rejeita_limite_nao_positivo()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.CriarConfiguracaoDeLimitesDeTaxa(
                CriarConfiguracao(limiteDeLogin: "0")));

        StringAssert.Contains(excecao.Message, "LimitesDeTaxa:Login:PermitLimit");
    }

    [TestMethod]
    public void CriarConfiguracaoDeLimitesDeTaxa_rejeita_janela_nao_positiva()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfiguration.CriarConfiguracaoDeLimitesDeTaxa(
                CriarConfiguracao(janelaDeLogin: "0")));

        StringAssert.Contains(excecao.Message, "LimitesDeTaxa:Login:WindowSeconds");
    }

    private static IConfiguration CriarConfiguracao(
        string? connectionString = "Server=localhost;Database=teste;User Id=teste;",
        string? issuer = "coleta-seletiva-api",
        string? audience = "coleta-seletiva-web",
        string? signingKey = ChaveValida,
        string? accessTokenMinutes = null,
        string? refreshTokenDias = null,
        string? origemPermitida = null,
        string? limiteDeLogin = null,
        string? janelaDeLogin = null)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:ColetaSeletiva"] = connectionString,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:SigningKey"] = signingKey,
            ["Sessao:AccessTokenMinutos"] = accessTokenMinutes,
            ["Sessao:RefreshTokenDias"] = refreshTokenDias,
            ["Sessao:OrigemPermitida"] = origemPermitida,
            ["LimitesDeTaxa:Login:PermitLimit"] = limiteDeLogin,
            ["LimitesDeTaxa:Login:WindowSeconds"] = janelaDeLogin,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }
}
