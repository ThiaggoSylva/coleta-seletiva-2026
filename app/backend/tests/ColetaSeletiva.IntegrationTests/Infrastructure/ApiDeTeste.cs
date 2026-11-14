using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.IntegrationTests.Infrastructure;

internal sealed class ApiDeTeste : WebApplicationFactory<Program>
{
    public const string Emissor = "coleta-seletiva-testes-api";
    public const string Publico = "coleta-seletiva-testes-api-web";
    public const string Chave = "chave-de-testes-com-pelo-menos-32-bytes!!";

    private readonly string _connectionString;
    private readonly IReadOnlyDictionary<string, string?> _configuracoesExtras;
    private readonly Action<ILoggingBuilder>? _configurarLogging;

    public ApiDeTeste(
        string connectionString,
        IReadOnlyDictionary<string, string?>? configuracoesExtras = null,
        Action<ILoggingBuilder>? configurarLogging = null)
    {
        _connectionString = connectionString;
        _configuracoesExtras = configuracoesExtras ?? new Dictionary<string, string?>();
        _configurarLogging = configurarLogging;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        var configuracoes = CriarConfiguracoes();
        foreach (var (chave, valor) in configuracoes)
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(configuracoes);
        });

        if (_configurarLogging is not null)
        {
            builder.ConfigureLogging(_configurarLogging);
        }
    }

    private Dictionary<string, string?> CriarConfiguracoes()
    {
        var configuracoes = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ConnectionStrings:ColetaSeletiva"] = _connectionString,
            ["Jwt:Issuer"] = Emissor,
            ["Jwt:Audience"] = Publico,
            ["Jwt:SigningKey"] = Chave,
            ["Sessao:AccessTokenMinutos"] = "15",
            ["Sessao:RefreshTokenDias"] = "7",
            ["Sessao:OrigemPermitida"] = "http://localhost",
        };

        foreach (var (chave, valor) in _configuracoesExtras)
        {
            configuracoes[chave] = valor;
        }

        return configuracoes;
    }
}
