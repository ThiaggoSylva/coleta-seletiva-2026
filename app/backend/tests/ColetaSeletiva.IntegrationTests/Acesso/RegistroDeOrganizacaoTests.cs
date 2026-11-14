using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class RegistroDeOrganizacaoTests
{
    private const string NomeBanco = "ColetaSeletivaRegistroOrganizacaoTests";
    private const string SenhaValida = "SenhaForte!123";

    private static readonly Guid CorrelacaoId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

    private static string _connectionString = null!;
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
    public async Task Registro_valido_persiste_organizacao_nao_publicada_administrador_papel_e_auditoria()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var entrada = new RegistroDeOrganizacaoEntrada(
            NomeOrganizacao: $"Cooperativa {sufixo}",
            Tipo: TipoOrganizacao.Cooperativa,
            Usuario: $"admin.{sufixo}",
            Email: $"admin.{sufixo}@example.org",
            Senha: SenhaValida);

        var resposta = await RegistrarComSucessoAsync(entrada);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        var organizacao = await contexto.Organizacoes.SingleAsync(
            item => item.Id == resposta.OrganizacaoId);
        Assert.IsFalse(organizacao.Publicada);
        Assert.AreEqual(entrada.NomeOrganizacao, organizacao.Nome);
        Assert.AreEqual(entrada.Tipo, organizacao.Tipo);
        Assert.AreEqual(organizacao.CriadoEmUtc, organizacao.AtualizadoEmUtc);

        var usuario = await contexto.Usuarios.SingleAsync(
            item => item.Id == resposta.AdministradorId);
        Assert.AreEqual(organizacao.Id, usuario.OrganizacaoId);
        Assert.AreEqual(entrada.Usuario, usuario.UserName);
        Assert.AreEqual(entrada.Email, usuario.Email);
        Assert.IsTrue(usuario.Ativo);
        Assert.IsFalse(usuario.TrocaSenhaPendente);
        Assert.IsNotNull(usuario.PasswordHash);
        Assert.AreNotEqual(entrada.Senha, usuario.PasswordHash);

        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Assert.IsTrue(await gerenciadorUsuarios.IsInRoleAsync(usuario, Papeis.Administrador));

        var auditoria = await contexto.Auditorias.SingleAsync(
            item => item.EntidadeId == organizacao.Id);
        Assert.AreEqual(organizacao.Id, auditoria.OrganizacaoId);
        Assert.AreEqual(usuario.Id, auditoria.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.OrganizacaoRegistrada, auditoria.Acao);
        Assert.AreEqual(nameof(Organizacao), auditoria.Entidade);
        Assert.AreEqual(CorrelacaoId, auditoria.CorrelacaoId);
        Assert.AreEqual(organizacao.CriadoEmUtc, auditoria.OcorridoEmUtc);
        Assert.IsNull(auditoria.CamposAlterados);
    }

    [TestMethod]
    public async Task Usuario_duplicado_retorna_conflito_e_nao_cria_registros_parciais()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var primeira = CriarEntrada(sufixo);
        await RegistrarComSucessoAsync(primeira);

        var segunda = primeira with
        {
            NomeOrganizacao = $"Outra cooperativa {sufixo}",
            Usuario = primeira.Usuario.ToUpperInvariant(),
            Email = $"outro.{sufixo}@example.org",
        };

        var resultado = await RegistrarAsync(segunda);

        Assert.IsTrue(resultado.IsFailed);
        var conflito = resultado.Errors.OfType<ErroDeConflito>().Single();
        Assert.AreEqual(CodigosDeErro.UsuarioEmUso, conflito.Codigo);
        Assert.IsFalse(await ExisteOrganizacaoAsync(segunda.NomeOrganizacao));
        Assert.IsFalse(await ExisteUsuarioPorEmailAsync(segunda.Email));
    }

    [TestMethod]
    public async Task Email_duplicado_retorna_conflito_e_nao_cria_registros_parciais()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var primeira = CriarEntrada(sufixo);
        await RegistrarComSucessoAsync(primeira);

        var segunda = primeira with
        {
            NomeOrganizacao = $"Outra cooperativa {sufixo}",
            Usuario = $"outro.{sufixo}",
            Email = primeira.Email.ToUpperInvariant(),
        };

        var resultado = await RegistrarAsync(segunda);

        Assert.IsTrue(resultado.IsFailed);
        var conflito = resultado.Errors.OfType<ErroDeConflito>().Single();
        Assert.AreEqual(CodigosDeErro.EmailEmUso, conflito.Codigo);
        Assert.IsFalse(await ExisteOrganizacaoAsync(segunda.NomeOrganizacao));
        Assert.IsFalse(await ExisteUsuarioPorNomeAsync(segunda.Usuario));
    }

    [TestMethod]
    public async Task Senha_fora_da_politica_retorna_erro_de_validacao_sem_registros()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var entrada = CriarEntrada(sufixo) with { Senha = "curta" };

        var resultado = await RegistrarAsync(entrada);

        Assert.IsTrue(resultado.IsFailed);
        var validacoes = resultado.Errors.OfType<ErroDeValidacao>().ToList();
        Assert.IsTrue(validacoes.Count > 0);
        Assert.IsTrue(validacoes.All(validacao => validacao.Campo == "Senha"));
        Assert.IsFalse(await ExisteOrganizacaoAsync(entrada.NomeOrganizacao));
        Assert.IsFalse(await ExisteUsuarioPorEmailAsync(entrada.Email));
    }

    [TestMethod]
    public async Task Falha_na_auditoria_desfaz_organizacao_e_administrador()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var entrada = CriarEntrada(sufixo);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddApplication();
        servicos.AddInfrastructure(_connectionString);
        servicos.RemoveAll<IRegistroDeAuditoria>();
        servicos.AddScoped<IRegistroDeAuditoria, AuditoriaQueFalha>();

        await using var provedor = servicos.BuildServiceProvider();
        await using var escopo = provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<RegistrarOrganizacaoHandler>();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.ExecutarAsync(entrada, CorrelacaoId));

        Assert.IsFalse(await ExisteOrganizacaoAsync(entrada.NomeOrganizacao));
        Assert.IsFalse(await ExisteUsuarioPorEmailAsync(entrada.Email));
    }

    private static RegistroDeOrganizacaoEntrada CriarEntrada(string sufixo)
    {
        return new RegistroDeOrganizacaoEntrada(
            NomeOrganizacao: $"Cooperativa {sufixo}",
            Tipo: TipoOrganizacao.Cooperativa,
            Usuario: $"admin.{sufixo}",
            Email: $"admin.{sufixo}@example.org",
            Senha: SenhaValida);
    }

    private static async Task<RegistroDeOrganizacaoResposta> RegistrarComSucessoAsync(
        RegistroDeOrganizacaoEntrada entrada)
    {
        var resultado = await RegistrarAsync(entrada);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));

        return resultado.Value;
    }

    private static async Task<Result<RegistroDeOrganizacaoResposta>> RegistrarAsync(
        RegistroDeOrganizacaoEntrada entrada)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<RegistrarOrganizacaoHandler>();

        return await handler.ExecutarAsync(entrada, CorrelacaoId);
    }

    private static async Task<bool> ExisteOrganizacaoAsync(string nome)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Organizacoes.AnyAsync(organizacao => organizacao.Nome == nome);
    }

    private static async Task<bool> ExisteUsuarioPorEmailAsync(string email)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Usuarios.AnyAsync(usuario => usuario.Email == email);
    }

    private static async Task<bool> ExisteUsuarioPorNomeAsync(string nome)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        return await contexto.Usuarios.AnyAsync(usuario => usuario.UserName == nome);
    }

    private static string Descrever(ResultBase resultado)
    {
        return string.Join(
            "; ",
            resultado.Errors.Select(erro =>
                erro.Metadata.TryGetValue("codigo", out var codigo)
                    ? $"{codigo}: {erro.Message}"
                    : erro.Message));
    }

    private static async Task InicializarAsync()
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddApplication();
        servicos.AddInfrastructure(_connectionString);
        _provedor = servicos.BuildServiceProvider();

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        await contexto.Database.EnsureDeletedAsync();
        await contexto.Database.MigrateAsync();

        var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await PapelSeeder.GarantirPapeisAsync(gerenciadorPapeis);
    }

    private static async Task LimparAsync()
    {
        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
        }

        await _provedor.DisposeAsync();
    }

    private sealed class AuditoriaQueFalha : IRegistroDeAuditoria
    {
        public Task RegistrarAsync(
            EventoDeAuditoria evento,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Falha simulada na auditoria.");
        }
    }
}
