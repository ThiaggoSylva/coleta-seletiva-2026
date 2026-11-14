using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.Infrastructure.Tenancy;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ColetaSeletiva.IntegrationTests.Tenancy;

[TestClass]
[DoNotParallelize]
public sealed class TenantIsolationTests
{
    private const string NomeBanco = "ColetaSeletivaTenantIsolationTests";

    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static string _connectionString = null!;
    private static Guid _organizacaoAId;
    private static Guid _organizacaoBId;
    private static Guid _administradorAId;
    private static Guid _operadorAId;
    private static Guid _administradorBId;
    private static Guid _cidadaoXId;
    private static Guid _cidadaoYId;
    private static Guid _refreshTokenAId;
    private static Guid _refreshTokenBId;
    private static Guid _refreshTokenXId;
    private static Guid _refreshTokenYId;
    private static Guid _auditoriaAId;
    private static Guid _auditoriaBId;
    private static Guid _auditoriaXId;
    private static Guid _auditoriaYId;

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
    public async Task Organizacao_A_le_somente_registros_da_propria_organizacao()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());

        var organizacoes = await contexto.Organizacoes.Select(item => item.Id).ToListAsync();
        CollectionAssert.AreEquivalent(new[] { _organizacaoAId }, organizacoes);

        var usuarios = await contexto.Usuarios.Select(item => item.Id).ToListAsync();
        CollectionAssert.Contains(usuarios, _administradorAId);
        CollectionAssert.Contains(usuarios, _operadorAId);
        CollectionAssert.DoesNotContain(usuarios, _administradorBId);
        CollectionAssert.DoesNotContain(usuarios, _cidadaoXId);
        CollectionAssert.DoesNotContain(usuarios, _cidadaoYId);

        var tokens = await contexto.RefreshTokens.Select(item => item.Id).ToListAsync();
        CollectionAssert.Contains(tokens, _refreshTokenAId);
        CollectionAssert.DoesNotContain(tokens, _refreshTokenBId);
        CollectionAssert.DoesNotContain(tokens, _refreshTokenXId);
        CollectionAssert.DoesNotContain(tokens, _refreshTokenYId);

        var auditorias = await contexto.Auditorias.Select(item => item.Id).ToListAsync();
        CollectionAssert.Contains(auditorias, _auditoriaAId);
        CollectionAssert.DoesNotContain(auditorias, _auditoriaBId);
        CollectionAssert.DoesNotContain(auditorias, _auditoriaXId);
        CollectionAssert.DoesNotContain(auditorias, _auditoriaYId);
    }

    [TestMethod]
    public async Task Cidadao_X_le_somente_o_proprio_perfil_tokens_e_auditorias()
    {
        await using var contexto = CriarContexto(CriarContextoDoCidadaoX());

        Assert.AreEqual(0, await contexto.Organizacoes.CountAsync());

        var usuarios = await contexto.Usuarios.Select(item => item.Id).ToListAsync();
        CollectionAssert.AreEquivalent(new[] { _cidadaoXId }, usuarios);

        var tokens = await contexto.RefreshTokens.Select(item => item.Id).ToListAsync();
        CollectionAssert.AreEquivalent(new[] { _refreshTokenXId }, tokens);

        var auditorias = await contexto.Auditorias.Select(item => item.Id).ToListAsync();
        CollectionAssert.AreEquivalent(new[] { _auditoriaXId }, auditorias);
    }

    [TestMethod]
    public async Task Identificador_conhecido_de_outra_organizacao_retorna_inexistente()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());

        Assert.IsNull(await contexto.Organizacoes.SingleOrDefaultAsync(item => item.Id == _organizacaoBId));
        Assert.IsNull(await contexto.Usuarios.SingleOrDefaultAsync(item => item.Id == _administradorBId));
        Assert.IsNull(await contexto.RefreshTokens.SingleOrDefaultAsync(item => item.Id == _refreshTokenBId));
        Assert.IsNull(await contexto.Auditorias.SingleOrDefaultAsync(item => item.Id == _auditoriaBId));
    }

    [TestMethod]
    public async Task Criacao_de_usuario_sem_organizacao_recebe_organizacao_do_contexto()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        var usuario = CriarUsuario(null, "novo.operador", $"novo.operador.{Guid.NewGuid():N}@example.org");

        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        Assert.AreEqual(_organizacaoAId, usuario.OrganizacaoId);

        await using var verificacao = CriarContexto(new TenantContext());
        Assert.IsTrue(await verificacao.Usuarios.AnyAsync(
            item => item.Id == usuario.Id && item.OrganizacaoId == _organizacaoAId));
    }

    [TestMethod]
    public async Task Criacao_de_usuario_com_organizacao_B_e_rejeitada()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        var usuario = CriarUsuario(
            _organizacaoBId,
            "intruso.criacao",
            $"intruso.criacao.{Guid.NewGuid():N}@example.org");

        contexto.Usuarios.Add(usuario);

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Alteracao_de_usuario_da_organizacao_B_e_rejeitada()
    {
        var usuarioB = await ObterUsuarioSemRastreamentoAsync(_administradorBId);
        var nomeOriginal = usuarioB.Nome;

        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        contexto.Usuarios.Attach(usuarioB);
        usuarioB.Nome = "Nome indevidamente alterado";

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());

        await using var verificacao = CriarContexto(new TenantContext());
        var persistido = await verificacao.Usuarios.SingleAsync(item => item.Id == _administradorBId);
        Assert.AreEqual(nomeOriginal, persistido.Nome);
    }

    [TestMethod]
    public async Task Attach_de_usuario_da_organizacao_B_com_organizacao_forjada_e_rejeitado()
    {
        var usuarioB = await ObterUsuarioSemRastreamentoAsync(_administradorBId);
        var nomeOriginal = usuarioB.Nome;

        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        usuarioB.OrganizacaoId = _organizacaoAId;
        contexto.Usuarios.Attach(usuarioB);
        usuarioB.Nome = "Nome forjado";

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());

        await using var verificacao = CriarContexto(new TenantContext());
        var persistido = await verificacao.Usuarios.SingleAsync(item => item.Id == _administradorBId);
        Assert.AreEqual(nomeOriginal, persistido.Nome);
    }

    [TestMethod]
    public async Task Exclusao_logica_de_usuario_da_organizacao_B_e_rejeitada()
    {
        var usuarioB = await ObterUsuarioSemRastreamentoAsync(_administradorBId);

        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        contexto.Usuarios.Attach(usuarioB);
        usuarioB.Ativo = false;

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());

        await using var verificacao = CriarContexto(new TenantContext());
        var persistido = await verificacao.Usuarios.SingleAsync(item => item.Id == _administradorBId);
        Assert.IsTrue(persistido.Ativo);
    }

    [TestMethod]
    public async Task Alteracao_da_organizacao_do_usuario_pela_sessao_e_rejeitada()
    {
        var administradorA = await ObterUsuarioSemRastreamentoAsync(_administradorAId);

        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        contexto.Usuarios.Attach(administradorA);
        administradorA.OrganizacaoId = _organizacaoBId;

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Cidadao_X_nao_altera_cidadao_Y()
    {
        var cidadaoY = await ObterUsuarioSemRastreamentoAsync(_cidadaoYId);
        var telefoneOriginal = cidadaoY.Telefone;

        await using var contexto = CriarContexto(CriarContextoDoCidadaoX());
        contexto.Usuarios.Attach(cidadaoY);
        cidadaoY.Telefone = "11999999999";

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());

        await using var verificacao = CriarContexto(new TenantContext());
        var persistido = await verificacao.Usuarios.SingleAsync(item => item.Id == _cidadaoYId);
        Assert.AreEqual(telefoneOriginal, persistido.Telefone);
    }

    [TestMethod]
    public async Task Cidadao_X_nao_cria_auditoria_de_organizacao()
    {
        await using var contexto = CriarContexto(CriarContextoDoCidadaoX());
        contexto.Auditorias.Add(CriarAuditoria(_organizacaoAId, _administradorAId));

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Auditoria_da_organizacao_recebe_organizacao_e_ator_do_contexto()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        var auditoria = CriarAuditoria(null, null);

        contexto.Auditorias.Add(auditoria);
        await contexto.SaveChangesAsync();

        Assert.AreEqual(_organizacaoAId, auditoria.OrganizacaoId);
        Assert.AreEqual(_administradorAId, auditoria.AtorUsuarioId);
    }

    [TestMethod]
    public async Task Auditoria_com_organizacao_B_e_rejeitada()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        contexto.Auditorias.Add(CriarAuditoria(_organizacaoBId, _administradorBId));

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Auditoria_com_ator_de_outra_organizacao_e_rejeitada()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        contexto.Auditorias.Add(CriarAuditoria(_organizacaoAId, _administradorBId));

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Refresh_token_de_outra_organizacao_e_rejeitado()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        contexto.RefreshTokens.Add(CriarRefreshToken(_administradorBId));

        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Refresh_token_de_usuario_da_propria_organizacao_e_aceito()
    {
        await using var contexto = CriarContexto(CriarContextoDaOrganizacaoA());
        var token = CriarRefreshToken(_operadorAId);

        contexto.RefreshTokens.Add(token);
        await contexto.SaveChangesAsync();

        await using var verificacao = CriarContexto(new TenantContext());
        Assert.IsTrue(await verificacao.RefreshTokens.AnyAsync(item => item.Id == token.Id));
    }

    [TestMethod]
    public async Task Sessao_autenticada_sem_escopo_valido_nao_le_nem_grava()
    {
        var tenant = new TenantContext();
        tenant.DefinirAutenticacaoInvalida(_administradorAId);

        await using var contexto = CriarContexto(tenant);

        Assert.AreEqual(0, await contexto.Usuarios.CountAsync());
        Assert.AreEqual(0, await contexto.Organizacoes.CountAsync());
        Assert.AreEqual(0, await contexto.Auditorias.CountAsync());

        contexto.Auditorias.Add(CriarAuditoria(null, null));
        await Assert.ThrowsExactlyAsync<ViolacaoDeTenantException>(() => contexto.SaveChangesAsync());
    }

    private static async Task InicializarAsync()
    {
        _connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        await using (var contexto = CriarContexto(new TenantContext()))
        {
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        await using (var contexto = CriarContexto(new TenantContext()))
        {
            var organizacaoA = Organizacao.Criar("Cooperativa A", TipoOrganizacao.Cooperativa, Agora);
            var organizacaoB = Organizacao.Criar("Associação B", TipoOrganizacao.Associacao, Agora);
            _organizacaoAId = organizacaoA.Id;
            _organizacaoBId = organizacaoB.Id;

            var administradorA = CriarUsuario(organizacaoA.Id, "administrador.a", "administrador.a@example.org");
            var operadorA = CriarUsuario(organizacaoA.Id, "operador.a", "operador.a@example.org");
            var administradorB = CriarUsuario(organizacaoB.Id, "administrador.b", "administrador.b@example.org");
            var cidadaoX = CriarUsuario(null, "cidadao.x", "cidadao.x@example.org");
            var cidadaoY = CriarUsuario(null, "cidadao.y", "cidadao.y@example.org");
            cidadaoX.Telefone = "11911111111";
            cidadaoY.Telefone = "11922222222";
            _administradorAId = administradorA.Id;
            _operadorAId = operadorA.Id;
            _administradorBId = administradorB.Id;
            _cidadaoXId = cidadaoX.Id;
            _cidadaoYId = cidadaoY.Id;

            var tokenA = CriarRefreshToken(administradorA.Id);
            var tokenB = CriarRefreshToken(administradorB.Id);
            var tokenX = CriarRefreshToken(cidadaoX.Id);
            var tokenY = CriarRefreshToken(cidadaoY.Id);
            _refreshTokenAId = tokenA.Id;
            _refreshTokenBId = tokenB.Id;
            _refreshTokenXId = tokenX.Id;
            _refreshTokenYId = tokenY.Id;

            var auditoriaA = CriarAuditoria(organizacaoA.Id, administradorA.Id);
            var auditoriaB = CriarAuditoria(organizacaoB.Id, administradorB.Id);
            var auditoriaX = CriarAuditoria(null, cidadaoX.Id);
            var auditoriaY = CriarAuditoria(null, cidadaoY.Id);
            _auditoriaAId = auditoriaA.Id;
            _auditoriaBId = auditoriaB.Id;
            _auditoriaXId = auditoriaX.Id;
            _auditoriaYId = auditoriaY.Id;

            contexto.Organizacoes.AddRange(organizacaoA, organizacaoB);
            contexto.Usuarios.AddRange(administradorA, operadorA, administradorB, cidadaoX, cidadaoY);
            contexto.RefreshTokens.AddRange(tokenA, tokenB, tokenX, tokenY);
            contexto.Auditorias.AddRange(auditoriaA, auditoriaB, auditoriaX, auditoriaY);

            await contexto.SaveChangesAsync();
        }
    }

    private static async Task LimparAsync()
    {
        await using var contexto = CriarContexto(new TenantContext());
        await contexto.Database.EnsureDeletedAsync();
    }

    private static ColetaSeletivaDbContext CriarContexto(TenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<ColetaSeletivaDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new ColetaSeletivaDbContext(options, tenant);
    }

    private static TenantContext CriarContextoDaOrganizacaoA()
    {
        var tenant = new TenantContext();
        tenant.DefinirEscopo(_administradorAId, _organizacaoAId, ehCidadao: false);
        return tenant;
    }

    private static TenantContext CriarContextoDoCidadaoX()
    {
        var tenant = new TenantContext();
        tenant.DefinirEscopo(_cidadaoXId, null, ehCidadao: true);
        return tenant;
    }

    private static async Task<Usuario> ObterUsuarioSemRastreamentoAsync(Guid usuarioId)
    {
        await using var contexto = CriarContexto(new TenantContext());
        return await contexto.Usuarios
            .AsNoTracking()
            .SingleAsync(item => item.Id == usuarioId);
    }

    private static Usuario CriarUsuario(Guid? organizacaoId, string usuario, string email)
    {
        return new Usuario
        {
            Id = Guid.NewGuid(),
            OrganizacaoId = organizacaoId,
            Nome = $"Pessoa {usuario}",
            UserName = usuario,
            NormalizedUserName = usuario.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Ativo = true,
            TrocaSenhaPendente = false,
            CriadoEmUtc = Agora,
            AtualizadoEmUtc = Agora,
        };
    }

    private static RefreshToken CriarRefreshToken(Guid usuarioId)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            FamiliaId = Guid.NewGuid(),
            Hash = (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")).ToUpperInvariant(),
            CriadoEmUtc = Agora,
            ExpiraEmUtc = Agora.AddDays(7),
        };
    }

    private static Auditoria CriarAuditoria(Guid? organizacaoId, Guid? atorUsuarioId)
    {
        return new Auditoria
        {
            Id = Guid.NewGuid(),
            OrganizacaoId = organizacaoId,
            AtorUsuarioId = atorUsuarioId,
            Acao = "teste.tenancy",
            Entidade = "Usuario",
            EntidadeId = atorUsuarioId,
            CorrelacaoId = Guid.NewGuid(),
            OcorridoEmUtc = Agora,
        };
    }
}
