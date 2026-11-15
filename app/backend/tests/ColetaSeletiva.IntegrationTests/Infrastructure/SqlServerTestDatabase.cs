using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace ColetaSeletiva.IntegrationTests.Infrastructure;

internal static class SqlServerTestDatabase
{
    private const string ImagemSqlServer = "mcr.microsoft.com/mssql/server:2022-latest";

    private static readonly Lazy<Task<MsSqlContainer>> Contêiner = new(
        CriarContêinerAsync,
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static async Task<string> ObterConnectionStringAsync(string nomeBanco)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeBanco);

        var contêiner = await Contêiner.Value;
        var builder = new SqlConnectionStringBuilder(contêiner.GetConnectionString())
        {
            InitialCatalog = nomeBanco,
        };

        return builder.ConnectionString;
    }

    private static async Task<MsSqlContainer> CriarContêinerAsync()
    {
        var contêiner = new MsSqlBuilder(ImagemSqlServer).Build();
        await contêiner.StartAsync();
        return contêiner;
    }
}
