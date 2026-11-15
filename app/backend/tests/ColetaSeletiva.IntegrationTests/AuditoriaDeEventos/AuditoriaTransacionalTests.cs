using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.AuditoriaDeEventos;

[TestClass]
[DoNotParallelize]
public sealed class AuditoriaTransacionalTests
{
    private const string NomeBanco = "ColetaSeletivaAuditoriaTransacionalTests";

    private static ServiceProvider _provedor = null!;
    private static Guid _organizacaoId;
    private static Guid _usuarioId;

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
    public async Task Alteracao_e_auditoria_sao_confirmadas_juntas()
    {
        var correlacaoId = Guid.NewGuid();
        var ocorridoEmUtc = DateTimeOffset.UtcNow;

        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            var unidadeDeTrabalho = escopo.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var auditoria = escopo.ServiceProvider.GetRequiredService<IRegistroDeAuditoria>();
            var usuario = await contexto.Usuarios.SingleAsync(item => item.Id == _usuarioId);

            await using var transacao = await unidadeDeTrabalho.IniciarAsync();
            usuario.Nome = "Nome Confirmado";
            await auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: _organizacaoId,
                    AtorUsuarioId: _usuarioId,
                    Acao: "teste.confirmado",
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: _usuarioId,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: ocorridoEmUtc,
                    CamposAlterados: "nome"));
            await transacao.CommitAsync();
        }

        await using var verificacao = _provedor.CreateAsyncScope();
        var contextoVerificacao = verificacao.ServiceProvider
            .GetRequiredService<ColetaSeletivaDbContext>();

        Assert.AreEqual(
            "Nome Confirmado",
            (await contextoVerificacao.Usuarios
                .AsNoTracking()
                .SingleAsync(item => item.Id == _usuarioId)).Nome);

        var registro = await contextoVerificacao.Auditorias
            .AsNoTracking()
            .SingleAsync(item => item.CorrelacaoId == correlacaoId);

        Assert.AreEqual(_organizacaoId, registro.OrganizacaoId);
        Assert.AreEqual(_usuarioId, registro.AtorUsuarioId);
        Assert.AreEqual("teste.confirmado", registro.Acao);
        Assert.AreEqual(EntidadesAuditadas.Usuario, registro.Entidade);
        Assert.AreEqual(_usuarioId, registro.EntidadeId);
        Assert.AreEqual("nome", registro.CamposAlterados);
        Assert.AreEqual(ocorridoEmUtc, registro.OcorridoEmUtc);
    }

    [TestMethod]
    public async Task Falha_da_auditoria_desfaz_a_alteracao()
    {
        var correlacaoId = Guid.NewGuid();
        var nomeOriginal = string.Empty;

        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            var unidadeDeTrabalho = escopo.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var auditoria = escopo.ServiceProvider.GetRequiredService<IRegistroDeAuditoria>();
            var usuario = await contexto.Usuarios.SingleAsync(item => item.Id == _usuarioId);
            nomeOriginal = usuario.Nome;

            await using var transacao = await unidadeDeTrabalho.IniciarAsync();
            usuario.Nome = "Nome Não Persistido";

            await Assert.ThrowsExactlyAsync<DbUpdateException>(
                () => auditoria.RegistrarAsync(
                    new EventoDeAuditoria(
                        OrganizacaoId: _organizacaoId,
                        AtorUsuarioId: _usuarioId,
                        Acao: "teste.falha",
                        Entidade: EntidadesAuditadas.Usuario,
                        EntidadeId: _usuarioId,
                        CorrelacaoId: correlacaoId,
                        OcorridoEmUtc: DateTimeOffset.UtcNow,
                        CamposAlterados: new string(
                            'x',
                            Auditoria.CamposAlteradosMaxLength + 1))));
            await transacao.RollbackAsync();
        }

        await using var verificacao = _provedor.CreateAsyncScope();
        var contextoVerificacao = verificacao.ServiceProvider
            .GetRequiredService<ColetaSeletivaDbContext>();

        Assert.AreEqual(
            nomeOriginal,
            (await contextoVerificacao.Usuarios
                .AsNoTracking()
                .SingleAsync(item => item.Id == _usuarioId)).Nome);
        Assert.IsFalse(
            await contextoVerificacao.Auditorias
                .AsNoTracking()
                .AnyAsync(item => item.CorrelacaoId == correlacaoId));
    }

    private static async Task InicializarAsync()
    {
        var connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(connectionString);
        _provedor = servicos.BuildServiceProvider();

        await using var escopo = _provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
        await contexto.Database.EnsureDeletedAsync();
        await contexto.Database.MigrateAsync();

        var agora = DateTimeOffset.UtcNow;
        var organizacao = Organizacao.Criar(
            "Cooperativa Auditoria",
            TipoOrganizacao.Cooperativa,
            agora);
        var usuario = new Usuario
        {
            UserName = "operador.auditoria",
            Email = "operador.auditoria@example.org",
            Nome = "Nome Original",
            OrganizacaoId = organizacao.Id,
            Ativo = true,
            TrocaSenhaPendente = false,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };
        _organizacaoId = organizacao.Id;
        _usuarioId = usuario.Id;

        contexto.Organizacoes.Add(organizacao);
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
    }

    private static async Task LimparAsync()
    {
        if (_provedor is null)
        {
            return;
        }

        await using (var escopo = _provedor.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
        }

        await _provedor.DisposeAsync();
    }
}
