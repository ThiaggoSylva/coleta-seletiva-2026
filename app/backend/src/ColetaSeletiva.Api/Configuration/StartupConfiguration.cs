using System.Text;
using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Infrastructure.Seguranca;

namespace ColetaSeletiva.Api.Configuration;

/// <summary>
/// Valida a configuração mínima de segurança antes de a API começar a ouvir.
/// Nenhuma mensagem inclui o valor das credenciais.
/// </summary>
public static class StartupConfiguration
{
    public const string ConnectionStringName = "ColetaSeletiva";
    public const int MinimumJwtSigningKeyBytes = 32;

    private const string JwtSectionName = "Jwt";

    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ValidateConnectionString(configuration);

        var jwt = configuration.GetSection(JwtSectionName);

        RequireValue(jwt, "Issuer", "Jwt__Issuer");
        RequireValue(jwt, "Audience", "Jwt__Audience");
        ValidateSigningKey(jwt);
        ValidateOrigemPermitida(configuration);
    }

    public static JwtSettings CriarJwtSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Validate(configuration);

        var jwt = configuration.GetSection(JwtSectionName);

        return new JwtSettings(
            issuer: jwt["Issuer"]!,
            audience: jwt["Audience"]!,
            signingKey: jwt["SigningKey"]!);
    }

    public static ConfiguracaoDeSessao CriarConfiguracaoDeSessao(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var minutosDoToken = configuration.GetValue("Sessao:AccessTokenMinutos", 15);
        var diasDoRefresh = configuration.GetValue("Sessao:RefreshTokenDias", 7);

        if (minutosDoToken <= 0)
        {
            throw new InvalidOperationException(
                "A configuração 'Sessao:AccessTokenMinutos' deve ser positiva.");
        }

        if (diasDoRefresh <= 0)
        {
            throw new InvalidOperationException(
                "A configuração 'Sessao:RefreshTokenDias' deve ser positiva.");
        }

        var validadeDoToken = TimeSpan.FromMinutes(minutosDoToken);
        var validadeDoRefresh = TimeSpan.FromDays(diasDoRefresh);

        if (validadeDoRefresh <= validadeDoToken)
        {
            throw new InvalidOperationException(
                "A validade do refresh deve ser maior que a validade do token de acesso.");
        }

        return new ConfiguracaoDeSessao(validadeDoToken, validadeDoRefresh);
    }

    private static void ValidateConnectionString(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionStringName)))
        {
            throw new InvalidOperationException(
                $"A connection string '{ConnectionStringName}' é obrigatória. "
                + "Configure-a com user-secrets no desenvolvimento ou com a variável de ambiente "
                + "'ConnectionStrings__ColetaSeletiva'.");
        }
    }

    private static void ValidateSigningKey(IConfigurationSection jwt)
    {
        RequireValue(jwt, "SigningKey", "Jwt__SigningKey");

        var signingKey = jwt["SigningKey"];
        if (Encoding.UTF8.GetByteCount(signingKey!) < MinimumJwtSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"A configuração 'Jwt:SigningKey' deve ter pelo menos {MinimumJwtSigningKeyBytes} bytes.");
        }
    }

    private static void ValidateOrigemPermitida(IConfiguration configuration)
    {
        var origem = configuration["Sessao:OrigemPermitida"];
        if (string.IsNullOrWhiteSpace(origem))
        {
            return;
        }

        if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "A configuração 'Sessao:OrigemPermitida' deve ser uma URL absoluta http ou https.");
        }
    }

    private static void RequireValue(IConfigurationSection section, string key, string environmentVariable)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
        {
            throw new InvalidOperationException(
                $"A configuração 'Jwt:{key}' é obrigatória. "
                + $"Defina-a em appsettings ou na variável de ambiente '{environmentVariable}'.");
        }
    }
}
