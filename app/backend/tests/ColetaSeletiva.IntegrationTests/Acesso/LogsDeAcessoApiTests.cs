using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class LogsDeAcessoApiTests
{
    private const string NomeBanco = "ColetaSeletivaLogsDeAcessoApiTests";
    private const string Senha = "SenhaForte!123";

    private static string _connectionString = null!;
    private static ColetorDeLogs _coletor = null!;
    private static ApiDeTeste _fabrica = null!;
    private static string _adminEmail = null!;

    [ClassInitialize]
    public static async Task Inicializar(TestContext _)
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(_connectionString);
        await using (var provedor = servicos.BuildServiceProvider())
        {
            await using var escopo = provedor.CreateAsyncScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        _coletor = new ColetorDeLogs();
        _fabrica = new ApiDeTeste(
            _connectionString,
            configurarLogging: logging => logging.AddProvider(_coletor));

        await using var escopoSemeando = _fabrica.Services.CreateAsyncScope();
        var registrarOrganizacao = escopoSemeando.ServiceProvider
            .GetRequiredService<RegistrarOrganizacaoHandler>();

        var sufixo = Guid.NewGuid().ToString("N");
        var usuario = $"admin.{sufixo}";
        _adminEmail = $"admin.{sufixo}@example.org";

        var organizacao = await registrarOrganizacao.ExecutarAsync(
            new RegistroDeOrganizacaoEntrada(
                NomeOrganizacao: $"Organização {sufixo}",
                Tipo: TipoOrganizacao.Cooperativa,
                Usuario: usuario,
                Email: _adminEmail,
                Senha: Senha),
            Guid.NewGuid());
        Assert.IsTrue(
            organizacao.IsSuccess,
            string.Join("; ", organizacao.Errors.Select(erro => erro.Message)));
    }

    [ClassCleanup]
    public static async Task Limpar()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(_connectionString);
        await using var provedor = servicos.BuildServiceProvider();
        await using var escopo = provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        await contexto.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task Login_bem_sucedido_nao_registra_email_senha_nem_token()
    {
        _coletor.Limpar();
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador = _adminEmail, senha = Senha });
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var token = corpo.RootElement.GetProperty("tokenDeAcesso").GetString()!;
        var logs = await AguardarLogsDaRotaAsync("/api/v1/auth/login");

        Assert.IsFalse(Contem(logs, _adminEmail));
        Assert.IsFalse(Contem(logs, Senha));
        Assert.IsFalse(Contem(logs, token));
    }

    [TestMethod]
    public async Task Login_malsucedido_nao_registra_email_nem_senha_informados()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var email = $"pessoa.{sufixo}@example.org";
        var senha = "SegredoUnico!987";

        _coletor.Limpar();
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { identificador = email, senha });
        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);

        var logs = await AguardarLogsDaRotaAsync("/api/v1/auth/login");

        Assert.IsFalse(Contem(logs, email));
        Assert.IsFalse(Contem(logs, sufixo));
        Assert.IsFalse(Contem(logs, senha));
    }

    [TestMethod]
    public async Task Registro_de_cidadao_nao_registra_nome_email_telefone_nem_senha()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var nome = $"Nome Sensível {sufixo}";
        var email = $"cidadao.{sufixo}@example.org";
        var telefone = "(11) 97777-6666";
        var senha = "SenhaSecreta!555";

        _coletor.Limpar();
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/registro-cidadao",
            new { nome, email, telefone, senha });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);

        var logs = await AguardarLogsDaRotaAsync("/api/v1/auth/registro-cidadao");

        Assert.IsFalse(Contem(logs, nome));
        Assert.IsFalse(Contem(logs, email));
        Assert.IsFalse(Contem(logs, sufixo));
        Assert.IsFalse(Contem(logs, telefone));
        Assert.IsFalse(Contem(logs, senha));
    }

    private static bool Contem(IReadOnlyList<string> logs, string valor)
    {
        return logs.Any(mensagem => mensagem.Contains(valor, StringComparison.Ordinal));
    }

    private static async Task<IReadOnlyList<string>> AguardarLogsDaRotaAsync(string caminho)
    {
        for (var tentativa = 0; tentativa < 40; tentativa++)
        {
            var logs = _coletor.ObterMensagens();
            if (logs.Any(mensagem => mensagem.Contains(caminho, StringComparison.Ordinal)))
            {
                return logs;
            }

            await Task.Delay(50);
        }

        Assert.Fail($"O log da requisição para {caminho} não foi capturado.");
        return [];
    }
}
