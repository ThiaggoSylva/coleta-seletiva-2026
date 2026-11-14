using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Api.Seguranca;
using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.AuditoriaDeEventos;

[TestClass]
[DoNotParallelize]
public sealed class AuditoriaApiTests
{
    private const string NomeBanco = "ColetaSeletivaAuditoriaApiTests";
    private const string Senha = "SenhaForte!123";
    private const string NovaSenha = "NovaSenhaForte!456";

    private static ApiDeTeste _fabrica = null!;
    private static ServiceProvider _provedor = null!;

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
    public async Task Eventos_criticos_de_acesso_sao_auditados_sem_segredos()
    {
        using var cliente = _fabrica.CreateClient();
        var sufixo = Guid.NewGuid().ToString("N");
        var nomeOrganizacao = $"Cooperativa Auditoria {sufixo}";
        var adminUsuario = $"admin.{sufixo}";
        var adminEmail = $"admin.{sufixo}@example.org";
        var cidadaoNome = $"Cidadão {sufixo}";
        var cidadaoEmail = $"cidadao.{sufixo}@example.org";
        var cidadaoTelefone = "(11) 98888-7777";
        var antes = DateTimeOffset.UtcNow.AddSeconds(-2);

        var correlacaoOrganizacao = Guid.NewGuid();
        var registroOrganizacao = await EnviarComCorrelacaoAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/auth/registro-organizacao",
            correlacaoOrganizacao,
            JsonContent.Create(new
            {
                nomeOrganizacao,
                tipo = "Cooperativa",
                usuario = adminUsuario,
                email = adminEmail,
                senha = Senha,
            }));
        Assert.AreEqual(HttpStatusCode.Created, registroOrganizacao.StatusCode);

        using var corpoOrganizacao = JsonDocument.Parse(
            await registroOrganizacao.Content.ReadAsStringAsync());
        var organizacaoId = corpoOrganizacao.RootElement
            .GetProperty("organizacaoId")
            .GetGuid();
        var administradorId = corpoOrganizacao.RootElement
            .GetProperty("administradorId")
            .GetGuid();

        var auditoriaOrganizacao = await ObterAuditoriaPorCorrelacaoAsync(correlacaoOrganizacao);
        Assert.AreEqual(AcoesAuditadas.OrganizacaoRegistrada, auditoriaOrganizacao.Acao);
        Assert.AreEqual(organizacaoId, auditoriaOrganizacao.OrganizacaoId);
        Assert.AreEqual(administradorId, auditoriaOrganizacao.AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Organizacao, auditoriaOrganizacao.Entidade);
        Assert.AreEqual(organizacaoId, auditoriaOrganizacao.EntidadeId);
        Assert.IsNull(auditoriaOrganizacao.CamposAlterados);
        AssertMomentoValido(auditoriaOrganizacao, antes);

