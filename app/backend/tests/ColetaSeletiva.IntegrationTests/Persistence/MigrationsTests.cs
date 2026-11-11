using System.Data;
using ColetaSeletiva.Domain.Identidade;
using ColetaSeletiva.Infrastructure;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using ColetaSeletiva.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.IntegrationTests.Persistence;

[TestClass]
[DoNotParallelize]
public sealed class MigrationsTests
{
    private const string NomeBanco = "ColetaSeletivaMigrationsTests";

    [TestMethod]
    public async Task Banco_novo_recebe_schema_completo_e_papeis()
    {
        var connectionString = await SqlServerTestDatabase.ObterConnectionStringAsync(NomeBanco);

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddInfrastructure(connectionString);
        await using var provedor = servicos.BuildServiceProvider();
        await using var escopo = provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ColetaSeletivaDbContext>();

        await contexto.Database.EnsureDeletedAsync();
        try
        {
            await contexto.Database.MigrateAsync();

            var pendentes = await contexto.Database.GetPendingMigrationsAsync();
            Assert.IsFalse(
                pendentes.Any(),
                $"Migrations pendentes após atualização: {string.Join(", ", pendentes)}.");

            var tabelas = await ConsultarTabelasAsync(contexto);
            foreach (var tabela in new[]
                     {
                         "AspNetRoles",
                         "AspNetUsers",
                         "Organizacao",
                         "RefreshToken",
                         "Auditoria",
                         "__EFMigrationsHistory",
                     })
            {
                CollectionAssert.Contains(tabelas, tabela);
            }

            var indices = await ConsultarIndicesAsync(contexto);
            var indiceEmail = indices.Single(indice => indice.Nome == "UX_Usuario_NormalizedEmail");
            Assert.IsTrue(indiceEmail.Unico);
            Assert.AreEqual("([NormalizedEmail] IS NOT NULL)", indiceEmail.Filtro);

            var indiceUsuario = indices.Single(indice => indice.Nome == "UX_Usuario_NormalizedUserName");
            Assert.IsTrue(indiceUsuario.Unico);
            Assert.AreEqual("([NormalizedUserName] IS NOT NULL)", indiceUsuario.Filtro);

            var indiceOrganizacao = indices.Single(indice => indice.Nome == "UX_Usuario_OrganizacaoId_Id");
            Assert.IsTrue(indiceOrganizacao.Unico);
            Assert.AreEqual("([OrganizacaoId] IS NOT NULL)", indiceOrganizacao.Filtro);

            var indiceRefresh = indices.Single(indice => indice.Nome == "UX_RefreshToken_Hash");
            Assert.IsTrue(indiceRefresh.Unico);

            var colunasRowVersion = await ConsultarColunasRowVersionAsync(contexto);
            CollectionAssert.Contains(colunasRowVersion, "Organizacao.RowVersion");
            CollectionAssert.Contains(colunasRowVersion, "AspNetUsers.RowVersion");

            var exclusoes = await ConsultarExclusoesDeChavesEstrangeirasAsync(contexto);
            Assert.AreEqual(4, exclusoes.Count, "Chaves estrangeiras da F1 não encontradas.");
            foreach (var exclusao in exclusoes)
            {
                Assert.AreEqual("NO_ACTION", exclusao, $"FK com cascata indevida: {exclusao}.");
            }

            var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            await PapelSeeder.GarantirPapeisAsync(gerenciadorPapeis);

            foreach (var papel in Papeis.Todos)
            {
                Assert.IsTrue(
                    await gerenciadorPapeis.RoleExistsAsync(papel),
                    $"Papel '{papel}' não encontrado após o seed.");
            }
        }
        finally
        {
            await contexto.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<List<string>> ConsultarTabelasAsync(ColetaSeletivaDbContext contexto)
    {
        return await ConsultarListaAsync(
            contexto,
            "SELECT name FROM sys.tables ORDER BY name;");
    }

    private static async Task<List<string>> ConsultarColunasRowVersionAsync(ColetaSeletivaDbContext contexto)
    {
        return await ConsultarListaAsync(
            contexto,
            """
            SELECT OBJECT_NAME(colunas.object_id) + '.' + colunas.name
            FROM sys.columns AS colunas
            JOIN sys.types AS tipos ON colunas.user_type_id = tipos.user_type_id
            WHERE tipos.name = 'timestamp'
            ORDER BY colunas.object_id, colunas.column_id;
            """);
    }

    private static async Task<List<string>> ConsultarExclusoesDeChavesEstrangeirasAsync(
        ColetaSeletivaDbContext contexto)
    {
        return await ConsultarListaAsync(
            contexto,
            """
            SELECT chaves.delete_referential_action_desc
            FROM sys.foreign_keys AS chaves
            WHERE chaves.name IN (
                'FK_AspNetUsers_Organizacao_OrganizacaoId',
                'FK_RefreshToken_AspNetUsers_UsuarioId',
                'FK_Auditoria_AspNetUsers_AtorUsuarioId',
                'FK_Auditoria_Organizacao_OrganizacaoId'
            )
            ORDER BY chaves.name;
            """);
    }

    private static async Task<List<Indice>> ConsultarIndicesAsync(ColetaSeletivaDbContext contexto)
    {
        var conexao = contexto.Database.GetDbConnection();
        if (conexao.State != ConnectionState.Open)
        {
            await conexao.OpenAsync();
        }

        await using var comando = conexao.CreateCommand();
        comando.CommandText =
            """
            SELECT indices.name, indices.is_unique, indices.filter_definition
            FROM sys.indexes AS indices
            JOIN sys.tables AS tabelas ON tabelas.object_id = indices.object_id
            WHERE tabelas.name IN ('AspNetUsers', 'RefreshToken', 'Auditoria')
              AND indices.name LIKE 'UX[_]%'
            ORDER BY indices.name;
            """;

        var indices = new List<Indice>();
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            indices.Add(new Indice(
                leitor.GetString(0),
                leitor.GetBoolean(1),
                leitor.IsDBNull(2) ? null : leitor.GetString(2)));
        }

        return indices;
    }

    private static async Task<List<string>> ConsultarListaAsync(
        ColetaSeletivaDbContext contexto,
        string consulta)
    {
        var conexao = contexto.Database.GetDbConnection();
        if (conexao.State != ConnectionState.Open)
        {
            await conexao.OpenAsync();
        }

        await using var comando = conexao.CreateCommand();
        comando.CommandText = consulta;

        var valores = new List<string>();
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            valores.Add(leitor.GetString(0));
        }

        return valores;
    }

    private sealed record Indice(string Nome, bool Unico, string? Filtro);
}
