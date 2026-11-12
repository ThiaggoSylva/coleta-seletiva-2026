using ColetaSeletiva.Application;
using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Acesso;

[TestClass]
[DoNotParallelize]
public sealed class RegistroEPerfilDoCidadaoTests
{
    private const string NomeBanco = "ColetaSeletivaCidadaoTests";
    private const string SenhaValida = "SenhaForte!123";

    private static readonly Guid CorrelacaoId = Guid.Parse("dddddddd-eeee-ffff-aaaa-bbbbbbbbbbbb");

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
    public async Task Registro_valido_cria_cidadao_sem_organizacao_com_papel_e_auditoria()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var entrada = CriarEntrada(sufixo);

        var resposta = await RegistrarComSucessoAsync(entrada);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        var usuario = await contexto.Usuarios.SingleAsync(item => item.Id == resposta.UsuarioId);
        Assert.IsNull(usuario.OrganizacaoId);
        Assert.AreEqual(entrada.Nome, usuario.Nome);
        Assert.AreEqual(entrada.Email, usuario.Email);
        Assert.AreEqual(entrada.Telefone, usuario.Telefone);
        Assert.IsTrue(usuario.Ativo);
        Assert.IsFalse(usuario.TrocaSenhaPendente);
        Assert.IsNotNull(usuario.PasswordHash);
        Assert.AreNotEqual(entrada.Senha, usuario.PasswordHash);

        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Assert.IsTrue(await gerenciadorUsuarios.IsInRoleAsync(usuario, Papeis.Cidadao));

