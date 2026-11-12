using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class SessaoApiTests
{
    private const string NomeBanco = "ColetaSeletivaSessaoApiTests";
    private const string Senha = "SenhaForte!123";
    private const string NovaSenha = "NovaSenhaForte!456";

    private static ApiDeTeste _fabrica = null!;
    private static ServiceProvider _provedor = null!;
    private static string _adminUsuario = null!;
    private static string _adminEmail = null!;
    private static string _cidadaoEmail = null!;
    private static Guid _organizacaoId;

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
    public async Task Login_com_sucesso_devolve_token_de_15_minutos_e_cookie_seguro()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await EntrarAsync(cliente, _adminUsuario, Senha);

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.IsTrue(
            resposta.Headers.TryGetValues("Cache-Control", out var cacheControl)
            && cacheControl.Any(valor => valor.Contains("no-store", StringComparison.OrdinalIgnoreCase)));

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var token = corpo.RootElement.GetProperty("tokenDeAcesso").GetString();
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        Assert.AreEqual(
            "Administrador",
            corpo.RootElement.GetProperty("usuario").GetProperty("papel").GetString());
        Assert.AreEqual(
            _adminEmail,
            corpo.RootElement.GetProperty("usuario").GetProperty("email").GetString());

        var expiracao = LerExpiracao(token!);
        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.IsTrue(expiracao > agora + (int)TimeSpan.FromMinutes(14).TotalSeconds);
        Assert.IsTrue(expiracao <= agora + (int)TimeSpan.FromMinutes(16).TotalSeconds);

        var cookie = ObterCookieDeRefresh(resposta);
        Assert.IsNotNull(cookie);
        Assert.IsTrue(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(cookie.Contains($"path={ConstantesDeSessao.CaminhoDoCookie}", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(ExtrairRefresh(cookie));
    }

    [TestMethod]
    public async Task Login_invalido_usa_mesma_mensagem_generica_e_nao_cria_cookie()
    {
        using var cliente = _fabrica.CreateClient();

        var senhaIncorreta = await EntrarAsync(cliente, _adminUsuario, "SenhaErrada!999");
        var inexistente = await EntrarAsync(cliente, "nao.existe", Senha);

        Assert.AreEqual(HttpStatusCode.Unauthorized, senhaIncorreta.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, inexistente.StatusCode);

        var tituloSenhaIncorreta = await LerTituloAsync(senhaIncorreta);
        var tituloInexistente = await LerTituloAsync(inexistente);
        Assert.AreEqual(tituloSenhaIncorreta, tituloInexistente);
        Assert.IsNull(ObterCookieDeRefresh(senhaIncorreta));
        Assert.IsNull(ObterCookieDeRefresh(inexistente));
    }

    [TestMethod]
    public async Task Rota_protegida_exige_token_e_nega_perfil_de_cidadao_para_administrador()
    {
        using var cliente = _fabrica.CreateClient();

        var semToken = await cliente.GetAsync("/api/v1/conta");
        Assert.AreEqual(HttpStatusCode.Unauthorized, semToken.StatusCode);

        var loginCidadao = await EntrarAsync(cliente, _cidadaoEmail, Senha);
        var tokenCidadao = await LerTokenAsync(loginCidadao);
        var perfil = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/api/v1/conta", tokenCidadao));
        Assert.AreEqual(HttpStatusCode.OK, perfil.StatusCode);
        using var perfilJson = JsonDocument.Parse(await perfil.Content.ReadAsStringAsync());
        Assert.AreEqual(
            _cidadaoEmail,
            perfilJson.RootElement.GetProperty("email").GetString());

        var loginAdmin = await EntrarAsync(cliente, _adminUsuario, Senha);
        var tokenAdmin = await LerTokenAsync(loginAdmin);
        var negado = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/api/v1/conta", tokenAdmin));
        Assert.AreEqual(HttpStatusCode.Forbidden, negado.StatusCode);
        using var negadoJson = JsonDocument.Parse(await negado.Content.ReadAsStringAsync());
        Assert.AreEqual(
            "acesso_negado",
            negadoJson.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Refresh_exige_mesma_origem_e_cabecalho_customizado()
    {
        using var cliente = _fabrica.CreateClient();
        var login = await EntrarAsync(cliente, _adminUsuario, Senha);
        var refresh = ExtrairRefresh(ObterCookieDeRefresh(login));

        var semCabecalhos = await EnviarRefreshAsync(cliente, refresh, origem: null, solicitadoCom: null);
        var origemEstranha = await EnviarRefreshAsync(
            cliente,
            refresh,
            origem: "https://malicioso.example",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);
        var semCabecalhoCustomizado = await EnviarRefreshAsync(
            cliente,
            refresh,
            origem: "http://localhost",
            solicitadoCom: null);
        var valido = await EnviarRefreshAsync(
            cliente,
            refresh,
            origem: "http://localhost",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);

        Assert.AreEqual(HttpStatusCode.Forbidden, semCabecalhos.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, origemEstranha.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, semCabecalhoCustomizado.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, valido.StatusCode);

        var novoCookie = ObterCookieDeRefresh(valido);
        Assert.IsNotNull(novoCookie);
        Assert.AreNotEqual(refresh, ExtrairRefresh(novoCookie));
    }

    [TestMethod]
    public async Task Reuso_de_refresh_revoga_a_familia_e_limpa_o_cookie()
    {
        using var cliente = _fabrica.CreateClient();
        var login = await EntrarAsync(cliente, _adminUsuario, Senha);
        var refreshOriginal = ExtrairRefresh(ObterCookieDeRefresh(login));

        var rotacao = await EnviarRefreshAsync(
            cliente,
            refreshOriginal,
            origem: "http://localhost",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);
        Assert.AreEqual(HttpStatusCode.OK, rotacao.StatusCode);
        var refreshRotacionado = ExtrairRefresh(ObterCookieDeRefresh(rotacao));
        Assert.IsNotNull(refreshRotacionado);
        Assert.AreNotEqual(refreshOriginal, refreshRotacionado);

        var reuso = await EnviarRefreshAsync(
            cliente,
            refreshOriginal,
            origem: "http://localhost",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);
        Assert.AreEqual(HttpStatusCode.Unauthorized, reuso.StatusCode);
        Assert.IsNull(ExtrairRefresh(ObterCookieDeRefresh(reuso)));

        var aposReuso = await EnviarRefreshAsync(
            cliente,
            refreshRotacionado,
            origem: "http://localhost",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);
        Assert.AreEqual(HttpStatusCode.Unauthorized, aposReuso.StatusCode);
    }

    [TestMethod]
    public async Task Logout_revoga_a_familia_e_limpa_o_cookie()
    {
        using var cliente = _fabrica.CreateClient();
        var login = await EntrarAsync(cliente, _adminUsuario, Senha);
        var refresh = ExtrairRefresh(ObterCookieDeRefresh(login));

        var requisicao = CriarRequisicaoDeCookie(HttpMethod.Post, "/api/v1/auth/logout", refresh);
        requisicao.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoDeOrigem,
            "http://localhost");
        requisicao.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoSolicitadoCom,
            ConstantesDeSessao.ValorSolicitadoCom);
        var logout = await cliente.SendAsync(requisicao);

        Assert.AreEqual(HttpStatusCode.NoContent, logout.StatusCode);
        var cookieDeLimpeza = ObterCookieDeRefresh(logout);
        Assert.IsNotNull(cookieDeLimpeza);
        Assert.IsNull(ExtrairRefresh(cookieDeLimpeza));
        Assert.IsTrue(
            cookieDeLimpeza.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

        var renovacao = await EnviarRefreshAsync(
            cliente,
            refresh,
            origem: "http://localhost",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);
        Assert.AreEqual(HttpStatusCode.Unauthorized, renovacao.StatusCode);
    }

    [TestMethod]
    public async Task Primeiro_acesso_bloqueia_rotas_ate_a_troca_de_senha()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var operador = await CriarOperadorPendenteAsync(sufixo);
        using var cliente = _fabrica.CreateClient();

        var login = await EntrarAsync(cliente, operador, Senha);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);

        using var corpoLogin = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var usuario = corpoLogin.RootElement.GetProperty("usuario");
        Assert.IsTrue(usuario.GetProperty("trocaSenhaPendente").GetBoolean());
        var token = corpoLogin.RootElement.GetProperty("tokenDeAcesso").GetString()!;
        var refresh = ExtrairRefresh(ObterCookieDeRefresh(login));
        Assert.IsNotNull(refresh);

        var contaAtual = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/api/v1/auth/me", token));
        Assert.AreEqual(HttpStatusCode.OK, contaAtual.StatusCode);
        using (var corpoConta = JsonDocument.Parse(await contaAtual.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(corpoConta.RootElement.GetProperty("trocaSenhaPendente").GetBoolean());
        }

        var rotaBloqueada = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/health", token));
        Assert.AreEqual(HttpStatusCode.Forbidden, rotaBloqueada.StatusCode);
        using (var corpoBloqueio = JsonDocument.Parse(await rotaBloqueada.Content.ReadAsStringAsync()))
        {
            Assert.AreEqual(
                "acesso_negado",
                corpoBloqueio.RootElement.GetProperty("code").GetString());
        }

        var alteracao = await EnviarAlteracaoDeSenhaAsync(cliente, token, Senha, NovaSenha);
        Assert.AreEqual(HttpStatusCode.NoContent, alteracao.StatusCode);

        var cookieDeLimpeza = ObterCookieDeRefresh(alteracao);
        Assert.IsNotNull(cookieDeLimpeza);
        Assert.IsNull(ExtrairRefresh(cookieDeLimpeza));

        var renovacao = await EnviarRefreshAsync(
            cliente,
            refresh,
            origem: "http://localhost",
            solicitadoCom: ConstantesDeSessao.ValorSolicitadoCom);
        Assert.AreEqual(HttpStatusCode.Unauthorized, renovacao.StatusCode);

        var novoLogin = await EntrarAsync(cliente, operador, NovaSenha);
        Assert.AreEqual(HttpStatusCode.OK, novoLogin.StatusCode);

        using var corpoNovoLogin = JsonDocument.Parse(await novoLogin.Content.ReadAsStringAsync());
        Assert.IsFalse(
            corpoNovoLogin.RootElement
                .GetProperty("usuario")
                .GetProperty("trocaSenhaPendente")
                .GetBoolean());
        var novoToken = corpoNovoLogin.RootElement.GetProperty("tokenDeAcesso").GetString()!;

        var liberado = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/health", novoToken));
        Assert.AreEqual(HttpStatusCode.OK, liberado.StatusCode);
    }

    [TestMethod]
    public async Task Troca_de_senha_invalida_mantem_a_pendencia_e_a_sessao()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var operador = await CriarOperadorPendenteAsync(sufixo);
        using var cliente = _fabrica.CreateClient();
        var login = await EntrarAsync(cliente, operador, Senha);
        var token = await LerTokenAsync(login);

        var senhaAtualIncorreta = await EnviarAlteracaoDeSenhaAsync(
            cliente,
            token,
            "SenhaErrada!999",
            NovaSenha);
        Assert.AreEqual(HttpStatusCode.BadRequest, senhaAtualIncorreta.StatusCode);
        var errosSenhaAtual = await LerErrosDeValidacaoAsync(senhaAtualIncorreta);
        Assert.IsTrue(errosSenhaAtual.ContainsKey("SenhaAtual"));
        StringAssert.Contains(errosSenhaAtual["SenhaAtual"].Single(), "incorreta");

        var senhaFraca = await EnviarAlteracaoDeSenhaAsync(cliente, token, Senha, "curta");
        Assert.AreEqual(HttpStatusCode.BadRequest, senhaFraca.StatusCode);
        var errosNovaSenha = await LerErrosDeValidacaoAsync(senhaFraca);
        Assert.IsTrue(errosNovaSenha.ContainsKey("NovaSenha"));
        Assert.IsTrue(
            errosNovaSenha["NovaSenha"].Any(erro => erro.Contains("12", StringComparison.Ordinal)));

        var contaAtual = await cliente.SendAsync(
            CriarRequisicaoAutenticada(HttpMethod.Get, "/api/v1/auth/me", token));
        Assert.AreEqual(HttpStatusCode.OK, contaAtual.StatusCode);
        using (var corpo = JsonDocument.Parse(await contaAtual.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(corpo.RootElement.GetProperty("trocaSenhaPendente").GetBoolean());
        }

        var loginComSenhaOriginal = await EntrarAsync(cliente, operador, Senha);
        Assert.AreEqual(HttpStatusCode.OK, loginComSenhaOriginal.StatusCode);
    }

    private static async Task<string> CriarOperadorPendenteAsync(string sufixo)
    {
        await using var escopo = _fabrica.Services.CreateAsyncScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var agora = DateTimeOffset.UtcNow;
        var usuario = $"operador.{sufixo}";
        var conta = new Usuario
        {
            UserName = usuario,
            Email = $"{usuario}@example.org",
            Nome = $"Operador {sufixo}",
            OrganizacaoId = _organizacaoId,
            Ativo = true,
            TrocaSenhaPendente = true,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };

        var criacao = await gerenciador.CreateAsync(conta, Senha);
        Assert.IsTrue(criacao.Succeeded, Descrever(criacao));

        var atribuicao = await gerenciador.AddToRoleAsync(conta, Papeis.Operador);
        Assert.IsTrue(atribuicao.Succeeded, Descrever(atribuicao));

        return usuario;
    }

    private static async Task<HttpResponseMessage> EnviarAlteracaoDeSenhaAsync(
        HttpClient cliente,
        string token,
        string senhaAtual,
        string novaSenha)
    {
        var requisicao = CriarRequisicaoAutenticada(
            HttpMethod.Post,
            "/api/v1/auth/alterar-senha",
            token);
        requisicao.Content = JsonContent.Create(new { senhaAtual, novaSenha });

        return await cliente.SendAsync(requisicao);
    }

    private static async Task<Dictionary<string, string[]>> LerErrosDeValidacaoAsync(
        HttpResponseMessage resposta)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var erros = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var propriedade in corpo.RootElement.GetProperty("errors").EnumerateObject())
        {
            erros[propriedade.Name] = propriedade.Value
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
        }

        return erros;
    }

    private static string Descrever(IdentityResult resultado)
    {
        return string.Join("; ", resultado.Errors.Select(erro => $"{erro.Code}: {erro.Description}"));
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

    private static async Task<HttpResponseMessage> EnviarRefreshAsync(
        HttpClient cliente,
        string? refresh,
        string? origem,
        string? solicitadoCom)
    {
        var requisicao = CriarRequisicaoDeCookie(
            HttpMethod.Post,
            "/api/v1/auth/refresh",
            refresh);

        if (origem is not null)
        {
            requisicao.Headers.TryAddWithoutValidation(
                ConstantesDeSessao.CabecalhoDeOrigem,
                origem);
        }

        if (solicitadoCom is not null)
        {
            requisicao.Headers.TryAddWithoutValidation(
                ConstantesDeSessao.CabecalhoSolicitadoCom,
                solicitadoCom);
        }

        return await cliente.SendAsync(requisicao);
    }

    private static HttpRequestMessage CriarRequisicaoDeCookie(
        HttpMethod metodo,
        string caminho,
        string? refresh)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);

        if (refresh is not null)
        {
            requisicao.Headers.TryAddWithoutValidation(
                "Cookie",
                $"{ConstantesDeSessao.NomeDoCookie}={refresh}");
        }

        return requisicao;
    }

    private static HttpRequestMessage CriarRequisicaoAutenticada(
        HttpMethod metodo,
        string caminho,
        string token)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return requisicao;
    }

    private static async Task<string> LerTokenAsync(HttpResponseMessage resposta)
    {
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement.GetProperty("tokenDeAcesso").GetString()!;
    }

    private static async Task<string?> LerTituloAsync(HttpResponseMessage resposta)
    {
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement.GetProperty("title").GetString();
    }

    private static string? ObterCookieDeRefresh(HttpResponseMessage resposta)
    {
        if (!resposta.Headers.TryGetValues("Set-Cookie", out var valores))
        {
            return null;
        }

        return valores.FirstOrDefault(valor => valor.StartsWith(
            $"{ConstantesDeSessao.NomeDoCookie}=",
            StringComparison.Ordinal));
    }

    private static string? ExtrairRefresh(string? cookie)
    {
        if (cookie is null)
        {
            return null;
        }

        var valor = cookie.Split(';', 2)[0];
        var separador = valor.IndexOf('=', StringComparison.Ordinal);
        if (separador < 0)
        {
            return null;
        }

        var conteudo = valor[(separador + 1)..];
        return string.IsNullOrWhiteSpace(conteudo) ? null : conteudo;
    }

    private static long LerExpiracao(string token)
    {
        var partes = token.Split('.');
        Assert.AreEqual(3, partes.Length);

        var payload = partes[1]
            .Replace('-', '+')
            .Replace('_', '/');

        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var documento = JsonDocument.Parse(json);

        return documento.RootElement.GetProperty("exp").GetInt64();
    }

    private static async Task InicializarAsync()
    {
        var connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(connectionString);
        _provedor = servicos.BuildServiceProvider();

        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        _fabrica = new ApiDeTeste(connectionString);
        _ = _fabrica.Services;

        await using var escopoSemeando = _fabrica.Services.CreateAsyncScope();
        var registrarOrganizacao = escopoSemeando.ServiceProvider
            .GetRequiredService<RegistrarOrganizacaoHandler>();
        var registrarCidadao = escopoSemeando.ServiceProvider
            .GetRequiredService<RegistrarCidadaoHandler>();

        var sufixo = Guid.NewGuid().ToString("N");
        _adminUsuario = $"admin.{sufixo}";
        _adminEmail = $"admin.{sufixo}@example.org";
        _cidadaoEmail = $"cidadao.{sufixo}@example.org";

        var organizacao = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização {sufixo}",
                Tipo: TipoOrganizacao.Cooperativa,
                Usuario: _adminUsuario,
                Email: _adminEmail,
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(
            organizacao.IsSuccess,
            string.Join("; ", organizacao.Errors.Select(erro => erro.Message)));
        _organizacaoId = organizacao.Value.OrganizacaoId;

        var cidadao = await registrarCidadao.ExecutarAsync(
            new RegistroDeCidadaoEntrada(
                Nome: $"Cidadão {sufixo}",
                Email: _cidadaoEmail,
                Telefone: "(11) 99999-0000",
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(
            cidadao.IsSuccess,
            string.Join("; ", cidadao.Errors.Select(erro => erro.Message)));
    }

    private static async Task LimparAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        if (_provedor is not null)
        {
            await using (var escopo = _provedor.CreateAsyncScope())
            {
                var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
                await contexto.Database.EnsureDeletedAsync();
            }

            await _provedor.DisposeAsync();
        }
    }
}