        var correlacaoCidadao = Guid.NewGuid();
        var registroCidadao = await EnviarComCorrelacaoAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/auth/registro-cidadao",
            correlacaoCidadao,
            JsonContent.Create(new
            {
                nome = cidadaoNome,
                email = cidadaoEmail,
                telefone = cidadaoTelefone,
                senha = Senha,
            }));
        Assert.AreEqual(HttpStatusCode.Created, registroCidadao.StatusCode);

        using var corpoCidadao = JsonDocument.Parse(
            await registroCidadao.Content.ReadAsStringAsync());
        var cidadaoId = corpoCidadao.RootElement.GetProperty("usuarioId").GetGuid();

        var auditoriaCidadao = await ObterAuditoriaPorCorrelacaoAsync(correlacaoCidadao);
        Assert.AreEqual(AcoesAuditadas.CidadaoRegistrado, auditoriaCidadao.Acao);
        Assert.IsNull(auditoriaCidadao.OrganizacaoId);
        Assert.AreEqual(cidadaoId, auditoriaCidadao.AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoriaCidadao.Entidade);
        Assert.AreEqual(cidadaoId, auditoriaCidadao.EntidadeId);
        Assert.IsNull(auditoriaCidadao.CamposAlterados);
        AssertMomentoValido(auditoriaCidadao, antes);

        var correlacaoLogin = Guid.NewGuid();
        var login = await EntrarAsync(cliente, adminUsuario, Senha, correlacaoLogin);
        var tokenAdmin = await LerTokenAsync(login);
        var refreshInicial = ExtrairRefresh(ObterCookieDeRefresh(login));
        Assert.IsNotNull(refreshInicial);

        var auditoriaLogin = await ObterAuditoriaPorCorrelacaoAsync(correlacaoLogin);
        Assert.AreEqual(AcoesAuditadas.SessaoIniciada, auditoriaLogin.Acao);
        Assert.AreEqual(organizacaoId, auditoriaLogin.OrganizacaoId);
        Assert.AreEqual(administradorId, auditoriaLogin.AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoriaLogin.Entidade);
        Assert.AreEqual(administradorId, auditoriaLogin.EntidadeId);
        Assert.IsNull(auditoriaLogin.CamposAlterados);
        AssertMomentoValido(auditoriaLogin, antes);

        var correlacaoSenha = Guid.NewGuid();
        var requisicaoSenha = CriarRequisicaoAutenticada(
            HttpMethod.Post,
            "/api/v1/auth/alterar-senha",
            tokenAdmin);
        requisicaoSenha.Headers.Add("X-Correlation-Id", correlacaoSenha.ToString());
        requisicaoSenha.Content = JsonContent.Create(new
        {
            senhaAtual = Senha,
            novaSenha = NovaSenha,
        });
        var alteracaoSenha = await cliente.SendAsync(requisicaoSenha);
        Assert.AreEqual(HttpStatusCode.NoContent, alteracaoSenha.StatusCode);

        var auditoriaSenha = await ObterAuditoriaPorCorrelacaoAsync(correlacaoSenha);
        Assert.AreEqual(AcoesAuditadas.SenhaAlterada, auditoriaSenha.Acao);
        Assert.AreEqual(organizacaoId, auditoriaSenha.OrganizacaoId);
        Assert.AreEqual(administradorId, auditoriaSenha.AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoriaSenha.Entidade);
        Assert.AreEqual(administradorId, auditoriaSenha.EntidadeId);
        Assert.IsNull(auditoriaSenha.CamposAlterados);
        AssertMomentoValido(auditoriaSenha, antes);

        var correlacaoNovoLogin = Guid.NewGuid();
        var novoLogin = await EntrarAsync(cliente, adminUsuario, NovaSenha, correlacaoNovoLogin);
        var refreshNovo = ExtrairRefresh(ObterCookieDeRefresh(novoLogin));
        Assert.IsNotNull(refreshNovo);

        var correlacaoLogout = Guid.NewGuid();
        var requisicaoLogout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        requisicaoLogout.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{ConstantesDeSessao.NomeDoCookie}={refreshNovo}");
        requisicaoLogout.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoDeOrigem,
            "http://localhost");
        requisicaoLogout.Headers.TryAddWithoutValidation(
            ConstantesDeSessao.CabecalhoSolicitadoCom,
            ConstantesDeSessao.ValorSolicitadoCom);
        requisicaoLogout.Headers.Add("X-Correlation-Id", correlacaoLogout.ToString());
        var logout = await cliente.SendAsync(requisicaoLogout);
        Assert.AreEqual(HttpStatusCode.NoContent, logout.StatusCode);

        var auditoriaLogout = await ObterAuditoriaPorCorrelacaoAsync(correlacaoLogout);
        Assert.AreEqual(AcoesAuditadas.SessaoEncerrada, auditoriaLogout.Acao);
        Assert.AreEqual(organizacaoId, auditoriaLogout.OrganizacaoId);
        Assert.AreEqual(administradorId, auditoriaLogout.AtorUsuarioId);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoriaLogout.Entidade);
        Assert.AreEqual(administradorId, auditoriaLogout.EntidadeId);
        Assert.IsNull(auditoriaLogout.CamposAlterados);
        AssertMomentoValido(auditoriaLogout, antes);

        await VerificarAusenciaDeSegredosEPiiAsync(new[]
        {
            Senha,
            NovaSenha,
            adminUsuario,
            adminEmail,
            cidadaoNome,
            cidadaoEmail,
            cidadaoTelefone,
            tokenAdmin,
            refreshInicial,
            refreshNovo,
        });
    }

    private static void AssertMomentoValido(
        Auditoria auditoria,
        DateTimeOffset antes)
    {
        var depois = DateTimeOffset.UtcNow.AddSeconds(5);
        Assert.IsTrue(
            auditoria.OcorridoEmUtc >= antes && auditoria.OcorridoEmUtc <= depois,
            "O momento da auditoria deve estar dentro da janela da requisição.");
    }

    private static async Task VerificarAusenciaDeSegredosEPiiAsync(
        IReadOnlyCollection<string> valoresProibidos)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        var registros = await contexto.Auditorias
            .AsNoTracking()
            .Select(auditoria => new
            {
                auditoria.Acao,
                auditoria.Entidade,
                auditoria.CamposAlterados,
            })
            .ToListAsync();

        foreach (var registro in registros)
        {
            var conteudo = string.Join(
                '|',
                registro.Acao,
                registro.Entidade,
                registro.CamposAlterados ?? string.Empty);

            foreach (var proibido in valoresProibidos)
            {
                Assert.IsFalse(
                    !string.IsNullOrEmpty(proibido)
                        && conteudo.Contains(proibido, StringComparison.OrdinalIgnoreCase),
                    $"A auditoria '{registro.Acao}' não pode conter segredo ou PII.");
            }
        }
    }

    private static async Task<HttpResponseMessage> EnviarComCorrelacaoAsync(
        HttpClient cliente,
        HttpMethod metodo,
        string caminho,
        Guid correlacaoId,
        HttpContent conteudo)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);
        requisicao.Headers.Add("X-Correlation-Id", correlacaoId.ToString());
        requisicao.Content = conteudo;

        return await cliente.SendAsync(requisicao);
    }

    private static async Task<HttpResponseMessage> EntrarAsync(
        HttpClient cliente,
        string identificador,
        string senha,
        Guid correlacaoId)
    {
        return await EnviarComCorrelacaoAsync(
            cliente,
            HttpMethod.Post,
            "/api/v1/auth/login",
            correlacaoId,
            JsonContent.Create(new { identificador, senha }));
    }

    private static async Task<string> LerTokenAsync(HttpResponseMessage resposta)
    {
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return corpo.RootElement.GetProperty("tokenDeAcesso").GetString()!;
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

    private static async Task<Auditoria> ObterAuditoriaPorCorrelacaoAsync(Guid correlacaoId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Auditorias
            .AsNoTracking()
            .SingleAsync(auditoria => auditoria.CorrelacaoId == correlacaoId);
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

        _fabrica = new ApiDeTeste(
            connectionString,
            new Dictionary<string, string?>
            {
                ["LimitesDeTaxa:Login:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Refresh:PermitLimit"] = "1000",
                ["LimitesDeTaxa:Cadastro:PermitLimit"] = "1000",
            });
        _ = _fabrica.Services;
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
