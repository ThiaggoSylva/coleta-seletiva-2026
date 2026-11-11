using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Identidade;

[TestClass]
[DoNotParallelize]
public sealed class IdentityConfigurationTests
{
    private const string NomeBanco = "ColetaSeletivaIdentityTests";
    private const string SenhaValida = "SenhaForte!123";

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
    public async Task Papeis_administrador_operador_e_cidadao_sao_criados_pelo_seed()
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var papel in Papeis.Todos)
        {
            Assert.IsTrue(
                await gerenciadorPapeis.RoleExistsAsync(papel),
                $"Papel '{papel}' não encontrado.");
        }

        var papeisPersistidos = await gerenciadorPapeis.Roles
            .Select(papel => papel.Name)
            .ToListAsync();

        Assert.AreEqual(Papeis.Todos.Count, papeisPersistidos.Count);
        foreach (var papel in Papeis.Todos)
        {
            CollectionAssert.Contains(papeisPersistidos, papel);
        }
    }

    [TestMethod]
    public void Normalizacao_de_usuario_e_email_usa_caixa_alta_invariante()
    {
        using var escopo = _provedor.CreateScope();
        var normalizador = escopo.ServiceProvider.GetRequiredService<ILookupNormalizer>();

        Assert.AreEqual("ADMIN", normalizador.NormalizeName("Admin"));
        Assert.AreEqual("ADMIN@EXAMPLE.ORG", normalizador.NormalizeEmail("Admin@Example.org"));
    }

    [TestMethod]
    public async Task Usuario_duplicado_e_rejeitado()
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var primeiro = CriarUsuario("usuario.duplicado", "usuario.duplicado@example.org");
        var primeiroResultado = await gerenciadorUsuarios.CreateAsync(primeiro, SenhaValida);
        Assert.IsTrue(primeiroResultado.Succeeded, Descrever(primeiroResultado));

        var duplicado = CriarUsuario("USUARIO.DUPLICADO", "outro.usuario@example.org");
        var resultado = await gerenciadorUsuarios.CreateAsync(duplicado, SenhaValida);

        Assert.IsFalse(resultado.Succeeded);
        Assert.IsTrue(
            resultado.Errors.Any(erro => erro.Code == "DuplicateUserName"),
            Descrever(resultado));
    }

    [TestMethod]
    public async Task Email_duplicado_e_rejeitado_ignorando_caixa()
    {
        await using var escopo = _provedor.CreateAsyncScope();
        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var primeiro = CriarUsuario("cidadao.email", "cidadao.email@example.org");
        var primeiroResultado = await gerenciadorUsuarios.CreateAsync(primeiro, SenhaValida);
        Assert.IsTrue(primeiroResultado.Succeeded, Descrever(primeiroResultado));

        var duplicado = CriarUsuario("outro.cidadao", "CIDADAO.EMAIL@EXAMPLE.ORG");
        var resultado = await gerenciadorUsuarios.CreateAsync(duplicado, SenhaValida);

        Assert.IsFalse(resultado.Succeeded);
        Assert.IsTrue(
            resultado.Errors.Any(erro => erro.Code == "DuplicateEmail"),
            Descrever(resultado));
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
        await contexto.Database.EnsureCreatedAsync();

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

    private static Usuario CriarUsuario(string nomeUsuario, string email)
    {
        var agora = DateTimeOffset.UtcNow;

        return new Usuario
        {
            UserName = nomeUsuario,
            Email = email,
            Nome = "Pessoa de teste",
            Ativo = true,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora,
        };
    }

    private static string Descrever(IdentityResult resultado)
    {
        return string.Join("; ", resultado.Errors.Select(erro => $"{erro.Code}: {erro.Description}"));
    }
}
