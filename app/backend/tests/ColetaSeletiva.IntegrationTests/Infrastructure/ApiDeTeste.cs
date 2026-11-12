using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ColetaSeletiva.IntegrationTests.Infrastructure;

internal sealed class ApiDeTeste : WebApplicationFactory<Program>
{
    public const string Emissor = "coleta-seletiva-testes-api";
    public const string Publico = "coleta-seletiva-testes-api-web";
    public const string Chave = "chave-de-testes-com-pelo-menos-32-bytes!!";

    private readonly string _connectionString;

    public ApiDeTeste(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ColetaSeletiva", _connectionString);
        builder.UseSetting("Jwt:Issuer", Emissor);
        builder.UseSetting("Jwt:Audience", Publico);
        builder.UseSetting("Jwt:SigningKey", Chave);
        builder.UseSetting("Sessao:AccessTokenMinutos", "15");
        builder.UseSetting("Sessao:RefreshTokenDias", "7");
        builder.UseSetting("Sessao:OrigemPermitida", "http://localhost");

        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ColetaSeletiva"] = _connectionString,
                ["Jwt:Issuer"] = Emissor,
                ["Jwt:Audience"] = Publico,
                ["Jwt:SigningKey"] = Chave,
                ["Sessao:AccessTokenMinutos"] = "15",
                ["Sessao:RefreshTokenDias"] = "7",
                ["Sessao:OrigemPermitida"] = "http://localhost",
            });
        });
    }
}