        var auditoria = await contexto.Auditorias.SingleAsync(
            item => item.EntidadeId == usuario.Id);
        Assert.IsNull(auditoria.OrganizacaoId);
        Assert.AreEqual(usuario.Id, auditoria.AtorUsuarioId);
        Assert.AreEqual(AcoesAuditadas.CidadaoRegistrado, auditoria.Acao);
        Assert.AreEqual(EntidadesAuditadas.Usuario, auditoria.Entidade);
        Assert.AreEqual(CorrelacaoId, auditoria.CorrelacaoId);
        Assert.IsNull(auditoria.CamposAlterados);
    }

    [TestMethod]
    public async Task Email_duplicado_retorna_conflito_e_nao_cria_conta()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var primeira = CriarEntrada(sufixo);
        await RegistrarComSucessoAsync(primeira);

        var segunda = primeira with
        {
            Nome = "Outro cidadão",
            Email = primeira.Email.ToUpperInvariant(),
            Telefone = "(11) 97777-6666",
        };

        var resultado = await RegistrarAsync(segunda);

        Assert.IsTrue(resultado.IsFailed);
        var conflito = resultado.Errors.OfType<ErroDeConflito>().First();
        Assert.AreEqual(CodigosDeErro.EmailEmUso, conflito.Codigo);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        Assert.AreEqual(
            1,
            await contexto.Usuarios.CountAsync(usuario => usuario.Email == primeira.Email));
        Assert.IsFalse(await contexto.Usuarios.AnyAsync(usuario => usuario.Nome == segunda.Nome));
    }

    [TestMethod]
    public async Task Senha_fora_da_politica_retorna_erros_de_validacao_sem_criar_conta()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var entrada = CriarEntrada(sufixo) with { Senha = "curta" };

        var resultado = await RegistrarAsync(entrada);

        Assert.IsTrue(resultado.IsFailed);
        var validacoes = resultado.Errors.OfType<ErroDeValidacao>().ToList();
        Assert.IsTrue(validacoes.Count > 0);
        Assert.IsTrue(validacoes.All(validacao => validacao.Campo == "Senha"));

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        Assert.IsFalse(await contexto.Usuarios.AnyAsync(usuario => usuario.Email == entrada.Email));
    }

    [TestMethod]
    public async Task Cidadao_consulta_e_edita_somente_o_proprio_perfil()
    {
        var sufixoX = Guid.NewGuid().ToString("N");
        var sufixoY = Guid.NewGuid().ToString("N");
        var cidadaoX = await RegistrarComSucessoAsync(CriarEntrada(sufixoX));
        var cidadaoY = await RegistrarComSucessoAsync(CriarEntrada(sufixoY));

        var novoEmail = $"novo.{sufixoX}@example.org";
        var resultado = await AtualizarPerfilAsync(
            cidadaoX.UsuarioId,
            new AtualizarPerfilDoCidadaoEntrada(
                Nome: "Nome atualizado",
                Email: novoEmail,
                Telefone: "(11) 98888-7777"));

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));
        Assert.AreEqual(cidadaoX.UsuarioId, resultado.Value.Id);
        Assert.AreEqual("Nome atualizado", resultado.Value.Nome);
        Assert.AreEqual(novoEmail, resultado.Value.Email);

        var perfil = await ObterPerfilAsync(cidadaoX.UsuarioId);
        Assert.IsTrue(perfil.IsSuccess, Descrever(perfil));
        Assert.AreEqual(novoEmail, perfil.Value.Email);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        var usuarioX = await contexto.Usuarios.SingleAsync(item => item.Id == cidadaoX.UsuarioId);
        Assert.AreEqual("Nome atualizado", usuarioX.Nome);
        Assert.AreEqual("(11) 98888-7777", usuarioX.Telefone);
        Assert.AreEqual(novoEmail, usuarioX.Email);
        Assert.IsNull(usuarioX.OrganizacaoId);

        var usuarioY = await contexto.Usuarios.SingleAsync(item => item.Id == cidadaoY.UsuarioId);
        Assert.AreEqual($"Cidadão {sufixoY}", usuarioY.Nome);
        Assert.AreNotEqual("Nome atualizado", usuarioY.Nome);

        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Assert.IsTrue(await gerenciadorUsuarios.IsInRoleAsync(usuarioX, Papeis.Cidadao));
        Assert.IsTrue(await gerenciadorUsuarios.IsInRoleAsync(usuarioY, Papeis.Cidadao));
    }

    [TestMethod]
    public async Task Cidadao_nao_pode_usar_email_de_outro_cidadao_no_perfil()
    {
        var sufixoX = Guid.NewGuid().ToString("N");
        var sufixoY = Guid.NewGuid().ToString("N");
        var cidadaoX = await RegistrarComSucessoAsync(CriarEntrada(sufixoX));
        var cidadaoY = await RegistrarComSucessoAsync(CriarEntrada(sufixoY));

        var resultado = await AtualizarPerfilAsync(
            cidadaoX.UsuarioId,
            new AtualizarPerfilDoCidadaoEntrada(
                Nome: "Tentativa de conflito",
                Email: $"cidadao.{sufixoY}@example.org",
                Telefone: null));

        Assert.IsTrue(resultado.IsFailed);
        var conflito = resultado.Errors.OfType<ErroDeConflito>().Single();
        Assert.AreEqual(CodigosDeErro.EmailEmUso, conflito.Codigo);

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        var usuarioX = await contexto.Usuarios.SingleAsync(item => item.Id == cidadaoX.UsuarioId);
        Assert.AreEqual($"cidadao.{sufixoX}@example.org", usuarioX.Email);
        Assert.AreEqual($"Cidadão {sufixoX}", usuarioX.Nome);

        var usuarioY = await contexto.Usuarios.SingleAsync(item => item.Id == cidadaoY.UsuarioId);
        Assert.AreEqual($"cidadao.{sufixoY}@example.org", usuarioY.Email);
    }

    [TestMethod]
    public async Task Perfil_sem_autenticacao_retorna_nao_autenticado_e_nao_altera_dados()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var cidadao = await RegistrarComSucessoAsync(CriarEntrada(sufixo));

        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<AtualizarPerfilDoCidadaoHandler>();

        var resultado = await handler.ExecutarAsync(new AtualizarPerfilDoCidadaoEntrada(
            Nome: "Sem sessão",
            Email: $"sem.sessao.{sufixo}@example.org",
            Telefone: null));

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());

        await using var verificacao = _provedor.CreateAsyncScope();
        var contexto = verificacao.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        var usuario = await contexto.Usuarios.SingleAsync(item => item.Id == cidadao.UsuarioId);
        Assert.AreEqual($"Cidadão {sufixo}", usuario.Nome);
        Assert.AreEqual($"cidadao.{sufixo}@example.org", usuario.Email);
    }

    private static RegistroDeCidadaoEntrada CriarEntrada(string sufixo)
    {
        return new RegistroDeCidadaoEntrada(
            Nome: $"Cidadão {sufixo}",
            Email: $"cidadao.{sufixo}@example.org",
            Telefone: "(11) 99999-0000",
            Senha: SenhaValida);
    }

    private static async Task<RegistroDeCidadaoResposta> RegistrarComSucessoAsync(
        RegistroDeCidadaoEntrada entrada)
    {
        var resultado = await RegistrarAsync(entrada);

        Assert.IsTrue(resultado.IsSuccess, Descrever(resultado));

        return resultado.Value;
    }

    private static async Task<Result<RegistroDeCidadaoResposta>> RegistrarAsync(
        RegistroDeCidadaoEntrada entrada)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<RegistrarCidadaoHandler>();

        return await handler.ExecutarAsync(entrada, CorrelacaoId);
    }

    private static async Task<Result<PerfilDoCidadao>> AtualizarPerfilAsync(
        Guid cidadaoId,
        AtualizarPerfilDoCidadaoEntrada entrada)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var tenantContext = escopo.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.DefinirEscopo(cidadaoId, organizacaoId: null, ehCidadao: true);

        var handler = escopo.ServiceProvider.GetRequiredService<AtualizarPerfilDoCidadaoHandler>();

        return await handler.ExecutarAsync(entrada);
    }

    private static async Task<Result<PerfilDoCidadao>> ObterPerfilAsync(Guid cidadaoId)
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var tenantContext = escopo.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.DefinirEscopo(cidadaoId, organizacaoId: null, ehCidadao: true);

        var handler = escopo.ServiceProvider.GetRequiredService<ObterPerfilDoCidadaoHandler>();

        return await handler.ExecutarAsync();
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
}
